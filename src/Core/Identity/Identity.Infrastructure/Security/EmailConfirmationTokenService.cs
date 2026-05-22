using System.Security.Cryptography;
using System.Text;
using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Product.Template.Core.Identity.Application.Security;

namespace Product.Template.Core.Identity.Infrastructure.Security;

public sealed class EmailConfirmationTokenService : IEmailConfirmationTokenService
{
    private readonly byte[] _secretKey;

    public EmailConfirmationTokenService(IOptions<JwtSettings> options)
    {
        var secret = options.Value.Secret;
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Jwt:Secret is required for email confirmation tokens.");

        _secretKey = Encoding.UTF8.GetBytes(secret);
    }

    public string GenerateToken(Guid userId, string securityStamp)
    {
        var payload = $"{userId:N}:{securityStamp}";
        var hash = HMACSHA256.HashData(_secretKey, Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    public bool ValidateToken(Guid userId, string securityStamp, string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var expected = GenerateToken(userId, securityStamp);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var tokenBytes = Encoding.UTF8.GetBytes(token);

        return expectedBytes.Length == tokenBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, tokenBytes);
    }
}
