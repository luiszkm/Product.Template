using Product.Template.Core.Authorization.Infrastructure;
using Product.Template.Core.Identity.Infrastructure;
using Product.Template.Core.Tenants.Infrastructure;

namespace Product.Template.Api.Configurations
{
    public static class CoreConfiguration
    {
        public static IServiceCollection AddUseCases(
            this IServiceCollection services)
        {
            services.AddIdentityInJections();
            services.AddAuthorizationModule();
            services.AddTenantsModule();
            services.AddHttpContextAccessor();
            return services;
        }
    }
}
