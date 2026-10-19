using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Samagra.Application.Interfaces;
using Samagra.Infrastructure.Identity;
using Samagra.API.Contracts;
using Samagra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Samagra.Domain.Entities;
namespace Samagra.API.Controllers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/auth")]
public class LoginController : ControllerBase
{
    private const string AccessTokenCookie = "access_token";

    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;
    private readonly AppDbContext _dbcontext;

    private readonly IBuyerRepository _buyers;
    public LoginController(UserManager<ApplicationUser> user,
    IBuyerRepository buyers, ITokenService tokens, AppDbContext dbcontext)
    {
        _users = user;
        _tokens = tokens;
        _buyers = buyers;
        _dbcontext = dbcontext;
    }

    // Cookie ko delete karne ke liye options login ke jaise hi hone chahiye.
    private static CookieOptions TokenCookieOptions(DateTimeOffset? expires = null) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Expires = expires
    };

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { error = "Invalid credentials." });

        var roles = await _users.GetRolesAsync(user);
        var token = _tokens.CreateAccessToken(user.Id, user.Email!, user.BuyerId, roles);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(60);
        Response.Cookies.Append(AccessTokenCookie, token, TokenCookieOptions(DateTime.UtcNow.AddHours(1)));
        return Ok(new
        {
            email = user.Email,
            roles,
            expiresAt
        });
    }

    // GET /api/auth/me — SPA reload ke baad session wapas padhne ke liye.
    // Cookie HttpOnly hai, to frontend khud token nahi padh sakta.
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");
        if (userId is null) return Unauthorized();

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var roles = await _users.GetRolesAsync(user);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            fullName = user.FullName,
            buyerId = user.BuyerId,
            roles
        });
    }

    // POST /api/auth/logout — cookie HttpOnly hai, isliye server hi hata sakta hai.
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AccessTokenCookie, TokenCookieOptions());
        return NoContent();
    }

  [Authorize]
[HttpPost("mcp-token")]
public async Task<IActionResult> CreateMcpToken()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");
    var user = await _users.FindByIdAsync(userId!);
    if (user is null) return Unauthorized();

    var roles = await _users.GetRolesAsync(user);
    var token = _tokens.CreateAccessToken(user.Id, user.Email!, user.BuyerId, roles);

    return Ok(new { token });
}

}

public record LoginRequest(string Email, string Password);
