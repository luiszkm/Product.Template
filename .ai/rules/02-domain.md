# 02 — Domain Rules

## Base Types (Kernel.Domain)

| Type | Usage |
|------|-------|
| `Entity` | Has `Id (Guid)`, `LogicalId (long)`, full audit fields (`CreatedAt/By`, `UpdatedAt/By`, `DeletedAt/By`, `RestoredAt/By`), `SoftDelete()`, `Restore()`. |
| `AggregateRoot : Entity` | Root of an aggregate. Owns a `DomainEvents` collection. Only aggregate roots have repositories. |
| `IAuditableEntity` | Marker interface — audit fields are populated automatically by `AuditableEntityInterceptor`. |
| `IMultiTenantEntity` | Requires `TenantId (long)`. All module entities must implement this. |
| `IDomainEvent` | Marker for domain events raised inside aggregates. |

## Modeling Rules

### Entities
- All entities **inherit from `Entity`** (or `AggregateRoot` if they are aggregate roots).
- Entity constructors are **`private`**. Use a **static `Create(...)` factory method** that enforces invariants.
- Properties that represent state have **`private set`**.
- State changes are done via **explicit behavior methods** (e.g., `Deactivate()`, `UpdatePassword()`), never by setting properties directly from outside.
- Every entity implements `IMultiTenantEntity`.

### Aggregate Roots
- The aggregate root is the **only entry point** for modifying the aggregate.
- Child entities are accessed via **read-only collections** (e.g., `IReadOnlyCollection<UserRole>`).
- Raise domain events via `AddDomainEvent(...)` inside aggregate behavior methods.
- One repository per aggregate root. No repository for child entities.

### Value Objects
- Use C# `record` types.
- Validate in a static `Create(...)` factory.
- Immutable — no setters.
- Examples: `Email`, `Password`.

### Domain Events
- Named in past tense: `UserRegisteredEvent`, `UserLoggedInEvent`.
- Carry only the minimal data needed (IDs, not full entities).
- Defined in `{Module}.Domain/Events/`.

### Invariants
- Enforce all business rules **inside the domain entity**, not in the handler.
- Throw `ArgumentException` or `DomainException` from Kernel.Domain when an invariant is violated.
- Handlers throw `BusinessRuleException` or `NotFoundException` (from Kernel.Application) for application-level rules.

## File Organization

```
src/Core/{Module}/{Module}.Domain/
├── Entities/
│   ├── {AggregateRoot}.cs
│   ├── {ChildEntity}.cs
│   └── {JoinEntity}.cs
├── ValueObjects/
│   └── {ValueObject}.cs
├── Events/
│   └── {PastTenseEvent}Event.cs
├── Repositories/
│   └── I{AggregateRoot}Repository.cs
└── {Module}.Domain.csproj
```

## Examples from This Template

- **Aggregate Root**: `User` (owns `UserRole` children, raises `UserRegisteredEvent`).
- **Child Entity**: `UserRole`, `RolePermission`.
- **Entity**: `Role`, `Permission`.
- **Value Objects**: `Email`, `Password`.
- **Repository interfaces**: `IUserRepository`, `IRoleRepository`.

