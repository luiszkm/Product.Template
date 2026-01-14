using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Kernel.Application.Behaviors;
using System.Reflection;

namespace Product.Template.Kernel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddKernelApplication(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // Registra MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
        });

        // Registra FluentValidation validators
        foreach (var assembly in assemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        // Registra os behaviors do MediatR
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}
