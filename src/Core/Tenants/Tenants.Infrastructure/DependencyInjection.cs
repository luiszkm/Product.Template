using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Tenants.Application.Permissions;
using Product.Template.Core.Tenants.Domain.Repositories;
using Product.Template.Core.Tenants.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Core.Tenants.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantsModule(this IServiceCollection services)
    {
        services.AddTransient<ITenantRepository, TenantRepository>();
        services.AddSingleton<IPermissionCatalogSeeder, TenantsPermissionCatalogSeeder>();
        return services;
    }
}
