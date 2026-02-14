using Microsoft.AspNetCore.Http;

namespace Product.Template.Kernel.Domain.MultiTenancy;

public interface ITenantResolver
{
    string? ResolveTenantKey(HttpContext httpContext);
}
