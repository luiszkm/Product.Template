# 01 — Architecture

## Layers & Projects

```
┌─────────────────────────────────────────────────────────┐
│  src/Api  (ASP.NET Core host — thin controllers)        │
├─────────────────────────────────────────────────────────┤
│  src/Core/{Module}/{Module}.Application                 │
│  src/Shared/Kernel.Application                          │
│  (Use cases, Commands, Queries, Handlers, Validators)   │
├─────────────────────────────────────────────────────────┤
│  src/Core/{Module}/{Module}.Infrastructure              │
│  src/Shared/Kernel.Infrastructure                       │
│  (EF Core, Repositories, External services)             │
├─────────────────────────────────────────────────────────┤
│  src/Core/{Module}/{Module}.Domain                      │
│  src/Shared/Kernel.Domain                               │
│  (Entities, VOs, Events, Repository interfaces)         │
└─────────────────────────────────────────────────────────┘
```

## Dependency Rules (STRICT)

| Project | May reference |
|---------|--------------|
| `Kernel.Domain` | Nothing (pure) |
| `{Module}.Domain` | `Kernel.Domain` |
| `Kernel.Application` | `Kernel.Domain` |
| `{Module}.Application` | `{Module}.Domain`, `Kernel.Application` |
| `Kernel.Infrastructure` | `Kernel.Application`, `{Module}.Domain` (for EF configs) |
| `{Module}.Infrastructure` | `{Module}.Application`, `Kernel.Infrastructure` |
| `Api` | `Kernel.Application`, `Kernel.Infrastructure`, `{Module}.Application`, `{Module}.Infrastructure` |

### Forbidden

- Domain **MUST NOT** reference Application, Infrastructure, or Api.
- Application **MUST NOT** reference Infrastructure or Api.
- Infrastructure **MUST NOT** reference Api.
- No circular references between modules.
- No project may reference a test project.

## Adding a New Module

When adding a new bounded context (e.g., `Catalog`):

1. Create `src/Core/Catalog/Catalog.Domain/Catalog.Domain.csproj` → reference `Kernel.Domain`.
2. Create `src/Core/Catalog/Catalog.Application/Catalog.Application.csproj` → reference `Catalog.Domain` + `Kernel.Application`.
3. Create `src/Core/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj` → reference `Catalog.Application` + `Kernel.Infrastructure`.
4. Register the module's DI in a `DependencyInjection.cs` inside the Infrastructure project.
5. Wire it up in `Api/Configurations/CoreConfiguration.cs`.
6. Add the Infrastructure assembly to the MediatR scan in `KernelConfigurations.cs`.

## Patterns

| Pattern | Where |
|---------|-------|
| CQRS | `ICommand` / `IQuery` via MediatR |
| Repository | Interface in Domain, implementation in Infrastructure |
| Unit of Work | `IUnitOfWork` wrapping EF `SaveChangesAsync` |
| Domain Events | `IDomainEvent` raised inside AggregateRoot, published after commit |
| Pipeline Behaviors | Validation → Logging → Performance (MediatR pipeline) |
| Global Exception Filter | `ApiGlobalExceptionFilter` maps domain/app exceptions to ProblemDetails |

## Anti-patterns (NEVER)

- Service classes that mix read and write in the same method.
- Repositories that return `IQueryable` to upper layers.
- Handlers that call other handlers (use domain services instead).
- Controllers with more than ~20 lines per action.
- Static mutable state.

