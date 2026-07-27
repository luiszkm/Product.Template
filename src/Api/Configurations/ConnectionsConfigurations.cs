namespace Product.Template.Api.Configurations;

public static class ConnectionsConfigurations
{
    private const string PlaceholderPassword = "YourStrong!Pass123";

    public static IServiceCollection AddAppConnections(this IServiceCollection services, IConfiguration config)
    {
        // Connection routing for AppDb/HostDb is configured in Identity.Infrastructure -> AddDatabaseConfiguration.
        return services;
    }

    // appsettings.json ships HostDb/AppDb with a placeholder password and no override in
    // appsettings.Production.json — without this guard it goes to prod verbatim if ops forgets
    // to set the real connection string via environment/secrets manager.
    public static void ValidateConnectionStrings(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return;

        foreach (var child in configuration.GetSection("ConnectionStrings").GetChildren())
        {
            if (child.Value?.Contains(PlaceholderPassword, StringComparison.Ordinal) == true)
            {
                throw new InvalidOperationException(
                    $"ConnectionStrings:{child.Key} is still using the template placeholder password. " +
                    "Set a real connection string via environment or secrets manager.");
            }
        }
    }
}
