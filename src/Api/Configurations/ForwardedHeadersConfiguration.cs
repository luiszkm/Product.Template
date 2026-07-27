using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Product.Template.Api.Configurations;

public static class ForwardedHeadersConfiguration
{
    // Sem KnownProxies/KnownNetworks configurados, o ForwardedHeadersMiddleware só confia em
    // loopback — atrás de um reverse proxy real (nginx/ALB/ingress) os headers X-Forwarded-*
    // são silenciosamente ignorados e todo cliente cai no mesmo RemoteIpAddress (o do proxy),
    // quebrando rate limiting e IP allowlisting por cliente. Configure via
    // "ForwardedHeaders:KnownProxies" / "ForwardedHeaders:KnownNetworks" (CIDR) por ambiente.
    public static WebApplication UseForwardedHeadersConfiguration(this WebApplication app)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        var knownProxies = app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        foreach (var proxy in knownProxies)
        {
            if (IPAddress.TryParse(proxy, out var ip))
                options.KnownProxies.Add(ip);
        }

        var knownNetworks = app.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
        foreach (var network in knownNetworks)
        {
            var parts = network.Split('/', StringSplitOptions.TrimEntries);
            if (parts.Length == 2 &&
                IPAddress.TryParse(parts[0], out var prefix) &&
                int.TryParse(parts[1], out var prefixLength))
            {
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, prefixLength));
            }
        }

        app.UseForwardedHeaders(options);

        return app;
    }
}
