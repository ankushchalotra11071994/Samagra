// Samagra.API/Controllers/AdminDashboardController.cs
//
// Feeds the single admin landing screen, so it is one round trip rather than
// five separate calls from the browser.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samagra.Infrastructure.Data;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public sealed class AdminDashboardController : ControllerBase
{
    private const int LowStockThreshold = 10;

    private readonly AppDbContext _db;

    public AdminDashboardController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var sevenDaysAgo = DateTime.UtcNow.Date.AddDays(-6);

        var productCounts = await _db.Products
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(p => p.IsActive),
                LowStock = g.Count(p => p.IsActive && p.Stock <= LowStockThreshold),
                OutOfStock = g.Count(p => p.IsActive && p.Stock == 0)
            })
            .FirstOrDefaultAsync(ct);

        // CONFIRMED hone ke baad hi paisa aaya maante hain — PENDING/CANCELLED count nahi.
        var paidStatuses = new[]
        {
            OrderStatuses.Confirmed, OrderStatuses.Shipped, OrderStatuses.Delivered
        };

        var revenue = await _db.Orders
            .Where(o => paidStatuses.Contains(o.Status))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                AllTime = g.Sum(o => o.TotalAmount),
                LastSevenDays = g.Where(o => o.CreatedAt >= sevenDaysAgo).Sum(o => o.TotalAmount)
            })
            .FirstOrDefaultAsync(ct);

        var ordersByStatus = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var recentOrders = await _db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new
            {
                o.Id,
                o.UserId,
                o.TotalAmount,
                o.Status,
                o.CreatedAt
            })
            .ToListAsync(ct);

        var lowStock = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= LowStockThreshold)
            .OrderBy(p => p.Stock)
            .Take(8)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Stock,
                CategoryName = p.Category.Name
            })
            .ToListAsync(ct);

        return Ok(new
        {
            Products = new
            {
                Total = productCounts?.Total ?? 0,
                Active = productCounts?.Active ?? 0,
                LowStock = productCounts?.LowStock ?? 0,
                OutOfStock = productCounts?.OutOfStock ?? 0
            },
            Revenue = new
            {
                AllTime = revenue?.AllTime ?? 0m,
                LastSevenDays = revenue?.LastSevenDays ?? 0m
            },
            Orders = new
            {
                Total = ordersByStatus.Sum(x => x.Count),
                ByStatus = OrderStatuses.All.Select(s => new
                {
                    Status = s,
                    Count = ordersByStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0
                })
            },
            CategoryCount = await _db.Categories.CountAsync(c => c.IsActive, ct),
            LowStockThreshold,
            RecentOrders = recentOrders,
            LowStockProducts = lowStock
        });
    }
}
