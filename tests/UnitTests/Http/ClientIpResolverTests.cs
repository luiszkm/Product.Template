using System.Net;
using Microsoft.AspNetCore.Http;
using Product.Template.Api.Http;

namespace UnitTests.Http;

public class ClientIpResolverTests
{
    [Fact]
    public void GetClientIp_ShouldReturnRemoteIpAddress()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        var result = ClientIpResolver.GetClientIp(context);

        Assert.Equal("203.0.113.10", result);
    }

    [Fact]
    public void GetClientIp_ShouldReturnNull_WhenRemoteIpAddressIsNull()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;

        var result = ClientIpResolver.GetClientIp(context);

        Assert.Null(result);
    }

    [Fact]
    public void GetClientIp_ShouldIgnoreXForwardedForHeader_EvenWhenPresent()
    {
        // Regression guard: reading X-Forwarded-For directly here would let any caller spoof
        // its IP and bypass IP allowlisting/rate limiting. Only the (proxy-validated)
        // RemoteIpAddress set by UseForwardedHeaders may be trusted.
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.99";

        var result = ClientIpResolver.GetClientIp(context);

        Assert.Equal("203.0.113.10", result);
    }

    [Fact]
    public void GetClientIp_ShouldIgnoreXRealIpHeader_EvenWhenPresent()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        context.Request.Headers["X-Real-IP"] = "198.51.100.99";

        var result = ClientIpResolver.GetClientIp(context);

        Assert.Equal("203.0.113.10", result);
    }
}
