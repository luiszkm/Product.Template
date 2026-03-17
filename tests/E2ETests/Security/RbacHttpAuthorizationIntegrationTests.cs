using System.Net.Http.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit.Sdk;
using Product.Template.Core.Authorization.Application.Permissions;
using Product.Template.Core.Identity.Application.Permissions;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

namespace E2ETests.Security;

public class RbacHttpAuthorizationIntegrationTests : IClassFixture<RbacWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly RbacWebApplicationFactory _factory;

    public RbacHttpAuthorizationIntegrationTests(RbacWebApplicationFactory factory)
    {
        _factory = factory;
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
        await AssertDefaultSchemeIsTestAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", IdentityPermissions.UserRead);

        var response = await _client.SendAsync(request);

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new XunitException($"Status: {response.StatusCode} Body: {body}");
        }

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    private async Task AssertDefaultSchemeIsTestAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var schemeProvider = scope.ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();

        var defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        Assert.Equal(TestAuthHandler.Scheme, defaultScheme?.Name);
    }



    [Fact]
    public async Task ListRoles_ShouldReturn403_WhenUserHasNoUsersReadPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/authorization/roles");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListRoles_ShouldReturn200_WhenUserHasUsersReadPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/authorization/roles");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", AuthorizationPermissions.RoleRead);

        var response = await _client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateRole_ShouldReturn403_WhenUserHasNoUsersManagePermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/authorization/roles");
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
        request.Headers.Add("X-Test-Permissions", IdentityPermissions.UserManage);

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
        request.Headers.Add("X-Test-Permissions", IdentityPermissions.UserManage);

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
        var userId = RbacWebApplicationFactory.SeededOwnerId;

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
    public static readonly Guid SeededOwnerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("DisableDatabaseInitialization", "true");
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
                ["ConnectionStrings:HostDb"] = "InMemory",
                ["ConnectionStrings:AppDb"] = "InMemory"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Swap the real SQL/SQLite DbContexts for in-memory providers to avoid external dependencies during E2E auth tests.
            RemoveDbContextRegistrations<AppDbContext>(services);
            RemoveDbContextRegistrations<HostDbContext>(services);

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
                    options.DefaultScheme = TestAuthHandler.Scheme;
                    options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                    options.DefaultChallengeScheme = TestAuthHandler.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.Scheme;
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            });

            services.PostConfigure<AuthorizationOptions>(options =>
            {
                var defaultPolicy = new AuthorizationPolicyBuilder(TestAuthHandler.Scheme)
                    .RequireAuthenticatedUser()
                    .Build();

                options.DefaultPolicy = defaultPolicy;
                options.FallbackPolicy = defaultPolicy;
            });

            // Ensure in-memory databases are created and seeded for tests
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

            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetTenant(new TenantConfig
            {
                TenantId = 1,
                TenantKey = "public",
                IsolationMode = TenantIsolationMode.SharedDb,
                IsActive = true
            });

            var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            appDb.Database.EnsureCreated();

            var owner = User.Create(1L, "owner@e2e.test", "dummyhash", "E2E", "Owner");
            typeof(User).BaseType!.GetProperty("Id")!.SetValue(owner, SeededOwnerId);
            appDb.Set<User>().Add(owner);
            appDb.SaveChanges();
        });
    }
    private static void RemoveDbContextRegistrations<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        services.RemoveAll(typeof(TContext));
        services.RemoveAll(typeof(DbContextOptions<TContext>));
        services.RemoveAll(typeof(IDbContextFactory<TContext>));
        services.RemoveAll(typeof(IConfigureOptions<DbContextOptions<TContext>>));
        services.RemoveAll(typeof(IPostConfigureOptions<DbContextOptions<TContext>>));
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

