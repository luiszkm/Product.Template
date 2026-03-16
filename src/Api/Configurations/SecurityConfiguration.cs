using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Product.Template.Kernel.Application.Security;

namespace Product.Template.Api.Configurations;

public static class SecurityConfiguration
{
    public const string AuthenticatedPolicy = "Authenticated";
    public const string AdminOnlyPolicy = "AdminOnly";
    public const string UserOnlyPolicy = "UserOnly";
    public const string UsersReadPolicy = "UsersRead";
    public const string UsersManagePolicy = "UsersManage";
    public const string UserReadOrSelfPolicy = "UserReadOrSelf";
    public const string UserManageOrSelfPolicy = "UserManageOrSelf";

    public const string PermissionClaimType = AuthorizationClaimTypes.Permission;

    private const string DefaultCorsPolicyName = "DefaultCorsPolicy";

    public static IServiceCollection AddSecurityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.AddCorsFromConfiguration(configuration);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthenticatedPolicy, policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(AdminOnlyPolicy, policy => policy.RequireRole("Admin"));
            options.AddPolicy(UserOnlyPolicy, policy => policy.RequireRole("User", "Admin", "Manager"));
            options.AddPolicy(UsersReadPolicy, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Admin") ||
                    context.User.HasClaim(PermissionClaimType, "users.read")));
            options.AddPolicy(UsersManagePolicy, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Admin") ||
                    context.User.HasClaim(PermissionClaimType, "users.manage")));

            options.AddPolicy(UserReadOrSelfPolicy, policy =>
                policy.AddRequirements(new Authorization.UserOwnershipRequirement("users.read")));

            options.AddPolicy(UserManageOrSelfPolicy, policy =>
                policy.AddRequirements(new Authorization.UserOwnershipRequirement("users.manage")));
        });

        services.AddSingleton<IAuthorizationHandler, Authorization.UserOwnershipHandler>();

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

                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role
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


        return services;
    }

    public static WebApplication UseSecurityConfiguration(this WebApplication app)
    {
        app.UseCors(DefaultCorsPolicyName);


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
