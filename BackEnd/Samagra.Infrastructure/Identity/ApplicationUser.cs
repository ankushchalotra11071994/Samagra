using Microsoft.AspNetCore.Identity;

namespace Samagra.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid BuyerId { get; set; }
    public string FullName { get; set; } = string.Empty;
}

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }
}

public static class Roles
{
    public const string Buyer = "Buyer";
    public const string Admin = "Admin";
}