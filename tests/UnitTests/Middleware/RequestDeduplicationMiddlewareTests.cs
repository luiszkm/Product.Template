using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Api.Middleware;

namespace UnitTests.Middleware;

public class RequestDeduplicationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenMethodIsGet()
    {
        var cache = new FakeDistributedCache();
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = new RequestDeduplicationMiddleware(
            next,
            cache,
            NullLogger<RequestDeduplicationMiddleware>.Instance);

        var context = CreateContext(HttpMethods.Get, "/api/v1/test");

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenFirstPostRequest()
    {
        var cache = new FakeDistributedCache();
        var invoked = false;
        RequestDelegate next = context =>
        {
            invoked = true;
            context.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        };

        var middleware = new RequestDeduplicationMiddleware(
            next,
            cache,
            NullLogger<RequestDeduplicationMiddleware>.Instance);

        var context = CreateContext(HttpMethods.Post, "/api/v1/test");
        context.Request.Headers["X-Idempotency-Key"] = "key-1";

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn409_WhenDuplicateIdempotencyKeyIsCached()
    {
        var cache = new FakeDistributedCache();
        var idempotencyKey = "duplicate-key";
        var entry = JsonSerializer.Serialize(new
        {
            IdempotencyKey = idempotencyKey,
            Timestamp = DateTime.UtcNow,
            Method = HttpMethods.Post,
            Path = "/api/v1/test"
        });
        await cache.SetStringAsync($"dedup:{idempotencyKey}", entry);

        RequestDelegate next = _ => throw new InvalidOperationException("Next should not be invoked");

        var middleware = new RequestDeduplicationMiddleware(
            next,
            cache,
            NullLogger<RequestDeduplicationMiddleware>.Instance);

        var context = CreateContext(HttpMethods.Post, "/api/v1/test");
        context.Request.Headers["X-Idempotency-Key"] = idempotencyKey;

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("true", context.Response.Headers["X-Duplicate-Request"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn409_WhenSameRequestIsInFlight()
    {
        var cache = new FakeDistributedCache();
        var idempotencyKey = "in-flight-key";
        await cache.SetStringAsync($"dedup:processing:{idempotencyKey}", "1");

        RequestDelegate next = _ => throw new InvalidOperationException("Next should not be invoked");

        var middleware = new RequestDeduplicationMiddleware(
            next,
            cache,
            NullLogger<RequestDeduplicationMiddleware>.Instance);

        var context = CreateContext(HttpMethods.Post, "/api/v1/test");
        context.Request.Headers["X-Idempotency-Key"] = idempotencyKey;

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("true", context.Response.Headers["X-Duplicate-Request"].ToString());
    }

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        context.Request.ContentLength = context.Request.Body.Length;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

        public byte[]? Get(string key) =>
            _store.TryGetValue(key, out var value) ? value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult(Get(key));

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _store[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}
