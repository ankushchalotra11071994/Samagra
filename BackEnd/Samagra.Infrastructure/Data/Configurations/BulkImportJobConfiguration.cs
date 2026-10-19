using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Samagra.Domain.Entities;

namespace Samagra.Infrastructure.Persistence.Configurations;

public class BulkImportJobConfiguration : IEntityTypeConfiguration<BulkImportJob>
{
    public void Configure(EntityTypeBuilder<BulkImportJob> builder)
    {
        builder.ToTable("BulkImportJobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.FileName).IsRequired().HasMaxLength(260);
        builder.Property(j => j.BlobName).IsRequired().HasMaxLength(500);
        builder.Property(j => j.ErrorMessage).HasMaxLength(2000);

        // string में store — Function plain SQL से यही values लिखता है
        builder.Property(j => j.Status)
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.HasIndex(j => new { j.CreatedBy, j.CreatedAtUtc });

        builder.HasMany(j => j.RowErrors)
               .WithOne(e => e.Job)
               .HasForeignKey(e => e.JobId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BulkImportRowErrorConfiguration : IEntityTypeConfiguration<BulkImportRowError>
{
    public void Configure(EntityTypeBuilder<BulkImportRowError> builder)
    {
        builder.ToTable("BulkImportRowErrors");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Message).IsRequired().HasMaxLength(1000);
        builder.HasIndex(e => new { e.JobId, e.RowNumber });
    }
}
