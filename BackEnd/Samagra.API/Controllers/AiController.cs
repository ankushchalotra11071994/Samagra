using Microsoft.AspNetCore.Mvc;
using Samagra.Application.Interfaces;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AiController : ControllerBase
{
    private readonly IAiAssistant _assistant;

    public AiController(IAiAssistant assistant) => _assistant = assistant;

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var answer = await _assistant.AskAsync(request.Question, ct);
        return Ok(new AskResponse(answer));
    }
}

public sealed record AskRequest(string Question);
public sealed record AskResponse(string Answer);