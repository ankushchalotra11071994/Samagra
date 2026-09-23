namespace Samagra.Application.Interfaces;

public interface IDocumentIndexer
{
    Task<int> IndexAsync(
        string source,
        string content,
        int chunkSize = 500,
        int overlap = 50,
        CancellationToken ct = default);

    Task ClearAsync(CancellationToken ct = default);
}