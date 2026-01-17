using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Product.Template.Core.Identity.Infrastructure.Data.Seeders;
using Product.Template.Kernel.Infrastructure.Persistence;
using Kernel.Infrastructure.Persistence.Interceptors;

namespace Product.Template.Core.Identity.Infrastructure.Data;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection services,
        string databaseName = "ProductTemplateDb")
    {
        // Configure InMemory Database
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(databaseName);
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();

            // Adicionar interceptor de auditoria
            var auditInterceptor = sp.GetService<AuditableEntityInterceptor>();
            if (auditInterceptor != null)
            {
                options.AddInterceptors(auditInterceptor);
            }
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed data
        await RoleSeeder.SeedAsync(context);
        await UserSeeder.SeedAsync(context);
    }
}
