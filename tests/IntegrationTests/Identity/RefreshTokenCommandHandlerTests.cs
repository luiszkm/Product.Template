using CommonTests.Builders;
using IntegrationTests.Common;
using Kernel.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Core.Identity.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

namespace IntegrationTests.Identity;

public class RefreshTokenCommandHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();
    private readonly StubHttpContextAccessor _httpContextAccessor = new();

    private RefreshTokenCommandHandler CreateHandler() => new(
        new RefreshTokenRepository(_fixture.DbContext),
        _fixture.UserRepository(),
        CreateJwtTokenService(),
        _fixture.UnitOfWork(),
        _httpContextAccessor,
        _fixture.TenantContext,
        new StubUserRolesProvider(),
        NullLogger<RefreshTokenCommandHandler>.Instance);

    private static JwtTokenService CreateJwtTokenService() => new(
        Options.Create(new JwtSettings
        {
            Secret = "integration-test-secret-at-least-32-chars-long",
            Issuer = "test",
            Audience = "test",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 30
        }));

    [Fact]
    public async Task Handle_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        var user = await _fixture.SeedUserAsync(
            new UserBuilder().WithEmail("refresh-valid@test.com").WithConfirmedEmail().Build());
        const string rawToken = "valid-refresh-token";
        var refreshToken = RefreshToken.Create(WellKnownTenants.Public, user.Id, rawToken, 30, "127.0.0.1");
        await _fixture.DbContext.RefreshTokens.AddAsync(refreshToken);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await CreateHandler().Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresIn > 0);
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.NotEqual(rawToken, result.RefreshToken);
        Assert.Equal(user.Id, result.User.Id);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenRefreshTokenIsRevoked()
    {
        var user = await _fixture.SeedUserAsync();
        const string rawToken = "revoked-refresh-token";
        var refreshToken = RefreshToken.Create(WellKnownTenants.Public, user.Id, rawToken, 30, "127.0.0.1");
        refreshToken.Revoke("127.0.0.1");
        await _fixture.DbContext.RefreshTokens.AddAsync(refreshToken);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler().Handle(new RefreshTokenCommand(rawToken), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenRefreshTokenBelongsToAnotherTenant()
    {
        var user = await _fixture.SeedUserAsync();
        const string rawToken = "other-tenant-refresh-token";
        var refreshToken = RefreshToken.Create(WellKnownTenants.Public, user.Id, rawToken, 30, "127.0.0.1");
        await _fixture.DbContext.RefreshTokens.AddAsync(refreshToken);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        _fixture.TenantContext.SetTenant(new TenantConfig
        {
            TenantId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            TenantKey = "other",
            IsolationMode = TenantIsolationMode.SharedDb,
            IsActive = true
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler().Handle(new RefreshTokenCommand(rawToken), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}
