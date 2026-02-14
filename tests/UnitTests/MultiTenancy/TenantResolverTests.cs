using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

namespace UnitTests.MultiTenancy;

public class TenantResolverTests
{
    [Fact]
    public void ResolveTenantKey_ShouldUseHeader_WhenHeaderExists()
    {
        var resolver = new HeaderAndSubdomainTenantResolver(Options.Create(new TenantResolutionOptions()));
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant"] = "acme";

        var result = resolver.ResolveTenantKey(context);

        Assert.Equal("acme", result);
    }

    [Fact]
    public void ResolveTenantKey_ShouldUseSubdomain_WhenHeaderMissing()
    {
        var resolver = new HeaderAndSubdomainTenantResolver(Options.Create(new TenantResolutionOptions()));
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("premium.example.com");

        var result = resolver.ResolveTenantKey(context);

        Assert.Equal("premium", result);
    }
}
