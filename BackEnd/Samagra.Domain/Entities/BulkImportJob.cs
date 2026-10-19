namespace Samagra.Domain.Entities;

public enum BulkImportStatus
{
    Pending,     // upload URL issued, file not uploaded yet
    Queued,      // file uploaded, message sent to Service Bus
    Processing,  // function picked it up
    Completed,
    Failed
}

public class BulkImportJob
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
    public BulkImportStatus Status { get; set; } = BulkImportStatus.Pending;

    public int TotalRows { get; set; }
    public int SucceededRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorMessage { get; set; }

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<BulkImportRowError> RowErrors { get; set; } = new List<BulkImportRowError>();
}
