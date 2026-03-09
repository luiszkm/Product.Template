# Checklist: New Feature

> Use this checklist when adding a complete new feature or module.

## 1. Domain Layer

- [ ] Entity created inheriting `Entity` or `AggregateRoot`
- [ ] Entity implements `IMultiTenantEntity`
- [ ] Private constructor + static `Create(...)` factory
- [ ] Invariants enforced inside the entity (not in handlers)
- [ ] Properties have `private set`
- [ ] State changes via explicit behavior methods
- [ ] Value Objects created for complex value types (if applicable)
- [ ] Domain Events defined in past tense (if applicable)
- [ ] Repository interface created: `I{Entity}Repository : IBaseRepository<T>`
- [ ] `.csproj` references only `Kernel.Domain`

## 2. Application Layer

- [ ] Command records created implementing `ICommand` or `ICommand<TOutput>`
- [ ] Command handlers created implementing `ICommandHandler<TCommand, TOutput>`
- [ ] Query records created implementing `IQuery<TOutput>`
- [ ] Query handlers created implementing `IQueryHandler<TQuery, TOutput>`
- [ ] Output DTOs are `record` types suffixed with `Output`
- [ ] Mapper extension methods created (`entity.ToOutput()`)
- [ ] FluentValidation validator created for **every** command
- [ ] Handlers return DTOs — never domain entities
- [ ] Handlers call `IUnitOfWork.Commit()` for commands; never for queries
- [ ] `CancellationToken` accepted and forwarded in all async methods
- [ ] Structured logging with `ILogger<T>` in all handlers
- [ ] `.csproj` references only `{Module}.Domain` + `Kernel.Application`

## 3. Infrastructure Layer

- [ ] Repository implementation created in `Data/Persistence/`
- [ ] EF Core entity configuration created in `Kernel.Infrastructure/Persistence/Configurations/`
- [ ] `DbSet<T>` added to `AppDbContext`
- [ ] Seeder created (if initial data is needed)
- [ ] `DependencyInjection.cs` updated with repository registration
- [ ] Tenant query filter applies automatically (via `IMultiTenantEntity`)
- [ ] `.csproj` references `{Module}.Application` + `Kernel.Infrastructure`

## 4. API Layer

- [ ] Controller action(s) created in `Controllers/v1/`
- [ ] `[Authorize(Policy = "...")]` with explicit policy on protected endpoints
- [ ] `[AllowAnonymous]` on public endpoints
- [ ] `[ProducesResponseType]` for all possible status codes
- [ ] `CancellationToken` as last parameter in all actions
- [ ] Controller dispatches to MediatR — no business logic
- [ ] DI wired in `CoreConfiguration.cs`
- [ ] Assembly registered in `KernelConfigurations.cs` for MediatR scan

## 5. Security

- [ ] `docs/security/RBAC_MATRIX.md` updated with new endpoints
- [ ] New policy added to `SecurityConfiguration.cs` (if needed)
- [ ] Policy constant added as `public const string` in `SecurityConfiguration`

## 6. Tests

- [ ] Unit tests for each command handler (happy path + failure)
- [ ] Unit tests for each validator (valid + invalid inputs)
- [ ] Authorization integration tests (401/403/200)
- [ ] RBAC matrix consistency test passes
- [ ] Architecture tests pass (layer dependencies, naming)

## 7. Final Validation

- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet test` passes all tests
- [ ] No `[Authorize]` without explicit `Policy`
- [ ] No domain entities exposed in API responses
- [ ] No business logic in controllers

