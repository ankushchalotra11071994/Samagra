using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Samagra.Application.Interfaces;
using Samagra.Infrastructure.Identity;
using Samagra.API.Contracts;
namespace Samagra.API.Controllers;

[ApiController]
[Route("api/auth")]
public class LoginController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;

    private readonly IBuyerRepository _buyers;
    public LoginController(UserManager<ApplicationUser> user,
    IBuyerRepository buyers, ITokenService tokens)
    {
        _users = user;
        _tokens = tokens;
        _buyers = buyers;
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);

        if (user is null || !await _users.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { error = "Invalid credentials." });

        var roles = await _users.GetRolesAsync(user);
        var token = _tokens.CreateAccessToken(user.Id, user.Email!, user.BuyerId, roles);
        return Ok(new AuthResponse(token, DateTimeOffset.UtcNow.AddMinutes(60)));
    }
}

public record LoginRequest(string Email, string Password);