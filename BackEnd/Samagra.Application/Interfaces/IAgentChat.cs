namespace Samagra.Application.Interfaces;

public sealed record AgentReply(string ConversationId, string Text);

public interface IAgentChat
{
    Task<AgentReply> ChatAsync(
        string? conversationId,
        string message,
        CancellationToken ct = default);

    IAsyncEnumerable<string> StreamAsync(
        string conversationId,
        string message,
        CancellationToken ct = default);
}