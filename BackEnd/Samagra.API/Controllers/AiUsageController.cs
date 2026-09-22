 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Samagra.Application.Interfaces;
using Samagra.Infrastructure.Identity;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/admin/ai-usage")]
[Authorize ]
public sealed class AiUsageController : ControllerBase
{
    private readonly IAiUsageRecorder _recorder;

    public AiUsageController(IAiUsageRecorder recorder) => _recorder = recorder;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int days = 7,
        CancellationToken ct = default)
    {
        if (days is < 1 or > 90)
            return BadRequest("days must be between 1 and 90.");

        var fromUtc = DateTime.UtcNow.Date.AddDays(-(days - 1));
        var summary = await _recorder.GetSummaryAsync(fromUtc, ct);

        return Ok(summary);
    }









}