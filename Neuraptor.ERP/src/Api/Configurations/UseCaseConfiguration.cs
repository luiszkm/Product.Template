using Neuraptor.ERP.Kernel.Application.Data;
using Neuraptor.ERP.Kernel.Infrastructure.Persistence;

namespace Neuraptor.ERP.Api.Configurations
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

