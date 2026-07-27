using Kernel.Infrastructure.Security;

namespace UnitTests.Security;

public class HashServicesTests
{
    private readonly HashServices _sut = new();

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_ForCorrectPassword()
    {
        var hash = _sut.GeneratePasswordHash("Str0ng-Passw0rd!");

        Assert.True(_sut.VerifyPassword("Str0ng-Passw0rd!", hash));
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_ForWrongPassword()
    {
        var hash = _sut.GeneratePasswordHash("Str0ng-Passw0rd!");

        Assert.False(_sut.VerifyPassword("wrong-password", hash));
    }

    [Fact]
    public void GeneratePasswordHash_ShouldProduceDifferentHashes_ForSamePasswordDueToRandomSalt()
    {
        var hash1 = _sut.GeneratePasswordHash("same-password");
        var hash2 = _sut.GeneratePasswordHash("same-password");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenHashFormatIsInvalid()
    {
        Assert.False(_sut.VerifyPassword("any-password", "not-a-valid-hash"));
    }
}
