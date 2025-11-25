using Microsoft.EntityFrameworkCore;

namespace Neuraptor.ERP.Api.Configurations
{
    public static class ConnectionsConfigurations
    {
        public static IServiceCollection AddAppConnections(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.AddTestConnections();

            return services;
        }

        public static IServiceCollection AddTestConnections(
            this IServiceCollection services)
        {
            services.AddDbContext<Neuraptor.ERP.Kernel.Infrastructure.Persistence.AppDbContext>(
                options => options.UseInMemoryDatabase("e2e-tests-db"));

            return services;
        }

    }
}

