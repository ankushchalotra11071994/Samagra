namespace Samagra.Domain.Entities;

public class BulkImportRowError
{
    public long Id { get; set; }
    public Guid JobId { get; set; }
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;

    public BulkImportJob Job { get; set; } = null!;
}
