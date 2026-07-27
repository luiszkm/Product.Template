# Pattern: Domain Event

> Past-tense notification raised inside an aggregate when something meaningful happens.

## When to use

- State change in aggregate that other parts of the system should react to
- Cross-aggregate coordination (prefer events over handler calling handler)
- Audit trail of business occurrences

## File locations

```
src/Core/{Module}/{Module}.Domain/Events/{Noun}{PastVerb}Event.cs
src/Core/{Module}/{Module}.Application/Handlers/Events/{Noun}{PastVerb}EventHandler.cs  # optional
```

## Structure checklist

- [ ] Named past tense: `UserRegisteredEvent`, `OrderShippedEvent`
- [ ] Implements `IDomainEvent`
- [ ] `record` type with minimal payload (IDs + primitive fields, not full entities)
- [ ] `OccurredOn` timestamp (`DateTime.UtcNow` default)
- [ ] Raised via `aggregate.AddDomainEvent(...)` inside behavior method or `Create()`
- [ ] EF ignores `DomainEvents` collection in configuration
- [ ] Handlers implement `INotificationHandler<{Event}>` in Application layer

## Annotated template

### Event

```csharp
using Product.Template.Kernel.Domain.SeedWorks;

namespace Product.Template.Core.{Module}.Domain.Events;

public sealed record {Noun}{PastVerb}Event(
    Guid {Aggregate}Id,
    string RelevantField
) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
```

### Raising inside aggregate

```csharp
public static {Aggregate} Create(/* ... */)
{
    var aggregate = new {Aggregate}(/* ... */);
    aggregate.AddDomainEvent(new {Noun}{PastVerb}Event(aggregate.Id, aggregate.Field));
    return aggregate;
}

public void {BehaviorMethod}()
{
    // mutate state
    AddDomainEvent(new {Noun}{PastVerb}Event(Id, Field));
}
```

### Event handler (Application)

```csharp
using MediatR;
using Microsoft.Extensions.Logging;

namespace Product.Template.Core.{Module}.Application.Handlers.Events;

public sealed class {Noun}{PastVerb}EventHandler : INotificationHandler<{Noun}{PastVerb}Event>
{
    private readonly ILogger<{Noun}{PastVerb}EventHandler> _logger;

    public {Noun}{PastVerb}EventHandler(ILogger<{Noun}{PastVerb}EventHandler> logger)
        => _logger = logger;

    public async Task Handle({Noun}{PastVerb}Event notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "{Aggregate} {EntityId} — {EventName} processed",
            nameof({Aggregate}),
            notification.{Aggregate}Id,
            nameof({Noun}{PastVerb}Event));

        // side effects: send email, update read model, call external service via infra interface
        await Task.CompletedTask;
    }
}
```

## Rules

- Events are raised **inside** the aggregate — never constructed in handlers
- Handlers react to events — they do not mutate the originating aggregate's invariants
- Do not publish events before `Commit()` — MediatR dispatches after successful save (pipeline behavior)
- Keep payloads small — reference entities by ID

## EF configuration

```csharp
builder.Ignore(a => a.DomainEvents);
```

## Reference

- Live: `UserRegisteredEvent`, `UserLoggedInEvent`, `UserRegisteredEventHandler`
- Rules: `.cursor/rules/domain.mdc`, `application.mdc`
- Skills: `/new-entity`, `/new-feature`
- Pattern: `.agents/patterns/domain-aggregate.md`
