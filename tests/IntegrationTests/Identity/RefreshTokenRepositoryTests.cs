using IntegrationTests.Common;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace IntegrationTests.Identity;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    [Fact]
    public async Task TryRevokeAsync_ShouldReturnFalse_WhenTokenAlreadyRevoked()
    {
        var user = await _fixture.SeedUserAsync();
        var token = RefreshToken.Create(WellKnownTenants.Public, user.Id, "raw-refresh-token", 30, "127.0.0.1");
        await _fixture.DbContext.RefreshTokens.AddAsync(token);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var repository = new RefreshTokenRepository(_fixture.DbContext);

        var firstRevoke = await repository.TryRevokeAsync(
            "raw-refresh-token",
            "127.0.0.1",
            "replacement-token",
            CancellationToken.None);

        var secondRevoke = await repository.TryRevokeAsync(
            "raw-refresh-token",
            "127.0.0.1",
            "another-replacement",
            CancellationToken.None);

        Assert.True(firstRevoke);
        Assert.False(secondRevoke);
    }

    public void Dispose() => _fixture.Dispose();
}
