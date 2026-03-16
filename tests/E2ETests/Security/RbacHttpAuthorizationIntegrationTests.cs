using System.Net.Http.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

namespace E2ETests.Security;

public class RbacHttpAuthorizationIntegrationTests : IClassFixture<RbacWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RbacHttpAuthorizationIntegrationTests(RbacWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task ListUsers_ShouldReturn401_WhenNoTokenIsProvided()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListUsers_ShouldReturn403_WhenUserHasNoUsersReadPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListUsers_ShouldReturn200_WhenUserHasUsersReadPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", "users.read");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }



    [Fact]
    public async Task ListRoles_ShouldReturn403_WhenUserHasNoUsersReadPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/roles");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListRoles_ShouldReturn200_WhenUserHasUsersReadPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/roles");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", "users.read");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateRole_ShouldReturn403_WhenUserHasNoUsersManagePermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/roles");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Content = JsonContent.Create(new { name = "Auditor", description = "Auditor role" });

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturn403_WhenUserHasNoUsersManagePermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/identity/{Guid.NewGuid()}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturn404_WhenUserHasUsersManagePermissionAndTargetDoesNotExist()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/identity/{Guid.NewGuid()}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", "users.manage");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserRoles_ShouldReturn403_WhenUserHasNoUsersManagePermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{Guid.NewGuid()}/roles");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserRoles_ShouldReturn404_WhenUserHasUsersManagePermissionAndTargetDoesNotExist()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{Guid.NewGuid()}/roles");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", "users.manage");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
    [Fact]
    public async Task GetById_ShouldReturn403_WhenUserIsNotOwnerAndNotAdmin()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{ownerId}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");
        request.Headers.Add("X-Test-UserId", callerId.ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturn200_WhenUserIsOwner()
    {
        var userId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/identity/{userId}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");
        request.Headers.Add("X-Test-UserId", userId.ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}

public class RbacWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("DisableDatabaseInitialization", "true");
        builder.UseSetting("DisableTenantMiddleware", "true");
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
                ["DisableTenantMiddleware"] = "true",
                ["ConnectionStrings:HostDb"] = "InMemory",
                ["ConnectionStrings:AppDb"] = "InMemory"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Swap the real SQL Server DbContexts for in-memory providers to avoid external dependencies during E2E auth tests.
            services.RemoveAll(typeof(AppDbContext));
            services.RemoveAll(typeof(HostDbContext));
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(DbContextOptions<HostDbContext>));

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("E2E_Rbac_App"));
            services.AddDbContext<HostDbContext>(options => options.UseInMemoryDatabase("E2E_Rbac_Host"));

            // Re-wire minimal multi-tenancy services required by middleware during tests.
            services.Configure<TenantResolutionOptions>(_ => { });
            services.RemoveAll(typeof(ITenantResolver));
            services.RemoveAll(typeof(ITenantStore));
            services.RemoveAll(typeof(ITenantContext));

            services.AddScoped<ITenantContext, TenantContext>();
            services.AddSingleton<ITenantResolver, TestTenantResolver>();
            services.AddSingleton<ITenantStore, TestTenantStore>();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                    options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            });
        });
    }
}

internal sealed class TestTenantResolver : ITenantResolver
{
    public string? ResolveTenantKey(HttpContext httpContext)
    {
        // Honor test header, otherwise fall back to default tenant.
        if (httpContext.Request.Headers.TryGetValue("X-Tenant", out var header) && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return "public";
    }
}

internal sealed class TestTenantStore : ITenantStore
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


