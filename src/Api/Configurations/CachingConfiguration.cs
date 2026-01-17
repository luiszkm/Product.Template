using Microsoft.AspNetCore.OutputCaching;

namespace Product.Template.Api.Configurations;

/// <summary>
/// Configuração de Output Caching (.NET 8+)
/// </summary>
public static class CachingConfiguration
{
    public static IServiceCollection AddCachingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheEnabled = configuration.GetValue<bool>("Caching:Enabled", true);

        if (!cacheEnabled)
            return services;

        services.AddOutputCache(options =>
        {
            // Política base padrão
            options.AddBasePolicy(builder => builder
                .Expire(TimeSpan.FromMinutes(10))
                .SetVaryByQuery("*"));

            // Cache de usuários (5 minutos)
            options.AddPolicy("UserCache", builder => builder
                .Expire(TimeSpan.FromMinutes(5))
                .Tag("users")
                .SetVaryByQuery("pageNumber", "pageSize"));

            // Cache de consultas públicas (15 minutos)
            options.AddPolicy("PublicCache", builder => builder
                .Expire(TimeSpan.FromMinutes(15))
                .Tag("public"));

            // Cache de lookup/reference data (30 minutos)
            options.AddPolicy("ReferenceDataCache", builder => builder
                .Expire(TimeSpan.FromMinutes(30))
                .Tag("reference"));

            // Não cachear por usuário autenticado
            options.AddPolicy("NoCache", builder => builder
                .NoCache());
        });

        return services;
    }

    public static IApplicationBuilder UseCachingConfiguration(this IApplicationBuilder app)
    {
        app.UseOutputCache();
        return app;
    }
}

