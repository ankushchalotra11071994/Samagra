using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Samagra.AI.Options;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;
using System.Runtime.CompilerServices;
namespace Samagra.AI.Middleware;

internal sealed class CostTrackingChatClient : DelegatingChatClient
{
    public const string FeatureKey = "samagra.feature";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _http;
    private readonly AiOptions _options;
    private readonly ILogger<CostTrackingChatClient> _logger;

    public CostTrackingChatClient(
        IChatClient inner,
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor http,
        AiOptions options,
        ILogger<CostTrackingChatClient> logger) : base(inner)
    {
        _scopeFactory = scopeFactory;
        _http = http;
        _options = options;
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // feature tag निकालो, और आगे Azure को मत भेजो
        var feature = "chat";
        if (options?.AdditionalProperties?.TryGetValue(FeatureKey, out var f) == true)
        {
            feature = f?.ToString() ?? "chat";
            options = options.Clone();
            options.AdditionalProperties!.Remove(FeatureKey);
        }

        var sw = Stopwatch.StartNew();
        ChatResponse? response = null;

        try
        {
            response = await base.GetResponseAsync(messages, options, cancellationToken);
            return response;
        }
        finally
        {
            sw.Stop();
            await RecordAsync(response, feature, sw.ElapsedMilliseconds);
        }
    }
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var feature = "chat";
    if (options?.AdditionalProperties?.TryGetValue(FeatureKey, out var f) == true)
    {
        feature = f?.ToString() ?? "chat";
        options = options.Clone();
        options.AdditionalProperties!.Remove(FeatureKey);
    }

    var sw = Stopwatch.StartNew();
    var updates = new List<ChatResponseUpdate>();

    try
    {
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            updates.Add(update);
            yield return update;
        }
    }
    finally
    {
        sw.Stop();

        // सारे updates जोड़कर एक ChatResponse बनाओ — usage उसी में आता है
        var response = updates.Count > 0 ? updates.ToChatResponse() : null;
        await RecordAsync(response, feature, sw.ElapsedMilliseconds);
    }
}

    private async Task RecordAsync(ChatResponse? response, string feature, long elapsedMs)
    {
        try
        {
            var input = (int)(response?.Usage?.InputTokenCount ?? 0);
            var output = (int)(response?.Usage?.OutputTokenCount ?? 0);
            var model = _options.ChatModel;

            var cost = 0m;
            if (_options.Pricing.TryGetValue(model, out var price))
            {
                cost = (input * price.InputPerMillion + output * price.OutputPerMillion) / 1_000_000m;
            }

            var usage = new AiUsage
            {
                CreatedAtUtc = DateTime.UtcNow,
                UserId = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
                Feature = feature,
                Model = model,
                InputTokens = input,
                OutputTokens = output,
                CostUsd = cost,
                DurationMs = (int)elapsedMs,
                Success = response is not null
            };

            // singleton के अंदर scoped service — अपना scope बनाओ
            await using var scope = _scopeFactory.CreateAsyncScope();
            var recorder = scope.ServiceProvider.GetRequiredService<IAiUsageRecorder>();
            await recorder.RecordAsync(usage, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record AI usage");
        }
    }
}