# Prompt: Create Entity

> Scaffold a DDD entity following the template's domain conventions.

---

## Context Files
- `.ai/rules/02-domain.md`
- `.ai/rules/11-naming.md`
- `.ai/rules/12-folder-structure.md`
- Reference: `src/Core/Identity/Identity.Domain/Entities/User.cs`

## Instruction

Create a domain entity named **`{ENTITY_NAME}`** in module **`{MODULE_NAME}`**.

Requirements:
1. Place it in `src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/Entities/{ENTITY_NAME}.cs`.
2. Inherit from `AggregateRoot` (if aggregate root) or `Entity` (if child).
3. Implement `IMultiTenantEntity` (`public long TenantId { get; set; }`).
4. Private parameterless constructor for EF Core.
5. Private constructor with parameters for the `Create` factory.
6. Public static `Create(...)` factory method that:
   - Validates invariants (throw `ArgumentException` for violations).
   - Initializes all properties.
   - Raises a domain event (if applicable).
7. Properties with `private set`.
8. Behavior methods for state changes (e.g., `Activate()`, `Deactivate()`, `Update...(...)`).
9. If aggregate root: private collection for children with public `IReadOnlyCollection<>` accessor.

Also create:
- Repository interface: `src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/Repositories/I{ENTITY_NAME}Repository.cs`
- Value Objects (if any): `src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/ValueObjects/{VO_NAME}.cs`
- Domain Events (if any): `src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/Events/{ENTITY_NAME}{PastVerb}Event.cs`

## Output

Provide complete file contents with correct namespaces following the pattern:
```
namespace Product.Template.Core.{MODULE_NAME}.Domain.Entities;
```

