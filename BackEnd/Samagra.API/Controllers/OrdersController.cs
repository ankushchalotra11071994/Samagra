// Samagra.API/Controllers/OrdersController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    // POST /api/orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // Step 1 — Validate products exist
        foreach (var item in request.Items)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive);

            if (product is null)
                return BadRequest(new { message = $"Product {item.ProductId} not found" });

            if (product.Stock < item.Quantity)
                return BadRequest(new { message = $"Insufficient stock for {product.Name}" });
        }

        // Step 2 — Calculate total
        decimal total = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            var orderItem = new OrderItem
            {
                Id        = Guid.NewGuid(),
                ProductId = item.ProductId,
                Quantity  = item.Quantity,
                UnitPrice = product!.Price
            };

            total += product.Price * item.Quantity;
            orderItems.Add(orderItem);
        }

        // Step 3 — Create Order
        var order = new Order
        {
            Id          = Guid.NewGuid(),
            UserId      = request.UserId,
            TotalAmount = total,
            Status      = "PENDING",
            OrderItems  = orderItems
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            order.Id,
            order.TotalAmount,
            order.Status,
            order.CreatedAt
        });
    }

    // GET /api/orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        return Ok(new
        {
            order.Id,
            order.UserId,
            order.TotalAmount,
            order.Status,
            order.CreatedAt,
            Items = order.OrderItems.Select(oi => new
            {
                oi.ProductId,
                ProductName = oi.Product.Name,
                oi.Quantity,
                oi.UnitPrice,
                SubTotal = oi.Quantity * oi.UnitPrice
            })
        });
    }
}

// Request DTOs
public record CreateOrderRequest(
    string UserId,
    List<OrderItemRequest> Items
);

public record OrderItemRequest(
    Guid ProductId,
    int  Quantity
);