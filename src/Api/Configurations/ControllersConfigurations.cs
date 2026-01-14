using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

namespace Product.Template.Api.Configurations;

public static class ControllersConfigurations
{
    public static IServiceCollection AddControllersConfigurations(
        this IServiceCollection services)
    {
        services.AddControllers(options =>
            options.Filters.Add(typeof(Product.Template.Api.GlobalFilter.Exceptions.ApiGlobalExceptionFilter)));

        services.AddOpenApiDocumentation();

        return services;
    }

    private static IServiceCollection AddOpenApiDocumentation(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        // ⚠️ Nota de Arquiteto: BuildServiceProvider aqui é aceitável para configuração de startup,
        // mas evite usá-lo dentro de métodos de requisição para não gerar "Memory Leaks" ou antipatterns.
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in provider.ApiVersionDescriptions)
        {
            services.AddOpenApi(description.GroupName, options =>
            {
                // ========================================
                // 1️⃣ Transformer: Metadados da API
                // ========================================
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info.Title = "Product Template API";
                    document.Info.Version = description.ApiVersion.ToString();
                    document.Info.Description = description.IsDeprecated
                        ? "⚠️ **Esta versão da API foi descontinuada.** Por favor, migre para a versão mais recente."
                        : """
                        # 🚀 Product Template API
                        
                        API RESTful moderna construída com .NET 10 seguindo princípios de **Clean Architecture**.
                        
                        ## 📌 Recursos Principais
                        - ✅ Autenticação JWT
                        - ✅ Rate Limiting
                        - ✅ Versionamento de API
                        - ✅ Health Checks
                        - ✅ OpenTelemetry (Tracing & Metrics)
                        - ✅ Retry Policies com Polly
                        
                        ## 🔐 Autenticação
                        Para endpoints protegidos, use o header:
                        ```
                        Authorization: Bearer {seu-token-jwt}
                        ```
                        
                        Obtenha o token através do endpoint `/api/v1/identity/login`.
                        """;

                    document.Info.Contact = new()
                    {
                        Name = "Product Team",
                        Email = "template@neuraptor.com",
                        Url = new Uri("https://github.com/luiszkm/Product.Template")
                    };

                    document.Info.License = new()
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    };

                    return Task.CompletedTask;
                });

                // ========================================
                // 2️⃣ Transformer: Servidores (Environments)
                // ========================================
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    // Limpa servidores padrão e adiciona personalizados
                    document.Servers.Clear();
                    document.Servers.Add(new() { Url = "https://localhost:7254", Description = "🏠 Desenvolvimento (HTTPS)" });
                    document.Servers.Add(new() { Url = "http://localhost:5117", Description = "🏠 Desenvolvimento (HTTP)" });
                    document.Servers.Add(new() { Url = "https://api-staging.exemplo.com", Description = "🧪 Staging" });
                    document.Servers.Add(new() { Url = "https://api.exemplo.com", Description = "🚀 Produção" });

                    return Task.CompletedTask;
                });

            });
        }

        return services;
    }

    public static WebApplication UseDocumentation(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            // 1. Gera os endpoints JSON (/openapi/v1.json, etc)
            foreach (var description in provider.ApiVersionDescriptions)
            {
                app.MapOpenApi($"/openapi/{description.GroupName}.json")
                    .AllowAnonymous();
            }

            // ========================================
            // 2. Configuração Avançada do Scalar UI
            // ========================================
            app.MapScalarApiReference(options =>
            {
                // 🎨 Temas Disponíveis:
                // - ScalarTheme.Default (Claro/Escuro automático)
                // - ScalarTheme.DeepSpace (Escuro profundo - ATUAL)
                // - ScalarTheme.Saturn (Roxo escuro)
                // - ScalarTheme.BluePlanet (Azul)
                // - ScalarTheme.Mars (Laranja/Vermelho)
                options
                    .WithTitle("Product API Documentation")
                    .WithTheme(ScalarTheme.DeepSpace)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithSidebar(true)
                    .WithModels(true)
                    .WithDownloadButton(true)
                    .WithSearchHotKey("k")
                    .WithPreferredScheme("https")
                    .WithDefaultOpenAllTags();

                // 🔗 Configura documentos OpenAPI para cada versão
                // O Scalar automaticamente detecta /openapi/v{version}.json
            });
        }

        return app;
    }
}
