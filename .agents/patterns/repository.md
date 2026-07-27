# Pattern: Repository

> Persistence abstraction for aggregate roots only. Interface in Domain, implementation in Infrastructure.

## When to use

- One repository per **aggregate root**
- Child entities accessed through aggregate root's repository with `Include`/`ThenInclude`

**No repository for:** child entities, join tables, read-only projections (use read service for complex reads).

## File locations

```
src/Core/{Module}/{Module}.Domain/Repositories/I{Aggregate}Repository.cs
src/Core/{Module}/{Module}.Infrastructure/Data/Persistence/{Aggregate}Repository.cs
src/Core/{Module}/{Module}.Infrastructure/DependencyInjection.cs   # registration
```

## Structure checklist

- [ ] Interface: `I{Aggregate}Repository : IBaseRepository<{Aggregate}>`
- [ ] Implementation injects `AppDbContext` only
- [ ] Methods return concrete types or `Task<T?>` — **never** `IQueryable<T>`
- [ ] `GetByIdAsync` uses `Include`/`ThenInclude` when aggregate has children
- [ ] `ListAllAsync` accepts `ListInput`, applies `Skip`/`Take` at DB level
- [ ] Registered in `DependencyInjection.cs` as `AddScoped` or `AddTransient`
- [ ] Tenant filter applied automatically via EF global query filters — no manual `TenantId` in every query unless bypassing (requires justification)

## Interface template

```csharp
using Kernel.Domain.SeedWorks;
using Product.Template.Core.{Module}.Domain.Entities;

namespace Product.Template.Core.{Module}.Domain.Repositories;

public interface I{Aggregate}Repository : IBaseRepository<{Aggregate}>
{
    Task<{Aggregate}?> GetBy{UniqueField}Async(string value, CancellationToken cancellationToken = default);
}
```

`IBaseRepository<T>` provides: `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `ListAllAsync`.

## Implementation template

```csharp
using Microsoft.EntityFrameworkCore;
using Product.Template.Core.{Module}.Domain.Entities;
using Product.Template.Core.{Module}.Domain.Repositories;
using Product.Template.Kernel.Domain.SeedWorks;
using Product.Template.Kernel.Infrastructure.Persistence;

namespace Product.Template.Core.{Module}.Infrastructure.Data.Persistence;

public sealed class {Aggregate}Repository : I{Aggregate}Repository
{
    private readonly AppDbContext _context;

    public {Aggregate}Repository(AppDbContext context) => _context = context;

    public async Task<{Aggregate}?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.{Aggregates}
            .Include(a => a.{ChildCollection})  // when needed
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task AddAsync({Aggregate} entity, CancellationToken cancellationToken = default)
    {
        await _context.{Aggregates}.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync({Aggregate} entity, CancellationToken cancellationToken = default)
    {
        _context.{Aggregates}.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync({Aggregate} entity, CancellationToken cancellationToken = default)
    {
        entity.SoftDelete();  // prefer soft delete via Entity base
        _context.{Aggregates}.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<PaginatedListOutput<{Aggregate}>> ListAllAsync(
        ListInput listInput,
        CancellationToken cancellationToken = default)
    {
        var query = _context.{Aggregates}.AsQueryable();

        if (!string.IsNullOrWhiteSpace(listInput.SearchTerm))
        {
            var term = listInput.SearchTerm.Trim();
            query = query.Where(a => a.Name.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((listInput.PageNumber - 1) * listInput.PageSize)
            .Take(listInput.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedListOutput<{Aggregate}>(
            listInput.PageNumber, listInput.PageSize, totalCount, items);
    }
}
```

## DI registration

```csharp
services.AddScoped<I{Aggregate}Repository, {Aggregate}Repository>();
```

## Anti-patterns

- ❌ Returning `IQueryable<T>` to Application layer
- ❌ Repository for child entity (`UserRoleRepository`)
- ❌ Business logic in repository (validation, event raising)
- ❌ `IgnoreQueryFilters()` without documented reason

## Reference

- Live: `IUserRepository`, `UserRepository` in `src/Core/Identity/`
- Rules: `.cursor/rules/infrastructure.mdc`, `domain.mdc`
- Skills: `/new-entity`, `/new-feature`, `/optimize-query`
- Checklist: `.agents/checklists/persistence.md` § Repository
