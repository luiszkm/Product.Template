using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace Product.Template.Kernel.Infrastructure.MultiTenancy;

public class TenantResolutionMiddleware(
    RequestDelegate next,
    ITenantResolver tenantResolver,
    ITenantStore tenantStore,
    ITenantContext tenantContext,
    IOptions<TenantResolutionOptions> options,
    ILogger<TenantResolutionMiddleware> logger)
{
    private readonly TenantResolutionOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantKey = tenantResolver.ResolveTenantKey(context);

        if (string.IsNullOrWhiteSpace(tenantKey) && _options.AllowPublicFallback)
        {
            tenantKey = _options.PublicTenantKey;
        }

        if (string.IsNullOrWhiteSpace(tenantKey))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Tenant was not provided.");
            return;
        }

        var tenant = await tenantStore.GetByKeyAsync(tenantKey, context.RequestAborted);
        if (tenant is null || !tenant.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Tenant is invalid or inactive.");
            return;
        }

        tenantContext.SetTenant(tenant);

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["TenantId"] = tenant.TenantId,
                   ["TenantKey"] = tenant.TenantKey
               }))
        {
            await next(context);
        }
    }
}
