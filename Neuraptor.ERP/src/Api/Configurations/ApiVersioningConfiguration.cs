using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Neuraptor.ERP.Api.Configurations;

public static class ApiVersioningConfiguration
{
    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Assume a versão padrão quando não especificada
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);

            // Reporta as versões suportadas no header
            options.ReportApiVersions = true;

            // Lê a versão do header E da URL
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version")
            );
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            // Formato da versão: 'v'major[.minor][-status]
            options.GroupNameFormat = "'v'VVV";

            // Substituir a versão na URL
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}

/// <summary>
/// Configura o Swagger para suportar múltiplas versões de API
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        // Adiciona um documento Swagger para cada versão descoberta
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "Product Template API",
            Version = description.ApiVersion.ToString(),
            Description = "API Template com Clean Architecture, DDD, CQRS e funcionalidades avançadas",
            Contact = new OpenApiContact
            {
                Name = "Neuraptor",
                Email = "contact@neuraptor.com"
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        };

        if (description.IsDeprecated)
        {
            info.Description += " <strong>Esta versão da API foi descontinuada.</strong>";
        }

        return info;
    }
}
