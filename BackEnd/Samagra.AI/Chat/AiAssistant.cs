using Microsoft.Extensions.AI;
using Samagra.Application.Interfaces;

namespace Samagra.AI.Chat;

internal sealed class AiAssistant : IAiAssistant
{
    private const double MaxDistance = 0.6;

    private readonly IChatClient _chatClient;
    private readonly IVectorSearch _vectorSearch;

    public AiAssistant(IChatClient chatClient, IVectorSearch vectorSearch)
    {
        _chatClient = chatClient;
        _vectorSearch = vectorSearch;
    }

    public async Task<string> AskAsync(string question, CancellationToken ct = default)
    {
        var response = await _chatClient.GetResponseAsync(question, cancellationToken: ct);
        return response.Text;
    }

    public async Task<string> AskWithRagAsync(string question, CancellationToken ct = default)
    {
        // 1. Retrieval - relevant chunks ढूँढो
        var chunks = (await _vectorSearch.SearchAsync(question, topK: 3, ct))
            .Where(c => c.Distance < MaxDistance)
            .ToList();

        if (chunks.Count == 0)
            return "I don't have that information.";

        // 2. Augmentation - context बनाओ
        var context = string.Join("\n\n---\n\n",
            chunks.Select(c => $"[Source: {c.Source}]\n{c.Content}"));

        var prompt = $"""
            Answer the question using ONLY the context below.
            If the context does not contain the answer, say "I don't have that information."
            Always mention which source you used.

            CONTEXT:
            {context}

            QUESTION:
            {question}
            """;

        // 3. Generation - LLM से जवाब
        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: ct);
        return response.Text;
    }
}