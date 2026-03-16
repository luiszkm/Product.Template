using System.Linq;
using System.Collections.Generic;
using Kernel.Application.Security;
using Kernel.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Identity.Infrastructure.Data.Seeders;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Identity.Infrastructure.Data;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenantResolutionOptions>(configuration.GetSection(TenantResolutionOptions.SectionName));
        services.Configure<MultiTenancyOptions>(configuration.GetSection(MultiTenancyOptions.SectionName));
        services.Configure<TenantSeedOptions>(configuration.GetSection(TenantSeedOptions.SectionName));

        services.AddMemoryCache();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantResolver, HeaderAndSubdomainTenantResolver>();
        services.AddScoped<ITenantStore, CachedTenantStore>();
        services.AddScoped<ITenantConnectionStringResolver, TenantConnectionStringResolver>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<MultiTenantSaveChangesInterceptor>();
        services.AddScoped<SearchPathConnectionInterceptor>();
        services.AddScoped<AuditLogInterceptor>();

        var hostConnection = configuration.GetConnectionString("HostDb")
            ?? configuration.GetConnectionString("AppDb")
            ?? "Data Source=host.db";

        services.AddDbContext<HostDbContext>(options =>
        {
            if (LooksLikePostgres(hostConnection))
            {
                options.UseNpgsql(hostConnection);
            }
            else if (LooksLikeSqlServer(hostConnection))
            {
                options.UseSqlServer(hostConnection);
            }
            else
            {
                options.UseSqlite(hostConnection);
            }
        });

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionStringResolver>();
            var tenant = tenantContext.Tenant ?? new TenantConfig { IsolationMode = TenantIsolationMode.SharedDb };
            var appConnection = resolver.ResolveAppConnection(tenant);

            if (LooksLikePostgres(appConnection))
            {
                options.UseNpgsql(appConnection, npgsql =>
                {
                    if (tenant.IsolationMode == TenantIsolationMode.SchemaPerTenant && !string.IsNullOrWhiteSpace(tenant.SchemaName))
                    {
                        npgsql.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
                    }
                });
            }
            else if (LooksLikeSqlServer(appConnection))
            {
                options.UseSqlServer(appConnection, sqlServer =>
                {
                    if (tenant.IsolationMode == TenantIsolationMode.SchemaPerTenant && !string.IsNullOrWhiteSpace(tenant.SchemaName))
                    {
                        sqlServer.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
                    }
                });
            }
            else
            {
                options.UseSqlite(appConnection);
            }

            options.ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<MultiTenantSaveChangesInterceptor>(),
                sp.GetRequiredService<SearchPathConnectionInterceptor>(),
                sp.GetRequiredService<AuditLogInterceptor>());
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        using var scope = scopeFactory.CreateScope();
        var host = scope.ServiceProvider.GetRequiredService<HostDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await host.Database.MigrateAsync();

        var tenantSeeds = configuration.GetSection(TenantSeedOptions.SectionName).Get<TenantSeedOptions>()
                          ?? new TenantSeedOptions();

        if (!tenantSeeds.Any())
        {
            tenantSeeds.Add(new TenantSeedDefinition
            {
                TenantId = 1,
                TenantKey = "public",
                IsolationMode = TenantIsolationMode.SharedDb,
                IsActive = true
            });
        }

        await EnsureTenantsAsync(host, tenantSeeds);

        var activeTenants = await host.Tenants.AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync();

        foreach (var tenant in activeTenants)
        {
            using var tenantScope = scopeFactory.CreateScope();
            var tenantContext = tenantScope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetTenant(tenant);

            var context = tenantScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();

            var hashServices = tenantScope.ServiceProvider.GetRequiredService<IHashServices>();

            await RoleSeeder.SeedAsync(context);
            await PermissionSeeder.SeedAsync(context);
            await UserSeeder.SeedAsync(context, hashServices);
        }
    }

    private static async Task EnsureTenantsAsync(HostDbContext hostDbContext, IEnumerable<TenantSeedDefinition> tenantSeeds)
    {
        var existingTenants = await hostDbContext.Tenants.ToListAsync();
        var nextTenantId = existingTenants.Any() ? existingTenants.Max(x => x.TenantId) + 1 : 1;

        foreach (var seed in tenantSeeds)
        {
            if (string.IsNullOrWhiteSpace(seed.TenantKey))
                continue;

            var normalizedKey = seed.TenantKey.Trim().ToLowerInvariant();
            var tenant = existingTenants.FirstOrDefault(x => x.TenantKey == normalizedKey);

            if (tenant is null)
            {
                tenant = new TenantConfig
                {
                    TenantId = seed.TenantId ?? nextTenantId++,
                    TenantKey = normalizedKey,
                    IsolationMode = seed.IsolationMode,
                    SchemaName = seed.SchemaName,
                    ConnectionString = seed.ConnectionString,
                    IsActive = seed.IsActive
                };

                hostDbContext.Tenants.Add(tenant);
                existingTenants.Add(tenant);
            }
            else
            {
                tenant.IsolationMode = seed.IsolationMode;
                tenant.SchemaName = seed.SchemaName;
                tenant.ConnectionString = seed.ConnectionString;
                tenant.IsActive = seed.IsActive;
            }
        }

        await hostDbContext.SaveChangesAsync();
    }

    private static bool LooksLikePostgres(string connectionString)
        => connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeSqlServer(string connectionString)
        => connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase);
}
