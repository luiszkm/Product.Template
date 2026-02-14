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

        services.AddMemoryCache();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantResolver, HeaderAndSubdomainTenantResolver>();
        services.AddScoped<ITenantStore, CachedTenantStore>();
        services.AddScoped<ITenantConnectionStringResolver, TenantConnectionStringResolver>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<MultiTenantSaveChangesInterceptor>();
        services.AddScoped<SearchPathConnectionInterceptor>();

        var hostConnection = configuration.GetConnectionString("HostDb")
            ?? configuration.GetConnectionString("AppDb")
            ?? "Data Source=host.db";

        services.AddDbContext<HostDbContext>(options =>
        {
            if (LooksLikePostgres(hostConnection))
            {
                options.UseNpgsql(hostConnection);
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
                sp.GetRequiredService<SearchPathConnectionInterceptor>());
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var host = scope.ServiceProvider.GetRequiredService<HostDbContext>();

        await host.Database.MigrateAsync();


        if (!await host.Tenants.AnyAsync())
        {
            host.Tenants.Add(new TenantConfig
            {
                TenantId = 1,
                TenantKey = "public",
                IsolationMode = TenantIsolationMode.SharedDb,
                IsActive = true
            });
            await host.SaveChangesAsync();
        }

        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetTenant(await host.Tenants.FirstAsync(x => x.TenantKey == "public"));

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        await RoleSeeder.SeedAsync(context);
        await UserSeeder.SeedAsync(context);
    }

    private static bool LooksLikePostgres(string connectionString)
        => connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase);
}
