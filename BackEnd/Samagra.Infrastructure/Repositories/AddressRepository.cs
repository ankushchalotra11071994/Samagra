using Microsoft.EntityFrameworkCore;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;

namespace Samagra.Infrastructure.Repositories;

public sealed class AddressRepository : IAddressRepository
{
    private readonly AppDbContext _db;

    public AddressRepository(AppDbContext db) => _db = db;

    // Har query userId se filter hoti hai — doosre ka pata kabhi nahi mil sakta
    public async Task<IReadOnlyList<Address>> ListAsync(string userId, CancellationToken ct = default) =>
        await _db.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public Task<Address?> GetAsync(Guid id, string userId, CancellationToken ct = default) =>
        _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && a.IsActive, ct);

    public Task<Address?> GetDefaultAsync(string userId, CancellationToken ct = default) =>
        _db.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public Task<int> CountAsync(string userId, CancellationToken ct = default) =>
        _db.Addresses.CountAsync(a => a.UserId == userId && a.IsActive, ct);

    public async Task AddAsync(Address address, CancellationToken ct = default) =>
        await _db.Addresses.AddAsync(address, ct);

    public async Task ClearDefaultAsync(string userId, Guid exceptId, CancellationToken ct = default)
    {
        var others = await _db.Addresses
            .Where(a => a.UserId == userId && a.IsDefault && a.Id != exceptId)
            .ToListAsync(ct);

        foreach (var other in others) other.IsDefault = false;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
