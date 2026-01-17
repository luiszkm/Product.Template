using Product.Template.Core.Identity.Infrastructure;

namespace Product.Template.Api.Configurations
{
    public static class CoreConfiguration
    {
        public static IServiceCollection AddUseCases(
            this IServiceCollection services)
        {
            services.AddIdentityInJections();
            // HttpContextAccessor para CurrentUserService
            services.AddHttpContextAccessor();
            return services;
        }
    }
}

