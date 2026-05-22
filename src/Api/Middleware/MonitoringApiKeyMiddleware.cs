namespace Product.Template.Api.Middleware;

public sealed class MonitoringApiKeyMiddleware
{
    private static readonly PathString MetricsPath = new("/metrics");
    private static readonly PathString ReadyPath = new("/health/ready");

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<MonitoringApiKeyMiddleware> _logger;

    public MonitoringApiKeyMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<MonitoringApiKeyMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresApiKey(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (_environment.IsDevelopment() && !_configuration.GetValue<bool>("Monitoring:RequireApiKeyInDevelopment"))
        {
            await _next(context);
            return;
        }

        var requireApiKey = _configuration.GetValue("Monitoring:RequireApiKey", !_environment.IsDevelopment());
        if (!requireApiKey)
        {
            await _next(context);
            return;
        }

        var expectedKey = _configuration["Monitoring:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            _logger.LogWarning("Monitoring API key is not configured; denying {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        if (!TryReadApiKey(context.Request, out var providedKey) ||
            !string.Equals(expectedKey, providedKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Invalid or missing monitoring API key for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private static bool RequiresApiKey(PathString path) =>
        path.StartsWithSegments(MetricsPath, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments(ReadyPath, StringComparison.OrdinalIgnoreCase);

    private static bool TryReadApiKey(HttpRequest request, out string? apiKey)
    {
        if (request.Headers.TryGetValue("X-Monitoring-Api-Key", out var headerValues))
        {
            apiKey = headerValues.FirstOrDefault();
            return !string.IsNullOrWhiteSpace(apiKey);
        }

        if (request.Query.TryGetValue("api_key", out var queryValues))
        {
            apiKey = queryValues.FirstOrDefault();
            return !string.IsNullOrWhiteSpace(apiKey);
        }

        apiKey = null;
        return false;
    }
}
