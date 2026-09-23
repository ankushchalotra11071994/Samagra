using System.Text.Json.Serialization;

namespace Samagra.AiEval;

public sealed class TestCase
{
    public string Id { get; init; } = string.Empty;
    public string Question { get; init; } = string.Empty;
    public string? ExpectedSource { get; init; }
    public string[] ExpectedFacts { get; init; } = [];
    public bool ShouldRefuse { get; init; }
}

 public sealed class TestResult
{
    public required TestCase Case { get; init; }
    public string Answer { get; init; } = string.Empty;
    public string? RetrievedSource { get; init; }
    public double? Distance { get; init; }
    public int Rank { get; init; }
    public bool InTop3 { get; init; }
    public long LatencyMs { get; init; }

    public bool RetrievalPassed { get; init; }
    public bool AnswerPassed { get; init; }
    public string? FailureReason { get; init; }
}