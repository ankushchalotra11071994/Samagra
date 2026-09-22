using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Identity;

namespace Samagra.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Buyer> Buyers => Set<Buyer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order>     Orders     { get; set; }
public DbSet<OrderItem> OrderItems { get; set; }
public DbSet<Payment>   Payments   { get; set; }

public DbSet<AiUsage> AiUsages => Set<AiUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
    .Property(p => p.Price)
    .HasPrecision(18, 2); 

        modelBuilder.Entity<AiUsage>(e =>
{
    e.Property(x => x.CostUsd).HasPrecision(18, 8);
    e.Property(x => x.Feature).HasMaxLength(50);
    e.Property(x => x.Model).HasMaxLength(100);
    e.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
});
        base.OnModelCreating(modelBuilder);   // REQUIRED — builds the Identity tables

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}