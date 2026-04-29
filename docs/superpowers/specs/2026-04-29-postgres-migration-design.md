# PostgreSQL Migration Design

**Date:** 2026-04-29  
**Approach:** Idiomatic PostgreSQL (Option B)  
**Scope:** Full replacement of SQL Server with PostgreSQL 17 across infrastructure, docker, tests, and migrations.

---

## Context

The Product.Template is a multi-tenant SaaS template using .NET 10, EF Core 10, and SQL Server 2022. It supports three tenant isolation modes: `SharedDb`, `SchemaPerTenant`, and `DedicatedDb`. There is no production data — all existing EF Core migrations will be deleted and regenerated.

---

## Section 1: NuGet Package Changes

| Project | Remove | Add |
|---|---|---|
| `Kernel.Infrastructure` | `Microsoft.EntityFrameworkCore.SqlServer` | `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.*` |
| `Migrator` | `Microsoft.EntityFrameworkCore.SqlServer` | `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.*` |
| `E2ETests` | `Testcontainers.MsSql` | `Testcontainers.PostgreSql` |

`Identity.Infrastructure` and other module infrastructure projects have no direct SqlServer package reference — they inherit via `Kernel.Infrastructure`.

---

## Section 2: DbContext Configuration

**`DatabaseConfiguration.cs`** — replace all `UseSqlServer` calls with `UseNpgsql`:

```csharp
// HostDbContext
options.UseNpgsql(hostConnection);

// AppDbContext — SchemaPerTenant support preserved
options.UseNpgsql(appConnection, npgsql =>
{
    if (tenant.IsolationMode == TenantIsolationMode.SchemaPerTenant
        && !string.IsNullOrWhiteSpace(tenant.SchemaName))
        npgsql.MigrationsHistoryTable("__EFMigrationsHistory", tenant.SchemaName);
});
```

**Design-time factories** (`AppDbContextDesignTimeFactory`, `HostDbContextDesignTimeFactory`) — same `UseSqlServer` → `UseNpgsql` swap.

**Global Npgsql setting** — added in `Program.cs` before any DbContext registration:

```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);
```

This enforces `DateTimeKind.Utc` for all timestamps, preventing silent timezone bugs.

**`MultiTenancy.Provider`** in `appsettings.json` changes from `"SqlServer"` to `"PostgreSQL"`.

---

## Section 3: Entity Type Annotations

SQL Server-specific column type hints are removed or replaced in all EF entity configurations:

| .NET Type | Remove | Replace with |
|---|---|---|
| `Guid` | `.HasColumnType("uniqueidentifier")` | `.HasColumnType("uuid")` |
| `DateTime` / `DateTimeOffset` | `.HasColumnType("datetime2")` | `.HasColumnType("timestamptz")` |
| `bool` | `.HasColumnType("bit")` | remove — Npgsql maps to `boolean` automatically |
| `string` (long) | `.HasColumnType("text")` | keep — valid in PostgreSQL |

---

## Section 4: TenantProvisioningService

The only file with raw ADO.NET code. `SqlConnection` and T-SQL DDL are replaced with `NpgsqlConnection` and PostgreSQL DDL:

```csharp
// Before (SQL Server)
await using var connection = new SqlConnection(sharedConn);
cmd.CommandText = $"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{tenant.SchemaName}')\n    EXEC('CREATE SCHEMA [{tenant.SchemaName}]');";

// After (PostgreSQL)
await using var connection = new NpgsqlConnection(sharedConn);
cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{tenant.SchemaName}\"";
```

Key differences:
- `CREATE SCHEMA IF NOT EXISTS` is native PostgreSQL syntax — no need for `sys.schemas` check
- Schema names use double-quote delimiters (`"`) instead of brackets (`[]`)
- `NpgsqlConnection` comes from the `Npgsql` package already bundled with `Npgsql.EntityFrameworkCore.PostgreSQL`

---

## Section 5: Connection Strings and docker-compose

**`appsettings.json` and `appsettings.Development.json`:**

```json
"ConnectionStrings": {
  "HostDb": "Host=localhost;Port=5432;Database=ProductTemplateHost;Username=postgres;Password=YourStrong!Pass123",
  "AppDb": "Host=localhost;Port=5432;Database=ProductTemplateApp;Username=postgres;Password=YourStrong!Pass123"
}
```

SQL Server-specific parameters (`TrustServerCertificate`, `MultipleActiveResultSets`, `Encrypt`) are removed.

**`compose.yaml`** — replace `sqlserver` service with `postgres`:

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
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U postgres"]
    interval: 10s
    timeout: 5s
    retries: 5
```

The `api` service in compose updates its `depends_on` to `postgres` and its environment connection strings to the new format.

---

## Section 6: Migrations

**Delete** all existing migration files:
- `src/Shared/Kernel.Infrastructure/Migrations/20260318125417_Initialhost.cs`
- `src/Shared/Kernel.Infrastructure/Migrations/HostDbContextModelSnapshot.cs`
- `src/Shared/Kernel.Infrastructure/Migrations/AppDb/20260318125425_InitialApp.cs`
- `src/Shared/Kernel.Infrastructure/Migrations/AppDb/AppDbContextModelSnapshot.cs`

**Regenerate** two initial migrations after all config changes are applied:

```bash
# HostDb
dotnet ef migrations add InitialHost \
  --project src/Shared/Kernel.Infrastructure \
  --startup-project src/Api \
  --context HostDbContext \
  --output-dir Migrations

# AppDb
dotnet ef migrations add InitialApp \
  --project src/Shared/Kernel.Infrastructure \
  --startup-project src/Api \
  --context AppDbContext \
  --output-dir Migrations/AppDb
```

Generated migrations will use PostgreSQL-native types (`uuid`, `timestamptz`, `boolean`).

The `Migrator` tool (`src/Tools/Migrator/`) requires only the package swap — its orchestration logic uses the EF Core provider-agnostic API.

---

## Section 7: E2E Tests

**`SqlServerContainerFixture.cs`** → renamed to **`PostgresContainerFixture.cs`**:

```csharp
// Before
private MsSqlContainer _container = new MsSqlBuilder()
    .WithPassword("E2eTest@Strong123")
    .Build();

// After
private PostgreSqlContainer _container = new PostgreSqlBuilder()
    .WithImage("postgres:17")
    .WithPassword("E2eTest@Strong123")
    .Build();
```

`GetConnectionString()` already returns the correct Npgsql format — no downstream test changes needed.

**`appsettings.Test.json`** — update provider:

```json
"MultiTenancy": { "Provider": "PostgreSQL" }
```

Test collection attribute renamed from `[Collection("SqlServer")]` to `[Collection("Postgres")]` across all E2E test files for consistency.

---

## Files Changed Summary

| File | Change type |
|---|---|
| `Kernel.Infrastructure/Kernel.Infrastructure.csproj` | Package swap |
| `Migrator/Migrator.csproj` | Package swap |
| `E2ETests/E2ETests.csproj` | Package swap |
| `DatabaseConfiguration.cs` | `UseSqlServer` → `UseNpgsql` (×3) |
| `AppDbContextDesignTimeFactory.cs` | `UseSqlServer` → `UseNpgsql` |
| `HostDbContextDesignTimeFactory.cs` | `UseSqlServer` → `UseNpgsql` |
| `Program.cs` (Api) | Add `EnableLegacyTimestampBehavior` switch |
| `TenantProvisioningService.cs` | `SqlConnection` → `NpgsqlConnection`, T-SQL → PG DDL |
| `appsettings.json` | Connection strings + Provider |
| `appsettings.Development.json` | Connection strings |
| `appsettings.Test.json` | Provider value |
| `compose.yaml` | SQL Server → PostgreSQL 17 service |
| `Persistence/Configurations/AuditLogConfigurations.cs` | Column type annotations |
| `Migrations/` (all files) | Delete and regenerate |
| `SqlServerContainerFixture.cs` | Rename + `MsSqlContainer` → `PostgreSqlContainer` |
| E2E test files with `[Collection("SqlServer")]` | Rename collection attribute |
