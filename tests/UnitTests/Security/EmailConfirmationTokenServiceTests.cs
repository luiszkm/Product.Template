using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Product.Template.Core.Identity.Infrastructure.Security;

namespace UnitTests.Security;

public class EmailConfirmationTokenServiceTests
{
    private static EmailConfirmationTokenService CreateService(string secret = "super-secret-key-for-unit-tests-only") =>
        new(Options.Create(new JwtSettings { Secret = secret }));

    [Fact]
    public void Constructor_ShouldThrow_WhenSecretIsMissing()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new EmailConfirmationTokenService(Options.Create(new JwtSettings { Secret = "" })));
    }

    [Fact]
    public void ValidateToken_ShouldReturnTrue_ForTokenGeneratedWithSameUserAndStamp()
    {
        var sut = CreateService();
        var userId = Guid.NewGuid();
        var token = sut.GenerateToken(userId, "stamp-1");

        Assert.True(sut.ValidateToken(userId, "stamp-1", token));
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_WhenSecurityStampChanged()
    {
        var sut = CreateService();
        var userId = Guid.NewGuid();
        var token = sut.GenerateToken(userId, "stamp-1");

        Assert.False(sut.ValidateToken(userId, "stamp-2", token));
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_WhenUserIdDiffers()
    {
        var sut = CreateService();
        var token = sut.GenerateToken(Guid.NewGuid(), "stamp-1");

        Assert.False(sut.ValidateToken(Guid.NewGuid(), "stamp-1", token));
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_WhenTokenIsEmpty()
    {
        var sut = CreateService();

        Assert.False(sut.ValidateToken(Guid.NewGuid(), "stamp-1", ""));
    }

    [Fact]
    public void GenerateToken_ShouldBeStable_ForSameInputs()
    {
        var sut = CreateService();
        var userId = Guid.NewGuid();

        var token1 = sut.GenerateToken(userId, "stamp-1");
        var token2 = sut.GenerateToken(userId, "stamp-1");

        Assert.Equal(token1, token2);
    }

    [Fact]
    public void ValidateToken_ShouldReturnFalse_WhenSecretDiffers()
    {
        var userId = Guid.NewGuid();
        var token = CreateService("secret-one-secret-one-secret-one").GenerateToken(userId, "stamp-1");

        var sut = CreateService("secret-two-secret-two-secret-two");

        Assert.False(sut.ValidateToken(userId, "stamp-1", token));
    }
}
