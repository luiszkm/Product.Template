using Kernel.Application.Security;
using Kernel.Infrastructure.Configurations;
using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Kernel.Application.Data;

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

        // Nota: A configuração do banco de dados é feita em Identity.Infrastructure
        // via AddDatabaseConfiguration() para evitar dependência circular

        // Adicionar outras configurações de infraestrutura aqui
        // Ex: services.AddCachingConfiguration(configuration);
        services.AddScoped<IHashServices, HashServices>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
