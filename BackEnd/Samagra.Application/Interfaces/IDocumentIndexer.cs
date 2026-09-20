namespace Samagra.Application.Interfaces;

public interface IDocumentIndexer
{
    Task<int> IndexAsync(string source, string content, CancellationToken ct = default);
}