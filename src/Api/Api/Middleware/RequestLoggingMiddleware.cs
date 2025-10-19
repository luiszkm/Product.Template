using System.Diagnostics;
using System.Text;

namespace Product.Template.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly HashSet<string> _sensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "X-Api-Key",
        "Api-Key"
    };

    private readonly HashSet<string> _sensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "senha",
        "token",
        "secret",
        "apiKey",
        "api_key"
    };

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();

        // Log da requisição
        await LogRequest(context, correlationId);

        // Capturar o response body
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Log da resposta
            await LogResponse(context, correlationId, stopwatch.ElapsedMilliseconds);

            // Copiar o response de volta para o stream original
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task LogRequest(HttpContext context, string correlationId)
    {
        context.Request.EnableBuffering();

        var request = context.Request;
        var requestBody = await ReadRequestBody(request);

        var logData = new
        {
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            Headers = GetSafeHeaders(request.Headers),
            RemoteIp = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = request.Headers["User-Agent"].ToString(),
            Body = MaskSensitiveData(requestBody)
        };

        _logger.LogInformation(
            "HTTP Request: {Method} {Path} - CorrelationId: {CorrelationId}",
            request.Method,
            request.Path,
            correlationId);

        _logger.LogDebug("Request Details: {@RequestData}", logData);

        // Reset the request body stream position
        request.Body.Position = 0;
    }

    private async Task LogResponse(HttpContext context, string correlationId, long elapsedMs)
    {
        var response = context.Response;
        response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);

        var logData = new
        {
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            StatusCode = response.StatusCode,
            ElapsedMilliseconds = elapsedMs,
            Headers = GetSafeHeaders(response.Headers),
            Body = MaskSensitiveData(responseBody)
        };

        var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                       response.StatusCode >= 400 ? LogLevel.Warning :
                       LogLevel.Information;

        _logger.Log(
            logLevel,
            "HTTP Response: {StatusCode} - {ElapsedMs}ms - CorrelationId: {CorrelationId}",
            response.StatusCode,
            elapsedMs,
            correlationId);

        _logger.LogDebug("Response Details: {@ResponseData}", logData);
    }

    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        if (request.ContentLength == null || request.ContentLength == 0)
        {
            return string.Empty;
        }

        try
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
        catch
        {
            return "[Unable to read request body]";
        }
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) &&
            !string.IsNullOrEmpty(correlationId.ToString()))
        {
            return correlationId.ToString()!;
        }

        var newCorrelationId = Guid.NewGuid().ToString();
        context.Response.Headers.TryAdd("X-Correlation-ID", newCorrelationId);
        return newCorrelationId;
    }

    private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
    {
        return headers
            .Where(h => !_sensitiveHeaders.Contains(h.Key))
            .ToDictionary(
                h => h.Key,
                h => h.Value.ToString()
            );
    }

    private string MaskSensitiveData(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        // Limitar tamanho do log
        if (content.Length > 10000)
        {
            content = content.Substring(0, 10000) + "... [truncated]";
        }

        // Mascarar campos sensíveis (simplificado - em produção use regex mais robusto)
        foreach (var field in _sensitiveFields)
        {
            var patterns = new[]
            {
                $"\"{field}\"\\s*:\\s*\"[^\"]*\"",
                $"'{field}'\\s*:\\s*'[^']*'",
                $"{field}=[^&\\s]*"
            };

            foreach (var pattern in patterns)
            {
                content = System.Text.RegularExpressions.Regex.Replace(
                    content,
                    pattern,
                    $"\"{field}\": \"***MASKED***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }
        }

        return content;
    }
}
