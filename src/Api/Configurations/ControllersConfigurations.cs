using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Product.Template.Api.Configurations;

public static class ControllersConfigurations
{
    public static IServiceCollection AddControllersConfigurations(
        this IServiceCollection services)
    {
        services.AddControllers(options =>
            options.Filters.Add(typeof(Product.Template.Api.GlobalFilter.Exceptions.ApiGlobalExceptionFilter)));

        services.AddDocumentation();

        return services;
    }

    private static IServiceCollection AddDocumentation(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        services.AddSwaggerGen(options =>
        {
            // Adicionar autenticação JWT no Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Bearer {token}\""
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Comentários XML
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Ordenar por nome
            options.OrderActionsBy(apiDesc => apiDesc.RelativePath);

            // Exemplos personalizados
            options.EnableAnnotations();
        });

        return services;
    }

    public static WebApplication UseDocumentation(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();

            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            app.UseSwaggerUI(options =>
            {
                // Cria um endpoint Swagger para cada versão descoberta
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
                }

                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Product Template API";
                options.DisplayRequestDuration();
                options.EnableTryItOutByDefault();
                options.EnableDeepLinking();
            });
        }

        return app;
    }
}

