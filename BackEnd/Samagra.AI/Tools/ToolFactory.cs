using Microsoft.Extensions.AI;

namespace Samagra.AI.Tools;

internal static class ToolFactory
{
    public static IList<AITool> CreateAll(OrderTools tools,PolicyTools policy) =>
    [
        AIFunctionFactory.Create(
            tools.GetMyRecentOrdersAsync,
            name: "get_my_recent_orders",
            description: "Gets the signed-in customer's most recent orders, " +
                         "with order id, status, total amount and date placed. " +
                         "Use this when the customer asks about their orders in general."),

        AIFunctionFactory.Create(
            tools.GetOrderDetailsAsync,
            name: "get_order_details",
            description: "Gets full details of one specific order, including the products in it. " +
                         "Requires the order id. Use this when the customer asks about a particular order."),
   
   AIFunctionFactory.Create(
            policy.SearchPolicyAsync,
            name: "search_policy",
            description: "Searches Samagra's policy documents — returns, refunds, shipping, " +
                         "delivery times and exchanges. Use this for questions about rules or " +
                         "policies, not about a specific customer's own orders.")
    ];
}