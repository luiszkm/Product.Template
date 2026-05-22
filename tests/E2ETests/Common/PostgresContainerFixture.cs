using Npgsql;
using Testcontainers.PostgreSql;

namespace E2ETests.Common;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string HostDbConnectionString { get; private set; } = null!;
    public string AppDbConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        if (!File.Exists("/var/run/docker.sock"))
            throw new InvalidOperationException("DockerUnavailable");

        _container = new PostgreSqlBuilder("postgres:17")
            .WithPassword("E2eTest@Strong123")
            .Build();

        await _container.StartAsync();
        var baseCs = _container.GetConnectionString();

        var hostBuilder = new NpgsqlConnectionStringBuilder(baseCs) { Database = "ProductTemplateHost_E2E" };
        HostDbConnectionString = hostBuilder.ConnectionString;

        var appBuilder = new NpgsqlConnectionStringBuilder(baseCs) { Database = "ProductTemplateApp_E2E" };
        AppDbConnectionString = appBuilder.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresE2EFixture>
{
    public const string Name = "Postgres";
}
