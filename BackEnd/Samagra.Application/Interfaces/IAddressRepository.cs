using Samagra.Domain.Entities;

namespace Samagra.Application.Interfaces;

public interface IAddressRepository
{
    Task<IReadOnlyList<Address>> ListAsync(string userId, CancellationToken ct = default);

    /// <summary>Tracked — safe to mutate and then SaveChanges. Scoped to the owner.</summary>
    Task<Address?> GetAsync(Guid id, string userId, CancellationToken ct = default);

    Task<Address?> GetDefaultAsync(string userId, CancellationToken ct = default);
    Task<int> CountAsync(string userId, CancellationToken ct = default);
    Task AddAsync(Address address, CancellationToken ct = default);

    /// <summary>Clears IsDefault on every other address of this user.</summary>
    Task ClearDefaultAsync(string userId, Guid exceptId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
