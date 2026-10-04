namespace Samagra.AI.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    // "OpenAI" (Azure OpenAI) या "Ollama" — यहीं से provider switch होता है
    public string Provider { get; init; } = AiProviders.OpenAI;

    public string Endpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ChatModel { get; init; } = "gpt-4.1-mini";
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";
    public string VectorTable { get; init; } = "document_chunks";          // 1536 dims
    public string McpServerUrl { get; init; } = "http://localhost:5109/mcp";

    public OllamaOptions Ollama { get; init; } = new();

    public Dictionary<string, ModelPricing> Pricing { get; init; } = new();
    public decimal DailyBudgetPerUserUsd { get; init; } = 0.10m;

    public bool IsOllama => string.Equals(Provider, AiProviders.Ollama, StringComparison.OrdinalIgnoreCase);

    // Provider के हिसाब से active values
    public string ActiveChatModel => IsOllama ? Ollama.ChatModel : ChatModel;
    public string ActiveEmbeddingModel => IsOllama ? Ollama.EmbeddingModel : EmbeddingModel;
    public string ActiveVectorTable => IsOllama ? Ollama.VectorTable : VectorTable;
}

public static class AiProviders
{
    public const string OpenAI = "OpenAI";
    public const string Ollama = "Ollama";
}

public sealed class OllamaOptions
{
    public string Endpoint { get; init; } = "http://localhost:11434";
    public string ChatModel { get; init; } = "qwen2.5:7b";
    public string EmbeddingModel { get; init; } = "nomic-embed-text";
    public string VectorTable { get; init; } = "document_chunks_768";      // 768 dims
}

public sealed class ModelPricing
{
    public decimal InputPerMillion { get; init; }
    public decimal OutputPerMillion { get; init; }
}
