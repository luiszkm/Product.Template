using Microsoft.Extensions.DependencyInjection;

namespace Product.Template.Kernel.Infrastructure.Persistence;

public static class EfModelAssemblyRegistryExtensions
{
    public static EfModelAssemblyRegistry GetOrCreateEfRegistry(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(EfModelAssemblyRegistry));
        if (descriptor?.ImplementationInstance is EfModelAssemblyRegistry existing)
            return existing;

        var registry = new EfModelAssemblyRegistry();
        services.AddSingleton(registry);
        return registry;
    }
}
