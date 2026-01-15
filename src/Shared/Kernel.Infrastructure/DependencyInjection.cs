using Kernel.Infrastructure.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kernel.Infrastructure;

/// <summary>
/// Registro de serviços da infraestrutura Kernel
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddKernelInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Adicionar configuração de JWT
        services.AddJwtConfiguration(configuration);

        // Adicionar outras configurações de infraestrutura aqui
        // Ex: services.AddDatabaseConfiguration(configuration);
        //     services.AddCachingConfiguration(configuration);

        return services;
    }
}
