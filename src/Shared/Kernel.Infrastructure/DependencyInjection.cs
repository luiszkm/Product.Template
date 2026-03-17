using Kernel.Application.Security;
using Kernel.Infrastructure.Configurations;
using Kernel.Infrastructure.Persistence.Interceptors;
using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Kernel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKernelInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddJwtConfiguration(configuration);
        services.AddAuthenticationProviders(configuration);

        services.AddSingleton<PermissionCatalog>();
        services.AddSingleton<IPermissionCatalog>(sp =>
        {
            var catalog = sp.GetRequiredService<PermissionCatalog>();
            var seeders = sp.GetServices<IPermissionCatalogSeeder>();
            foreach (var seeder in seeders)
            {
                seeder.Register(catalog);
            }

            return catalog;
        });

        services.AddScoped<IHashServices, HashServices>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<ITenantContext, TenantContext>();

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<AuditLogInterceptor>();

        // Register Kernel.Infrastructure's own EF configurations
        services.GetOrCreateEfRegistry().Register(typeof(AppDbContext).Assembly);

        return services;
    }
}
