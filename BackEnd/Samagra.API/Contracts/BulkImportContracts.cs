using System.ComponentModel.DataAnnotations;

namespace Samagra.API.Contracts;

public record CreateUploadUrlRequest(
    [Required, MaxLength(200)] string FileName,
    [Range(1, long.MaxValue)] long FileSizeBytes);

public record CreateUploadUrlResponse(
    Guid JobId,
    string UploadUrl,
    DateTimeOffset ExpiresAt);

public record BulkImportJobResponse(
    Guid Id,
    string FileName,
    string Status,
    int TotalRows,
    int SucceededRows,
    int FailedRows,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public record BulkImportRowErrorResponse(int RowNumber, string Message);
