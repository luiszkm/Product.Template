using CommonTests.Builders;
using IntegrationTests.Common;
using Kernel.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Application.Exceptions;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace IntegrationTests.Identity;

public class LoginCommandHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();
    private readonly StubHttpContextAccessor _httpContextAccessor = new();

    private LoginCommandHandler CreateHandler() => new(
        _fixture.UserRepository(),
        new RefreshTokenRepository(_fixture.DbContext),
        _fixture.HashServices,
        CreateJwtTokenService(),
        _fixture.UnitOfWork(),
        _httpContextAccessor,
        _fixture.TenantContext,
        new StubUserRolesProvider(),
        NullLogger<LoginCommandHandler>.Instance);

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
    public async Task Handle_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        const string password = "Pass@123";
        var user = new UserBuilder()
            .WithEmail("login-success@test.com")
            .WithPasswordHash(_fixture.HashServices.GeneratePasswordHash(password))
            .WithConfirmedEmail()
            .Build();
        await _fixture.SeedUserAsync(user);

        var result = await CreateHandler().Handle(
            new LoginCommand("login-success@test.com", password),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresIn > 0);
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Equal(user.Id, result.User.Id);
        Assert.Equal("login-success@test.com", result.User.Email);

        var updated = await _fixture.UserRepository().GetByIdAsync(user.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated.LastLoginAt);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenPasswordIsWrong()
    {
        var user = new UserBuilder()
            .WithEmail("wrong-password@test.com")
            .WithPasswordHash(_fixture.HashServices.GeneratePasswordHash("Correct@123"))
            .WithConfirmedEmail()
            .Build();
        await _fixture.SeedUserAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler().Handle(new LoginCommand("wrong-password@test.com", "Wrong@123"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsInactive()
    {
        const string password = "Pass@123";
        var user = new UserBuilder()
            .WithEmail("inactive@test.com")
            .WithPasswordHash(_fixture.HashServices.GeneratePasswordHash(password))
            .WithConfirmedEmail()
            .Build();
        user.Deactivate();
        await _fixture.SeedUserAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler().Handle(new LoginCommand("inactive@test.com", password), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowBusinessRuleException_WhenEmailIsNotConfirmed()
    {
        const string password = "Pass@123";
        var user = new UserBuilder()
            .WithEmail("unconfirmed@test.com")
            .WithPasswordHash(_fixture.HashServices.GeneratePasswordHash(password))
            .Build();
        await _fixture.SeedUserAsync(user);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateHandler().Handle(new LoginCommand("unconfirmed@test.com", password), CancellationToken.None));
    }

    public void Dispose() => _fixture.Dispose();
}

internal sealed class StubHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; } = new DefaultHttpContext();
}

internal sealed class StubUserRolesProvider : IUserRolesProvider
{
    private readonly UserRolesData _data;

    public StubUserRolesProvider(IReadOnlyList<string>? roles = null, IReadOnlyList<string>? permissions = null)
    {
        _data = new UserRolesData(roles ?? [], permissions ?? []);
    }

    public Task<UserRolesData> GetUserRolesAndPermissionsAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult(_data);
}
