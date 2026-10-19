using Samagra.Domain.Entities;

namespace Samagra.Application.Interfaces;

public interface IBulkImportJobRepository
{
    Task AddAsync(BulkImportJob job, CancellationToken ct = default);
    Task<BulkImportJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BulkImportJob>> GetRecentAsync(Guid createdBy, int take, CancellationToken ct = default);
    Task<IReadOnlyList<BulkImportRowError>> GetRowErrorsAsync(Guid jobId, int take, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
