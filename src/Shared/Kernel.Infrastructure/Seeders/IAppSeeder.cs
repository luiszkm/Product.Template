using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Kernel.Infrastructure.Seeders;

public interface IAppSeeder
{
    Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default);
}
