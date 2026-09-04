namespace Samagra.API.Contracts;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string BusinessName);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt);