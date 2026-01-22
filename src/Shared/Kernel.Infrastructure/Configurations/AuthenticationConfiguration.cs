using Kernel.Application.Security;
using Kernel.Infrastructure.Security;
using Kernel.Infrastructure.Security.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kernel.Infrastructure.Configurations;

/// <summary>
/// Configuração de autenticação extensível (JWT, Microsoft, Google, etc.)
/// </summary>
public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ==================== JWT Provider ====================
        var jwtEnabled = configuration.GetValue<bool>("Jwt:Enabled");
        if (jwtEnabled)
        {
            // Configurar JwtSettings
            services.AddOptions<JwtSettings>()
                .Bind(configuration.GetSection("Jwt"))
                .ValidateOnStart();

            // Registrar serviço de geração de tokens JWT
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            // Registrar provider JWT
            services.AddScoped<IAuthenticationProvider, JwtAuthenticationProvider>();
        }

        // ==================== Microsoft Provider ====================
        var microsoftAuthEnabled = configuration.GetValue<bool>("MicrosoftAuth:Enabled");
        if (microsoftAuthEnabled)
        {
            // Configurar MicrosoftAuthSettings
            services.AddOptions<MicrosoftAuthSettings>()
                .Bind(configuration.GetSection("MicrosoftAuth"))
                .ValidateOnStart();

            // Registrar HttpClient para Microsoft provider
            services.AddHttpClient<MicrosoftAuthenticationProvider>();

            // Registrar provider Microsoft
            services.AddScoped<IAuthenticationProvider, MicrosoftAuthenticationProvider>();
        }

        // ==================== Factory ====================
        // Registrar factory que gerencia todos os providers
        services.AddScoped<IAuthenticationProviderFactory, AuthenticationProviderFactory>();

        return services;
    }
}
