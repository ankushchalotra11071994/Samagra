using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Samagra.Application.Interfaces;
using Samagra.Infrastructure.Identity;
using Samagra.API.Contracts;
using Samagra.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Samagra.Domain.Entities;
namespace Samagra.API.Controllers;

[ApiController]
[Route("api/auth")]
public class LoginController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITokenService _tokens;
    private readonly AppDbContext _dbcontext;

    private readonly IBuyerRepository _buyers;
    public LoginController(UserManager<ApplicationUser> user,
    IBuyerRepository buyers, ITokenService tokens,AppDbContext dbcontext)
    {
        _users = user;
        _tokens = tokens;
        _buyers = buyers;
        _dbcontext=dbcontext;
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    { 
        var user = await _users.FindByEmailAsync(request.Email); 
        if (user is null || !await _users.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { error = "Invalid credentials." });

        var roles = await _users.GetRolesAsync(user);
        var token = _tokens.CreateAccessToken(user.Id, user.Email!, user.BuyerId, roles);
        
        Response.Cookies.Append("access_token", token, new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.None,
    Expires = DateTime.UtcNow.AddHours(1)
});
        return Ok(new AuthResponse(token, DateTimeOffset.UtcNow.AddMinutes(60)));
    }

     
}

public record LoginRequest(string Email, string Password);