# Pattern: Aggregate Root

> DDD aggregate root with private constructor, static factory, invariants, and domain events.

## When to use

- Entity is the **only entry point** for modifying a cluster of related objects
- Needs a dedicated repository (`I{Aggregate}Repository`)
- Raises domain events on state changes

**Not an aggregate root:** child entities, join tables, lookup tables → use `Entity` base, no repository.

## File location

```
src/Core/{Module}/{Module}.Domain/Entities/{Aggregate}.cs
```

## Structure checklist

- [ ] Inherits `AggregateRoot` (not plain `Entity`)
- [ ] Implements `IMultiTenantEntity` (+ `ISoftDeletableEntity` via `Entity` base)
- [ ] Private parameterless constructor for EF
- [ ] Private parameterized constructor called only from `Create()`
- [ ] Static `Create(...)` factory — validates invariants, raises initial events
- [ ] All mutable properties have `private set`
- [ ] State changes via explicit behavior methods (`Deactivate()`, `UpdateProfile()`)
- [ ] Cross-aggregate references use **IDs only**, never navigation to foreign aggregates
- [ ] `TenantId` set once via private `SetTenant()` — throws on empty or change

## Annotated template

```csharp
using Product.Template.Kernel.Domain.Exceptions;
using Product.Template.Kernel.Domain.MultiTenancy;
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.{Module}.Domain.Entities;

public sealed class {Aggregate} : AggregateRoot, IMultiTenantEntity
{
    public Guid TenantId { get; private set; }
    // Value objects for validated fields; primitives for simple state
    public {ValueObject} {Property} { get; private set; }
    public bool IsActive { get; private set; }

    // EF Core requires parameterless ctor — never call from application code
    private {Aggregate}()
    {
        {Property} = null!;
    }

    private {Aggregate}(Guid id, Guid tenantId, /* ... */)
    {
        Id = id;
        SetTenant(tenantId);
        // assign fields
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static {Aggregate} Create(Guid tenantId, /* validated inputs */)
    {
        // Validate invariants here or delegate to VO.Create()
        var aggregate = new {Aggregate}(Guid.NewGuid(), tenantId, /* ... */);
        aggregate.AddDomainEvent(new {Aggregate}CreatedEvent(aggregate.Id));
        return aggregate;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        AddDomainEvent(new {Aggregate}DeactivatedEvent(Id));
    }

    private void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException("TenantId must be provided for multi-tenant entities.");
        if (TenantId != Guid.Empty && TenantId != tenantId)
            throw new DomainException("TenantId cannot be changed once set.");
        TenantId = tenantId;
    }

    void IMultiTenantEntity.AssignTenant(Guid tenantId) => SetTenant(tenantId);
}
```

## Invariants — where they live

| Concern | Layer | Exception |
|---------|-------|-----------|
| Format, length, required fields on input | FluentValidation (command) | `ValidationException` |
| Uniqueness, existence, orchestration | Command handler | `BusinessRuleException`, `NotFoundException` |
| Business rules on entity state | Aggregate behavior / `Create()` | `DomainException`, `ArgumentException` |

## Companion artifacts

| Artifact | Location |
|----------|----------|
| Domain events | `{Module}.Domain/Events/{Noun}{PastVerb}Event.cs` |
| Repository interface | `{Module}.Domain/Repositories/I{Aggregate}Repository.cs` |
| EF configuration | `Kernel.Infrastructure/Persistence/Configurations/{Aggregate}Configurations.cs` |
| Mapper | `{Module}.Application/Mappers/{Aggregate}Mapper.cs` |

## Reference

- Live: `src/Core/Identity/Identity.Domain/Entities/User.cs`
- Rules: `.cursor/rules/domain.mdc`
- Checklist: `.agents/checklists/new-feature.md` § Domain Layer
