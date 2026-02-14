namespace Product.Template.Api.Configurations;

public static class ConnectionsConfigurations
{
    public static IServiceCollection AddAppConnections(this IServiceCollection services, IConfiguration config)
    {
        // Connection routing for AppDb/HostDb is configured in Identity.Infrastructure -> AddDatabaseConfiguration.
        return services;
    }
}
