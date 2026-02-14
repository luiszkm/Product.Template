using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Infrastructure.HostDb;
using Product.Template.Kernel.Infrastructure.MultiTenancy;
using Product.Template.Kernel.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("src/Api/appsettings.json", optional: true)
    .AddEnvironmentVariables();

var hostConn = builder.Configuration.GetConnectionString("HostDb")
    ?? builder.Configuration.GetConnectionString("AppDb")
    ?? "Data Source=host.db";

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ITenantStore, CachedTenantStore>();
builder.Services.AddScoped<ITenantConnectionStringResolver, TenantConnectionStringResolver>();

builder.Services.AddDbContext<HostDbContext>(options =>
{
    if (hostConn.Contains("Host=", StringComparison.OrdinalIgnoreCase)) options.UseNpgsql(hostConn);
    else options.UseSqlite(hostConn);
});

var app = builder.Build();

using var scope = app.Services.CreateScope();
var hostDb = scope.ServiceProvider.GetRequiredService<HostDbContext>();
await hostDb.Database.MigrateAsync();

var tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>();
var resolver = scope.ServiceProvider.GetRequiredService<ITenantConnectionStringResolver>();

var command = args.FirstOrDefault() ?? "migrate";
var tenantKey = args.SkipWhile(a => a != "--tenant").Skip(1).FirstOrDefault();

if (command != "migrate")
{
    Console.WriteLine("Usage: migrate --all | migrate --tenant <key>");
    return;
}

var tenants = string.IsNullOrWhiteSpace(tenantKey)
    ? await tenantStore.ListActiveAsync()
    : (await tenantStore.GetByKeyAsync(tenantKey) is { } single ? new[] { single } : Array.Empty<TenantConfig>());

foreach (var tenant in tenants)
{
    if (tenant.IsolationMode == TenantIsolationMode.SharedDb)
    {
        continue;
    }

    var conn = resolver.ResolveAppConnection(tenant);
    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
    if (conn.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        optionsBuilder.UseNpgsql(conn, npgsql =>
        {
            if (tenant.IsolationMode == TenantIsolationMode.SchemaPerTenant && !string.IsNullOrWhiteSpace(tenant.SchemaName))
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
            }
        });
    }
    else
    {
        optionsBuilder.UseSqlite(conn);
    }

    var tenantContext = new TenantContext();
    tenantContext.SetTenant(tenant);
    await using var db = new AppDbContext(optionsBuilder.Options, tenantContext);
    await db.Database.MigrateAsync();
    Console.WriteLine($"Migrated tenant {tenant.TenantKey} ({tenant.IsolationMode}).");
}

Console.WriteLine("Migration finished.");
