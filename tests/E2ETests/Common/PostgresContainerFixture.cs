using Npgsql;
using Testcontainers.PostgreSql;

namespace E2ETests.Common;

/// <summary>
/// Starts a single PostgreSQL container shared across all E2E tests in the collection.
/// Using ICollectionFixture ensures the container is started once per test run, not per class.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .WithPassword("E2eTest@Strong123")
        .Build();

    public string HostDbConnectionString { get; private set; } = null!;
    public string AppDbConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var baseCs = _container.GetConnectionString();

        var hostBuilder = new NpgsqlConnectionStringBuilder(baseCs) { Database = "ProductTemplateHost_E2E" };
        HostDbConnectionString = hostBuilder.ConnectionString;

        var appBuilder = new NpgsqlConnectionStringBuilder(baseCs) { Database = "ProductTemplateApp_E2E" };
        AppDbConnectionString = appBuilder.ConnectionString;
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres";
}
