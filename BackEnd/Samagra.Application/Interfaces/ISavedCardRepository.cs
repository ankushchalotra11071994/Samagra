using Samagra.Domain.Entities;

namespace Samagra.Application.Interfaces;

public interface ISavedCardRepository
{
    Task<IReadOnlyList<SavedCard>> ListAsync(string userId, CancellationToken ct = default);

    /// <summary>Tracked — safe to mutate and then SaveChanges. Scoped to the owner.</summary>
    Task<SavedCard?> GetAsync(Guid id, string userId, CancellationToken ct = default);

    Task<SavedCard?> GetDefaultAsync(string userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string userId, string brand, string last4, int month, int year, CancellationToken ct = default);
    Task<int> CountAsync(string userId, CancellationToken ct = default);
    Task AddAsync(SavedCard card, CancellationToken ct = default);
    Task ClearDefaultAsync(string userId, Guid exceptId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
