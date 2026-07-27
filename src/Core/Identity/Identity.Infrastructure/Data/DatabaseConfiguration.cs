using Kernel.Application.Security;
using Kernel.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Product.Template.Core.Identity.Infrastructure.Data.Seeders;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;
using Product.Template.Kernel.Infrastructure.Seeders;

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

        var auditTrailEnabled = configuration.GetValue<bool>("FeatureFlags:EnableAuditTrail", true);

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<MultiTenantSaveChangesInterceptor>();
        services.AddScoped<SearchPathConnectionInterceptor>();
        if (auditTrailEnabled)
            services.AddScoped<AuditLogInterceptor>();

        services.AddDbContext<HostDbContext>((sp, options) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var hostConnection = cfg.GetConnectionString("HostDb")
                ?? throw new InvalidOperationException("ConnectionStrings:HostDb is required.");

            options.UseNpgsql(hostConnection);
        });

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionStringResolver>();
            var tenant = tenantContext.Tenant ?? new TenantConfig { IsolationMode = TenantIsolationMode.SharedDb };
            var appConnection = resolver.ResolveAppConnection(tenant);

            options.UseNpgsql(appConnection, npgsql =>
            {
                if (tenant.IsolationMode == TenantIsolationMode.SchemaPerTenant && !string.IsNullOrWhiteSpace(tenant.SchemaName))
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
                }
            });
            options.ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantModelCacheKeyFactory>();

            var env = sp.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            var interceptors = new List<IInterceptor>
            {
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<MultiTenantSaveChangesInterceptor>(),
                sp.GetRequiredService<SearchPathConnectionInterceptor>()
            };
            if (auditTrailEnabled)
                interceptors.Add(sp.GetRequiredService<AuditLogInterceptor>());
            options.AddInterceptors(interceptors.ToArray());
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
                TenantId = WellKnownTenants.Public,
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

            await UserSeeder.SeedAsync(context, hashServices);

            var appSeeders = tenantScope.ServiceProvider.GetServices<IAppSeeder>();
            foreach (var seeder in appSeeders)
                await seeder.SeedAsync(context);
        }
    }

    private static async Task EnsureTenantsAsync(HostDbContext hostDbContext, IEnumerable<TenantSeedDefinition> tenantSeeds)
    {
        var existingTenants = await hostDbContext.Tenants.ToListAsync();

        foreach (var seed in tenantSeeds)
        {
            if (string.IsNullOrWhiteSpace(seed.TenantKey))
                continue;

            var normalizedKey = seed.TenantKey.Trim().ToLowerInvariant();
            var tenant = existingTenants.FirstOrDefault(x => x.TenantKey == normalizedKey);

            if (tenant is null)
            {
                var tenantId = seed.TenantId
                    ?? (normalizedKey == "public" ? WellKnownTenants.Public : Guid.NewGuid());

                tenant = new TenantConfig
                {
                    TenantId = tenantId,
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

}
