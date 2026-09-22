using Microsoft.AspNetCore.Mvc;
using Samagra.Application.Interfaces;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AiController : ControllerBase
{
    private readonly IAiAssistant _assistant;
    private readonly IDocumentIndexer _indexer;
    private readonly IVectorSearch _search;

    public AiController(IAiAssistant assistant, IDocumentIndexer indexer, IVectorSearch search)
    {
        _assistant = assistant;
        _indexer = indexer;
        _search = search;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var answer = await _assistant.AskAsync(request.Question, ct);
        return Ok(new AskResponse(answer));
    }

    [HttpPost("index")]
    public async Task<IActionResult> Index(
    [FromBody] IndexRequest request,
    CancellationToken ct)
    {
        var count = await _indexer.IndexAsync(request.Source, request.Content, ct);
        return Ok(new { chunksIndexed = count });
    }


    [HttpGet("search")]
    public async Task<IActionResult> SearchVector(
    [FromQuery] string q,
    [FromQuery] int topK = 3,
    CancellationToken ct = default)
    {
        var results = await _search.SearchAsync(q, topK, ct);
        return Ok(results);
    }

    [HttpPost("ask-rag")]
    public async Task<IActionResult> AskRag(
    [FromBody] AskRequest request,
    CancellationToken ct)
    {
        var answer = await _assistant.AskWithRagAsync(request.Question, ct);
        return Ok(new AskResponse(answer));
    }

    [HttpPost("ask-tools")]
    public async Task<IActionResult> AskTools(
        [FromBody] AskRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var answer = await _assistant.AskWithToolsAsync(request.Question, ct);
        return Ok(new AskResponse(answer));
    }
}
public sealed record AskRequest(string Question);
public sealed record AskResponse(string Answer);
public sealed record IndexRequest(string Source, string Content);