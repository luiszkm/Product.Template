using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Api.Middleware;

/// <summary>
/// Bloqueia requisições sem tenant resolvido, exceto rotas explicitamente ignoradas (health, docs, etc.).
/// </summary>
public class TenantGuardMiddleware
{
    private static readonly PathString[] IgnoredPrefixes =
    {
        new("/health"),
        new("/healthchecks"),
        new("/swagger"),
        new("/scalar"),
        new("/openapi"),
        new("/docs"),
        new("/metrics"),
        new("/__otel"),
        new("/favicon.ico")
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantGuardMiddleware> _logger;

    public TenantGuardMiddleware(RequestDelegate next, ILogger<TenantGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (ShouldSkipGuard(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!tenantContext.IsResolved)
        {
            _logger.LogWarning("Tenant guard blocked request to {Path} without tenant.", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Tenant was not resolved for this request.");
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkipGuard(PathString path)
    {
        foreach (var prefix in IgnoredPrefixes)
        {
            var prefixValue = prefix.Value;
            if (!string.IsNullOrEmpty(prefixValue) && path.Value?.StartsWith(prefixValue, StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }

        return false;
    }
}




