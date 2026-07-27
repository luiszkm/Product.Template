using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Api.Middleware;

namespace UnitTests.Middleware;

public class IpWhitelistMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenSecurityDisabled()
    {
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>());
        var context = CreateContext("10.0.0.5");

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn403_WhenIpIsBlacklisted()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("next should not run");

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["IpSecurity:EnableBlacklist"] = "true",
            ["IpSecurity:BlockedIPs:0"] = "10.0.0.5"
        });
        var context = CreateContext("10.0.0.5");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn403_WhenIpNotInWhitelist()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("next should not run");

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["IpSecurity:EnableWhitelist"] = "true",
            ["IpSecurity:AllowedIPs:0"] = "192.168.1.10"
        });
        var context = CreateContext("10.0.0.5");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenIpMatchesCidrRange()
    {
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["IpSecurity:EnableWhitelist"] = "true",
            ["IpSecurity:AllowedIPs:0"] = "192.168.1.0/24"
        });
        var context = CreateContext("192.168.1.42");

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAlwaysAllowLocalhost_EvenWhenWhitelistEnabled()
    {
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["IpSecurity:EnableWhitelist"] = "true",
            ["IpSecurity:AllowedIPs:0"] = "192.168.1.10"
        });
        var context = CreateContext("127.0.0.1");

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
    }

    [Fact]
    public async Task InvokeAsync_BlacklistShouldTakePriority_OverWhitelist()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("next should not run");

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["IpSecurity:EnableWhitelist"] = "true",
            ["IpSecurity:AllowedIPs:0"] = "10.0.0.5",
            ["IpSecurity:EnableBlacklist"] = "true",
            ["IpSecurity:BlockedIPs:0"] = "10.0.0.5"
        });
        var context = CreateContext("10.0.0.5");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    private static IpWhitelistMiddleware CreateMiddleware(RequestDelegate next, Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new IpWhitelistMiddleware(next, NullLogger<IpWhitelistMiddleware>.Instance, configuration);
    }

    private static DefaultHttpContext CreateContext(string remoteIp)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(remoteIp);
        context.Response.Body = new MemoryStream();
        return context;
    }
}
