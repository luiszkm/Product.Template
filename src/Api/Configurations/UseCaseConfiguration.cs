using Product.Template.Kernel.Application.Data;
using Product.Template.Kernel.Infrastructure.Persistence;
using Product.Template.Core.Identity.Domain.Repositories;
using Product.Template.Core.Identity.Infrastructure.Data.Persistence;

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

            // Identity Repositories
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IRoleRepository, RoleRepository>();

            return services;
        }
    }
}

