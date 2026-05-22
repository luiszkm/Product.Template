using Product.Template.Api.Middleware;

namespace Product.Template.Api.Configurations;

public static class MonitoringConfiguration
{
    public static IApplicationBuilder UseMonitoringApiKey(this IApplicationBuilder app) =>
        app.UseMiddleware<MonitoringApiKeyMiddleware>();
}
