using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Product.Template.Kernel.Infrastructure.HostDb;

/// <summary>
/// Enables EF design-time tools (dotnet ef migrations add) to instantiate HostDbContext
/// without the full DI pipeline. Uses SQLite so no infrastructure is required.
/// </summary>
public class HostDbContextDesignTimeFactory : IDesignTimeDbContextFactory<HostDbContext>
{
    public HostDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HostDbContext>()
            .UseSqlite("Data Source=design-time-host.db")
            .Options;

        return new HostDbContext(options);
    }
}
