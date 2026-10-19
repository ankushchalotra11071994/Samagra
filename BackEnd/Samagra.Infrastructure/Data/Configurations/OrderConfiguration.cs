using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Samagra.Domain.Entities;

namespace Samagra.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.Status).IsRequired().HasMaxLength(20);

        // Shipping snapshot — sab nullable, kyunki purane orders mein pata nahi tha
        builder.Property(o => o.ShipToName).HasMaxLength(150);
        builder.Property(o => o.ShipToPhone).HasMaxLength(20);
        builder.Property(o => o.ShipToLine1).HasMaxLength(200);
        builder.Property(o => o.ShipToLine2).HasMaxLength(200);
        builder.Property(o => o.ShipToCity).HasMaxLength(100);
        builder.Property(o => o.ShipToState).HasMaxLength(100);
        builder.Property(o => o.ShipToPostalCode).HasMaxLength(20);
        builder.Property(o => o.ShipToCountry).HasMaxLength(100);

        // ShippingAddressId jaan boojh kar FK nahi hai: address soft-delete ho
        // sakta hai, aur order ko uska snapshot hi chahiye, live row nahi.
        builder.HasIndex(o => o.UserId);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(20);
        builder.Property(p => p.PaymentMethod).IsRequired().HasMaxLength(20);
        builder.Property(p => p.CardLast4).HasMaxLength(4).IsFixedLength();
    }
}
