using System.Security.Claims;

namespace Kernel.Application.Security;

public interface IJwtTokenService
{
    string CreateAccessToken(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        IEnumerable<Claim>? extraClaims = null);

    int GetExpiresInSeconds();
}
