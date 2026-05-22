namespace Product.Template.Api.Configurations;

public static class DistributedCacheConfiguration
{
    public static IServiceCollection AddDistributedCacheConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redis = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redis))
            services.AddStackExchangeRedisCache(options => options.Configuration = redis);
        else
            services.AddDistributedMemoryCache();

        return services;
    }
}
