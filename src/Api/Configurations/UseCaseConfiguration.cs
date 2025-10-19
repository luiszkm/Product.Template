using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Api.Configurations
{
    public static class UseCaseConfiguration
    {
        public static IServiceCollection AddUseCases(
            this IServiceCollection services)
        {
            services.AddRepositories();
            return services;
        }

        private static IServiceCollection AddRepositories(
            this IServiceCollection services)
        {
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}

