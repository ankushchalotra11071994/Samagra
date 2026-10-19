// Samagra.API/Controllers/PaymentsController.cs

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
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PaymentsController(AppDbContext db)
    {
        _db = db;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");

    private bool IsAdmin => User.IsInRole("Admin");

    // POST /api/payments
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        // Step 1 — Order exist karta hai?
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order is null)
            return NotFound(new { message = "Order not found" });

        // Step 2 — Sirf apna order pay kar sakte hain
        if (!IsAdmin && order.UserId != userId)
            return NotFound(new { message = "Order not found" });

        // Step 3 — Order already paid toh nahi?
        if (order.Status == OrderStatuses.Confirmed)
            return BadRequest(new { message = "Order already paid" });

        if (order.Status == OrderStatuses.Cancelled)
            return BadRequest(new { message = "Order was cancelled" });

        // Step 4 — Duplicate payment check
        var existingPayment = await _db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == request.OrderId
                                   && p.Status == "SUCCESS");

        if (existingPayment is not null)
            return BadRequest(new { message = "Payment already done for this order" });

        // Step 5 — Payment create karo
        var payment = new Payment
        {
            Id            = Guid.NewGuid(),
            OrderId       = request.OrderId,
            Amount        = order.TotalAmount,
            Status        = "PENDING",
            PaymentMethod = request.PaymentMethod
        };

        _db.Payments.Add(payment);

        // Step 6 — Simulate gateway — real mein Razorpay/Paytm call hoga
        var isSuccess = true; // abhi hardcode — baad mein gateway lagayenge

        // Step 7 — Status update karo
        payment.Status = isSuccess ? "SUCCESS" : "FAILED";
        order.Status   = isSuccess ? OrderStatuses.Confirmed : OrderStatuses.Cancelled;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            payment.Id,
            payment.OrderId,
            payment.Amount,
            payment.Status,
            payment.PaymentMethod,
            payment.CreatedAt
        });
    }

    // GET /api/payments/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var payment = await _db.Payments
            .AsNoTracking()
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null) return NotFound();

        if (!IsAdmin && payment.Order.UserId != userId) return NotFound();

        return Ok(new
        {
            payment.Id,
            payment.OrderId,
            payment.Amount,
            payment.Status,
            payment.PaymentMethod,
            payment.CreatedAt,
            Order = new
            {
                payment.Order.Id,
                payment.Order.Status,
                payment.Order.TotalAmount
            }
        });
    }
}

public record CreatePaymentRequest(
    Guid   OrderId,
    string PaymentMethod
);
