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

        if (string.IsNullOrWhiteSpace(options.Endpoint) || string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException(
                "Ai:Endpoint and Ai:ApiKey must be set in appsettings.Development.json or user secrets.");

        var vectorDbConnection = configuration.GetConnectionString("VectorDb")
            ?? throw new InvalidOperationException("VectorDb connection string is missing.");

        var azureClient = new AzureOpenAIClient(
            new Uri(options.Endpoint),
            new AzureKeyCredential(options.ApiKey));

        services.AddHttpContextAccessor();

        // Chat — budget guard → function invocation → cost tracking → Azure
        services.AddSingleton<IChatClient>(sp =>
            azureClient.GetChatClient(options.ChatModel)
                .AsIChatClient()
                .AsBuilder()
                .Use(inner => new BudgetGuardChatClient(
                    inner,
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    sp.GetRequiredService<IHttpContextAccessor>(),
                    options))
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
            azureClient.GetEmbeddingClient(options.EmbeddingModel)
                .AsIEmbeddingGenerator()
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
                vectorDbConnection));

        services.AddScoped<IDocumentIndexer>(sp =>
            new DocumentIndexer(
                sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                vectorDbConnection));

        // Tools
        services.AddScoped<OrderTools>();
        services.AddScoped<PolicyTools>();
        // Assistant
        services.AddScoped<IAiAssistant, AiAssistant>();

        return services;
    }
}