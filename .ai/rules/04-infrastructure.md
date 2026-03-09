# 04 — Infrastructure Rules

## Entity Framework Core

### DbContext
- Single `AppDbContext` in `Kernel.Infrastructure/Persistence/`.
- All `DbSet<T>` properties are declared here.
- `OnModelCreating` applies configurations from assembly + tenant query filters.
- Provider is resolved at runtime: SQLite (dev) or PostgreSQL (prod), detected by connection string.

### Entity Configurations
- One configuration class per entity: `{Entity}Configurations : IEntityTypeConfiguration<{Entity}>`.
- Located in `Kernel.Infrastructure/Persistence/Configurations/`.
- Table name is the **plural of the entity** (e.g., `Users`, `Roles`).
- Always set `HasKey`, `ValueGeneratedNever()` for `Id`, `IsRequired` for mandatory fields.
- Value objects are mapped with `OwnsOne()`.
- Relationships are configured with explicit FK and `OnDelete` behavior.

### Interceptors
| Interceptor | Purpose |
|-------------|---------|
| `AuditableEntityInterceptor` | Auto-fills `CreatedBy/At`, `UpdatedBy/At` on SaveChanges. |
| `MultiTenantSaveChangesInterceptor` | Sets `TenantId` on new `IMultiTenantEntity` entries. |
| `SearchPathConnectionInterceptor` | Sets PostgreSQL `search_path` for schema-per-tenant. |

### Migrations
- Host database (`HostDbContext`) and App database (`AppDbContext`) are separate.
- Migrations live in `src/Tools/Migrator/`.
- On startup, `InitializeDatabaseAsync()` runs migrations + seeders.

### Seeders
- Located in `{Module}.Infrastructure/Data/Seeders/`.
- Pattern: `{Entity}Seeder` with `static async Task SeedAsync(AppDbContext context)`.
- Seeders are idempotent — check `AnyAsync()` before inserting.

## Repositories

- Interface: `I{AggregateRoot}Repository : IBaseRepository<T>` in Domain.
- Implementation: `{AggregateRoot}Repository` in `{Module}.Infrastructure/Data/Persistence/`.
- Use **eager loading** (`Include`/`ThenInclude`) for aggregate children.
- Never return `IQueryable<T>` from a repository.
- For complex read queries, consider Dapper with raw SQL via a dedicated read service.

## Unit of Work

```csharp
// Usage in handlers:
await _repository.AddAsync(entity, cancellationToken);
await _unitOfWork.Commit(cancellationToken);
```

- `IUnitOfWork` wraps `AppDbContext.SaveChangesAsync()`.
- Only command handlers call `Commit()`. Query handlers never commit.

## Multi-Tenancy Infrastructure

| Component | Role |
|-----------|------|
| `TenantResolutionMiddleware` | Resolves tenant from `X-Tenant` header or subdomain. |
| `HeaderAndSubdomainTenantResolver` | Strategy: header first, then first segment of host. |
| `CachedTenantStore` | Loads `TenantConfig` from `HostDbContext` with 5-min memory cache. |
| `TenantContext` | Scoped service holding the resolved tenant for the request. |
| `TenantConnectionStringResolver` | Returns the correct connection string per tenant. |
| `ModelBuilderTenantExtensions` | Applies `HasQueryFilter` for `TenantId` on all `IMultiTenantEntity`. |

## External Integrations

- Use `HttpClient` via `IHttpClientFactory` (registered with `AddHttpClient<T>()`).
- Apply resilience policies from `ResilienceConfiguration` (Polly retry, circuit breaker, timeout).
- Never call external APIs from domain or application layers — wrap in an infrastructure service behind an interface.

## DI Registration Pattern

```csharp
// {Module}.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection Add{Module}InJections(this IServiceCollection services)
    {
        services.AddScoped<I{Entity}Repository, {Entity}Repository>();
        return services;
    }
}
```

Wire in `Api/Configurations/CoreConfiguration.cs`:
```csharp
services.Add{Module}InJections();
```

