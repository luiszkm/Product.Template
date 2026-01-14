using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
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
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;

                // Transformer 1: Metadados
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info.Title = "Product Template API";
                    document.Info.Version = description.ApiVersion.ToString();
                    document.Info.Description = description.IsDeprecated
                        ? "<strong>Esta versão da API foi descontinuada.</strong>"
                        : "API moderna utilizando .NET Native OpenAPI com Clean Architecture.";

                    document.Info.Contact = new OpenApiContact
                    {
                        Name = "Product Team",
                        Email = "template@neuraptor.com"
                    };

                    document.Info.License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT")
                    };

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
                app.MapOpenApi($"/openapi/{description.GroupName}.json");
            }

            // 2. Configura a UI do Scalar
            app.MapScalarApiReference(options =>
            {
                options.Title = "Product API Documentation";
                options.Theme = ScalarTheme.DeepSpace;
                options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

            });
        }

        return app;
    }
}

