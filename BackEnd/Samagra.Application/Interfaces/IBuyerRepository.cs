using Samagra.Domain.Entities;

namespace Samagra.Application.Interfaces;

public interface IBuyerRepository
{
    Task<Buyer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsWithNameAsync(string businessName, CancellationToken ct = default);
    Task AddAsync(Buyer buyer, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}