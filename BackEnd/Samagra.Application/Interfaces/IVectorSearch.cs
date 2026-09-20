namespace Samagra.Application.Interfaces;

public sealed record SearchResult(string Source, string Content, double Distance);

public interface IVectorSearch
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query, int topK = 3, CancellationToken ct = default);
}