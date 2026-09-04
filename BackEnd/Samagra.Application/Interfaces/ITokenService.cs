namespace Samagra.Application.Interfaces;

public interface ITokenService
{
    string CreateAccessToken(Guid userId, string email, Guid buyerId, IEnumerable<string> roles);
}