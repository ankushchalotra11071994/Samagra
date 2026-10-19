namespace Samagra.Application.Interfaces;

public interface IBulkImportQueue
{
    Task EnqueueAsync(BulkImportMessage message, CancellationToken ct = default);
}

// Samagra.Functions इसी shape को deserialize करता है
public sealed record BulkImportMessage(Guid JobId, string BlobName);
