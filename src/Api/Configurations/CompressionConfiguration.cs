using System.IO.Compression;
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
}

