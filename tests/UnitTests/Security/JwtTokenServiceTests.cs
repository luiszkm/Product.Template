using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace UnitTests.Security;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(JwtSettings? settings = null) =>
        new(Options.Create(settings ?? new JwtSettings
        {
            Secret = "super-secret-key-used-only-for-unit-tests-1234567890",
            Issuer = "product-template",
            Audience = "product-template-clients",
            ExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        }));

    [Fact]
    public void Constructor_ShouldThrow_WhenSecretIsMissing()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new JwtTokenService(Options.Create(new JwtSettings { Secret = "" })));
    }

    [Fact]
    public void CreateAccessToken_ShouldProduceNonEmptyToken()
    {
        var sut = CreateService();

        var token = sut.CreateAccessToken(Guid.NewGuid(), "user@acme.com", new[] { "Admin" });

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void CreateAccessToken_ShouldEncodeClaims_ForUserIdEmailAndRoles()
    {
        var sut = CreateService();
        var userId = Guid.NewGuid();

        var token = sut.CreateAccessToken(userId, "user@acme.com", new[] { "Admin", "Viewer" });
        var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);

        Assert.Equal(userId.ToString(), jwt.GetClaim("sub").Value);
        Assert.Equal("user@acme.com", jwt.GetClaim("email").Value);
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Viewer");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueValues()
    {
        var sut = CreateService();

        var token1 = sut.GenerateRefreshToken();
        var token2 = sut.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
        Assert.True(Convert.FromBase64String(token1).Length == 64);
    }

    [Fact]
    public void GetExpiresInSeconds_ShouldConvertMinutesToSeconds()
    {
        var sut = CreateService();

        Assert.Equal(1800, sut.GetExpiresInSeconds());
    }

    [Fact]
    public void GetRefreshTokenExpirationDays_ShouldReturnConfiguredValue()
    {
        var sut = CreateService();

        Assert.Equal(7, sut.GetRefreshTokenExpirationDays());
    }
}
