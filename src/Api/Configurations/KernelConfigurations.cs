using Product.Template.Kernel.Application;
using Kernel.Infrastructure;
using System.Reflection;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;
using Product.Template.Core.Authorization.Application.Permissions;
using Product.Template.Core.Tenants.Application.Permissions;

namespace Product.Template.Api.Configurations;

public static class KernelConfigurations
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddApplicationCore(
        this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var assemblies = new[]
        {
            Assembly.GetExecutingAssembly(), // Api
            typeof(Kernel.Application.DependencyInjection).Assembly, // Kernel.Application
            typeof(LoginCommand).Assembly, // Identity.Application
            typeof(AuthorizationPermissions).Assembly, // Authorization.Application
            typeof(TenantsPermissions).Assembly, // Tenants.Application
        };

        services.AddKernelApplication(assemblies);
        services.AddKernelInfrastructure(configuration);

        return services;
    }
}
