using System.ComponentModel;
using ModelContextProtocol.Server;
using Samagra.AI.Tools;

namespace Samagra.AI.Mcp;

[McpServerToolType]
public sealed class PolicyMcpTools
{
    private readonly PolicyTools _policy;

    public PolicyMcpTools(PolicyTools policy) => _policy = policy;

    [McpServerTool(Name = "search_policy")]
    [Description("Searches Samagra's policy documents — returns, refunds, shipping, " +
                 "warranty, payments and account rules. Use this for questions about " +
                 "rules or policies, not about a specific customer's own orders.")]
    public Task<object> SearchPolicyAsync(
        [Description("The customer's question, in their own words.")]
        string question)
        => _policy.SearchPolicyAsync(question);
}