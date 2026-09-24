using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Samagra.Infrastructure.Data;

namespace Samagra.AI.Tools;

public sealed class OrderTools
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public OrderTools(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    private string? CurrentUserId =>
        _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _http.HttpContext?.User.FindFirstValue("uid");

    public async Task<object> GetMyRecentOrdersAsync(int count = 3)
    {
        var userId = CurrentUserId;
        if (userId is null) return new { error = "Not signed in." };

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(Math.Clamp(count, 1, 10))
            .Select(o => new
            {
                orderId = o.Id,
                status = o.Status,
                total = o.TotalAmount,
                placedOn = o.CreatedAt
            })
            .ToListAsync();

        return orders.Count == 0
            ? new { message = "No orders found." }
            : (object)orders;
    }

    public async Task<object> GetOrderDetailsAsync(string orderId)
    {
        var userId = CurrentUserId;
        if (userId is null) return new { error = "Not signed in." };

        if (!Guid.TryParse(orderId, out var id))
            return new { error = "Invalid order id." };

        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == id && o.UserId == userId)
            .Select(o => new
            {
                orderId = o.Id,
                status = o.Status,
                total = o.TotalAmount,
                placedOn = o.CreatedAt,
                items = o.OrderItems.Select(i => new
                {
                    product = i.Product.Name,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice
                })
            })
            .FirstOrDefaultAsync();

        return order is null
            ? new { error = "Order not found." }
            : order;
    }
}