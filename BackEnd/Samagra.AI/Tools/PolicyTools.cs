using Samagra.Application.Interfaces;

namespace Samagra.AI.Tools;

public sealed class PolicyTools
{
    private const double MaxDistance = 0.75;

    private readonly IVectorSearch _vectorSearch;

    public PolicyTools(IVectorSearch vectorSearch) => _vectorSearch = vectorSearch;

    public async Task<object> SearchPolicyAsync(string question)
    {
        var chunks = (await _vectorSearch.SearchAsync(question, topK: 3))
            .Where(c => c.Distance < MaxDistance)
            .ToList();

        if (chunks.Count == 0)
            return new { message = "No matching policy information found." };

        return chunks.Select(c => new { source = c.Source, content = c.Content });
    }
}