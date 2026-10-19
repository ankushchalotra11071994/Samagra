using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Samagra.Application.Interfaces;

namespace Samagra.Infrastructure.BulkImport;

public sealed class AzureBlobBulkImportStorage : IBulkImportStorage
{
    private readonly BlobContainerClient _container;
    private volatile bool _containerEnsured;

    public AzureBlobBulkImportStorage(BlobServiceClient blobService, IOptions<BulkImportOptions> options)
        => _container = blobService.GetBlobContainerClient(options.Value.ContainerName);

    public async Task<Uri> GetUploadUrlAsync(string blobName, TimeSpan validFor, CancellationToken ct = default)
    {
        if (!_containerEnsured)
        {
            await _container.CreateIfNotExistsAsync(cancellationToken: ct);
            _containerEnsured = true;
        }

        var blob = _container.GetBlobClient(blobName);
        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException(
                "ConnectionStrings:BlobStorage must include an account key to generate SAS URLs.");

        // सिर्फ़ इसी एक blob पर create/write — read/list/delete नहीं
        return blob.GenerateSasUri(
            BlobSasPermissions.Create | BlobSasPermissions.Write,
            DateTimeOffset.UtcNow.Add(validFor));
    }

    public async Task<long?> GetSizeAsync(string blobName, CancellationToken ct = default)
    {
        try
        {
            var props = await _container.GetBlobClient(blobName).GetPropertiesAsync(cancellationToken: ct);
            return props.Value.ContentLength;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
