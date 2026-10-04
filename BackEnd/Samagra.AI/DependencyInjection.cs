 using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Samagra.AI.Chat;
using Samagra.AI.Middleware;
using Samagra.AI.Options;
using Samagra.AI.Rag;
using Samagra.AI.Tools;
using Samagra.Application.Interfaces;
using Samagra.AI.Agents;
using Samagra.AI.Mcp;
using OllamaSharp;
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

        // Provider switch — "Ai:Provider" = "OpenAI" | "Ollama"
        var (baseChatClient, baseEmbedder) = CreateProviderClients(options);

        services.AddHttpContextAccessor();

        // Chat — budget guard → function invocation → cost tracking → provider
      services.AddSingleton<IChatClient>(sp =>
    baseChatClient
        .AsBuilder()
        .Use(inner => new BudgetGuardChatClient(
            inner,
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IHttpContextAccessor>(),
            options))
        .Use(inner => new InputGuardChatClient(
            inner,
            sp.GetRequiredService<ILogger<InputGuardChatClient>>()))
        .UseFunctionInvocation()
        .Use(inner => new CostTrackingChatClient(
            inner,
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IHttpContextAccessor>(),
            options,
            sp.GetRequiredService<ILogger<CostTrackingChatClient>>()))
        .Build(sp));

        // Embeddings — cost tracking के साथ
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            baseEmbedder
                .AsBuilder()
                .Use(inner => new CostTrackingEmbeddingGenerator(
                    inner,
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    options,
                    sp.GetRequiredService<ILogger<CostTrackingEmbeddingGenerator>>()))
                .Build(sp));

        // RAG
        services.AddScoped<IVectorSearch>(sp =>
            new VectorSearch(
                sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                vectorDbConnection,
                options.ActiveVectorTable));

        services.AddScoped<IDocumentIndexer>(sp =>
            new DocumentIndexer(
                sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                vectorDbConnection,
                options.ActiveVectorTable));

        // Tools
        services.AddScoped<OrderTools>();
        services.AddScoped<PolicyTools>();

        
        // Assistant
        services.AddScoped<IAiAssistant, AiAssistant>();

        // Support agent
        services.AddMemoryCache(); 
services.AddSingleton(options);
services.AddScoped<IAgentChat, SupportAgent>();

        return services;
    }

    private static (IChatClient Chat, IEmbeddingGenerator<string, Embedding<float>> Embedder)
        CreateProviderClients(AiOptions options)
    {
        if (options.IsOllama)
        {
            if (string.IsNullOrWhiteSpace(options.Ollama.Endpoint))
                throw new InvalidOperationException("Ai:Ollama:Endpoint must be set when Ai:Provider is Ollama.");

            var endpoint = new Uri(options.Ollama.Endpoint);

            // OllamaApiClient खुद IChatClient और IEmbeddingGenerator दोनों है
            return (
                new OllamaApiClient(endpoint, options.Ollama.ChatModel),
                new OllamaApiClient(endpoint, options.Ollama.EmbeddingModel));
        }

        if (!string.Equals(options.Provider, AiProviders.OpenAI, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Unknown Ai:Provider '{options.Provider}'. Use '{AiProviders.OpenAI}' or '{AiProviders.Ollama}'.");

        if (string.IsNullOrWhiteSpace(options.Endpoint) || string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException(
                "Ai:Endpoint and Ai:ApiKey must be set in appsettings.Development.json or user secrets.");

        var azureClient = new AzureOpenAIClient(
            new Uri(options.Endpoint),
            new AzureKeyCredential(options.ApiKey));

        return (
            azureClient.GetChatClient(options.ChatModel).AsIChatClient(),
            azureClient.GetEmbeddingClient(options.EmbeddingModel).AsIEmbeddingGenerator());
    }
}