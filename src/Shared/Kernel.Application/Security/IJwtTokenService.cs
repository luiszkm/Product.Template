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

    /// <summary>
    /// Generates a cryptographically secure opaque refresh token string.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Number of days the refresh token remains valid (from configuration).
    /// </summary>
    int GetRefreshTokenExpirationDays();
}
