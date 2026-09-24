using System.ComponentModel;
using ModelContextProtocol.Server;
using Samagra.AI.Tools;

namespace Samagra.AI.Mcp;

[McpServerToolType]
public sealed class OrderMcpTools
{
    private readonly OrderTools _orders;

    public OrderMcpTools(OrderTools orders) => _orders = orders;

    [McpServerTool(Name = "get_my_recent_orders")]
    [Description("Gets the signed-in customer's most recent orders, with order id, " +
                 "status, total amount and date placed. Use this when the customer " +
                 "asks about their orders in general.")]
    public Task<object> GetMyRecentOrdersAsync(
        [Description("How many recent orders to return, between 1 and 10.")]
        int count = 3)
        => _orders.GetMyRecentOrdersAsync(count);

    [McpServerTool(Name = "get_order_details")]
    [Description("Gets full details of one specific order, including the products in it. " +
                 "Requires the order id. Use this when the customer asks about a particular order.")]
    public Task<object> GetOrderDetailsAsync(
        [Description("The order id, a GUID.")]
        string orderId)
        => _orders.GetOrderDetailsAsync(orderId);
}