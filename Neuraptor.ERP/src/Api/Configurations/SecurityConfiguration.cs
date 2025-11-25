using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Neuraptor.ERP.Api.Configurations;

public static class SecurityConfiguration
{
    public static IServiceCollection AddSecurityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // CORS Configuration
        services.AddCors(options =>
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                                 ?? new[] { "*" };

            var allowedMethods = configuration.GetSection("Cors:AllowedMethods").Get<string[]>()
                                 ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };

            var allowedHeaders = configuration.GetSection("Cors:AllowedHeaders").Get<string[]>()
                                 ?? new[] { "*" };

            options.AddPolicy("DefaultCorsPolicy", builder =>
            {
                if (allowedOrigins.Contains("*"))
                {
                    builder.AllowAnyOrigin();
                }
                else
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowCredentials();
                }

                if (allowedMethods.Contains("*"))
                {
                    builder.AllowAnyMethod();
                }
                else
                {
                    builder.WithMethods(allowedMethods);
                }

                if (allowedHeaders.Contains("*"))
                {
                    builder.AllowAnyHeader();
                }
                else
                {
                    builder.WithHeaders(allowedHeaders);
                }

                builder.WithExposedHeaders("X-Correlation-ID", "X-Pagination");
            });
        });

        // JWT Authentication
        var jwtEnabled = configuration.GetValue<bool>("Jwt:Enabled", false);

        if (jwtEnabled)
        {
            var jwtSecret = configuration["Jwt:Secret"];
            var jwtIssuer = configuration["Jwt:Issuer"];
            var jwtAudience = configuration["Jwt:Audience"];
            var jwtExpirationMinutes = configuration.GetValue<int>("Jwt:ExpirationMinutes", 60);

            if (string.IsNullOrEmpty(jwtSecret))
            {
                throw new InvalidOperationException(
                    "JWT Secret is required when JWT authentication is enabled. " +
                    "Please configure 'Jwt:Secret' in appsettings.json or environment variables.");
            }

            var key = Encoding.ASCII.GetBytes(jwtSecret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Remove o delay padrão de 5 minutos
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();

                        logger.LogWarning(
                            "JWT Authentication failed: {Error}",
                            context.Exception.Message);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();

                        var userId = context.Principal?.Identity?.Name;
                        logger.LogInformation(
                            "JWT Token validated for user: {UserId}",
                            userId);

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                // Política padrão - requer autenticação
                options.AddPolicy("Authenticated", policy =>
                    policy.RequireAuthenticatedUser());

                // Política baseada em claims - exemplo
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireClaim("role", "admin"));

                options.AddPolicy("UserOnly", policy =>
                    policy.RequireClaim("role", "user", "admin"));
            });
        }

        return services;
    }

    public static WebApplication UseSecurityConfiguration(this WebApplication app)
    {
        // CORS deve vir antes de Authentication e Authorization
        app.UseCors("DefaultCorsPolicy");

        var jwtEnabled = app.Configuration.GetValue<bool>("Jwt:Enabled", false);
        if (jwtEnabled)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        return app;
    }
}
