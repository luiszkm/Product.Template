# Pattern: EF Configuration

> Entity Framework Core mapping for domain entities. One configuration class per entity.

## When to use

- Every new entity or aggregate root persisted via EF Core
- Before running `dotnet ef migrations add` (see `/new-migration`)

## File location

```
src/Shared/Kernel.Infrastructure/Persistence/Configurations/{Entity}Configurations.cs
```

Module-specific configs may live in `{Module}.Infrastructure/Data/Configurations/` if the entity is module-local only.

## Structure checklist

- [ ] Class: `{Entity}Configurations : IEntityTypeConfiguration<{Entity}>`
- [ ] `internal sealed` — not public API
- [ ] Table name: plural PascalCase (`.ToTable("Users")`)
- [ ] Primary key: `.HasKey(e => e.Id)` + `.ValueGeneratedNever()`
- [ ] `TenantId` required + composite index `{ TenantId, Id }` or `{ TenantId, UniqueField }`
- [ ] String columns: `HasMaxLength()` + `IsRequired()` where applicable
- [ ] Value objects: `HasConversion` or `OwnsOne`
- [ ] Relationships: explicit FK + `OnDelete` behavior
- [ ] `builder.Ignore(e => e.DomainEvents)` for aggregate roots
- [ ] `DbSet<{Entity}>` added to `AppDbContext`

## Annotated template

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Template.Core.{Module}.Domain.Entities;
using Product.Template.Core.{Module}.Domain.ValueObjects;

namespace Product.Template.Kernel.Infrastructure.Persistence.Configurations;

internal sealed class {Entity}Configurations : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.ToTable("{Entities}");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Id });

        // Value object conversion
        builder.Property(e => e.Email)
            .HasConversion(vo => vo.Value, v => Email.Create(v))
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Email })
            .IsUnique();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Child relationship
        builder.HasMany(e => e.{Children})
            .WithOne()
            .HasForeignKey(c => c.{Entity}Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.DomainEvents);
    }
}
```

## AppDbContext registration

```csharp
public DbSet<{Entity}> {Entities} => Set<{Entity}>();
```

Configurations are auto-discovered via `ApplyConfigurationsFromAssembly`.

## Index guidelines

| Query pattern | Index |
|---|---|
| Get by ID within tenant | `{ TenantId, Id }` |
| Unique field per tenant | `{ TenantId, Email }` unique |
| List sorted by date | `{ TenantId, CreatedAt }` |
| Filter by status | `{ TenantId, IsActive }` |

## Migration workflow

1. Create/update this configuration
2. Add `DbSet` to `AppDbContext`
3. Run `/new-migration` — review generated SQL
4. Validate against `.agents/checklists/persistence.md`

## Reference

- Live: `UserConfigurations.cs` in `Kernel.Infrastructure/Persistence/Configurations/`
- Rules: `.cursor/rules/infrastructure.mdc`
- Skills: `/new-migration`, `/new-entity`
- Checklist: `.agents/checklists/persistence.md` § Entity Configuration
