using Kernel.Infrastructure.Ai;

namespace Product.Template.Api.Configurations;

public static class AiConfiguration
{
    public static IServiceCollection AddAiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var aiEnabled = configuration.GetValue<bool>("FeatureFlags:EnableAI");

        if (!aiEnabled)
        {
            services.AddNullAiServices();
            return services;
        }

        var redis = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrEmpty(redis))
            services.AddStackExchangeRedisCache(o => o.Configuration = redis);
        else
            services.AddDistributedMemoryCache();

        services.AddAiServices(configuration);
        return services;
    }
}
