namespace Samagra.Application.Interfaces;

public interface IAiAssistant
{
    Task<string> AskAsync(string question, CancellationToken ct = default);
    Task<string> AskWithRagAsync(string question, CancellationToken ct = default);
    Task<string> AskWithToolsAsync(string question, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamWithToolsAsync(string question, CancellationToken ct = default);
}