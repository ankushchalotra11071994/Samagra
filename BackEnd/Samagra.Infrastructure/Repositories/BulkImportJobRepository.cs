using Microsoft.EntityFrameworkCore;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;

namespace Samagra.Infrastructure.Repositories;

public sealed class BulkImportJobRepository : IBulkImportJobRepository
{
    private readonly AppDbContext _db;

    public BulkImportJobRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(BulkImportJob job, CancellationToken ct = default)
        => await _db.BulkImportJobs.AddAsync(job, ct);

    // tracked — controller status update करता है
    public async Task<BulkImportJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.BulkImportJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<IReadOnlyList<BulkImportJob>> GetRecentAsync(Guid createdBy, int take, CancellationToken ct = default)
        => await _db.BulkImportJobs
            .AsNoTracking()
            .Where(j => j.CreatedBy == createdBy)
            .OrderByDescending(j => j.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BulkImportRowError>> GetRowErrorsAsync(Guid jobId, int take, CancellationToken ct = default)
        => await _db.BulkImportRowErrors
            .AsNoTracking()
            .Where(e => e.JobId == jobId)
            .OrderBy(e => e.RowNumber)
            .Take(take)
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
