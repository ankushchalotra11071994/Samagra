// Samagra.API/Controllers/CardsController.cs
//
// Yahan card number aata hai, check hota hai, aur wahi reh jaata hai jo UI ko
// chahiye: brand + last 4. Poora number (PAN) aur CVV kahin store nahi hote —
// na DB mein, na log mein. Isi wajah se CVV ka field hi nahi hai.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Samagra.API.Contracts;
using Samagra.Application.Interfaces;
using Samagra.Domain;
using Samagra.Domain.Entities;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CardsController : ControllerBase
{
    private const int MaxPerUser = 10;

    private readonly ISavedCardRepository _cards;
    private readonly TimeProvider _clock;

    public CardsController(ISavedCardRepository cards, TimeProvider clock)
    {
        _cards = cards;
        _clock = clock;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");

    // GET /api/cards
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var now = _clock.GetUtcNow().UtcDateTime;
        var list = await _cards.ListAsync(userId, ct);

        return Ok(list.Select(c => Shape(c, now)));
    }

    // POST /api/cards
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveCardRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        if (!CardNumber.IsValid(request.Number))
            return BadRequest(new { message = "That card number doesn't look right. Check the digits." });

        var now = _clock.GetUtcNow().UtcDateTime;

        if (!CardNumber.IsExpiryValid(request.ExpiryMonth, request.ExpiryYear, now))
            return BadRequest(new { message = "That expiry date has already passed." });

        if (await _cards.CountAsync(userId, ct) >= MaxPerUser)
            return BadRequest(new { message = $"You can save at most {MaxPerUser} cards." });

        var brand = CardNumber.Brand(request.Number);
        var last4 = CardNumber.Last4(request.Number);

        if (await _cards.ExistsAsync(userId, brand, last4, request.ExpiryMonth, request.ExpiryYear, ct))
            return Conflict(new { message = $"{brand} ···· {last4} is already saved." });

        var card = new SavedCard
        {
            Id             = Guid.NewGuid(),
            UserId         = userId,
            Brand          = brand,
            Last4          = last4,
            CardholderName = request.CardholderName.Trim(),
            ExpiryMonth    = request.ExpiryMonth,
            ExpiryYear     = request.ExpiryYear,
            // Pehla card apne aap default
            IsDefault      = request.IsDefault || await _cards.CountAsync(userId, ct) == 0
        };

        await _cards.AddAsync(card, ct);

        if (card.IsDefault)
            await _cards.ClearDefaultAsync(userId, card.Id, ct);

        await _cards.SaveChangesAsync(ct);

        // request.Number yahin scope se bahar chala jaata hai — kahin likha nahi gaya
        return CreatedAtAction(nameof(GetMine), null, Shape(card, now));
    }

    // POST /api/cards/{id}/default
    [HttpPost("{id:guid}/default")]
    public async Task<IActionResult> MakeDefault(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var card = await _cards.GetAsync(id, userId, ct);
        if (card is null) return NotFound();

        card.IsDefault = true;
        await _cards.ClearDefaultAsync(userId, card.Id, ct);
        await _cards.SaveChangesAsync(ct);

        return NoContent();
    }

    // DELETE /api/cards/{id} — soft delete, payment rows ka last4 bacha rehta hai
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var card = await _cards.GetAsync(id, userId, ct);
        if (card is null) return NotFound();

        card.IsActive  = false;
        card.IsDefault = false;
        await _cards.SaveChangesAsync(ct);

        return NoContent();
    }

    private static object Shape(SavedCard c, DateTime nowUtc) => new
    {
        c.Id,
        c.Brand,
        c.Last4,
        c.CardholderName,
        c.ExpiryMonth,
        c.ExpiryYear,
        c.IsDefault,
        c.CreatedAt,
        Display = c.Display(),
        Expired = c.HasExpired(nowUtc)
    };
}
