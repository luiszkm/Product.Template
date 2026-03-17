using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Identity.Application.Permissions;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Core.Identity.Infrastructure.Data.Persistence;
using Product.Template.Core.Identity.Infrastructure.Security;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Security;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInJections(this IServiceCollection services)
    {
        services.AddRepositories();
        services.AddPermissionCatalogSeeders();
        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISecurityStampService, SecurityStampService>();

        return services;
    }

    private static IServiceCollection AddPermissionCatalogSeeders(this IServiceCollection services)
    {
        services.AddSingleton<IPermissionCatalogSeeder, IdentityPermissionCatalogSeeder>();
        return services;
    }
}
