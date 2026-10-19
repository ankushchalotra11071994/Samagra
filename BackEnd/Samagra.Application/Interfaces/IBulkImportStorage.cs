namespace Samagra.Application.Interfaces;

public interface IBulkImportStorage
{
    // Browser इस URL पर सीधे PUT करता है — file API से होकर नहीं जाती
    Task<Uri> GetUploadUrlAsync(string blobName, TimeSpan validFor, CancellationToken ct = default);

    // Blob नहीं है तो null
    Task<long?> GetSizeAsync(string blobName, CancellationToken ct = default);
}
