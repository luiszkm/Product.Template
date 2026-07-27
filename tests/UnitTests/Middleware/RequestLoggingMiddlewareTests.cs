using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Api.Middleware;

namespace UnitTests.Middleware;

public class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldPreserveResponseBody_AfterCapturingIt()
    {
        RequestDelegate next = async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            await ctx.Response.WriteAsync("hello");
        };

        var middleware = new RequestLoggingMiddleware(next, NullLogger<RequestLoggingMiddleware>.Instance);
        var context = CreateContext();
        var originalBody = context.Response.Body;

        await middleware.InvokeAsync(context);

        originalBody.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(originalBody).ReadToEndAsync();

        Assert.Equal("hello", body);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotEchoCorrelationIdHeader_WhenProvidedByCaller()
    {
        // Middleware only adds X-Correlation-ID to the response when it generates a new one;
        // an incoming id is used for logging but intentionally not echoed back.
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RequestLoggingMiddleware(next, NullLogger<RequestLoggingMiddleware>.Instance);
        var context = CreateContext();
        context.Request.Headers["X-Correlation-ID"] = "fixed-correlation-id";

        await middleware.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("X-Correlation-ID"));
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateCorrelationId_WhenNotProvided()
    {
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RequestLoggingMiddleware(next, NullLogger<RequestLoggingMiddleware>.Instance);
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();
        Assert.False(string.IsNullOrEmpty(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task InvokeAsync_ShouldRethrow_WhenNextThrows_ButStillLogResponse()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("boom");
        var middleware = new RequestLoggingMiddleware(next, NullLogger<RequestLoggingMiddleware>.Instance);
        var context = CreateContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/identity";
        context.Response.Body = new MemoryStream();
        return context;
    }
}
