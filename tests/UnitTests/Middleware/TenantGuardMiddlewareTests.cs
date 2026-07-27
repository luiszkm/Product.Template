using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Api.Middleware;
using Product.Template.Kernel.Domain.MultiTenancy;

namespace UnitTests.Middleware;

public class TenantGuardMiddlewareTests
{
    private sealed class FakeTenantContext : ITenantContext
    {
        public FakeTenantContext(bool isResolved) => IsResolved = isResolved;

        public Guid? TenantId => IsResolved ? Guid.NewGuid() : null;
        public string? TenantKey => IsResolved ? "acme" : null;
        public TenantConfig? Tenant => null;
        public bool IsResolved { get; }
        public void SetTenant(TenantConfig tenant) { }
    }

    [Fact]
    public async Task InvokeAsync_ShouldBlockWith400_WhenTenantNotResolved()
    {
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = new TenantGuardMiddleware(next, NullLogger<TenantGuardMiddleware>.Instance);
        var context = CreateContext("/api/v1/tenants");

        await middleware.InvokeAsync(context, new FakeTenantContext(isResolved: false));

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenTenantResolved()
    {
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = new TenantGuardMiddleware(next, NullLogger<TenantGuardMiddleware>.Instance);
        var context = CreateContext("/api/v1/tenants");

        await middleware.InvokeAsync(context, new FakeTenantContext(isResolved: true));

        Assert.True(invoked);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/healthchecks/detail")]
    [InlineData("/swagger/index.html")]
    [InlineData("/metrics")]
    public async Task InvokeAsync_ShouldSkipGuard_ForIgnoredPrefixes(string path)
    {
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = new TenantGuardMiddleware(next, NullLogger<TenantGuardMiddleware>.Instance);
        var context = CreateContext(path);

        await middleware.InvokeAsync(context, new FakeTenantContext(isResolved: false));

        Assert.True(invoked);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
