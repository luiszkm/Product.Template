using Kernel.Application.Security;
using Kernel.Infrastructure.Configurations;
using Kernel.Infrastructure.Persistence.Interceptors;
using Kernel.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Application.Security;

namespace Kernel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKernelInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddJwtConfiguration(configuration);

        services.AddAuthenticationProviders(configuration);

        services.AddScoped<IHashServices, HashServices>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<AuditLogInterceptor>();

        return services;
    }
}
