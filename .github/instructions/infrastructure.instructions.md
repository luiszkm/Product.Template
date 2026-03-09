# Infrastructure Instructions — Product.Template

> Regras da camada de infraestrutura derivadas do padrão real do repositório.

## EF Core — AppDbContext

Contexto único em `src/Shared/Kernel.Infrastructure/Persistence/AppDbContext.cs`:
- Todos os `DbSet<T>` são declarados aqui.
- `OnModelCreating` aplica configurações do assembly e filtros multi-tenant.
- Provider resolvido em runtime: SQLite (dev) ou PostgreSQL (prod), detectado pela connection string.

## Entity Configurations

Padrão real extraído de `UserConfigurations.cs`:

```csharp
// src/Shared/Kernel.Infrastructure/Persistence/Configurations/{Entity}Configurations.cs
namespace Product.Template.Kernel.Infrastructure.Persistence.Configurations;

public class {Entity}Configurations : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.ToTable("{EntitiesPlural}");     // nome plural
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        // ... propriedades com IsRequired, HasMaxLength, etc.
        // ... relacionamentos com HasMany/HasOne + FK + OnDelete
    }
}
```

Regras:
- Uma classe de configuração por entidade.
- Sufixo `Configurations` (ex: `UserConfigurations`, `RoleConfigurations`).
- Todas em `Kernel.Infrastructure/Persistence/Configurations/`.
- Table names no plural PascalCase (ex: `Users`, `Roles`, `RolePermissions`).
- `Id` com `ValueGeneratedNever()`.
- `TenantId` sempre `IsRequired()`.
- Índices compostos incluem `TenantId` como primeiro campo.
- Value Objects mapeados com `OwnsOne()`.
- `DomainEvents` ignorados: `.Ignore(e => e.DomainEvents)` para AggregateRoots.

## Interceptors

| Interceptor | Localização | Função |
|-------------|-------------|--------|
| `AuditableEntityInterceptor` | `Kernel.Infrastructure` | Auto-preenche `CreatedBy/At`, `UpdatedBy/At` |
| `MultiTenantSaveChangesInterceptor` | `Kernel.Infrastructure` | Seta `TenantId` em `IMultiTenantEntity` novas |
| `SearchPathConnectionInterceptor` | `Kernel.Infrastructure` | Seta `search_path` no PostgreSQL para schema-per-tenant |

## Repositories

Padrão real extraído de `UserRepository.cs`:

```csharp
// src/Core/{Module}/{Module}.Infrastructure/Data/Persistence/{Entity}Repository.cs
namespace Product.Template.Core.{Module}.Infrastructure.Data.Persistence;

public class {Entity}Repository : I{Entity}Repository
{
    private readonly AppDbContext _context;

    public {Entity}Repository(AppDbContext context) => _context = context;

    public async Task<{Entity}?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.{Entities}
            .Include(e => e.Children)
                .ThenInclude(c => c.GrandChild)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<PaginatedListOutput<{Entity}>> ListAllAsync(
        ListInput listInput, CancellationToken cancellationToken = default)
    {
        var query = _context.{Entities}.AsQueryable();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((listInput.PageNumber - 1) * listInput.PageSize)
            .Take(listInput.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedListOutput<{Entity}>(
            PageNumber: listInput.PageNumber,
            PageSize: listInput.PageSize,
            TotalCount: totalCount,
            Data: items
        );
    }

    public async Task AddAsync({Entity} entity, CancellationToken cancellationToken = default)
        => await _context.{Entities}.AddAsync(entity, cancellationToken);

    public Task UpdateAsync({Entity} entity, CancellationToken cancellationToken = default)
    {
        _context.{Entities}.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync({Entity} entity, CancellationToken cancellationToken = default)
    {
        _context.{Entities}.Remove(entity);
        return Task.CompletedTask;
    }
}
```

Regras:
- Um repository por aggregate root. Nunca para entidades filhas.
- Interface em `{Module}.Domain/Repositories/I{Entity}Repository.cs` herda `IBaseRepository<T>`.
- Implementação em `{Module}.Infrastructure/Data/Persistence/{Entity}Repository.cs`.
- Injeta `AppDbContext` diretamente.
- `Include`/`ThenInclude` para eager loading de todo o aggregate.
- Nunca retorna `IQueryable<T>`.
- Paginação via `Skip`/`Take` no banco.
- `AddAsync` e `Delete` são sync-over-async (EF pattern) — `Update` é síncrono.

## Unit of Work

```csharp
// Kernel.Infrastructure/Persistence/UnitOfWork.cs
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    public async Task Commit(CancellationToken cancellationToken)
        => await _context.SaveChangesAsync(cancellationToken);
}
```

- `IUnitOfWork` é definida em `Kernel.Application/Data/`.
- Registrada como `AddScoped<IUnitOfWork, UnitOfWork>` no DI.
- Chamada APENAS por command handlers, NUNCA por queries.

## Multi-Tenancy

| Componente | Descrição |
|------------|-----------|
| `TenantResolutionMiddleware` | Resolve tenant via header `X-Tenant` ou primeiro segmento do host |
| `HeaderAndSubdomainTenantResolver` | Estratégia: header primeiro, depois subdomínio |
| `CachedTenantStore` | Carrega `TenantConfig` do `HostDbContext` com cache em memória (5 min) |
| `TenantContext` | Scoped service com o tenant resolvido para a request |
| `ModelBuilderTenantExtensions` | Aplica `HasQueryFilter` para `TenantId` em toda `IMultiTenantEntity` |

## Seeders

Padrão em `{Module}.Infrastructure/Data/Seeders/{Entity}Seeder.cs`:

```csharp
public static class {Entity}Seeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.{Entities}.AnyAsync()) return;  // idempotente
        // ... inserir dados iniciais
    }
}
```

- Idempotentes: verificam `AnyAsync()` antes de inserir.
- Chamados em `DatabaseConfiguration.InitializeDatabaseAsync()`.

## DI Registration

```csharp
// {Module}.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection Add{Module}InJections(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddTransient<I{Entity}Repository, {Entity}Repository>();
        return services;
    }
}
```

Wired em `Api/Configurations/CoreConfiguration.cs`:
```csharp
public static IServiceCollection AddUseCases(this IServiceCollection services)
{
    services.Add{Module}InJections();
    services.AddHttpContextAccessor();
    return services;
}
```

## Logging técnico

- Serilog configurado em `Api/Configurations/SerilogConfiguration.cs`.
- Sinks: Console, File (rolling diário), Seq.
- `RequestLoggingMiddleware` adiciona `CorrelationId` e mascara headers/campos sensíveis.
- Usar structured templates: `_logger.LogInformation("User {UserId} created", user.Id)`.
- NUNCA string interpolation: `_logger.LogInformation($"User {user.Id} created")` ← ERRADO.

## HostDb (separado do AppDb)

- `HostDbContext` armazena tabela `Tenants`.
- Migrado automaticamente no startup.
- Localização: `Kernel.Infrastructure/HostDb/`.

## Authentication Infrastructure

- `JwtTokenService`: gera tokens com `JsonWebTokenHandler`.
- `HashServices`: PBKDF2-SHA256, 100K iterações.
- `CurrentUserService`: extrai claims do `HttpContext`.
- `AuthenticationProviderFactory`: gerencia providers (JWT, Microsoft OAuth2).

