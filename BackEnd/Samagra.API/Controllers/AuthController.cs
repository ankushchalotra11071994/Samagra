using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Samagra.API.Contracts;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Identity;

namespace Samagra.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IBuyerRepository _buyers;
    private readonly ITokenService _tokens;

    public AuthController(
        UserManager<ApplicationUser> users,
        IBuyerRepository buyers,
        ITokenService tokens)
    {
        _users = users;
        _buyers = buyers;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var existing = await _users.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Conflict(new { error = "Email already registered." });

        var buyer = new Buyer(request.BusinessName, priceTier: "Standard");
        await _buyers.AddAsync(buyer, ct);
        await _buyers.SaveChangesAsync(ct);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            BuyerId = buyer.Id
        };

        var result = await _users.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _users.AddToRoleAsync(user, Roles.Buyer);

        return Ok(new { userId = user.Id, buyerId = buyer.Id });
    }
}