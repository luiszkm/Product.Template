using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Authorization.Application.Permissions;
using Product.Template.Core.Authorization.Domain.Repositories;
using Product.Template.Core.Authorization.Infrastructure.Data.Persistence;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Authorization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationModule(this IServiceCollection services)
    {
        services.GetOrCreateEfRegistry().Register(typeof(DependencyInjection).Assembly);

        services.AddTransient<IRoleRepository, RoleRepository>();
        services.AddTransient<IPermissionRepository, PermissionRepository>();
        services.AddTransient<IUserAssignmentRepository, UserAssignmentRepository>();
        services.AddScoped<IUserRolesProvider, UserRolesProvider>();

        services.AddSingleton<IPermissionCatalogSeeder, AuthorizationPermissionCatalogSeeder>();

        return services;
    }
}
