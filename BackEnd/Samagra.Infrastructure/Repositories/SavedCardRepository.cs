using Microsoft.EntityFrameworkCore;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;

namespace Samagra.Infrastructure.Repositories;

public sealed class SavedCardRepository : ISavedCardRepository
{
    private readonly AppDbContext _db;

    public SavedCardRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SavedCard>> ListAsync(string userId, CancellationToken ct = default) =>
        await _db.SavedCards
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.IsDefault)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<SavedCard?> GetAsync(Guid id, string userId, CancellationToken ct = default) =>
        _db.SavedCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.IsActive, ct);

    public Task<SavedCard?> GetDefaultAsync(string userId, CancellationToken ct = default) =>
        _db.SavedCards
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.IsDefault)
            .ThenByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

    // Wahi card dobara save na ho — brand + last4 + expiry se pata chal jaata hai
    public Task<bool> ExistsAsync(
        string userId, string brand, string last4, int month, int year, CancellationToken ct = default) =>
        _db.SavedCards.AnyAsync(
            c => c.UserId == userId && c.IsActive &&
                 c.Brand == brand && c.Last4 == last4 &&
                 c.ExpiryMonth == month && c.ExpiryYear == year, ct);

    public Task<int> CountAsync(string userId, CancellationToken ct = default) =>
        _db.SavedCards.CountAsync(c => c.UserId == userId && c.IsActive, ct);

    public async Task AddAsync(SavedCard card, CancellationToken ct = default) =>
        await _db.SavedCards.AddAsync(card, ct);

    public async Task ClearDefaultAsync(string userId, Guid exceptId, CancellationToken ct = default)
    {
        var others = await _db.SavedCards
            .Where(c => c.UserId == userId && c.IsDefault && c.Id != exceptId)
            .ToListAsync(ct);

        foreach (var other in others) other.IsDefault = false;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
