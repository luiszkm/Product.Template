using Kernel.Application.Security;
using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kernel.Infrastructure.Configurations;

/// <summary>
/// Configuração de JWT (JSON Web Token) para autenticação
/// </summary>
public static class JwtConfiguration
{
    public static IServiceCollection AddJwtConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtEnabled = configuration.GetValue<bool>("Jwt:Enabled");

        if (!jwtEnabled)
            return services;

        // Configurar JwtSettings a partir do arquivo de configuração
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateOnStart();

        // Registrar o serviço de geração de tokens JWT
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}

