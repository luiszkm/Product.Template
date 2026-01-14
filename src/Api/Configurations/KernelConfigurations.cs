using Product.Template.Kernel.Application;
using System.Reflection;
using Product.Template.Core.Identity.Application.Handlers.Auth.Commands;

namespace Product.Template.Api.Configurations;

public static class KernelConfigurations
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
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

        return services;
    }
}



