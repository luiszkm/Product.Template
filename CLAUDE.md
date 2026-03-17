# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet restore
dotnet build
dotnet build --configuration Release

# Run API
cd src/Api && dotnet run

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/UnitTests
dotnet test tests/IntegrationTests
dotnet test tests/ArchitectureTests
dotnet test tests/E2ETests

# Run a specific test by filter
dotnet test tests/IntegrationTests --filter "FullyQualifiedName~Authorization"

# Start infrastructure (SQL Server + Seq + API)
docker compose up
```

## Architecture

Clean Architecture with strict enforced layer boundaries (validated by `tests/ArchitectureTests` using NetArchTest):

```
Domain ← Application ← Infrastructure ← Api
```

| Project | May reference |
|---------|--------------|
| `Kernel.Domain` | Nothing (pure) |
| `{Module}.Domain` | `Kernel.Domain` |
| `Kernel.Application` | `Kernel.Domain` |
| `{Module}.Application` | `{Module}.Domain`, `Kernel.Application` |
| `Kernel.Infrastructure` | `Kernel.Application`, `{Module}.Domain` |
| `{Module}.Infrastructure` | `{Module}.Application`, `Kernel.Infrastructure` |
| `Api` | All of the above |

**FORBIDDEN**: Domain → Application/Infrastructure/Api. Application → Infrastructure/Api. Handlers calling other handlers.

### Module structure

Each bounded context lives under `src/Core/{Module}/` with exactly 3 projects:
- `{Module}.Domain/` — Entities, Value Objects, Events, Repository interfaces
- `{Module}.Application/` — Commands, Queries, Handlers, Validators, Mappers, DTOs
- `{Module}.Infrastructure/` — EF Repositories, Seeders, `DependencyInjection.cs`

**Current modules:**
- `Identity/` — Users + Authentication only (no RBAC)
- `Authorization/` — Roles, Permissions, RolePermissions, UserAssignments
- `Tenants/` — Tenant domain entity + management (uses `HostDbContext`)

**`src/Core/Identity/` is the canonical reference implementation.** Look there first for any pattern.

The `src/Shared/` folder contains the Kernel (base) libraries shared across all modules.

### Adding a new module

1. Create the 3 projects in `src/Core/{Module}/` with the dependency references above.
2. Add a `DependencyInjection.cs` in the Infrastructure project.
3. Wire it in `Api/Configurations/CoreConfiguration.cs`.
4. Add the Infrastructure assembly to the MediatR scan in `KernelConfigurations.cs`.
5. Add the projects to `Product.Template.sln`.

## Key patterns

### CQRS (MediatR 14.x)

- Commands: `record` implementing `ICommand<TOutput>` or `ICommand`; handlers call `IUnitOfWork.Commit()` after mutation.
- Queries: `record` implementing `IQuery<TOutput>`; handlers **never** call `Commit()`.
- Every command **must** have a FluentValidation `AbstractValidator<TCommand>` — the pipeline behavior auto-runs it.
- Handlers return `record` DTOs suffixed `Output` — **never** domain entities.

### Domain entities

- Inherit from `Entity` or `AggregateRoot` (from `Kernel.Domain`).
- All implement `IMultiTenantEntity` (`TenantId long`).
- **Private constructor + static `Create(...)` factory** — enforces invariants inside.
- Properties with `private set`; state changes via explicit behavior methods (e.g., `Deactivate()`).
- Domain events: `AddDomainEvent(...)` inside aggregate, dispatched after `Commit()`.
- Only aggregate roots have repositories — **no repository for child entities**.

### Controllers

- Thin MediatR dispatchers, ~20 lines max per action, zero business logic.
- Always `[Authorize(Policy = SecurityConfiguration.{PolicyName})]` — **never bare `[Authorize]`**.
- Add `CancellationToken cancellationToken` as last parameter to every async action.
- Include `[ProducesResponseType]` for every possible status code.
- Every new protected endpoint **must** be added to `docs/security/RBAC_MATRIX.md`.

### Error handling → HTTP

| Exception | HTTP |
|-----------|------|
| `NotFoundException` | 404 |
| `BusinessRuleException` | 400 |
| `DomainException` | 422 |
| `ValidationException` | 400 |
| `UnauthorizedAccessException` | 401 |

Mapping happens in `ApiGlobalExceptionFilter` → ProblemDetails (RFC 9457).

### Multi-tenancy

Resolved from `X-Tenant` header or subdomain → `ITenantContext`. `MultiTenantSaveChangesInterceptor` auto-assigns `TenantId` on saves. All entities implement `IMultiTenantEntity`.

### Naming conventions

| Type | Convention | Example |
|------|-----------|---------|
| Command | `{Verb}{Noun}Command` | `RegisterUserCommand` |
| Query | `{Get\|List}{Noun}Query` | `GetUserByIdQuery` |
| Command handler | `{Verb}{Noun}CommandHandler` | `RegisterUserCommandHandler` |
| Query handler | `{Get\|List}{Noun}QueryHandler` | `GetUserByIdQueryHandler` |
| Validator | `{CommandName}Validator` | `RegisterUserCommandValidator` |
| Output DTO | `{Noun}Output` (record) | `UserOutput` |
| Mapper | `{Noun}Mapper` (extension: `.ToOutput()`) | `UserMapper` |
| Repository interface | `I{AggregateRoot}Repository` | `IUserRepository` |
| Repository impl | `{AggregateRoot}Repository` | `UserRepository` |
| EF config | `{Entity}Configurations` | `UserConfigurations` |
| Domain event | Past tense `{Noun}{PastVerb}Event` | `UserRegisteredEvent` |

### Testing

- Test name format: `{Method}_{Scenario}_{Result}` (e.g., `Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist`).
- **No mocking frameworks** — use inline fakes/stubs (sealed classes at bottom of test file). Use `NullLogger<T>.Instance` for loggers.
- Integration tests use `WebApplicationFactory<Program>` + `TestAuthHandler`; always send `X-Tenant: public` header; inject roles/permissions via `X-Test-Roles` / `X-Test-Permissions` headers.

### Logging

- Serilog structured logging: `_logger.LogInformation("User {UserId} created", user.Id)`.
- **Never** string interpolation in log templates.

## Additional rules and AI resources

Detailed rules by layer live in `.ai/rules/` (00–14). Reusable prompts are in `prompts/`. Specialized GitHub Copilot agents (e.g., `@feature-builder`, `@code-reviewer`) are in `.github/agents/`. Layer-specific Copilot instructions are in `.github/instructions/`. Implementation checklists are in `.ai/checklists/`.
