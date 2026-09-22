using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Samagra.AI.Options;
using Samagra.Application.Exceptions;
using Samagra.Application.Interfaces;

namespace Samagra.AI.Middleware;

internal sealed class BudgetGuardChatClient : DelegatingChatClient
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _http;
    private readonly AiOptions _options;

    public BudgetGuardChatClient(
        IChatClient inner,
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor http,
        AiOptions options) : base(inner)
    {
        _scopeFactory = scopeFactory;
        _http = http;
        _options = options;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureWithinBudgetAsync(cancellationToken);
        return await base.GetResponseAsync(messages, options, cancellationToken);
    }

    private async Task EnsureWithinBudgetAsync(CancellationToken ct)
    {
        var user = _http.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user?.FindFirstValue("uid")
                  ?? user?.FindFirstValue("sub");

        // कोई user नहीं (background job वगैरह) — limit लागू नहीं
        if (string.IsNullOrEmpty(userId))
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IAiUsageRecorder>();

        var spent = await recorder.GetTodaySpendAsync(userId, ct);
        var limit = _options.DailyBudgetPerUserUsd;

        if (spent >= limit)
            throw new AiBudgetExceededException(spent, limit);
    }
}