namespace Samagra.Infrastructure.BulkImport;

public sealed class BulkImportOptions
{
    public const string SectionName = "BulkImport";

    public string ContainerName { get; set; } = "bulk-imports";
    public string QueueName { get; set; } = "bulk-imports";
    public int UploadUrlExpiryMinutes { get; set; } = 15;
    public long MaxFileSizeBytes { get; set; } = 100L * 1024 * 1024;   // 100 MB
}
