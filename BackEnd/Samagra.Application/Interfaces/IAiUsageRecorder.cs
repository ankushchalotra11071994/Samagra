using Samagra.Domain.Entities;

namespace Samagra.Application.Interfaces;

public interface IAiUsageRecorder
{
    Task RecordAsync(AiUsage usage, CancellationToken ct = default);
    Task<decimal> GetTodaySpendAsync(string userId, CancellationToken ct = default);
    Task<AiUsageSummary> GetSummaryAsync(DateTime fromUtc, CancellationToken ct = default);
}

public sealed record AiUsageSummary(
    int TotalCalls,
    long InputTokens,
    long OutputTokens,
    decimal TotalCostUsd,
    IReadOnlyList<FeatureCost> ByFeature,
    IReadOnlyList<DailyCost> ByDay,
    IReadOnlyList<UserCost> TopUsers);

public sealed record FeatureCost(string Feature, int Calls, decimal CostUsd);
public sealed record DailyCost(DateOnly Day, int Calls, decimal CostUsd);
public sealed record UserCost(string? UserId, int Calls, decimal CostUsd);