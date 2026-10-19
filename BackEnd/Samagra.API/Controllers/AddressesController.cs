// Samagra.API/Controllers/AddressesController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Samagra.API.Contracts;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressesController : ControllerBase
{
    private const int MaxPerUser = 20;

    private readonly IAddressRepository _addresses;

    public AddressesController(IAddressRepository addresses) => _addresses = addresses;

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");

    // GET /api/addresses — sirf apne pate
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var list = await _addresses.ListAsync(userId, ct);
        return Ok(list.Select(Shape));
    }

    // GET /api/addresses/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var address = await _addresses.GetAsync(id, userId, ct);
        return address is null ? NotFound() : Ok(Shape(address));
    }

    // POST /api/addresses
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveAddressRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        if (await _addresses.CountAsync(userId, ct) >= MaxPerUser)
            return BadRequest(new { message = $"You can save at most {MaxPerUser} addresses." });

        var address = new Address
        {
            Id         = Guid.NewGuid(),
            UserId     = userId,
            Label      = request.Label.Trim(),
            FullName   = request.FullName.Trim(),
            Phone      = request.Phone.Trim(),
            Line1      = request.Line1.Trim(),
            Line2      = string.IsNullOrWhiteSpace(request.Line2) ? null : request.Line2.Trim(),
            City       = request.City.Trim(),
            State      = request.State.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Country    = request.Country.Trim(),
            // Pehla pata apne aap default ban jaata hai
            IsDefault  = request.IsDefault || await _addresses.CountAsync(userId, ct) == 0
        };

        await _addresses.AddAsync(address, ct);

        if (address.IsDefault)
            await _addresses.ClearDefaultAsync(userId, address.Id, ct);

        await _addresses.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = address.Id }, Shape(address));
    }

    // PUT /api/addresses/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveAddressRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var address = await _addresses.GetAsync(id, userId, ct);
        if (address is null) return NotFound();

        address.Label      = request.Label.Trim();
        address.FullName   = request.FullName.Trim();
        address.Phone      = request.Phone.Trim();
        address.Line1      = request.Line1.Trim();
        address.Line2      = string.IsNullOrWhiteSpace(request.Line2) ? null : request.Line2.Trim();
        address.City       = request.City.Trim();
        address.State      = request.State.Trim();
        address.PostalCode = request.PostalCode.Trim();
        address.Country    = request.Country.Trim();

        if (request.IsDefault)
        {
            address.IsDefault = true;
            await _addresses.ClearDefaultAsync(userId, address.Id, ct);
        }

        await _addresses.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST /api/addresses/{id}/default
    [HttpPost("{id:guid}/default")]
    public async Task<IActionResult> MakeDefault(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var address = await _addresses.GetAsync(id, userId, ct);
        if (address is null) return NotFound();

        address.IsDefault = true;
        await _addresses.ClearDefaultAsync(userId, address.Id, ct);
        await _addresses.SaveChangesAsync(ct);

        return NoContent();
    }

    // DELETE /api/addresses/{id} — soft delete, purane orders ka snapshot bacha rehta hai
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (userId is null) return Unauthorized();

        var address = await _addresses.GetAsync(id, userId, ct);
        if (address is null) return NotFound();

        address.IsActive  = false;
        address.IsDefault = false;
        await _addresses.SaveChangesAsync(ct);

        return NoContent();
    }

    private static object Shape(Address a) => new
    {
        a.Id,
        a.Label,
        a.FullName,
        a.Phone,
        a.Line1,
        a.Line2,
        a.City,
        a.State,
        a.PostalCode,
        a.Country,
        a.IsDefault,
        a.CreatedAt,
        OneLine = a.OneLine()
    };
}
