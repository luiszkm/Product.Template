using Microsoft.FeatureManagement;

namespace Product.Template.Api.Configurations;

/// <summary>
/// Configuração de Feature Flags
/// </summary>
public static class FeatureFlagsConfiguration
{
    public static IServiceCollection AddFeatureFlagsConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFeatureManagement(configuration.GetSection("FeatureFlags"));

        return services;
    }
}

