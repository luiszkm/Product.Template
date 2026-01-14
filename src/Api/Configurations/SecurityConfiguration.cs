using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Product.Template.Api.Configurations;

public static class SecurityConfiguration
{
    private const string DefaultCorsPolicyName = "DefaultCorsPolicy";

    public static IServiceCollection AddSecurityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.AddCorsFromConfiguration(configuration);

        if (!configuration.GetValue<bool>("Jwt:Enabled"))
            return services;

        var jwt = JwtOptions.From(configuration);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // "Bearer"
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Em dev, permitir HTTP local sem travar o pipeline (recomendado)
                options.RequireHttpsMetadata = !env.IsDevelopment();

                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.SigningKey),

                    ValidateIssuer = jwt.ValidateIssuer,
                    ValidIssuer = jwt.Issuer,

                    ValidateAudience = jwt.ValidateAudience,
                    ValidAudience = jwt.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("JWT Authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        var userId = context.Principal?.Identity?.Name;
                        logger.LogInformation("JWT Token validated for user: {UserId}", userId);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("AdminOnly", policy => policy.RequireClaim("role", "admin"));
            options.AddPolicy("UserOnly", policy => policy.RequireClaim("role", "user", "admin"));
        });

        return services;
    }

    public static WebApplication UseSecurityConfiguration(this WebApplication app)
    {
        app.UseCors(DefaultCorsPolicyName);

        if (app.Configuration.GetValue<bool>("Jwt:Enabled"))
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        return app;
    }

    // ===================== Helpers =====================

    private static IServiceCollection AddCorsFromConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"];
            var allowedMethods = configuration.GetSection("Cors:AllowedMethods").Get<string[]>() ?? ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"];
            var allowedHeaders = configuration.GetSection("Cors:AllowedHeaders").Get<string[]>() ?? ["*"];

            options.AddPolicy(DefaultCorsPolicyName, builder =>
            {
                // Origins
                if (allowedOrigins.Contains("*"))
                {
                    builder.AllowAnyOrigin();
                }
                else
                {
                    builder.WithOrigins(allowedOrigins).AllowCredentials();
                }

                // Methods
                if (allowedMethods.Contains("*"))
                    builder.AllowAnyMethod();
                else
                    builder.WithMethods(allowedMethods);

                // Headers
                if (allowedHeaders.Contains("*"))
                    builder.AllowAnyHeader();
                else
                    builder.WithHeaders(allowedHeaders);

                builder.WithExposedHeaders("X-Correlation-ID", "X-Pagination");
            });
        });

        return services;
    }

    private sealed record JwtOptions(byte[] SigningKey, string? Issuer, string? Audience, bool ValidateIssuer, bool ValidateAudience)
    {
        public static JwtOptions From(IConfiguration configuration)
        {
            var secret = configuration["Jwt:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    "JWT Secret is required when JWT authentication is enabled. " +
                    "Configure 'Jwt:Secret' in appsettings.json or environment variables.");
            }

            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];

            return new JwtOptions(
                SigningKey: Encoding.UTF8.GetBytes(secret),
                Issuer: issuer,
                Audience: audience,
                ValidateIssuer: !string.IsNullOrWhiteSpace(issuer),
                ValidateAudience: !string.IsNullOrWhiteSpace(audience)
            );
        }
    }
}
