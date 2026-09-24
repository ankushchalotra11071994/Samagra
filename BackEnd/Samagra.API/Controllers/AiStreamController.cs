using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Samagra.Application.Interfaces;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/ai/stream")]
[Authorize]
public sealed class AiStreamController : ControllerBase
{
    private readonly IAgentChat _agent;

    public AiStreamController(IAgentChat agent) => _agent = agent;

    [HttpPost]
    public async Task Stream([FromBody] StreamRequest request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            await WriteEventAsync("error", "Question is required.", ct);
            return;
        }

        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString("N");
        await WriteEventAsync("conversation", conversationId, ct);

        try
        {
            await foreach (var chunk in _agent.StreamAsync(conversationId, request.Question, ct))
                await WriteEventAsync("token", chunk, ct);

            await WriteEventAsync("done", "", ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            await WriteEventAsync("error", ex.Message, CancellationToken.None);
        }
    }

    private async Task WriteEventAsync(string eventName, string data, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.Append("event: ").Append(eventName).Append('\n');
        foreach (var line in data.Split('\n'))
            sb.Append("data: ").Append(line).Append('\n');
        sb.Append('\n');

        await Response.WriteAsync(sb.ToString(), ct);
        await Response.Body.FlushAsync(ct);
    }
}

public sealed record StreamRequest(string? ConversationId, string Question);