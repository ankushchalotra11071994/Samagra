using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Samagra.Domain.Entities;

namespace Samagra.Infrastructure.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).IsRequired().HasMaxLength(450);   // Identity key ki lambai
        builder.Property(a => a.Label).IsRequired().HasMaxLength(50);
        builder.Property(a => a.FullName).IsRequired().HasMaxLength(150);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Line1).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Line2).HasMaxLength(200);
        builder.Property(a => a.City).IsRequired().HasMaxLength(100);
        builder.Property(a => a.State).IsRequired().HasMaxLength(100);
        builder.Property(a => a.PostalCode).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Country).IsRequired().HasMaxLength(100);

        // Har list query "mere active addresses" hoti hai
        builder.HasIndex(a => new { a.UserId, a.IsActive });
    }
}
