using System.Data;
using System.Globalization;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;

namespace Samagra.Functions.Import;

public sealed class ProductCsvImporter
{
    private const int BatchSize = 5_000;
    private const int MaxStoredErrors = 1_000;

    private readonly ImportSettings _settings;
    private readonly BlobContainerClient _container;
    private readonly ILogger<ProductCsvImporter> _logger;

    public ProductCsvImporter(ImportSettings settings, BlobServiceClient blobService, ILogger<ProductCsvImporter> logger)
    {
        _settings = settings;
        _container = blobService.GetBlobContainerClient(settings.ContainerName);
        _logger = logger;
    }

    public async Task ImportAsync(BulkImportMessage message, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_settings.SqlConnectionString);
        await conn.OpenAsync(ct);

        if (!await TryMarkProcessingAsync(conn, message.JobId, ct))
        {
            _logger.LogInformation("Job {JobId} is missing or already completed — skipping", message.JobId);
            return;
        }

        try
        {
            await RunImportAsync(conn, message, ct);
        }
        catch (InvalidImportFileException ex)
        {
            // retry बेकार है — job fail करो और message complete होने दो
            await MarkFailedAsync(conn, message.JobId, ex.Message);
        }
        catch (Exception ex)
        {
            // transient (SQL/blob) — fail mark करो और rethrow, Service Bus redeliver करेगा
            _logger.LogError(ex, "Bulk import {JobId} failed", message.JobId);
            await MarkFailedAsync(conn, message.JobId, "Processing failed, will retry: " + ex.Message);
            throw;
        }
    }

    private async Task RunImportAsync(SqlConnection conn, BulkImportMessage message, CancellationToken ct)
    {
        var categories = await LoadCategoriesAsync(conn, ct);

        // सारे products + job update एक transaction में — retry पर आधा import duplicate नहीं होगा
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        if (await IsCompletedLockedAsync(conn, tx, message.JobId, ct))
            return;

        using var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.CheckConstraints, tx)
        {
            DestinationTableName = "dbo.Products",
            BulkCopyTimeout = 0
        };
        var products = CreateProductsTable();
        foreach (DataColumn c in products.Columns)
            bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);

        var errors = new List<(int Row, string Message)>();
        int total = 0, succeeded = 0, failed = 0;

        var blob = _container.GetBlobClient(message.BlobName);
        await using (var stream = await blob.OpenReadAsync(cancellationToken: ct))
        using (var reader = new StreamReader(stream))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = a => a.Header.Trim().ToLowerInvariant(),
            MissingFieldFound = null,
            BadDataFound = null
        }))
        {
            if (!await csv.ReadAsync() || !csv.ReadHeader())
                throw new InvalidImportFileException("File is empty");

            var headers = csv.HeaderRecord!.Select(h => h.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = ProductCsvColumns.Required.Where(r => !headers.Contains(r)).ToList();
            if (missing.Count > 0)
                throw new InvalidImportFileException("Missing columns: " + string.Join(", ", missing));

            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();
                total++;

                var error = TryAddRow(csv, categories, products);
                if (error is null)
                {
                    succeeded++;
                }
                else
                {
                    failed++;
                    if (errors.Count < MaxStoredErrors)
                        errors.Add((csv.Parser.Row, error));
                }

                if (products.Rows.Count >= BatchSize)
                {
                    await bulk.WriteToServerAsync(products, ct);
                    products.Clear();
                }
            }
        }

        if (products.Rows.Count > 0)
            await bulk.WriteToServerAsync(products, ct);

        await WriteErrorsAsync(conn, tx, message.JobId, errors, ct);
        await MarkCompletedAsync(conn, tx, message.JobId, total, succeeded, failed, ct);

        await tx.CommitAsync(ct);

        _logger.LogInformation("Job {JobId}: {Succeeded}/{Total} rows imported, {Failed} failed",
            message.JobId, succeeded, total, failed);
    }

    // Validation error हो तो message लौटाता है, वरना row add करके null
    private static string? TryAddRow(CsvReader csv, Dictionary<string, Guid> categories, DataTable products)
    {
        var name = Field(csv, ProductCsvColumns.Name);
        var description = Field(csv, ProductCsvColumns.Description);
        var priceRaw = Field(csv, ProductCsvColumns.Price);
        var stockRaw = Field(csv, ProductCsvColumns.Stock);
        var categoryName = Field(csv, ProductCsvColumns.CategoryName);
        var imageUrl = Field(csv, ProductCsvColumns.ImageUrl);

        if (string.IsNullOrWhiteSpace(name)) return "Name is required";
        if (name.Length > 200) return "Name is longer than 200 characters";
        if (description?.Length > 2000) return "Description is longer than 2000 characters";

        if (!decimal.TryParse(priceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0)
            return $"Invalid Price '{priceRaw}'";
        if (decimal.Round(price, 2) != price || price > 9_999_999_999_999_999.99m)
            return $"Price '{priceRaw}' must have at most 2 decimals";

        if (!int.TryParse(stockRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock) || stock < 0)
            return $"Invalid Stock '{stockRaw}'";

        if (string.IsNullOrWhiteSpace(categoryName) || !categories.TryGetValue(categoryName, out var categoryId))
            return $"Unknown category '{categoryName}'";

        // Full URL या site-relative path (/assets/1.webp) — दोनों चलेंगे
        if (!string.IsNullOrWhiteSpace(imageUrl)
            && !Uri.TryCreate(imageUrl, UriKind.Absolute, out _)
            && !imageUrl.StartsWith('/'))
            return $"Invalid ImageUrl '{imageUrl}'";

        products.Rows.Add(
            Guid.NewGuid(),
            name,
            string.IsNullOrWhiteSpace(description) ? DBNull.Value : description,
            price,
            stock,
            true,
            DateTime.UtcNow,
            categoryId,
            string.IsNullOrWhiteSpace(imageUrl) ? DBNull.Value : imageUrl);

        return null;
    }

    private static string? Field(CsvReader csv, string column)
        => csv.TryGetField<string>(column.ToLowerInvariant(), out var value) ? value : null;

    private static DataTable CreateProductsTable()
    {
        var t = new DataTable();
        t.Columns.Add("Id", typeof(Guid));
        t.Columns.Add("Name", typeof(string));
        t.Columns.Add("Description", typeof(string));
        t.Columns.Add("Price", typeof(decimal));
        t.Columns.Add("Stock", typeof(int));
        t.Columns.Add("IsActive", typeof(bool));
        t.Columns.Add("CreatedAt", typeof(DateTime));
        t.Columns.Add("CategoryId", typeof(Guid));
        t.Columns.Add("ImageUrl", typeof(string));
        return t;
    }

    private static async Task WriteErrorsAsync(
        SqlConnection conn, SqlTransaction tx, Guid jobId, List<(int Row, string Message)> errors, CancellationToken ct)
    {
        if (errors.Count == 0) return;

        var t = new DataTable();
        t.Columns.Add("JobId", typeof(Guid));
        t.Columns.Add("RowNumber", typeof(int));
        t.Columns.Add("Message", typeof(string));
        foreach (var (row, msg) in errors)
            t.Rows.Add(jobId, row, msg.Length > 1000 ? msg[..1000] : msg);

        using var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.CheckConstraints, tx)
        {
            DestinationTableName = "dbo.BulkImportRowErrors"
        };
        foreach (DataColumn c in t.Columns)
            bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);

        await bulk.WriteToServerAsync(t, ct);
    }

    private static async Task<Dictionary<string, Guid>> LoadCategoriesAsync(SqlConnection conn, CancellationToken ct)
    {
        var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new SqlCommand("SELECT Id, Name FROM dbo.Categories WHERE IsActive = 1", conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            map.TryAdd(r.GetString(1).Trim(), r.GetGuid(0));
        return map;
    }

    private static async Task<bool> TryMarkProcessingAsync(SqlConnection conn, Guid jobId, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("""
            UPDATE dbo.BulkImportJobs
            SET Status = @processing, ErrorMessage = NULL
            WHERE Id = @id AND Status IN (@queued, @processing, @failed)
            """, conn);
        cmd.Parameters.AddWithValue("@id", jobId);
        cmd.Parameters.AddWithValue("@queued", nameof(BulkImportStatus.Queued));
        cmd.Parameters.AddWithValue("@processing", nameof(BulkImportStatus.Processing));
        cmd.Parameters.AddWithValue("@failed", nameof(BulkImportStatus.Failed));
        return await cmd.ExecuteNonQueryAsync(ct) == 1;
    }

    // Row lock — दो deliveries साथ में आएँ तो दूसरी यहाँ wait करेगी और Completed देखकर लौट जाएगी
    private static async Task<bool> IsCompletedLockedAsync(SqlConnection conn, SqlTransaction tx, Guid jobId, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(
            "SELECT Status FROM dbo.BulkImportJobs WITH (UPDLOCK, HOLDLOCK) WHERE Id = @id", conn, tx);
        cmd.Parameters.AddWithValue("@id", jobId);
        var status = (string?)await cmd.ExecuteScalarAsync(ct);
        return status is null || status == nameof(BulkImportStatus.Completed);
    }

    private static async Task MarkCompletedAsync(
        SqlConnection conn, SqlTransaction tx, Guid jobId, int total, int succeeded, int failed, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("""
            UPDATE dbo.BulkImportJobs
            SET Status = @status, TotalRows = @total, SucceededRows = @succeeded, FailedRows = @failed,
                ErrorMessage = NULL, CompletedAtUtc = SYSUTCDATETIME()
            WHERE Id = @id
            """, conn, tx);
        cmd.Parameters.AddWithValue("@id", jobId);
        cmd.Parameters.AddWithValue("@status", nameof(BulkImportStatus.Completed));
        cmd.Parameters.AddWithValue("@total", total);
        cmd.Parameters.AddWithValue("@succeeded", succeeded);
        cmd.Parameters.AddWithValue("@failed", failed);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task MarkFailedAsync(SqlConnection conn, Guid jobId, string error)
    {
        await using var cmd = new SqlCommand("""
            UPDATE dbo.BulkImportJobs
            SET Status = @status, ErrorMessage = @error, CompletedAtUtc = SYSUTCDATETIME()
            WHERE Id = @id AND Status <> @completed
            """, conn);
        cmd.Parameters.AddWithValue("@id", jobId);
        cmd.Parameters.AddWithValue("@status", nameof(BulkImportStatus.Failed));
        cmd.Parameters.AddWithValue("@completed", nameof(BulkImportStatus.Completed));
        cmd.Parameters.AddWithValue("@error", error.Length > 2000 ? error[..2000] : error);
        // ct नहीं — cancel होने पर भी status लिखा जाना चाहिए
        await cmd.ExecuteNonQueryAsync();
    }
}
