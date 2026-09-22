 using Microsoft.EntityFrameworkCore;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;

namespace Samagra.Infrastructure.Repositories;

public sealed class AiUsageRecorder : IAiUsageRecorder
{
    private readonly AppDbContext _db;

    public AiUsageRecorder(AppDbContext db) => _db = db;

    public async Task RecordAsync(AiUsage usage, CancellationToken ct = default)
    {
        _db.AiUsages.Add(usage);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<decimal> GetTodaySpendAsync(string userId, CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;

        return await _db.AiUsages
            .Where(u => u.UserId == userId && u.CreatedAtUtc >= todayUtc)
            .SumAsync(u => u.CostUsd, ct);
    }

    public async Task<AiUsageSummary> GetSummaryAsync(DateTime fromUtc, CancellationToken ct = default)
    {
        var query = _db.AiUsages
            .AsNoTracking()
            .Where(u => u.CreatedAtUtc >= fromUtc);

        // 1. कुल
        var totals = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Calls = g.Count(),
                Input = g.Sum(x => (long)x.InputTokens),
                Output = g.Sum(x => (long)x.OutputTokens),
                Cost = g.Sum(x => x.CostUsd)
            })
            .FirstOrDefaultAsync(ct);

        // 2. Feature-wise
        var byFeature = await query
            .GroupBy(u => u.Feature)
            .Select(g => new FeatureCost(g.Key, g.Count(), g.Sum(x => x.CostUsd)))
            .OrderByDescending(x => x.CostUsd)
            .ToListAsync(ct);

        // 3. Day-wise
        var byDayRaw = await query
            .GroupBy(u => u.CreatedAtUtc.Date)
            .Select(g => new { Day = g.Key, Calls = g.Count(), Cost = g.Sum(x => x.CostUsd) })
            .OrderBy(x => x.Day)
            .ToListAsync(ct);

        var byDay = byDayRaw
            .Select(x => new DailyCost(DateOnly.FromDateTime(x.Day), x.Calls, x.Cost))
            .ToList();

        // 4. Top 10 users
        var topUsers = await query
            .GroupBy(u => u.UserId)
            .Select(g => new UserCost(g.Key, g.Count(), g.Sum(x => x.CostUsd)))
            .OrderByDescending(x => x.CostUsd)
            .Take(10)
            .ToListAsync(ct);

        return new AiUsageSummary(
            totals?.Calls ?? 0,
            totals?.Input ?? 0,
            totals?.Output ?? 0,
            totals?.Cost ?? 0,
            byFeature,
            byDay,
            topUsers);
    }
}