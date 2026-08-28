using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Samagra.Domain.Entities;

namespace Samagra.Infrastructure.Data.Configurations;

public class BuyerConfiguration : IEntityTypeConfiguration<Buyer>
{
    public void Configure(EntityTypeBuilder<Buyer> builder)
    {
        builder.ToTable("Buyers");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BusinessName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(b => b.PriceTier)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.IsActive)
            .IsRequired();

        builder.HasIndex(b => b.BusinessName);
    }
}