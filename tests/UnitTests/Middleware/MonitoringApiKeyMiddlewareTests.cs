using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Template.Api.Middleware;

namespace UnitTests.Middleware;

public class MonitoringApiKeyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenPathDoesNotRequireApiKey()
    {
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["Monitoring:RequireApiKey"] = "true",
            ["Monitoring:ApiKey"] = "secret-key"
        }, Environments.Production);

        var context = CreateContext("/api/v1/identity");

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_WhenMetricsPathHasInvalidApiKey()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Next should not be invoked");

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["Monitoring:RequireApiKey"] = "true",
            ["Monitoring:ApiKey"] = "secret-key"
        }, Environments.Production);

        var context = CreateContext("/metrics");
        context.Request.Headers["X-Monitoring-Api-Key"] = "wrong-key";

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenMetricsPathHasValidApiKey()
    {
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["Monitoring:RequireApiKey"] = "true",
            ["Monitoring:ApiKey"] = "secret-key"
        }, Environments.Production);

        var context = CreateContext("/metrics");
        context.Request.Headers["X-Monitoring-Api-Key"] = "secret-key";

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenDevelopmentDoesNotRequireApiKey()
    {
        var invoked = false;
        RequestDelegate next = _ =>
        {
            invoked = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["Monitoring:RequireApiKeyInDevelopment"] = "false",
            ["Monitoring:ApiKey"] = "secret-key"
        }, Environments.Development);

        var context = CreateContext("/metrics");

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn503_WhenApiKeyIsNotConfigured()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Next should not be invoked");

        var middleware = CreateMiddleware(next, new Dictionary<string, string?>
        {
            ["Monitoring:RequireApiKey"] = "true"
        }, Environments.Production);

        var context = CreateContext("/health/ready");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    private static MonitoringApiKeyMiddleware CreateMiddleware(
        RequestDelegate next,
        Dictionary<string, string?> settings,
        string environmentName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new MonitoringApiKeyMiddleware(
            next,
            configuration,
            new FakeHostEnvironment(environmentName),
            NullLogger<MonitoringApiKeyMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
            ApplicationName = "UnitTests";
            ContentRootPath = Directory.GetCurrentDirectory();
            ContentRootFileProvider = new NullFileProvider();
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
