using System.IO.Compression;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;

namespace Product.Template.Api.Configurations;

/// <summary>
/// Configuração de compressão de respostas HTTP
/// </summary>
public static class CompressionConfiguration
{
    public static IServiceCollection AddCompressionConfiguration(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

            // Tipos MIME que devem ser comprimidos
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/xml",
                "text/plain",
                "text/css",
                "text/html",
                "application/javascript",
                "text/javascript"
            });
        });

        // Brotli (melhor compressão, mas mais lento)
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        // Gzip (compressão média, mais rápido)
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.SmallestSize;
        });

        return services;
    }

    // Endpoints de auth retornam AccessToken/RefreshToken (secret) junto com dados do corpo
    // da requisição (email, etc.) na mesma resposta — combinação clássica de risco BREACH sob
    // compressão HTTPS. Excluídos da compressão como mitigação defensiva.
    private static readonly string[] CompressionExcludedPaths =
    [
        "/identity/login",
        "/identity/refresh",
        "/identity/register",
        "/identity/external-login"
    ];

    public static IApplicationBuilder UseCompressionConfiguration(this IApplicationBuilder app)
    {
        app.Use((context, next) =>
        {
            var path = context.Request.Path.Value;
            if (path is not null && CompressionExcludedPaths.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                var compressionFeature = context.Features.Get<IHttpsCompressionFeature>();
                if (compressionFeature is not null)
                    compressionFeature.Mode = HttpsCompressionMode.DoNotCompress;
            }

            return next();
        });

        app.UseResponseCompression();

        return app;
    }
}

