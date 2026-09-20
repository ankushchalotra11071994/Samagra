using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Samagra.AI.Chat;
using Samagra.AI.Options;
using Samagra.AI.Rag;
using Samagra.Application.Interfaces;

namespace Samagra.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(AiOptions.SectionName)
            .Get<AiOptions>()
            ?? throw new InvalidOperationException("Ai configuration section is missing.");

        var vectorDbConnection = configuration.GetConnectionString("VectorDb")
            ?? throw new InvalidOperationException("VectorDb connection string is missing.");

        var azureClient = new AzureOpenAIClient(
            new Uri(options.Endpoint),
            new AzureKeyCredential(options.ApiKey));

        // Chat
        services.AddSingleton<IChatClient>(
            azureClient.GetChatClient(options.ChatModel).AsIChatClient());

        // Embeddings
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
            azureClient.GetEmbeddingClient(options.EmbeddingModel).AsIEmbeddingGenerator());

        // RAG
        services.AddScoped<IVectorSearch>(sp =>
            new VectorSearch(
                sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                vectorDbConnection));

        services.AddScoped<IDocumentIndexer>(sp =>
            new DocumentIndexer(
                sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                vectorDbConnection));

        // Assistant  ← ये missing थी
        services.AddScoped<IAiAssistant, AiAssistant>();

        return services;
    }
}