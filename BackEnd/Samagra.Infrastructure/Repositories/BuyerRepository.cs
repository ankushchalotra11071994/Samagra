using Microsoft.EntityFrameworkCore;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;

namespace Samagra.Infrastructure.Repositories;

public class BuyerRepository : IBuyerRepository
{
    private readonly AppDbContext _db;

    public BuyerRepository(AppDbContext db) => _db = db;

    public async Task<Buyer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Buyers.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<bool> ExistsWithNameAsync(string businessName, CancellationToken ct = default)
        => await _db.Buyers.AnyAsync(b => b.BusinessName == businessName, ct);

    public async Task AddAsync(Buyer buyer, CancellationToken ct = default)
        => await _db.Buyers.AddAsync(buyer, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}