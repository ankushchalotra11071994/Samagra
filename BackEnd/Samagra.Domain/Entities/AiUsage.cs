namespace Samagra.Domain.Entities;

public sealed class AiUsage
{
    public long Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UserId { get; set; }
    public string Feature { get; set; } = string.Empty;   // "chat", "rag", "embedding"
    public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal CostUsd { get; set; }
    public int DurationMs { get; set; }
    public bool Success { get; set; }
}