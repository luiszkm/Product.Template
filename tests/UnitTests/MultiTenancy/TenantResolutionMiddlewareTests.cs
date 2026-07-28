using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

namespace UnitTests.MultiTenancy;

public class TenantResolutionMiddlewareTests
{
    private sealed class FakeTenantResolver(string? tenantKey) : ITenantResolver
    {
        public string? ResolveTenantKey(HttpContext httpContext) => tenantKey;
    }

    private sealed class FakeTenantStore(TenantConfig? tenant) : ITenantStore
    {
        public Task<TenantConfig?> GetByKeyAsync(string tenantKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(tenant is not null && tenant.TenantKey == tenantKey ? tenant : null);

        public Task<IReadOnlyList<TenantConfig>> ListActiveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TenantConfig>>([]);

        public Task UpsertAsync(TenantConfig tenantConfig, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MutableTenantContext : ITenantContext
    {
        public Guid? TenantId { get; private set; }
        public string? TenantKey { get; private set; }
        public TenantConfig? Tenant { get; private set; }
        public bool IsResolved => Tenant is not null;

        public void SetTenant(TenantConfig tenant)
        {
            Tenant = tenant;
            TenantId = tenant.TenantId;
            TenantKey = tenant.TenantKey;
        }
    }

    private static DefaultHttpContext CreateContext(ITenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddSingleton(tenantContext);
        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static TenantResolutionMiddleware CreateMiddleware(RequestDelegate next, TenantResolutionOptions? options = null) =>
        new(next, Options.Create(options ?? new TenantResolutionOptions()), NullLogger<TenantResolutionMiddleware>.Instance);

    [Fact]
    public async Task InvokeAsync_ShouldSetTenant_WhenResolverFindsActiveTenant()
    {
        var tenant = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb, IsActive = true };
        var tenantContext = new MutableTenantContext();
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next);
        var context = CreateContext(tenantContext);

        await middleware.InvokeAsync(context, new FakeTenantResolver("acme"), new FakeTenantStore(tenant));

        Assert.True(invoked);
        Assert.True(tenantContext.IsResolved);
        Assert.Equal(tenant.TenantId, tenantContext.TenantId);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_WhenTenantIsInactive()
    {
        var tenant = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "acme", IsolationMode = TenantIsolationMode.SharedDb, IsActive = false };
        var tenantContext = new MutableTenantContext();
        RequestDelegate next = _ => throw new InvalidOperationException("next should not run");

        var middleware = CreateMiddleware(next);
        var context = CreateContext(tenantContext);

        await middleware.InvokeAsync(context, new FakeTenantResolver("acme"), new FakeTenantStore(tenant));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(tenantContext.IsResolved);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_WhenTenantKeyDoesNotResolveToAnyTenant()
    {
        var tenantContext = new MutableTenantContext();
        RequestDelegate next = _ => throw new InvalidOperationException("next should not run");

        var middleware = CreateMiddleware(next);
        var context = CreateContext(tenantContext);

        await middleware.InvokeAsync(context, new FakeTenantResolver("unknown"), new FakeTenantStore(null));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenNoTenantKeyAndNoPublicFallback()
    {
        var tenantContext = new MutableTenantContext();
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next);
        var context = CreateContext(tenantContext);

        await middleware.InvokeAsync(context, new FakeTenantResolver(null), new FakeTenantStore(null));

        Assert.True(invoked);
        Assert.False(tenantContext.IsResolved);
    }

    [Fact]
    public async Task InvokeAsync_ShouldFallBackToPublicTenant_WhenAllowPublicFallbackIsEnabled()
    {
        var publicTenant = new TenantConfig { TenantId = Guid.NewGuid(), TenantKey = "public", IsolationMode = TenantIsolationMode.SharedDb, IsActive = true };
        var tenantContext = new MutableTenantContext();
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var options = new TenantResolutionOptions { AllowPublicFallback = true, PublicTenantKey = "public" };
        var middleware = CreateMiddleware(next, options);
        var context = CreateContext(tenantContext);

        await middleware.InvokeAsync(context, new FakeTenantResolver(null), new FakeTenantStore(publicTenant));

        Assert.True(invoked);
        Assert.True(tenantContext.IsResolved);
        Assert.Equal("public", tenantContext.TenantKey);
    }
}
