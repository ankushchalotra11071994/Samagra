// Samagra.API/Controllers/OrdersController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    // Same pattern as Samagra.AI/Tools/OrderTools — the user id comes from the
    // token, never from the request body.
    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");

    private bool IsAdmin => User.IsInRole("Admin");

    // POST /api/orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new { message = "Order must contain at least one item" });

        // Same product twice in one payload would skip the stock check on the
        // second row, so collapse duplicates first.
        var wanted = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (wanted.Any(i => i.Quantity < 1))
            return BadRequest(new { message = "Quantity must be at least 1" });

        // The stock check and the stock write have to see the same rows, so both
        // run inside one transaction.
        await using var tx = await _db.Database.BeginTransactionAsync();

        var ids = wanted.Select(i => i.ProductId).ToList();

        var products = await _db.Products
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        decimal total = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in wanted)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                return BadRequest(new { message = $"Product {item.ProductId} not found" });

            if (product.Stock < item.Quantity)
                return BadRequest(new { message = $"Insufficient stock for {product.Name}" });

            product.Stock -= item.Quantity;

            orderItems.Add(new OrderItem
            {
                Id        = Guid.NewGuid(),
                ProductId = item.ProductId,
                Quantity  = item.Quantity,
                UnitPrice = product.Price
            });

            total += product.Price * item.Quantity;
        }

        var order = new Order
        {
            Id          = Guid.NewGuid(),
            UserId      = userId,
            TotalAmount = total,
            Status      = OrderStatuses.Pending,
            OrderItems  = orderItems
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            order.Id,
            order.TotalAmount,
            order.Status,
            order.CreatedAt
        });
    }

    // GET /api/orders/mine — the signed-in user's own order history
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.TotalAmount,
                o.Status,
                o.CreatedAt,
                ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
                PaymentStatus = o.Payment == null ? null : o.Payment.Status
            })
            .ToListAsync(ct);

        return Ok(orders);
    }

    // GET /api/orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return NotFound();

        // A buyer may only read their own order; an admin may read any.
        if (!IsAdmin && order.UserId != userId) return NotFound();

        return Ok(new
        {
            order.Id,
            order.UserId,
            order.TotalAmount,
            order.Status,
            order.CreatedAt,
            Payment = order.Payment == null ? null : new
            {
                order.Payment.Id,
                order.Payment.Status,
                order.Payment.PaymentMethod,
                order.Payment.Amount,
                order.Payment.CreatedAt
            },
            Items = order.OrderItems.Select(oi => new
            {
                oi.ProductId,
                ProductName = oi.Product.Name,
                oi.Product.ImageUrl,
                oi.Quantity,
                oi.UnitPrice,
                SubTotal = oi.Quantity * oi.UnitPrice
            })
        });
    }

    // GET /api/orders?status=&page=&pageSize= — admin order management
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!OrderStatuses.IsValid(status))
                return BadRequest(new { message = $"Unknown status '{status}'" });

            query = query.Where(o => o.Status == status);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.UserId,
                o.TotalAmount,
                o.Status,
                o.CreatedAt,
                ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
                PaymentStatus = o.Payment == null ? null : o.Payment.Status
            })
            .ToListAsync(ct);

        return Ok(new { items, page, pageSize, total });
    }

    // PATCH /api/orders/{id}/status — admin moves an order along
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        if (!OrderStatuses.IsValid(request.Status))
            return BadRequest(new { message = $"Unknown status '{request.Status}'" });

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();

        if (!OrderStatuses.CanMove(order.Status, request.Status))
            return BadRequest(new
            {
                message = $"An order that is {order.Status} cannot move to {request.Status}"
            });

        order.Status = request.Status;
        await _db.SaveChangesAsync(ct);

        return Ok(new { order.Id, order.Status });
    }
}

// Status strings are stored on Order.Status as plain text (PaymentsController
// writes "CONFIRMED"/"CANCELLED" too), so the allowed set lives here.
public static class OrderStatuses
{
    public const string Pending   = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string Shipped   = "SHIPPED";
    public const string Delivered = "DELIVERED";
    public const string Cancelled = "CANCELLED";

    public static readonly string[] All =
        [Pending, Confirmed, Shipped, Delivered, Cancelled];

    public static bool IsValid(string? status) =>
        status is not null && All.Contains(status, StringComparer.Ordinal);

    // Allowed forward moves. Delivered and Cancelled are terminal.
    private static readonly Dictionary<string, string[]> Next = new(StringComparer.Ordinal)
    {
        [Pending]   = [Confirmed, Cancelled],
        [Confirmed] = [Shipped, Cancelled],
        [Shipped]   = [Delivered],
        [Delivered] = [],
        [Cancelled] = []
    };

    public static bool CanMove(string from, string to) =>
        Next.TryGetValue(from, out var allowed) && allowed.Contains(to, StringComparer.Ordinal);
}

// Request DTOs — UserId is no longer accepted from the client, it comes from the token.
public record CreateOrderRequest(
    List<OrderItemRequest> Items
);

public record OrderItemRequest(
    Guid ProductId,
    int  Quantity
);

public record UpdateOrderStatusRequest(string Status);
