// Samagra.API/Controllers/PaymentsController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Data;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PaymentsController(AppDbContext db)
    {
        _db = db;
    }

    // POST /api/payments
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        // Step 1 — Order exist karta hai?
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order is null)
            return NotFound(new { message = "Order not found" });

        // Step 2 — Order already paid toh nahi?
        if (order.Status == "CONFIRMED")
            return BadRequest(new { message = "Order already paid" });

        // Step 3 — Duplicate payment check
        var existingPayment = await _db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == request.OrderId
                                   && p.Status == "SUCCESS");

        if (existingPayment is not null)
            return BadRequest(new { message = "Payment already done for this order" });

        // Step 4 — Payment create karo
        var payment = new Payment
        {
            Id            = Guid.NewGuid(),
            OrderId       = request.OrderId,
            Amount        = order.TotalAmount,
            Status        = "PENDING",
            PaymentMethod = request.PaymentMethod
        };

        _db.Payments.Add(payment);

        // Step 5 — Simulate gateway — real mein Razorpay/Paytm call hoga
        var isSuccess = true; // abhi hardcode — baad mein gateway lagayenge

        // Step 6 — Status update karo
        payment.Status = isSuccess ? "SUCCESS" : "FAILED";
        order.Status   = isSuccess ? "CONFIRMED" : "CANCELLED";

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
        var payment = await _db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment is null) return NotFound();

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