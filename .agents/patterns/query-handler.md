# Pattern: Query Handler

> CQRS read-side handler: loads data, maps to Output DTO. Never mutates state or commits.

## When to use

- Read-only use cases (get by ID, list, search, projections)
- One handler per query — never call another handler

## File locations

```
src/Core/{Module}/{Module}.Application/
├── Queries/{Feature}/Commands/{Get|List}{Noun}Query.cs
├── Queries/{Feature}/{Get|List}{Noun}QueryHandler.cs
├── Mappers/{Noun}Mapper.cs
└── Queries/{Feature}/{Noun}Output.cs
```

## Structure checklist

- [ ] Query is a `record` implementing `IQuery<{Noun}Output>` or `IQuery<PaginatedListOutput<{Noun}Output>>`
- [ ] Handler implements `IQueryHandler<{Query}, {Response}>`
- [ ] Handler returns Output DTO — **never** domain entity
- [ ] Handler does **NOT** call `IUnitOfWork.Commit()`
- [ ] `CancellationToken` forwarded to repository calls
- [ ] `NotFoundException` when entity missing (single-item queries)
- [ ] List queries inherit `ListInput` for pagination params
- [ ] Structured logging on entry and success

## Responsibility split

| Query handler does | Query handler does NOT |
|---|---|
| Load via repository / read service | Validate input shape (→ validator if complex) |
| Map entity → Output | Mutate entities |
| Throw `NotFoundException` | Call `Commit()` |
| Return paginated wrapper for lists | Call another handler |

## Annotated templates

### Get-by-ID query

```csharp
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.{Module}.Application.Queries.{Feature};

public sealed record Get{Noun}ByIdQuery(Guid Id) : IQuery<{Noun}Output>;
```

```csharp
public sealed class Get{Noun}ByIdQueryHandler : IQueryHandler<Get{Noun}ByIdQuery, {Noun}Output>
{
    private readonly I{Aggregate}Repository _repository;
    private readonly ILogger<Get{Noun}ByIdQueryHandler> _logger;

    public Get{Noun}ByIdQueryHandler(
        I{Aggregate}Repository repository,
        ILogger<Get{Noun}ByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<{Noun}Output> Handle(Get{Noun}ByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching {Noun} {EntityId}", nameof({Noun}), request.Id);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"{nameof({Noun})} with ID {request.Id} not found.");

        return entity.ToOutput();
    }
}
```

### Paginated list query

```csharp
using Product.Template.Kernel.Application.Messaging.Interfaces;
using Product.Template.Kernel.Domain.SeedWorks;

public sealed record List{Noun}Query() : ListInput, IQuery<PaginatedListOutput<{Noun}Output>>;
```

```csharp
public sealed class List{Noun}QueryHandler : IQueryHandler<List{Noun}Query, PaginatedListOutput<{Noun}Output>>
{
    private readonly I{Aggregate}Repository _repository;

    public List{Noun}QueryHandler(I{Aggregate}Repository repository) => _repository = repository;

    public async Task<PaginatedListOutput<{Noun}Output>> Handle(
        List{Noun}Query request,
        CancellationToken cancellationToken)
    {
        var page = await _repository.ListAllAsync(request, cancellationToken);

        return new PaginatedListOutput<{Noun}Output>(
            PageNumber: page.PageNumber,
            PageSize: page.PageSize,
            TotalCount: page.TotalCount,
            Data: page.Data.ToOutputList().ToList());
    }
}
```

## Performance notes

- Read-only queries: repository should use `.AsNoTracking()` internally
- Project with `.Select()` when loading large aggregates is unnecessary
- Pagination at DB level — never `ToList()` then `Skip`/`Take` in handler
- For complex reads → Dapper read service (see `/optimize-query`)

## Verification after scaffold

```bash
dotnet build
dotnet test tests/UnitTests --filter "FullyQualifiedName~{Get|List}{Noun}QueryHandler"
```

## Reference

- Live: `GetUserByIdQueryHandler`, `ListUserQueryHandler` in `src/Core/Identity/`
- Rules: `.cursor/rules/application.mdc`, `infrastructure.mdc`
- Skills: `/new-query`, `/optimize-query`
- Checklist: `.agents/checklists/new-feature.md` § Application Layer
