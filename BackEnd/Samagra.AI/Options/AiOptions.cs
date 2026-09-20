namespace Samagra.AI.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Endpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ChatModel { get; init; } = "gpt-4.1-mini";
     public string EmbeddingModel { get; init; } = "text-embedding-3-small";
}