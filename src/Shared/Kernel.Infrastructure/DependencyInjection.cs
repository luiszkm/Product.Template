using Kernel.Application.Security;
using Kernel.Infrastructure.Configurations;
using Kernel.Infrastructure.Security;
using Kernel.Infrastructure.Persistence.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Security;

namespace Kernel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKernelInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddJwtConfiguration(configuration);

        // Adicionar Authentication Providers (JWT, Microsoft, Google, etc.)
        services.AddAuthenticationProviders(configuration);

        // Nota: A configuração do banco de dados é feita em Identity.Infrastructure
        // via AddDatabaseConfiguration() para evitar dependência circular

        // Serviços de segurança
        services.AddScoped<IHashServices, HashServices>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<AuditableEntityInterceptor>();

        return services;
    }
}
