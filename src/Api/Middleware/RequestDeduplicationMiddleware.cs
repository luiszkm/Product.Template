using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Product.Template.Api.Middleware;

public class RequestDeduplicationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RequestDeduplicationMiddleware> _logger;
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromSeconds(1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RequestDeduplicationMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        ILogger<RequestDeduplicationMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldCheckDuplication(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey))
            idempotencyKey = await GenerateRequestHashAsync(context);

        var cacheKey = $"dedup:{idempotencyKey}";
        var inFlightKey = $"dedup:processing:{idempotencyKey}";
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DeduplicationWindow
        };

        var existingPayload = await _cache.GetStringAsync(cacheKey, context.RequestAborted);
        if (existingPayload is not null)
        {
            var existingEntry = JsonSerializer.Deserialize<DeduplicationEntry>(existingPayload, JsonOptions);
            await WriteDuplicateResponseAsync(context, idempotencyKey, existingEntry?.Timestamp);
            return;
        }

        var inFlight = await _cache.GetStringAsync(inFlightKey, context.RequestAborted);
        if (inFlight is not null)
        {
            await WriteDuplicateResponseAsync(context, idempotencyKey, null, processing: true);
            return;
        }

        await _cache.SetStringAsync(inFlightKey, "1", cacheOptions, context.RequestAborted);

        try
        {
            await _next(context);

            if (context.Response.StatusCode >= StatusCodes.Status200OK &&
                context.Response.StatusCode < StatusCodes.Status300MultipleChoices)
            {
                var entry = new DeduplicationEntry
                {
                    IdempotencyKey = idempotencyKey,
                    Timestamp = DateTime.UtcNow,
                    Method = context.Request.Method,
                    Path = context.Request.Path.Value ?? string.Empty
                };

                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(entry, JsonOptions),
                    cacheOptions,
                    context.RequestAborted);

                _logger.LogDebug("Requisição registrada para deduplicação. Key: {IdempotencyKey}", idempotencyKey);
            }
        }
        finally
        {
            await _cache.RemoveAsync(inFlightKey, context.RequestAborted);
        }
    }

    private async Task WriteDuplicateResponseAsync(
        HttpContext context,
        string idempotencyKey,
        DateTime? originalRequestTime,
        bool processing = false)
    {
        _logger.LogWarning(
            "Requisição duplicada detectada. Idempotency-Key: {IdempotencyKey}, Path: {Path}, Processing: {Processing}",
            idempotencyKey,
            context.Request.Path,
            processing);

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.Headers["X-Duplicate-Request"] = "true";

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Duplicate request detected",
            message = processing
                ? "Esta requisição já está a ser processada. Por favor, aguarde antes de tentar novamente."
                : "Esta requisição já foi processada recentemente. Por favor, aguarde antes de tentar novamente.",
            idempotencyKey,
            originalRequestTime
        });
    }

    private static bool ShouldCheckDuplication(string method) =>
        method is HttpMethods.Post or HttpMethods.Put or HttpMethods.Patch;

    private static async Task<string> GenerateRequestHashAsync(HttpContext context)
    {
        var sb = new StringBuilder();
        sb.Append(context.Request.Method);
        sb.Append(context.Request.Path);
        sb.Append(context.Request.QueryString);

        if (context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            sb.Append(body);
            context.Request.Body.Position = 0;
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToBase64String(hashBytes);
    }

    private sealed class DeduplicationEntry
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}
