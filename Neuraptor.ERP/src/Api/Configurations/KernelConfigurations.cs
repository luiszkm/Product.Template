using Neuraptor.ERP.Kernel.Application.Behaviors;
using Neuraptor.ERP.Kernel.Application.Messaging;
using Neuraptor.ERP.Kernel.Application.Messaging.Interfaces;

namespace Neuraptor.ERP.Api.Configurations;

public static class KernelConfigurations
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        // -------------------------------
        // CommandBus e QueryBus
        // -------------------------------
        services.AddScoped<ICommandBus, CommandBus>();
        services.AddScoped<IQueryBus, QueryBus>();

        // -------------------------------
        // Behaviors
        // -------------------------------
        // Todos os behaviors podem ser resolvidos como IEnumerable<ICommandBehavior> ou IQueryBehavior
        services.AddScoped<ICommandBehavior, LoggingBehavior>();
        services.AddScoped<ICommandBehavior, PerformanceBehavior>();
        services.AddScoped<ICommandBehavior, ValidationBehavior>();

        services.AddScoped<IQueryBehavior, LoggingBehavior>();
        services.AddScoped<IQueryBehavior, PerformanceBehavior>();
        services.AddScoped<IQueryBehavior, ValidationBehavior>();

        // -------------------------------
        // Handlers (Command e Query)
        // -------------------------------
        services.Scan(scan => scan
            .FromApplicationDependencies()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        return services;
    }
}

