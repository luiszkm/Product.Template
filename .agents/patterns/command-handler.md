# Pattern: Command Handler

> CQRS write-side handler: orchestrates domain, persists, commits, returns Output DTO.

## When to use

- Any mutation use case (create, update, delete, state transition)
- One handler per command — never call another handler

## File locations

```
src/Core/{Module}/{Module}.Application/
├── Handlers/{Feature}/Commands/{Verb}{Noun}Command.cs
├── Handlers/{Feature}/{Verb}{Noun}CommandHandler.cs
├── Validators/{Verb}{Noun}CommandValidator.cs
├── Mappers/{Noun}Mapper.cs
└── Queries/{Feature}/{Noun}Output.cs   # or shared Output in Queries/
```

## Structure checklist

- [ ] Command is a `record` implementing `ICommand<{Noun}Output>` (or `ICommand` for void)
- [ ] Handler implements `ICommandHandler<{Command}, {Output}>`
- [ ] Validator exists: `{Command}Validator : AbstractValidator<{Command}>`
- [ ] Handler returns `{Noun}Output` record — **never** the domain entity
- [ ] Handler calls `await _unitOfWork.Commit(cancellationToken)` after mutations
- [ ] `CancellationToken` forwarded to all async calls
- [ ] Structured logging: `Warning` on business failure, `Information` on success
- [ ] No string interpolation in log templates

## Responsibility split

| Handler does | Handler does NOT |
|---|---|
| Load aggregate via repository | Enforce input shape (→ validator) |
| Check existence/uniqueness | Mutate entity properties directly |
| Call `entity.Create()` / behavior methods | Call another handler |
| Map entity → Output via mapper | Return domain entity |
| Commit unit of work | Catch generic `Exception` |

## Annotated template

### Command

```csharp
using Product.Template.Kernel.Application.Messaging.Interfaces;

namespace Product.Template.Core.{Module}.Application.Handlers.{Feature}.Commands;

public sealed record {Verb}{Noun}Command(
    string Field1,
    string Field2
) : ICommand<{Noun}Output>;
```

### Validator (shape only)

```csharp
using FluentValidation;

public sealed class {Verb}{Noun}CommandValidator : AbstractValidator<{Verb}{Noun}Command>
{
    public {Verb}{Noun}CommandValidator()
    {
        RuleFor(x => x.Field1)
            .NotEmpty()
            .MaximumLength(100);
    }
}
```

### Handler

```csharp
public sealed class {Verb}{Noun}CommandHandler : ICommandHandler<{Verb}{Noun}Command, {Noun}Output>
{
    private readonly I{Aggregate}Repository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<{Verb}{Noun}CommandHandler> _logger;

    public {Verb}{Noun}CommandHandler(
        I{Aggregate}Repository repository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<{Verb}{Noun}CommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<{Noun}Output> Handle({Verb}{Noun}Command request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        if (tenantId == Guid.Empty)
            throw new BusinessRuleException("Tenant must be resolved.");

        var existing = await _repository.GetByUniqueFieldAsync(request.Field1, cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning("Duplicate {Field1}: {Value}", nameof(request.Field1), request.Field1);
            throw new BusinessRuleException("Already exists.");
        }

        var entity = Domain.Entities.{Aggregate}.Create(tenantId, request.Field1, request.Field2);

        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation("{Aggregate} created: {EntityId}", nameof(Domain.Entities.{Aggregate}), entity.Id);
        return entity.ToOutput();
    }
}
```

### Mapper

```csharp
public static class {Noun}Mapper
{
    public static {Noun}Output ToOutput(this {Aggregate} entity) =>
        new(entity.Id, entity.Field1, entity.CreatedAt);
}
```

### Output DTO

```csharp
public sealed record {Noun}Output(Guid Id, string Field1, DateTime CreatedAt);
```

## Verification after scaffold

```bash
dotnet build
dotnet test tests/UnitTests --filter "FullyQualifiedName~{Verb}{Noun}CommandHandler"
```

## Reference

- Live: `RegisterUserCommand` + `RegisterUserCommandHandler` + `RegisterUserCommandValidator` in `src/Core/Identity/`
- Rules: `.cursor/rules/application.mdc`, `observability.mdc`
- Skills: `/new-command`, `/test-writer`
- Checklist: `.agents/checklists/new-feature.md` § Application Layer
