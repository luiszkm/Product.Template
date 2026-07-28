using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Product.Template.Core.Identity.Application.Security;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace E2ETests.Security;

// Regression coverage for the tenant-binding fix in SecurityConfiguration.OnTokenValidated
// (see commit 290c57c): a JWT is only valid for the tenant it was issued in — presenting it
// under a different tenant (via X-Tenant) must be rejected even though the signature and
// security stamp are otherwise valid. Uses real JWT bearer auth (Jwt:Enabled=true), unlike
// RbacWebApplicationFactory/TestContainerWebApplicationFactory which both disable JWT and
// substitute TestAuthHandler — so this path had no coverage before.
[Collection(JwtTenantBindingE2ECollection.Name)]
public class JwtTenantBindingE2ETests
{
    private readonly HttpClient _client;
    private readonly JwtTenantBindingWebApplicationFactory _factory;

    public JwtTenantBindingE2ETests(JwtTenantBindingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SelfGet_ShouldReturn200_WhenTokenTenantMatchesRequestTenant()
    {
        var (userId, accessToken) = await RegisterAndLoginAsync(TwoTenantStore.TenantAKey);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{userId}");
        request.Headers.Add("X-Tenant", TwoTenantStore.TenantAKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SelfGet_ShouldReturn401_WhenTokenIssuedForDifferentTenant()
    {
        var (userId, accessToken) = await RegisterAndLoginAsync(TwoTenantStore.TenantAKey);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{userId}");
        request.Headers.Add("X-Tenant", TwoTenantStore.TenantBKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(Guid UserId, string AccessToken)> RegisterAndLoginAsync(string tenantKey)
    {
        var email = $"jwt-tenant-{Guid.NewGuid():N}@e2e.test";
        const string password = "Pass@123";

        using var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/register")
        {
            Content = JsonContent.Create(new
            {
                email,
                password,
                firstName = "Jwt",
                lastName = "Tenant"
            })
        };
        registerRequest.Headers.Add("X-Tenant", tenantKey);

        var registerResponse = await _client.SendAsync(registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisteredUserResponse>();
        Assert.NotNull(registered);

        await ConfirmEmailAsync(tenantKey, registered.Id);

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/login")
        {
            Content = JsonContent.Create(new { email, password })
        };
        loginRequest.Headers.Add("X-Tenant", tenantKey);

        var loginResponse = await _client.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        Assert.NotNull(auth);

        return (registered.Id, auth.AccessToken);
    }

    private async Task ConfirmEmailAsync(string tenantKey, Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(TwoTenantStore.ByKey(tenantKey));

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        var tokenService = scope.ServiceProvider.GetRequiredService<IEmailConfirmationTokenService>();
        var token = tokenService.GenerateToken(user.Id, user.SecurityStamp);

        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/identity/{userId}/confirm-email")
        {
            Content = JsonContent.Create(new { token })
        };
        confirmRequest.Headers.Add("X-Tenant", tenantKey);

        var response = await _client.SendAsync(confirmRequest);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private sealed record RegisteredUserResponse(Guid Id, string Email);

    private sealed record AuthTokenResponse(
        string AccessToken,
        string TokenType,
        int ExpiresIn,
        string RefreshToken,
        object User);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class JwtTenantBindingE2ECollection : ICollectionFixture<JwtTenantBindingWebApplicationFactory>
{
    public const string Name = "JwtTenantBindingE2E";
}

public sealed class JwtTenantBindingWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("DisableDatabaseInitialization", "true");
        builder.UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateScopes = false;
            options.ValidateOnBuild = false;
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DisableDatabaseInitialization"] = "true",
                // Deliberately left enabled (default in appsettings.json) — this factory exists
                // specifically to exercise the real JwtBearer pipeline, unlike the other E2E
                // factories which disable it and substitute TestAuthHandler.
                ["Jwt:Enabled"] = "true",
                ["ConnectionStrings:HostDb"] = "InMemory",
                ["ConnectionStrings:AppDb"] = "InMemory"
            });
        });

        builder.ConfigureServices(services =>
        {
            RemoveDbContextRegistrations<AppDbContext>(services);
            RemoveDbContextRegistrations<HostDbContext>(services);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("E2E_JwtTenant_App"));
            services.AddDbContext<HostDbContext>(options => options.UseInMemoryDatabase("E2E_JwtTenant_Host"));

            services.Configure<TenantResolutionOptions>(_ => { });
            services.RemoveAll(typeof(ITenantResolver));
            services.RemoveAll(typeof(ITenantStore));
            services.RemoveAll(typeof(ITenantContext));

            services.AddScoped<ITenantContext, TenantContext>();
            services.AddSingleton<ITenantResolver, TwoTenantResolver>();
            services.AddSingleton<ITenantStore, TwoTenantStore>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<HostDbContext>().Database.EnsureCreated();

        return host;
    }

    private static void RemoveDbContextRegistrations<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        services.RemoveAll(typeof(TContext));
        services.RemoveAll(typeof(DbContextOptions<TContext>));
        services.RemoveAll(typeof(DbContextOptions));
        services.RemoveAll(typeof(IDbContextFactory<TContext>));
        services.RemoveAll(typeof(IDbContextOptionsConfiguration<TContext>));
        services.RemoveAll(typeof(IConfigureOptions<DbContextOptions<TContext>>));
        services.RemoveAll(typeof(IPostConfigureOptions<DbContextOptions<TContext>>));
    }
}

internal sealed class TwoTenantResolver : ITenantResolver
{
    public string? ResolveTenantKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Tenant", out var header) && !string.IsNullOrWhiteSpace(header))
            return header.ToString();

        return TwoTenantStore.TenantAKey;
    }
}

internal sealed class TwoTenantStore : ITenantStore
{
    public const string TenantAKey = "tenant-a";
    public const string TenantBKey = "tenant-b";

    private static readonly Guid TenantAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly TenantConfig TenantA = new()
    {
        TenantId = TenantAId,
        TenantKey = TenantAKey,
        IsolationMode = TenantIsolationMode.SharedDb,
        IsActive = true
    };

    private static readonly TenantConfig TenantB = new()
    {
        TenantId = TenantBId,
        TenantKey = TenantBKey,
        IsolationMode = TenantIsolationMode.SharedDb,
        IsActive = true
    };

    public static TenantConfig ByKey(string tenantKey) =>
        tenantKey == TenantBKey ? TenantB : TenantA;

    public Task<TenantConfig?> GetByKeyAsync(string tenantKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<TenantConfig?>(ByKey(tenantKey));

    public Task<IReadOnlyList<TenantConfig>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TenantConfig>>(new List<TenantConfig> { TenantA, TenantB });

    public Task UpsertAsync(TenantConfig tenantConfig, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
