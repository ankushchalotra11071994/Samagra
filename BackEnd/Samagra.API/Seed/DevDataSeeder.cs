// Samagra.API/Seed/DevDataSeeder.cs

using Microsoft.AspNetCore.Identity;
using Samagra.Application.Interfaces;
using Samagra.Domain.Entities;
using Samagra.Infrastructure.Identity;

namespace Samagra.API.Seed;

public static class DevDataSeeder
{
    private sealed record SeedUser(
        string Email,
        string UserName,
        string Password,
        string Role,
        string FullName,
        string? BusinessName);

    // Local test logins. Passwords yahan jaan boojh kar kamzor hain —
    // SeedDevUsersAsync sirf Development mein chalta hai.
    private static readonly SeedUser[] DevUsers =
    [
        new("admin@samagra.dev", "admin", "admin", Roles.Admin, "Dev Admin", null),
        new("admin@admin.com", "admin@admin.com", "admin", Roles.Admin, "Test Admin", null),
        new("buyer@buyer.com", "buyer@buyer.com", "buyer", Roles.Buyer, "Test Buyer", "Buyer Test Co"),
    ];

    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in new[] { Roles.Buyer, Roles.Admin })
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new ApplicationRole(role));
        }
    }

    public static async Task SeedDevUsersAsync(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var hasher = services.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var buyers = services.GetRequiredService<IBuyerRepository>();

        foreach (var seed in DevUsers)
        {
            if (await users.FindByEmailAsync(seed.Email) is not null) continue;

            var buyerId = Guid.Empty;

            // Buyer accounts ko apna Buyer row milta hai, jaise /api/auth/register mein.
            // Register ise inactive chhodta hai; test account ready dikhna chahiye, to activate kar dete hain.
            if (seed.BusinessName is not null)
            {
                var buyer = new Buyer(seed.BusinessName, priceTier: "Standard");
                buyer.Activate();
                await buyers.AddAsync(buyer);
                await buyers.SaveChangesAsync();
                buyerId = buyer.Id;
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = seed.UserName,
                Email = seed.Email,
                FullName = seed.FullName,
                BuyerId = buyerId,
                EmailConfirmed = true
            };

            // "admin" / "buyer" Identity ki password policy (digit, uppercase, symbol)
            // pass nahi karte, isliye CreateAsync(user, password) ki jagah hash seedha set karte hain.
            user.PasswordHash = hasher.HashPassword(user, seed.Password);

            var created = await users.CreateAsync(user);
            if (!created.Succeeded)
                throw new InvalidOperationException(
                    $"Dev user seed failed for {seed.Email}: " +
                    string.Join("; ", created.Errors.Select(e => e.Description)));

            await users.AddToRoleAsync(user, seed.Role);
        }
    }
}
