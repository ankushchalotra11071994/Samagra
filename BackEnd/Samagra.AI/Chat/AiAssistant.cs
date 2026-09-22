 using Microsoft.Extensions.AI;
using Samagra.AI.Middleware;
using Samagra.AI.Tools;
using Samagra.Application.Interfaces;

namespace Samagra.AI.Chat;

internal sealed class AiAssistant : IAiAssistant
{
    private const double MaxDistance = 0.75;

    private readonly IChatClient _chatClient;
    private readonly IVectorSearch _vectorSearch;
    private readonly OrderTools _orderTools;
    private readonly PolicyTools _policyTools;


    public AiAssistant(
        IChatClient chatClient,
        IVectorSearch vectorSearch,
        OrderTools orderTools,
        PolicyTools policyTools)
    {
        _chatClient = chatClient;
        _vectorSearch = vectorSearch;
        _orderTools = orderTools;
        _policyTools = policyTools;
    }

    public async Task<string> AskAsync(string question, CancellationToken ct = default)
    {
        var response = await _chatClient.GetResponseAsync(question, CreateOptions("chat"), ct);
        return response.Text;
    }

    public async Task<string> AskWithRagAsync(string question, CancellationToken ct = default)
    {
        var chunks = (await _vectorSearch.SearchAsync(question, topK: 3, ct))
            .Where(c => c.Distance < MaxDistance)
            .ToList();

        if (chunks.Count == 0)
            return "I don't have that information.";

        var context = string.Join("\n\n---\n\n",
            chunks.Select(c => $"[Source: {c.Source}]\n{c.Content}"));

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                """
                You are a support assistant for Samagra.
                Answer using ONLY the provided context.
                If the context does not contain the answer, say "I don't have that information."
                Always mention which source you used.
                """),
            new(ChatRole.User,
                $"""
                CONTEXT:
                {context}

                QUESTION:
                {question}
                """)
        };

        var response = await _chatClient.GetResponseAsync(messages, CreateOptions("rag"), ct);
        return response.Text;
    }
  public async Task<string> AskWithToolsAsync(string question, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                """
                You are a support assistant for Samagra, an e-commerce platform.

                You have tools for two kinds of information:
                - the signed-in customer's own orders
                - Samagra's policy documents

                Decide which tools you need. A question may need both — for example,
                when a customer asks about their order and the rules that apply to it.
                Call them one at a time and use the results together.

                Only state facts returned by the tools. Never invent order ids, statuses,
                amounts or policy terms. If a tool returns an error or no data, say so plainly.
                Keep answers short and friendly.
                """),
            new(ChatRole.User, question)
        };

        var options = CreateOptions("tools");
        options.Tools = ToolFactory.CreateAll(_orderTools, _policyTools);

        var response = await _chatClient.GetResponseAsync(messages, options, ct);
        return response.Text;
    }
    private static ChatOptions CreateOptions(string feature) => new()
    {
        AdditionalProperties = new() { [CostTrackingChatClient.FeatureKey] = feature }
    };
}