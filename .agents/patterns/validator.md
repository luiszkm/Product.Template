# Pattern: Validator (FluentValidation)

> Input shape validation at the MediatR pipeline boundary. Business rules belong in the handler.

## When to use

- **Every command** must have a validator
- Queries only when input is complex (multi-field filters, custom formats)

## File location

```
src/Core/{Module}/{Module}.Application/Validators/{CommandName}Validator.cs
```

## Structure checklist

- [ ] Class: `{CommandName}Validator : AbstractValidator<{CommandName}>`
- [ ] Validates **shape**: required, length, format, range
- [ ] Does **NOT** check uniqueness, existence, or cross-entity rules (→ handler)
- [ ] Error messages are user-facing (can be PT-BR or EN)
- [ ] Unit tests cover: valid input, each required field empty, edge cases

## Responsibility split

| Validator checks | Handler checks |
|---|---|
| `NotEmpty`, `MaximumLength`, `MinimumLength` | Entity exists / not found |
| Email format, regex patterns | Duplicate email / unique constraint |
| Password complexity rules | Tenant resolved, config flags |
| Guid not empty | Authorization / ownership |

## Annotated template

```csharp
using FluentValidation;
using Product.Template.Core.{Module}.Application.Handlers.{Feature}.Commands;

namespace Product.Template.Core.{Module}.Application.Validators;

public sealed class {Verb}{Noun}CommandValidator : AbstractValidator<{Verb}{Noun}Command>
{
    public {Verb}{Noun}CommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Id)
            .NotEmpty().When(x => x.Id != Guid.Empty); // route/body ID checks
    }
}
```

## Pipeline integration

Validators run automatically via `ValidationBehavior` before the handler:

```
Request → ValidationBehavior → LoggingBehavior → PerformanceBehavior → Handler
              ↓ failure
         ValidationException (400)
```

No manual `ValidateAsync` call needed in handlers.

## Unit test template

```csharp
namespace UnitTests.Validators;

public class {Verb}{Noun}CommandValidatorTests
{
    private readonly {Verb}{Noun}CommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldPass_WhenCommandIsValid()
    {
        var result = await _validator.ValidateAsync(new {Verb}{Noun}Command(/* valid */));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenEmailIsEmpty()
    {
        var result = await _validator.ValidateAsync(new {Verb}{Noun}Command(/* empty email */));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof({Verb}{Noun}Command.Email));
    }
}
```

Naming: `{Method}_{Scenario}_{ExpectedResult}` per `tests.mdc`.

## Reference

- Live: `RegisterUserCommandValidator` + `RegisterUserCommandValidatorTests`
- Rules: `.cursor/rules/application.mdc`, `tests.mdc`
- Skills: `/new-command`, `/test-writer`
- Pattern: `.agents/patterns/unit-test-handler.md` (validator test variant)
