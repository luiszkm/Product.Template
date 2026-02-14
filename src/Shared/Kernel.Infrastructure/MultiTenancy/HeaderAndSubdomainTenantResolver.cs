using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class HeaderAndSubdomainTenantResolver(IOptions<TenantResolutionOptions> options) : ITenantResolver
{
    private readonly TenantResolutionOptions _options = options.Value;

    public string? ResolveTenantKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(_options.HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString().Trim().ToLowerInvariant();
        }

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 3 ? segments[0].Trim().ToLowerInvariant() : null;
    }
}
