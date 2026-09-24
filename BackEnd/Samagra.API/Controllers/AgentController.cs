using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Samagra.Application.Interfaces;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/ai/agent")]
[Authorize]
public sealed class AgentController : ControllerBase
{
    private readonly IAgentChat _agent;

    public AgentController(IAgentChat agent) => _agent = agent;

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] AgentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        var reply = await _agent.ChatAsync(request.ConversationId, request.Message, ct);
        return Ok(reply);
    }
}

public sealed record AgentRequest(string? ConversationId, string Message);