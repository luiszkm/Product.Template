using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Product.Template.Core.Identity.Domain.Entities;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;
using E2ETests.Security;

namespace E2ETests.Common;

/// <summary>
/// WebApplicationFactory that runs the full application stack against a real PostgreSQL
/// container (via <see cref="PostgresContainerFixture"/>).
/// Migrations and seeders are executed during <see cref="InitializeAsync"/>.
/// </summary>
public sealed class TestContainerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly Guid SeededOwnerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly PostgresContainerFixture _sql;

    public TestContainerWebApplicationFactory(PostgresContainerFixture sql)
    {
        _sql = sql;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseDefaultServiceProvider((_, options) =>
        {
            options.ValidateScopes = false;
            options.ValidateOnBuild = false;
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile("appsettings.Test.json", optional: false);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DisableDatabaseInitialization"] = "false",
                ["Jwt:Enabled"] = "false",
                ["ConnectionStrings:HostDb"] = _sql.HostDbConnectionString,
                ["ConnectionStrings:AppDb"] = _sql.AppDbConnectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(ITenantResolver));
            services.RemoveAll(typeof(ITenantStore));

            services.AddSingleton<ITenantResolver, ContainerTestTenantResolver>();
            services.AddSingleton<ITenantStore, ContainerTestTenantStore>();

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
        });
    }

    public async Task InitializeAsync()
    {
        // Force server creation → runs Program.cs startup → triggers InitializeDatabaseAsync
        // (migrations + seeders) because DisableDatabaseInitialization = false.
        _ = Server;

        // Seed the fixed E2E owner user used by tests that rely on a known user ID.
        using var scope = Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(new TenantConfig
        {
            TenantId = 1,
            TenantKey = "public",
            IsolationMode = TenantIsolationMode.SharedDb,
            IsActive = true
        });

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == SeededOwnerId))
        {
            var owner = User.Create(1L, "owner@e2e.test", "dummyhash", "E2E", "Owner");
            typeof(User).BaseType!.GetProperty("Id")!.SetValue(owner, SeededOwnerId);
            db.Set<User>().Add(owner);
            await db.SaveChangesAsync();
        }
    }

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}

internal sealed class ContainerTestTenantResolver : ITenantResolver
{
    public string? ResolveTenantKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Tenant", out var header) &&
            !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return "public";
    }
}

internal sealed class ContainerTestTenantStore : ITenantStore
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
