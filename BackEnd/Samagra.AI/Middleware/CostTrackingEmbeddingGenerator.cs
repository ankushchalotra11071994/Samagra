using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Samagra.AI.Options;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;

namespace Samagra.AI.Middleware;

internal sealed class CostTrackingEmbeddingGenerator
    : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _http;
    private readonly AiOptions _options;
    private readonly ILogger<CostTrackingEmbeddingGenerator> _logger;

    public CostTrackingEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> inner,
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor http,
        AiOptions options,
        ILogger<CostTrackingEmbeddingGenerator> logger) : base(inner)
    {
        _scopeFactory = scopeFactory;
        _http = http;
        _options = options;
        _logger = logger;
    }

    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var feature = "embedding";
        if (options?.AdditionalProperties?.TryGetValue(CostTrackingChatClient.FeatureKey, out var f) == true)
        {
            feature = f?.ToString() ?? "embedding";
            options = options.Clone();
            options.AdditionalProperties!.Remove(CostTrackingChatClient.FeatureKey);
        }

        var sw = Stopwatch.StartNew();
        GeneratedEmbeddings<Embedding<float>>? result = null;

        try
        {
            result = await base.GenerateAsync(values, options, cancellationToken);
            return result;
        }
        finally
        {
            sw.Stop();
            await RecordAsync(result, feature, sw.ElapsedMilliseconds);
        }
    }

    private async Task RecordAsync(
        GeneratedEmbeddings<Embedding<float>>? result, string feature, long elapsedMs)
    {
        try
        {
            var input = (int)(result?.Usage?.InputTokenCount ?? 0);
            var model = _options.EmbeddingModel;

            var cost = 0m;
            if (_options.Pricing.TryGetValue(model, out var price))
            {
                cost = input * price.InputPerMillion / 1_000_000m;
            }

            var user = _http.HttpContext?.User;
            var usage = new AiUsage
            {
                CreatedAtUtc = DateTime.UtcNow,
                UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user?.FindFirstValue("uid")
                      ?? user?.FindFirstValue("sub"),
                Feature = feature,
                Model = model,
                InputTokens = input,
                OutputTokens = 0,
                CostUsd = cost,
                DurationMs = (int)elapsedMs,
                Success = result is not null
            };

            await using var scope = _scopeFactory.CreateAsyncScope();
            var recorder = scope.ServiceProvider.GetRequiredService<IAiUsageRecorder>();
            await recorder.RecordAsync(usage, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record embedding usage");
        }
    }
}