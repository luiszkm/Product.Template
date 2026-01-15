using Product.Template.Kernel.Application;
using Kernel.Infrastructure;
using System.Reflection;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;

namespace Product.Template.Api.Configurations;

public static class KernelConfigurations
{
    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddApplicationCore(
        this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // Obtém todos os assemblies da aplicação que contém handlers
        var assemblies = new[]
        {
            Assembly.GetExecutingAssembly(), // Api
            typeof(Kernel.Application.DependencyInjection).Assembly, // Kernel.Application
            typeof(LoginCommand).Assembly, // Identity.Application
        };

        // Registra o MediatR e todos os behaviors
        services.AddKernelApplication(assemblies);

        // Registra serviços de infraestrutura (JWT, etc.)
        services.AddKernelInfrastructure(configuration);

        return services;
    }
}



