using Microsoft.AspNetCore.OutputCaching;

namespace Product.Template.Api.Configurations;

public static class CachingConfiguration
{
    public static IServiceCollection AddCachingConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (!IsCachingEnabled(configuration))
            return services;

        services.AddOutputCache(options =>
        {
            // Política base: nada é cacheado por padrão. Endpoints devem optar
            // explicitamente por uma das políticas nomeadas abaixo via [OutputCache(PolicyName = "...")].
            // Isso evita cachear respostas autenticadas/tenant-scoped sem opt-in explícito
            // (nenhum controller usa [OutputCache] hoje — cachear por padrão vazaria dados
            // entre usuários/tenants que batem na mesma URL+query).
            options.AddBasePolicy(builder => builder.NoCache());

            // Cache de usuários (5 minutos) — vary by Authorization garante que a resposta
            // cacheada nunca é compartilhada entre identidades diferentes na mesma URL+query.
            options.AddPolicy("UserCache", builder => builder
                .Expire(TimeSpan.FromMinutes(5))
                .Tag("users")
                .SetVaryByQuery("pageNumber", "pageSize")
                .SetVaryByHeader("Authorization"));

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
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        if (IsCachingEnabled(configuration))
            app.UseOutputCache();

        return app;
    }

    private static bool IsCachingEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("Caching:Enabled", true)
        && configuration.GetValue<bool>("FeatureFlags:EnableCaching", true);
}

