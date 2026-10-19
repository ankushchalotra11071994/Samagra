using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Samagra.API.Contracts;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.BulkImport;
using Samagra.Infrastructure.Identity;

namespace Samagra.API.Controllers;

// Flow: upload-url → browser PUTs file to blob → start → Service Bus → Samagra.Functions
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class BulkImportsController : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".csv"];

    private readonly IBulkImportJobRepository _jobs;
    private readonly IBulkImportStorage _storage;
    private readonly IBulkImportQueue _queue;
    private readonly BulkImportOptions _options;

    public BulkImportsController(
        IBulkImportJobRepository jobs,
        IBulkImportStorage storage,
        IBulkImportQueue queue,
        IOptions<BulkImportOptions> options)
    {
        _jobs = jobs;
        _storage = storage;
        _queue = queue;
        _options = options.Value;
    }

    // POST /api/bulkimports/upload-url
    [HttpPost("upload-url")]
    public async Task<IActionResult> CreateUploadUrl([FromBody] CreateUploadUrlRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var fileName = Path.GetFileName(request.FileName);
        if (!AllowedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only .csv files are supported" });

        if (request.FileSizeBytes > _options.MaxFileSizeBytes)
            return BadRequest(new { message = $"File is larger than {_options.MaxFileSizeBytes / (1024 * 1024)} MB" });

        var jobId = Guid.NewGuid();
        // blob name user input से नहीं बनता — सिर्फ़ jobId
        var blobName = $"products/{jobId}.csv";

        var validFor = TimeSpan.FromMinutes(_options.UploadUrlExpiryMinutes);
        var uploadUrl = await _storage.GetUploadUrlAsync(blobName, validFor, ct);

        await _jobs.AddAsync(new BulkImportJob
        {
            Id = jobId,
            FileName = fileName,
            BlobName = blobName,
            Status = BulkImportStatus.Pending,
            CreatedBy = userId.Value
        }, ct);
        await _jobs.SaveChangesAsync(ct);

        return Ok(new CreateUploadUrlResponse(jobId, uploadUrl.ToString(), DateTimeOffset.UtcNow.Add(validFor)));
    }

    // POST /api/bulkimports/{id}/start — browser upload पूरा होने के बाद
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var job = await GetOwnJobAsync(id, ct);
        if (job is null) return NotFound();

        if (job.Status != BulkImportStatus.Pending)
            return Conflict(new { message = $"Job is already {job.Status}" });

        var size = await _storage.GetSizeAsync(job.BlobName, ct);
        if (size is null)
            return BadRequest(new { message = "File has not been uploaded yet" });
        if (size > _options.MaxFileSizeBytes)
            return BadRequest(new { message = "Uploaded file exceeds the size limit" });

        // पहले Queued save करो, फिर message भेजो — वरना Function को DB में Pending job मिल सकता है
        job.Status = BulkImportStatus.Queued;
        await _jobs.SaveChangesAsync(ct);

        try
        {
            await _queue.EnqueueAsync(new BulkImportMessage(job.Id, job.BlobName), ct);
        }
        catch
        {
            job.Status = BulkImportStatus.Pending;   // user दोबारा start कर सके
            await _jobs.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        return Accepted(ToResponse(job));
    }

    // GET /api/bulkimports/{id} — UI इसे poll करता है
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var job = await GetOwnJobAsync(id, ct);
        return job is null ? NotFound() : Ok(ToResponse(job));
    }

    // GET /api/bulkimports
    [HttpGet]
    public async Task<IActionResult> GetRecent(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var jobs = await _jobs.GetRecentAsync(userId.Value, 20, ct);
        return Ok(jobs.Select(ToResponse));
    }

    // GET /api/bulkimports/{id}/errors
    [HttpGet("{id:guid}/errors")]
    public async Task<IActionResult> GetErrors(Guid id, CancellationToken ct)
    {
        var job = await GetOwnJobAsync(id, ct);
        if (job is null) return NotFound();

        var errors = await _jobs.GetRowErrorsAsync(id, 500, ct);
        return Ok(errors.Select(e => new BulkImportRowErrorResponse(e.RowNumber, e.Message)));
    }

    private async Task<BulkImportJob?> GetOwnJobAsync(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var job = await _jobs.GetByIdAsync(id, ct);
        return job is not null && job.CreatedBy == userId ? job : null;
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static BulkImportJobResponse ToResponse(BulkImportJob j) => new(
        j.Id, j.FileName, j.Status.ToString(), j.TotalRows, j.SucceededRows, j.FailedRows,
        j.ErrorMessage, j.CreatedAtUtc, j.CompletedAtUtc);
}
