using System.Net;
using System.Net.Http.Json;
using E2ETests.Common;
using E2ETests.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Identity.Application.Handlers.Auth;
using Product.Template.Core.Identity.Application.Security;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace E2ETests.Identity;

[Collection(RbacE2ECollection.Name)]
public class IdentityAuthE2ETests
{
    private readonly HttpClient _client;
    private readonly RbacWebApplicationFactory _factory;

    public IdentityAuthE2ETests(RbacWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task RegisterAndLogin_ShouldSucceed_WhenEmailIsConfirmed()
    {
        var email = $"auth-{Guid.NewGuid():N}@e2e.test";
        const string password = "Pass@123";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email,
            password,
            firstName = "Auth",
            lastName = "User"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisteredUserResponse>();
        Assert.NotNull(registered);

        await ConfirmEmailAsync(registered.Id);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Equal(email, auth.User.Email);
    }

    [Fact]
    public async Task Refresh_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        var email = $"refresh-{Guid.NewGuid():N}@e2e.test";
        const string password = "Pass@123";

        var registered = await RegisterUserAsync(email, password);
        await ConfirmEmailAsync(registered.Id);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(loginAuth);

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/identity/refresh", new
        {
            refreshToken = loginAuth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshed.RefreshToken));
        Assert.NotEqual(loginAuth.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(email, refreshed.User.Email);
    }

    private async Task<RegisteredUserResponse> RegisterUserAsync(string email, string password)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/identity/register", new
        {
            email,
            password,
            firstName = "Auth",
            lastName = "User"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisteredUserResponse>();
        Assert.NotNull(registered);
        return registered;
    }

    private async Task ConfirmEmailAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(new TenantConfig
        {
            TenantId = WellKnownTenants.Public,
            TenantKey = "public",
            IsolationMode = TenantIsolationMode.SharedDb,
            IsActive = true
        });

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        var tokenService = scope.ServiceProvider.GetRequiredService<IEmailConfirmationTokenService>();
        var token = tokenService.GenerateToken(user.Id, user.SecurityStamp);

        var response = await _client.PostAsJsonAsync($"/api/v1/identity/{userId}/confirm-email", new { token });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private sealed record RegisteredUserResponse(Guid Id, string Email);

    private sealed record AuthTokenResponse(
        string AccessToken,
        string TokenType,
        int ExpiresIn,
        string RefreshToken,
        UserAuthResponse User);

    private sealed record UserAuthResponse(
        Guid Id,
        string Email,
        string FirstName,
        DateTime? LastLoginAt,
        IEnumerable<string> Roles);
}
