using Microsoft.EntityFrameworkCore;
using Samagra.Domain.Entities;

namespace Samagra.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
       
    }

    public DbSet<Buyer> Buyers => Set<Buyer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}