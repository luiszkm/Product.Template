namespace E2ETests.Common;

public sealed class PostgresE2EFixture : IAsyncLifetime
{
    public PostgresContainerFixture Postgres { get; } = new();
    public TestContainerWebApplicationFactory? Factory { get; private set; }
    public bool IsDockerAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await Postgres.InitializeAsync();
            Factory = new TestContainerWebApplicationFactory(Postgres);
            await Factory.InitializeAsync();
            IsDockerAvailable = true;
        }
        catch (Exception ex) when (ex.Message.Contains("Docker", StringComparison.OrdinalIgnoreCase) || ex is InvalidOperationException)
        {
            IsDockerAvailable = false;
            Factory = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();
        if (IsDockerAvailable)
            await Postgres.DisposeAsync();
    }
}
