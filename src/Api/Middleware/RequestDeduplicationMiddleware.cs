using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace Product.Template.Api.Middleware;

/// <summary>
/// Middleware para prevenir requisições duplicadas (idempotência)
/// </summary>
public class RequestDeduplicationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RequestDeduplicationMiddleware> _logger;
    private static readonly TimeSpan _deduplicationWindow = TimeSpan.FromMinutes(5);

    public RequestDeduplicationMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RequestDeduplicationMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Apenas para POST, PUT, PATCH (operações que modificam estado)
        if (!ShouldCheckDuplication(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Verificar se há X-Idempotency-Key no header
        var idempotencyKey = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(idempotencyKey))
        {
            // Se não houver chave, gera uma baseada no conteúdo da requisição
            idempotencyKey = await GenerateRequestHashAsync(context);
        }

        var cacheKey = $"dedup:{idempotencyKey}";

        // Verificar se já existe uma requisição com essa chave
        if (_cache.TryGetValue(cacheKey, out DeduplicationEntry? existingEntry))
        {
            _logger.LogWarning(
                "Requisição duplicada detectada. Idempotency-Key: {IdempotencyKey}, Path: {Path}",
                idempotencyKey,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.Headers["X-Duplicate-Request"] = "true";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Duplicate request detected",
                message = "Esta requisição já foi processada recentemente. Por favor, aguarde antes de tentar novamente.",
                idempotencyKey,
                originalRequestTime = existingEntry?.Timestamp
            });

            return;
        }

        // Registrar a requisição no cache
        var entry = new DeduplicationEntry
        {
            IdempotencyKey = idempotencyKey,
            Timestamp = DateTime.UtcNow,
            Method = context.Request.Method,
            Path = context.Request.Path
        };

        _cache.Set(cacheKey, entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _deduplicationWindow
        });

        _logger.LogDebug(
            "Requisição registrada para deduplicação. Key: {IdempotencyKey}",
            idempotencyKey);

        await _next(context);
    }

    private static bool ShouldCheckDuplication(string method)
    {
        return method == HttpMethods.Post ||
               method == HttpMethods.Put ||
               method == HttpMethods.Patch;
    }

    private async Task<string> GenerateRequestHashAsync(HttpContext context)
    {
        // Criar hash baseado em: método + path + body (se houver)
        var sb = new StringBuilder();
        sb.Append(context.Request.Method);
        sb.Append(context.Request.Path);
        sb.Append(context.Request.QueryString);

        // Adicionar corpo da requisição ao hash (se houver)
        if (context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            sb.Append(body);

            // Reset the stream position
            context.Request.Body.Position = 0;
        }

        // Gerar hash SHA256
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToBase64String(hashBytes);
    }

    private class DeduplicationEntry
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}

