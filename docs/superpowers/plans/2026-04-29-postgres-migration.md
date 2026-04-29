# PostgreSQL Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace SQL Server with PostgreSQL 17 across all infrastructure, docker, and test projects using idiomatic Npgsql/EF Core 10 configuration.

**Architecture:** Three-layer swap — NuGet packages first, then provider configuration and raw ADO.NET code, then docker/config/tests last. Migrations are deleted and regenerated from scratch after all code changes are in place.

**Tech Stack:** .NET 10, EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.*, Testcontainers.PostgreSql, PostgreSQL 17.

---

## Task 1: Swap NuGet packages

**Files:**
- Modify: `src/Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj`
- Modify: `src/Tools/Migrator/Migrator.csproj`
- Modify: `tests/E2ETests/E2ETests.csproj`

- [ ] **Step 1: Replace SqlServer package in Kernel.Infrastructure.csproj**

Open `src/Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj` and replace:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.*" />
```
with:
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
```

- [ ] **Step 2: Replace SqlServer package in Migrator.csproj**

Open `src/Tools/Migrator/Migrator.csproj` and replace:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.*" />
```
with:
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
```

- [ ] **Step 3: Replace MsSql testcontainer in E2ETests.csproj**

Open `tests/E2ETests/E2ETests.csproj` and replace:
```xml
<PackageReference Include="Testcontainers.MsSql" Version="4.*" />
```
with:
```xml
<PackageReference Include="Testcontainers.PostgreSql" Version="4.*" />
```

- [ ] **Step 4: Restore packages**

```bash
dotnet restore
```

Expected: `Restore completed` with no errors. If Npgsql 10.0.* is not yet on NuGet, use the latest `9.0.*` — check with `dotnet package search Npgsql.EntityFrameworkCore.PostgreSQL`.

- [ ] **Step 5: Commit**

```bash
git add src/Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj \
        src/Tools/Migrator/Migrator.csproj \
        tests/E2ETests/E2ETests.csproj
git commit -m "chore: swap SQL Server NuGet packages for Npgsql and PostgreSql Testcontainers"
```

---

## Task 2: Add global Npgsql timestamp switch to Program.cs

**Files:**
- Modify: `src/Api/Program.cs`

- [ ] **Step 1: Add the AppContext switch before any service registration**

Open `src/Api/Program.cs`. At the very top, before `var builder = WebApplication.CreateBuilder(args);`, add:

```csharp
// Enforce UTC timestamps — Npgsql rejects DateTime without DateTimeKind.Utc otherwise.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);
```

The top of the file should look like:

```csharp
using Microsoft.AspNetCore.HttpOverrides;
using Product.Template.Api.Configurations;
using Product.Template.Api.Middleware;
using Product.Template.Core.Identity.Infrastructure.Data;
using Product.Template.Kernel.Infrastructure.MultiTenancy;

// Enforce UTC timestamps — Npgsql rejects DateTime without DateTimeKind.Utc otherwise.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

var builder = WebApplication.CreateBuilder(args);
```

- [ ] **Step 2: Build to verify no compile errors**

```bash
dotnet build src/Api/Api.csproj
```

Expected: `Build succeeded` (will have errors about UseSqlServer — those are fixed in Task 3).

- [ ] **Step 3: Commit**

```bash
git add src/Api/Program.cs
git commit -m "chore: add Npgsql.EnableLegacyTimestampBehavior=false to enforce UTC timestamps"
```

---

## Task 3: Replace UseSqlServer with UseNpgsql in DbContext configuration

**Files:**
- Modify: `src/Core/Identity/Identity.Infrastructure/Data/DatabaseConfiguration.cs`
- Modify: `src/Api/AppDbContextDesignTimeFactory.cs`
- Modify: `src/Shared/Kernel.Infrastructure/HostDb/HostDbContextDesignTimeFactory.cs`

- [ ] **Step 1: Update DatabaseConfiguration.cs — HostDbContext**

In `src/Core/Identity/Identity.Infrastructure/Data/DatabaseConfiguration.cs`, replace:
```csharp
options.UseSqlServer(hostConnection);
```
with:
```csharp
options.UseNpgsql(hostConnection);
```

- [ ] **Step 2: Update DatabaseConfiguration.cs — AppDbContext**

In the same file, replace:
```csharp
options.UseSqlServer(appConnection, sqlServer =>
{
    if (tenant.IsolationMode == TenantIsolationMode.SchemaPerTenant && !string.IsNullOrWhiteSpace(tenant.SchemaName))
    {
        sqlServer.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
    }
});
```
with:
```csharp
options.UseNpgsql(appConnection, npgsql =>
{
    if (tenant.IsolationMode == TenantIsolationMode.SchemaPerTenant && !string.IsNullOrWhiteSpace(tenant.SchemaName))
    {
        npgsql.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
    }
});
```

- [ ] **Step 3: Update the using directive in DatabaseConfiguration.cs**

At the top of `DatabaseConfiguration.cs`, the `Microsoft.EntityFrameworkCore` using is already present (covers `UseNpgsql` extension from Npgsql package). No new using needed — Npgsql's extension method lives in `Microsoft.EntityFrameworkCore` namespace. Verify the using list doesn't import `Microsoft.Data.SqlClient` — if it does, remove it.

- [ ] **Step 4: Update AppDbContextDesignTimeFactory.cs**

In `src/Api/AppDbContextDesignTimeFactory.cs`, replace:
```csharp
builder.UseSqlServer(connectionString);
```
with:
```csharp
builder.UseNpgsql(connectionString);
```

- [ ] **Step 5: Update HostDbContextDesignTimeFactory.cs**

In `src/Shared/Kernel.Infrastructure/HostDb/HostDbContextDesignTimeFactory.cs`, replace:
```csharp
builder.UseSqlServer(connectionString);
```
with:
```csharp
builder.UseNpgsql(connectionString);
```

- [ ] **Step 6: Build to verify**

```bash
dotnet build src/Api/Api.csproj
```

Expected: `Build succeeded` with 0 errors. Warnings about remaining SqlConnection in TenantProvisioningService are fine — fixed in Task 4.

- [ ] **Step 7: Commit**

```bash
git add src/Core/Identity/Identity.Infrastructure/Data/DatabaseConfiguration.cs \
        src/Api/AppDbContextDesignTimeFactory.cs \
        src/Shared/Kernel.Infrastructure/HostDb/HostDbContextDesignTimeFactory.cs
git commit -m "chore: replace UseSqlServer with UseNpgsql in all DbContext configuration"
```

---

## Task 4: Replace SqlConnection with NpgsqlConnection in TenantProvisioningService

**Files:**
- Modify: `src/Shared/Kernel.Infrastructure/MultiTenancy/TenantProvisioningService.cs`

- [ ] **Step 1: Replace the using directive**

In `src/Shared/Kernel.Infrastructure/MultiTenancy/TenantProvisioningService.cs`, replace:
```csharp
using Microsoft.Data.SqlClient;
```
with:
```csharp
using Npgsql;
```

- [ ] **Step 2: Replace the raw ADO.NET schema creation block**

Replace:
```csharp
var sharedConn = connectionStringResolver.ResolveAppConnection(new TenantConfig { IsolationMode = TenantIsolationMode.SharedDb });
await using var connection = new SqlConnection(sharedConn);
await connection.OpenAsync(cancellationToken);
await using var cmd = connection.CreateCommand();
cmd.CommandText = $"""
    IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{tenant.SchemaName}')
        EXEC('CREATE SCHEMA [{tenant.SchemaName}]');
    """;
await cmd.ExecuteNonQueryAsync(cancellationToken);
```
with:
```csharp
var sharedConn = connectionStringResolver.ResolveAppConnection(new TenantConfig { IsolationMode = TenantIsolationMode.SharedDb });
await using var connection = new NpgsqlConnection(sharedConn);
await connection.OpenAsync(cancellationToken);
await using var cmd = connection.CreateCommand();
cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{tenant.SchemaName}\"";
await cmd.ExecuteNonQueryAsync(cancellationToken);
```

- [ ] **Step 3: Update the default DedicatedDb connection string template**

In the same file, the `CreateTenantAsync` method builds a default connection string for `DedicatedDb` mode. Replace the SQL Server format:
```csharp
ConnectionString = isolationMode == TenantIsolationMode.DedicatedDb
    ? $"Server=localhost,1433;Database={normalized}_db;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=True;Encrypt=False"
    : null,
```
with:
```csharp
ConnectionString = isolationMode == TenantIsolationMode.DedicatedDb
    ? $"Host=localhost;Port=5432;Database={normalized}_db;Username=postgres;Password=YourStrong!Pass123"
    : null,
```

- [ ] **Step 4: Build to verify**

```bash
dotnet build src/Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/Shared/Kernel.Infrastructure/MultiTenancy/TenantProvisioningService.cs
git commit -m "chore: replace SqlConnection with NpgsqlConnection and T-SQL DDL with PostgreSQL syntax"
```

---

## Task 5: Fix entity type annotations in AuditLogConfigurations

**Files:**
- Modify: `src/Shared/Kernel.Infrastructure/Persistence/Configurations/AuditLogConfigurations.cs`

This is the only entity configuration file with explicit `HasColumnType` calls. The `text` type is already valid in PostgreSQL, so no change needed there. There are no `uniqueidentifier`, `datetime2`, or `bit` annotations in this file — verify and confirm.

- [ ] **Step 1: Verify the file needs no changes**

Open `src/Shared/Kernel.Infrastructure/Persistence/Configurations/AuditLogConfigurations.cs` and confirm:
- `.HasColumnType("text")` on `Changes` — keep as-is, valid in PostgreSQL
- No `uniqueidentifier`, `datetime2`, or `bit` annotations present

No edits required for this file. The type annotations in other configuration files (User, RefreshToken, Role, Permission, etc.) use no explicit `HasColumnType` — Npgsql will auto-map `Guid` → `uuid`, `DateTime` → `timestamptz`, `bool` → `boolean`.

- [ ] **Step 2: Build entire solution to confirm no SQL Server type references remain**

```bash
dotnet build
```

Expected: `Build succeeded`. If there are CS errors about missing `Microsoft.Data.SqlClient` types anywhere, trace them and replace with Npgsql equivalents.

- [ ] **Step 3: Commit (if any changes were needed)**

```bash
git add src/Shared/Kernel.Infrastructure/Persistence/Configurations/AuditLogConfigurations.cs
git commit -m "chore: verify entity configurations have no SQL Server-specific column type hints"
```

---

## Task 6: Update connection strings and MultiTenancy provider

**Files:**
- Modify: `src/Api/appsettings.json`
- Modify: `src/Api/appsettings.Development.json`
- Modify: `tests/E2ETests/appsettings.Test.json`

- [ ] **Step 1: Update appsettings.json — ConnectionStrings**

In `src/Api/appsettings.json`, replace:
```json
"ConnectionStrings": {
  "HostDb": "Server=localhost,1433;Database=ProductTemplateHost;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False",
  "AppDb": "Server=localhost,1433;Database=ProductTemplateApp;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False"
},
```
with:
```json
"ConnectionStrings": {
  "HostDb": "Host=localhost;Port=5432;Database=ProductTemplateHost;Username=postgres;Password=YourStrong!Pass123",
  "AppDb": "Host=localhost;Port=5432;Database=ProductTemplateApp;Username=postgres;Password=YourStrong!Pass123"
},
```

- [ ] **Step 2: Update appsettings.json — MultiTenancy Provider**

In the same file, replace:
```json
"Provider": "SqlServer"
```
with:
```json
"Provider": "PostgreSQL"
```

- [ ] **Step 3: Update appsettings.Development.json — ConnectionStrings**

In `src/Api/appsettings.Development.json`, replace:
```json
"ConnectionStrings": {
  "HostDb": "Server=localhost,1433;Database=ProductTemplateHost;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False",
  "AppDb": "Server=localhost,1433;Database=ProductTemplateApp;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False"
}
```
with:
```json
"ConnectionStrings": {
  "HostDb": "Host=localhost;Port=5432;Database=ProductTemplateHost;Username=postgres;Password=YourStrong!Pass123",
  "AppDb": "Host=localhost;Port=5432;Database=ProductTemplateApp;Username=postgres;Password=YourStrong!Pass123"
}
```

- [ ] **Step 4: Update appsettings.Test.json — Provider**

In `tests/E2ETests/appsettings.Test.json`, replace:
```json
"Provider": "SqlServer"
```
with:
```json
"Provider": "PostgreSQL"
```

- [ ] **Step 5: Commit**

```bash
git add src/Api/appsettings.json \
        src/Api/appsettings.Development.json \
        tests/E2ETests/appsettings.Test.json
git commit -m "chore: update connection strings and MultiTenancy provider to PostgreSQL format"
```

---

## Task 7: Replace SQL Server with PostgreSQL in docker-compose

**Files:**
- Modify: `compose.yaml`

- [ ] **Step 1: Replace the sqlserver service block**

In `compose.yaml`, replace the entire `sqlserver:` service block:
```yaml
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: product-template-sqlserver
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "YourStrong!Pass123"
      MSSQL_PID: "Developer"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    networks:
      - product-template-network
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong!Pass123" -Q "SELECT 1" -C || exit 1
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
```
with:
```yaml
  postgres:
    image: postgres:17
    container_name: product-template-postgres
    environment:
      POSTGRES_PASSWORD: "YourStrong!Pass123"
      POSTGRES_USER: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - product-template-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s
```

- [ ] **Step 2: Update the api service depends_on**

Replace:
```yaml
    depends_on:
      sqlserver:
        condition: service_healthy
```
with:
```yaml
    depends_on:
      postgres:
        condition: service_healthy
```

- [ ] **Step 3: Update the api service environment connection strings**

Replace:
```yaml
      - ConnectionStrings__HostDb=Server=sqlserver,1433;Database=ProductTemplateHost;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False
      - ConnectionStrings__AppDb=Server=sqlserver,1433;Database=ProductTemplateApp;User Id=sa;Password=YourStrong!Pass123;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False
      - MultiTenancy__Provider=SqlServer
```
with:
```yaml
      - ConnectionStrings__HostDb=Host=postgres;Port=5432;Database=ProductTemplateHost;Username=postgres;Password=YourStrong!Pass123
      - ConnectionStrings__AppDb=Host=postgres;Port=5432;Database=ProductTemplateApp;Username=postgres;Password=YourStrong!Pass123
      - MultiTenancy__Provider=PostgreSQL
```

- [ ] **Step 4: Update the volumes section**

Replace:
```yaml
volumes:
  sqlserver_data:
```
with:
```yaml
volumes:
  postgres_data:
```

Keep all other volume entries (`seq_data`, `tempo_data`, `prometheus_data`, `grafana_data`) unchanged.

- [ ] **Step 5: Commit**

```bash
git add compose.yaml
git commit -m "chore: replace SQL Server 2022 with PostgreSQL 17 in docker-compose"
```

---

## Task 8: Update E2E test Testcontainers fixture

**Files:**
- Modify: `tests/E2ETests/Common/SqlServerContainerFixture.cs` (rename contents — keep filename for now, rename in Step 3)
- Modify: `tests/E2ETests/Common/TestContainerWebApplicationFactory.cs`

- [ ] **Step 1: Rewrite SqlServerContainerFixture.cs as PostgresContainerFixture.cs**

Delete the contents of `tests/E2ETests/Common/SqlServerContainerFixture.cs` and replace entirely with:

```csharp
using Npgsql;
using Testcontainers.PostgreSql;

namespace E2ETests.Common;

/// <summary>
/// Starts a single PostgreSQL container shared across all E2E tests in the collection.
/// Using ICollectionFixture ensures the container is started once per test run, not per class.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
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
```

- [ ] **Step 2: Rename the file**

```bash
git mv tests/E2ETests/Common/SqlServerContainerFixture.cs tests/E2ETests/Common/PostgresContainerFixture.cs
```

Then paste the content from Step 1 into the renamed file (git mv preserves history).

- [ ] **Step 3: Update TestContainerWebApplicationFactory.cs**

In `tests/E2ETests/Common/TestContainerWebApplicationFactory.cs`:

Replace the summary comment:
```csharp
/// container (via <see cref="SqlServerContainerFixture"/>).
```
with:
```csharp
/// container (via <see cref="PostgresContainerFixture"/>).
```

Replace the field declaration:
```csharp
private readonly SqlServerContainerFixture _sql;
```
with:
```csharp
private readonly PostgresContainerFixture _sql;
```

Replace the constructor parameter:
```csharp
public TestContainerWebApplicationFactory(SqlServerContainerFixture sql)
```
with:
```csharp
public TestContainerWebApplicationFactory(PostgresContainerFixture sql)
```

- [ ] **Step 4: Build the test project**

```bash
dotnet build tests/E2ETests/E2ETests.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add tests/E2ETests/Common/PostgresContainerFixture.cs \
        tests/E2ETests/Common/TestContainerWebApplicationFactory.cs
git commit -m "chore: replace MsSqlContainerFixture with PostgresContainerFixture using postgres:17"
```

---

## Task 9: Delete existing migrations and regenerate for PostgreSQL

**Files:**
- Delete: `src/Shared/Kernel.Infrastructure/Migrations/20260318125417_Initialhost.cs`
- Delete: `src/Shared/Kernel.Infrastructure/Migrations/20260318125417_Initialhost.Designer.cs`
- Delete: `src/Shared/Kernel.Infrastructure/Migrations/HostDbContextModelSnapshot.cs`
- Delete: `src/Shared/Kernel.Infrastructure/Migrations/AppDb/20260318125425_InitialApp.cs`
- Delete: `src/Shared/Kernel.Infrastructure/Migrations/AppDb/20260318125425_InitialApp.Designer.cs`
- Delete: `src/Shared/Kernel.Infrastructure/Migrations/AppDb/AppDbContextModelSnapshot.cs`
- Create: new `InitialHost` and `InitialApp` migration files (generated by EF tooling)

- [ ] **Step 1: Delete all existing migration files**

```bash
rm src/Shared/Kernel.Infrastructure/Migrations/20260318125417_Initialhost.cs
rm src/Shared/Kernel.Infrastructure/Migrations/20260318125417_Initialhost.Designer.cs
rm src/Shared/Kernel.Infrastructure/Migrations/HostDbContextModelSnapshot.cs
rm src/Shared/Kernel.Infrastructure/Migrations/AppDb/20260318125425_InitialApp.cs
rm src/Shared/Kernel.Infrastructure/Migrations/AppDb/20260318125425_InitialApp.Designer.cs
rm src/Shared/Kernel.Infrastructure/Migrations/AppDb/AppDbContextModelSnapshot.cs
```

- [ ] **Step 2: Ensure dotnet-ef tool is installed**

```bash
dotnet tool restore
```

If `dotnet ef` is not in the manifest, install it globally:
```bash
dotnet tool install --global dotnet-ef
```

Verify:
```bash
dotnet ef --version
```

Expected: version 10.x (or 9.x).

- [ ] **Step 3: Regenerate HostDb migration**

Run from the repo root. The startup project (`src/Api`) provides the connection string and host configuration. Set the env var to point at a running PostgreSQL instance, or skip and use the appsettings.json value:

```bash
dotnet ef migrations add InitialHost \
  --project src/Shared/Kernel.Infrastructure \
  --startup-project src/Api \
  --context HostDbContext \
  --output-dir Migrations
```

Expected output:
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

New files created:
- `src/Shared/Kernel.Infrastructure/Migrations/<timestamp>_InitialHost.cs`
- `src/Shared/Kernel.Infrastructure/Migrations/<timestamp>_InitialHost.Designer.cs`
- `src/Shared/Kernel.Infrastructure/Migrations/HostDbContextModelSnapshot.cs`

- [ ] **Step 4: Verify HostDb migration uses PostgreSQL types**

Open the generated migration file. The `CreateTable` for `Tenants` should use types like:
- `type: "text"` for strings
- `type: "bigint"` for longs
- `type: "boolean"` for booleans
- `type: "timestamp with time zone"` for dates

If it shows `nvarchar`, `bit`, or `datetime2`, something is still pointing at SQL Server — check that `HostDbContextDesignTimeFactory.cs` uses `UseNpgsql`.

- [ ] **Step 5: Regenerate AppDb migration**

```bash
dotnet ef migrations add InitialApp \
  --project src/Shared/Kernel.Infrastructure \
  --startup-project src/Api \
  --context AppDbContext \
  --output-dir Migrations/AppDb
```

Expected output: same as Step 3.

New files created in `src/Shared/Kernel.Infrastructure/Migrations/AppDb/`.

- [ ] **Step 6: Verify AppDb migration uses PostgreSQL types**

Open the generated AppDb migration. Check that:
- `Users.Id` column type is `uuid`
- `Users.CreatedAt` / `UpdatedAt` type is `timestamp with time zone`
- `Users.IsActive` type is `boolean`
- `RefreshTokens.Id` type is `uuid`

- [ ] **Step 7: Commit**

```bash
git add src/Shared/Kernel.Infrastructure/Migrations/
git commit -m "chore: delete SQL Server migrations and regenerate for PostgreSQL"
```

---

## Task 10: Full build and E2E test run

- [ ] **Step 1: Build the entire solution**

```bash
dotnet build
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 2: Run unit and integration tests (no container needed)**

```bash
dotnet test tests/UnitTests
dotnet test tests/IntegrationTests
dotnet test tests/ArchitectureTests
```

Expected: all tests pass. Architecture tests validate layer boundaries — they are provider-agnostic and should pass unchanged.

- [ ] **Step 3: Run E2E tests**

Requires Docker running (Testcontainers starts postgres:17 automatically):

```bash
dotnet test tests/E2ETests
```

Expected: all E2E tests pass. The `PostgresContainerFixture` starts a fresh `postgres:17` container, applies migrations, seeds data, and tears down after the run.

- [ ] **Step 4: Verify docker-compose stack starts cleanly**

```bash
docker compose up --build -d postgres
```

Wait for health check to pass, then:

```bash
docker compose up --build -d api
docker compose logs api --tail=50
```

Expected: API logs show `Host started` and no database connection errors.

- [ ] **Step 5: Final commit**

```bash
git add -A
git commit -m "feat: complete migration from SQL Server to PostgreSQL 17"
```
