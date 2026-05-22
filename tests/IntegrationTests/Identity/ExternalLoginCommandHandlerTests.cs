using CommonTests.Builders;
using IntegrationTests.Common;
using Kernel.Application.Security;
using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Identity.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Application.Security;

namespace IntegrationTests.Identity;

public class ExternalLoginCommandHandlerTests : IDisposable
{
    private readonly HandlerTestFixture _fixture = new();

    private ExternalLoginCommandHandler CreateHandler(
        IAuthenticationProviderFactory providerFactory,
        IConfiguration? configuration = null) => new(
        providerFactory,
        _fixture.UserRepository(),
        new RefreshTokenRepository(_fixture.DbContext),
        CreateJwtTokenService(),
        _fixture.UnitOfWork(),
        _fixture.TenantContext,
        new StubUserRolesProvider(["Viewer"], ["identity.user.read"]),
        configuration ?? CreateConfiguration(),
        NullLogger<ExternalLoginCommandHandler>.Instance);

    private static JwtTokenService CreateJwtTokenService() => new(
        Options.Create(new JwtSettings
        {
            Secret = "integration-test-secret-at-least-32-chars-long",
            Issuer = "test",
            Audience = "test",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 30
        }));

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:AllowExternalLoginAutoProvision"] = "true",
                ["Identity:ConfirmEmailOnExternalProvision"] = "false"
            })
            .Build();

    [Fact]
    public async Task Handle_ShouldReturnTokens_WhenExistingUserAuthenticatesViaExternalProvider()
    {
        const string email = "external-login@test.com";
        await _fixture.SeedUserAsync(
            new UserBuilder().WithEmail(email).WithConfirmedEmail().Build());

        var provider = new StubAuthenticationProvider(
            "stub",
            new AuthenticationResult(
                Success: true,
                UserInfo: new Dictionary<string, string>
                {
                    ["email"] = email,
                    ["firstName"] = "External",
                    ["lastName"] = "User"
                }));

        var handler = CreateHandler(new StubAuthenticationProviderFactory(provider));

        var result = await handler.Handle(
            new ExternalLoginCommand("stub", "auth-code"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresIn > 0);
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.Equal(email, result.User.Email);
        Assert.Contains("Viewer", result.User.Roles);
    }

    public void Dispose() => _fixture.Dispose();
}

internal sealed class StubAuthenticationProviderFactory : IAuthenticationProviderFactory
{
    private readonly IAuthenticationProvider? _provider;

    public StubAuthenticationProviderFactory(IAuthenticationProvider? provider = null) =>
        _provider = provider;

    public IAuthenticationProvider GetProvider(string providerName) =>
        _provider ?? throw new InvalidOperationException($"Provider '{providerName}' is not configured.");

    public IEnumerable<string> GetAvailableProviders() =>
        _provider is null ? [] : [_provider.ProviderName];

    public bool IsProviderAvailable(string providerName) =>
        _provider?.ProviderName == providerName;
}

internal sealed class StubAuthenticationProvider : IAuthenticationProvider
{
    private readonly AuthenticationResult _result;

    public StubAuthenticationProvider(string providerName, AuthenticationResult result)
    {
        ProviderName = providerName;
        _result = result;
    }

    public string ProviderName { get; }

    public Task<AuthenticationResult> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);

    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
