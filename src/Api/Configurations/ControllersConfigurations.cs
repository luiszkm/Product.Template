using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Product.Template.Api.Configurations;

public static class ControllersConfigurations
{
    public static IServiceCollection AddControllersConfigurations(this IServiceCollection services, IHostEnvironment env)
    {
        services.AddControllers(options =>
            options.Filters.Add(typeof(Product.Template.Api.GlobalFilter.Exceptions.ApiGlobalExceptionFilter)));

        services.AddOpenApiDocumentation(env);

        return services;
    }

    private static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services,
        IHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return services;

        services.AddEndpointsApiExplorer();

        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer(new ApiInfoTransformer("v1", isDeprecated: false));
            options.AddDocumentTransformer<ServersTransformer>();

            // ✅ JWT Bearer (igual doc) + aplica requirement em todas operações
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

            // ✅ OAuth2 Microsoft para autenticação externa
            options.AddDocumentTransformer<OAuth2SecuritySchemeTransformer>();
        });

        return services;
    }

    public static WebApplication UseDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            app.MapOpenApi("/openapi/v1.json").AllowAnonymous();

            app.MapScalarApiReference(options =>
            {
                options.OpenApiRoutePattern = "/openapi/{documentName}.json";

                options
                    .WithTitle("Product API Documentation")
                    .WithTheme(ScalarTheme.DeepSpace)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .AddPreferredSecuritySchemes(new[] { "Bearer" });
            });
        }

        return app;
    }
}

// ===================== Transformers =====================

internal sealed class ApiInfoTransformer(string version, bool isDeprecated) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "Product Template API";
        document.Info.Version = version;

        document.Info.Description = isDeprecated
            ? "⚠️ Esta versão da API foi descontinuada."
            : """
              # 🚀 Product Template API

              API RESTful moderna em .NET 10 seguindo Clean Architecture.

              ## 🔐 Autenticação
              Authorization: Bearer {token}
              """;

        document.Info.Contact = new()
        {
            Name = "Luis Soares",
            Email = "luiszkm@gmail.com",
            Url = new Uri("https://github.com/luiszkm/Product.Template")
        };

        document.Info.License = new()
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        };

        return Task.CompletedTask;
    }
}

internal sealed class ServersTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Servers.Clear();
        document.Servers.Add(new() { Url = "https://localhost:7254", Description = "Desenvolvimento HTTPS" });
        document.Servers.Add(new() { Url = "http://localhost:5117", Description = "Desenvolvimento HTTP" });
        document.Servers.Add(new() { Url = "https://api-staging.exemplo.com", Description = "Homologação" });

        return Task.CompletedTask;
    }
}

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authenticationSchemes.Any(s => s.Name == "Bearer"))
            return;

        // 1) Security scheme no documento
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "🔐 Insira o token JWT obtido através do endpoint `/login`.\n\n" +
                              "**Fluxo de autenticação:**\n" +
                              "1. Registre um usuário via `POST /api/v1/Identity/register`\n" +
                              "2. Faça login via `POST /api/v1/Identity/login` para obter o token\n" +
                              "3. Cole apenas o valor do token (sem o prefixo 'Bearer') no campo abaixo\n" +
                              "4. Clique em 'Set Token' para autenticar suas requisições\n\n" +
                              "Exemplo de token:\n`eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`"
            }
        };

                // 2) Requirement em todas as operações (igual exemplo da doc)
                foreach (var operation in document.Paths.Values.SelectMany(p => p.Operations))
                {
                    operation.Value.Security ??= [];
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    });
                }
            }
        }

        /// <summary>
        /// Adiciona suporte a OAuth2 Microsoft no Scalar
        /// </summary>
        internal sealed class OAuth2SecuritySchemeTransformer : IOpenApiDocumentTransformer
        {
            public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                // Adicionar OAuth2 Microsoft
                document.Components.SecuritySchemes["OAuth2-Microsoft"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Description = "🔐 Autenticação via Microsoft / Azure AD / Entra ID\n\n" +
                                  "**Passo a passo:**\n" +
                                  "1. Clique em 'Authorize' e autentique com sua conta Microsoft\n" +
                                  "2. Após aprovação, o código será trocado por um token JWT\n" +
                                  "3. Use o token JWT no header Authorization das próximas requisições\n\n" +
                                  "**Observação:** Esta é a autenticação via Microsoft OAuth2. " +
                                  "O token final retornado é um JWT do sistema.",
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/authorize"),
                            TokenUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                ["openid"] = "OpenID Connect",
                                ["profile"] = "Informações do perfil",
                                ["email"] = "Endereço de email"
                            }
                        }
                    }
                };

                return Task.CompletedTask;
            }
        }

