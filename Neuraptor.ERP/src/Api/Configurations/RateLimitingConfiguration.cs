using AspNetCoreRateLimit;

namespace Neuraptor.ERP.Api.Configurations;

public static class RateLimitingConfiguration
{
    public static IServiceCollection AddRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Necessário para armazenar contadores de rate limit
        services.AddMemoryCache();

        // Configuração do Rate Limiting
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));

        // Injeção dos serviços necessários
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }

    public static WebApplication UseRateLimiting(this WebApplication app)
    {
        app.UseIpRateLimiting();
        return app;
    }
}
