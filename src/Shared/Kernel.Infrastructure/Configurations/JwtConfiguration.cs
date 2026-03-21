using Kernel.Application.Security;
using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kernel.Infrastructure.Configurations;

public static class JwtConfiguration
{
    public static IServiceCollection AddJwtConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtEnabled = configuration.GetValue<bool>("Jwt:Enabled");

        if (!jwtEnabled)
            return services;
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateOnStart();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}

