using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Samagra.Domain.Entities;

namespace Samagra.Infrastructure.Persistence.Configurations;

public class SavedCardConfiguration : IEntityTypeConfiguration<SavedCard>
{
    public void Configure(EntityTypeBuilder<SavedCard> builder)
    {
        builder.ToTable("SavedCards");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired().HasMaxLength(450);
        builder.Property(c => c.Brand).IsRequired().HasMaxLength(30);

        // 4 digits — is table mein poora card number aane ki koi jagah hi nahi hai
        builder.Property(c => c.Last4).IsRequired().HasMaxLength(4).IsFixedLength();

        builder.Property(c => c.CardholderName).IsRequired().HasMaxLength(150);

        builder.HasIndex(c => new { c.UserId, c.IsActive });
    }
}
