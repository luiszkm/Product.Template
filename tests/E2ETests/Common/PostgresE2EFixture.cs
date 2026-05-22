namespace E2ETests.Common;

public sealed class PostgresE2EFixture : IAsyncLifetime
{
    public PostgresContainerFixture Postgres { get; } = new();
    public TestContainerWebApplicationFactory? Factory { get; private set; }
    public bool IsDockerAvailable { get; private set; }
    public string? UnavailableReason { get; private set; }

    public bool TryEnsureAvailable()
    {
        if (IsDockerAvailable && Factory is not null)
            return true;

        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Fail(
                $"PostgreSQL E2E tests require Docker in CI. IsDockerAvailable={IsDockerAvailable}, Factory={(Factory is null ? "null" : "ready")}. {UnavailableReason ?? "Docker or Testcontainers startup failed."}");
        }

        return false;
    }

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
            UnavailableReason = ex.Message;
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
