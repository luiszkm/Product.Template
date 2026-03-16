using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace E2ETests.Security;

public class TenantGuardIntegrationTests : IClassFixture<TenantGuardWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TenantGuardIntegrationTests(TenantGuardWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturn400_WhenTenantHeaderIsMissing()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity?tenant=public");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", "users.read");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ShouldReturn200_WhenTenantHeaderIsPresent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity");
        request.Headers.TryAddWithoutValidation("Authorization", "Test token");
        request.Headers.TryAddWithoutValidation("X-Test-Roles", "Manager");
        request.Headers.TryAddWithoutValidation("X-Test-Permissions", "users.read");
        request.Headers.TryAddWithoutValidation("X-Tenant", "public");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}

public class TenantGuardWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateScopes = false;
            options.ValidateOnBuild = false;
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["DisableDatabaseInitialization"] = "true",
                ["Jwt:Enabled"] = "false",
                ["ConnectionStrings:HostDb"] = "Filename=tenant_guard_host.db",
                ["ConnectionStrings:AppDb"] = "Filename=tenant_guard_app.db"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(ITenantResolver));
            services.AddSingleton<ITenantResolver, StrictTenantResolver>();

            services.RemoveAll(typeof(ITenantStore));
            services.AddSingleton<ITenantStore, StrictTenantStore>();

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthHandler.Scheme;
                    options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                    options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.PostConfigure<AuthorizationOptions>(options =>
            {
                var defaultPolicy = new AuthorizationPolicyBuilder(TestAuthHandler.Scheme)
                    .RequireAuthenticatedUser()
                    .Build();

                options.DefaultPolicy = defaultPolicy;
                options.FallbackPolicy = defaultPolicy;
            });

            // Seed host/app databases with public tenant
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var hostDb = scope.ServiceProvider.GetRequiredService<HostDbContext>();
            hostDb.Database.EnsureCreated();
            if (!hostDb.Tenants.Any())
            {
                hostDb.Tenants.Add(new TenantConfig
                {
                    TenantId = 1,
                    TenantKey = "public",
                    IsolationMode = TenantIsolationMode.SharedDb,
                    IsActive = true
                });
                hostDb.SaveChanges();
            }

            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            appDb.Database.EnsureCreated();
        });
    }
}

internal sealed class StrictTenantResolver : ITenantResolver
{
    public string? ResolveTenantKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Tenant", out var header) && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        if (httpContext.Request.Query.TryGetValue("tenant", out var tenant) && !string.IsNullOrWhiteSpace(tenant))
        {
            return tenant.ToString();
        }

        return null;
    }
}

internal sealed class StrictTenantStore : ITenantStore
{
    private static readonly TenantConfig PublicTenant = new()
    {
        TenantId = 1,
        TenantKey = "public",
        IsolationMode = TenantIsolationMode.SharedDb,
        IsActive = true
    };

    public Task<TenantConfig?> GetByKeyAsync(string tenantKey, CancellationToken cancellationToken = default)
        => Task.FromResult<TenantConfig?>(PublicTenant);

    public Task<IReadOnlyList<TenantConfig>> ListActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TenantConfig>>(new List<TenantConfig> { PublicTenant });

    public Task UpsertAsync(TenantConfig tenantConfig, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}


