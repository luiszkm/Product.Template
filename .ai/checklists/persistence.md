# Checklist: Persistence

> Use this checklist when adding or modifying database entities, repositories, or configurations.

## Entity Configuration (EF Core)

- [ ] Configuration class: `{Entity}Configurations : IEntityTypeConfiguration<{Entity}>`
- [ ] Located in `src/Shared/Kernel.Infrastructure/Persistence/Configurations/`
- [ ] Table name set: `.ToTable("{PluralEntityName}")`
- [ ] Primary key: `.HasKey(e => e.Id)`
- [ ] Id is `ValueGeneratedNever()`
- [ ] `TenantId` property is `IsRequired()`
- [ ] Composite index on `{ TenantId, Id }` or `{ TenantId, UniqueField }`
- [ ] All required properties have `IsRequired()`
- [ ] String properties have `HasMaxLength()`
- [ ] Value Objects mapped with `OwnsOne()`
- [ ] Relationships configured with explicit FK + `OnDelete` behavior
- [ ] Navigation collections use `HasMany().WithOne().HasForeignKey()`
- [ ] `DomainEvents` ignored: `.Ignore(e => e.DomainEvents)` (if AggregateRoot)

## AppDbContext

- [ ] `DbSet<{Entity}>` added to `AppDbContext`
- [ ] No manual query filter needed — `ApplyTenantQueryFilters()` handles `IMultiTenantEntity` automatically

## Repository

- [ ] Interface: `I{Entity}Repository : IBaseRepository<T>` in Domain
- [ ] Implementation in `{Module}.Infrastructure/Data/Persistence/`
- [ ] Constructor injects `AppDbContext`
- [ ] `GetByIdAsync` uses `Include`/`ThenInclude` for aggregate children
- [ ] `ListAllAsync` applies `Skip`/`Take` at DB level (not in-memory)
- [ ] No `IQueryable<T>` returned from any method
- [ ] Registered in `DependencyInjection.cs` as `AddTransient` or `AddScoped`

## Seeder (if applicable)

- [ ] Located in `{Module}.Infrastructure/Data/Seeders/{Entity}Seeder.cs`
- [ ] Static `SeedAsync(AppDbContext context)` method
- [ ] Idempotent: checks `AnyAsync()` before inserting
- [ ] Called from `DatabaseConfiguration.InitializeDatabaseAsync()`
- [ ] Uses `Entity.Create(...)` factory (not direct constructor)
- [ ] Sets Id via reflection only for seed data with fixed GUIDs

## Migration

- [ ] EF migration generated and reviewed
- [ ] `Down()` method correctly reverses the `Up()`
- [ ] No data loss without explicit confirmation
- [ ] Indexes support expected query patterns
- [ ] FK constraints match domain relationships

## Performance Considerations

- [ ] Frequently queried fields have indexes
- [ ] Large text fields use appropriate `HasMaxLength`
- [ ] Eager loading doesn't cause cartesian explosion
- [ ] For read-heavy queries, consider Dapper read service

