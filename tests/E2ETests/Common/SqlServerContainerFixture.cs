using Testcontainers.MsSql;

namespace E2ETests.Common;

/// <summary>
/// Starts a single SQL Server container shared across all E2E tests in the collection.
/// Using ICollectionFixture ensures the container is started once per test run, not per class.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("E2eTest@Strong123")
        .Build();

    public string HostDbConnectionString { get; private set; } = null!;
    public string AppDbConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var baseCs = _container.GetConnectionString();
        HostDbConnectionString = $"{baseCs};Initial Catalog=ProductTemplateHost_E2E";
        AppDbConnectionString  = $"{baseCs};Initial Catalog=ProductTemplateApp_E2E";
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>
{
    public const string Name = "SqlServer";
}
