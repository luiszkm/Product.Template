# Pattern: Value Object

> Immutable domain primitive with validation encapsulated in a static factory.

## When to use

- Concept defined by its value, not identity (email, money, address, password rules)
- Validation belongs in the domain, not only in FluentValidation
- Used as property type on entities (`Email Email`, not `string Email`)

## File location

```
src/Core/{Module}/{Module}.Domain/ValueObjects/{Name}.cs
```

Shared VOs used across modules may live in `Kernel.Domain/ValueObjects/` (e.g., `Email`).

## Structure checklist

- [ ] C# `record` or `record class` with private constructor
- [ ] Static `Create(...)` factory — sole public construction path
- [ ] Immutable — no public setters; expose read-only `Value` property
- [ ] Validation throws `ArgumentException` on invalid input
- [ ] `ToString()` overridden when sensitive (`Password` → `"********"`)
- [ ] Optional implicit conversion to `string` when safe

## Annotated template

```csharp
namespace Product.Template.Core.{Module}.Domain.ValueObjects;

public sealed record {Name}
{
    public string Value { get; }

    private {Name}(string value) => Value = value;

    public static {Name} Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("{Name} cannot be empty.", nameof(raw));

        var normalized = raw.Trim();

        // domain-specific validation (format, length, regex)
        if (normalized.Length > 100)
            throw new ArgumentException("{Name} exceeds maximum length.", nameof(raw));

        return new {Name}(normalized);
    }

    public override string ToString() => Value;

    public static implicit operator string({Name} vo) => vo.Value;
}
```

## Sensitive value object (password)

```csharp
public sealed record Password
{
    public string Value { get; }
    private Password(string value) => Value = value;

    public static Password Create(string password)
    {
        // complexity rules
        return new Password(password);
    }

    public override string ToString() => "********";  // never log raw value
}
```

## EF Core mapping

Value objects map via `HasConversion` or `OwnsOne`:

```csharp
builder.Property(u => u.Email)
    .HasConversion(e => e.Value, v => Email.Create(v))
    .HasMaxLength(256)
    .IsRequired();
```

## Validator vs VO

| Layer | Validates |
|---|---|
| FluentValidation (command) | Input shape before domain (required, max length) |
| Value Object `Create()` | Domain rules (format, normalization, invariants) |

Both can overlap on format — VO is the source of truth for domain correctness.

## Reference

- Live: `Email.cs`, `Password.cs` in `Identity.Domain/ValueObjects/`
- Rules: `.cursor/rules/domain.mdc`
- Skills: `/new-entity`
- Checklist: `.agents/checklists/new-feature.md` § Domain Layer
