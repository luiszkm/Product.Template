# 03 — Application Rules

## CQRS Pattern

| Concern | Interface | Base | Returns |
|---------|-----------|------|---------|
| Command (no return) | `ICommand` | `IRequest` | `Unit` |
| Command (with return) | `ICommand<TResponse>` | `IRequest<TResponse>` | DTO |
| Query | `IQuery<TResponse>` | `IRequest<TResponse>` | DTO or `PaginatedListOutput<T>` |
| Command Handler | `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>` | `IRequestHandler` | — |
| Query Handler | `IQueryHandler<TQuery, TResponse>` | `IRequestHandler` | — |

### Rules

1. **Commands** mutate state and call `IUnitOfWork.Commit()`.
2. **Queries** are read-only — they NEVER call `IUnitOfWork.Commit()`.
3. Handlers are the **single orchestration point** for a use case.
4. Handlers MUST NOT call other handlers. Extract shared logic to a domain service or domain method.
5. Every command/query is a **`record`** type.
6. Return types from handlers are **`record`** DTOs (suffixed `Output`), never domain entities.

## File Organization

```
src/Core/{Module}/{Module}.Application/
├── Handlers/
│   └── {Feature}/
│       ├── Commands/
│       │   └── {Verb}{Noun}Command.cs        # e.g., RegisterUserCommand
│       ├── {Verb}{Noun}CommandHandler.cs      # e.g., RegisterUserCommandHandler
│       └── {Noun}Output.cs                    # shared output DTO (if handler-specific)
├── Queries/
│   └── {Feature}/
│       ├── Commands/                          # query definitions
│       │   └── {Get|List}{Noun}Query.cs
│       ├── {Get|List}{Noun}QueryHandler.cs
│       └── {Noun}Output.cs
├── Validators/
│   └── {CommandName}Validator.cs
├── Mappers/
│   └── {Noun}Mapper.cs                       # extension methods: entity.ToOutput()
├── Security/
│   └── (RBAC metrics, etc.)
└── {Module}.Application.csproj
```

## Validators (FluentValidation)

- **Every command MUST have a validator.** Queries only need validators if they have complex input.
- Validator class: `{CommandName}Validator : AbstractValidator<{CommandName}>`.
- Validators run automatically via `ValidationBehavior` in the MediatR pipeline.
- Validators validate **shape** (required, max-length, format). Business rules (uniqueness, existence) belong in the handler.

## DTOs / Outputs

- Suffix: `Output` (e.g., `UserOutput`, `RoleOutput`, `AuthTokenOutput`).
- Always a `record`.
- Never expose domain entities or EF-tracked objects in API responses.
- Use mapper extension methods (`entity.ToOutput()`) in a `Mappers/` folder.

## Pagination

- Queries that return lists inherit from `ListInput` (provides `PageNumber`, `PageSize`, `SearchTerm`, `SortBy`, `SortDirection`).
- Return type: `PaginatedListOutput<TOutput>`.

## Pipeline Behaviors (Kernel.Application)

| Order | Behavior | Purpose |
|-------|----------|---------|
| 1 | `ValidationBehavior` | Runs FluentValidation validators; throws `ValidationException` on failure. |
| 2 | `LoggingBehavior` | Logs request type entry/exit. |
| 3 | `PerformanceBehavior` | Logs slow requests (> threshold). |

## Dependency Injection

- Application DI is in `Kernel.Application.DependencyInjection.AddKernelApplication(assemblies)`.
- Pass all assemblies containing handlers/validators to the MediatR scan.
- Module-specific DI (repositories) is in `{Module}.Infrastructure.DependencyInjection`.

## Exceptions

| Exception | HTTP Status | When |
|-----------|-------------|------|
| `NotFoundException` | 404 | Entity not found by ID. |
| `BusinessRuleException` | 400 | Application-level rule violation. |
| `DomainException` | 422 | Domain invariant violation. |
| `ValidationException` (FluentValidation) | 400 | Input shape invalid. |
| `UnauthorizedAccessException` | 401 | Authentication failure. |

