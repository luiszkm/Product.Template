---
description: Scaffold a DDD domain entity or aggregate root with factory method, private setters, IMultiTenantEntity, domain events, and repository interface
tools: Read, Edit, Write, Glob, Grep
disable-model-invocation: true
---

# Skill: /new-entity

> Creates a DDD domain entity (or aggregate root) following the template's domain conventions, plus its repository interface and optional value objects / domain events.

## Arguments

`$ARGUMENTS` format: `{MODULE_NAME} {ENTITY_NAME}`

Example: `/new-entity Products Product`

## Context — read these files before generating any code

- `.ai/rules/02-domain.md`
- `.ai/rules/11-naming.md`
- `.ai/rules/12-folder-structure.md`
- `.ai/prompts/create-entity.md`
- `src/Core/Identity/Identity.Domain/Entities/User.cs` — canonical reference

## Instruction

Parse `$ARGUMENTS` as `MODULE_NAME` (first token) and `ENTITY_NAME` (second token).

Determine if this is an **aggregate root** or a **child entity** — default to aggregate root unless the user specifies otherwise.

### 1. Entity class
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/Entities/{ENTITY_NAME}.cs`

Requirements:
- Inherit from `AggregateRoot` (aggregate root) or `Entity` (child entity)
- Implement `IMultiTenantEntity` (`public long TenantId { get; set; }`)
- Private parameterless constructor (required for EF Core)
- Private constructor with all parameters (called by the factory)
- Public static `Create(...)` factory method:
  - Validates all invariants — throw `ArgumentException` for violations
  - Initializes all properties
  - Raises a domain event (e.g., `{ENTITY_NAME}CreatedEvent`)
- All properties with `private set`
- Behavior methods for state changes (e.g., `Activate()`, `Deactivate()`, `Update{Property}(...)`)
- If aggregate root: private `List<{ChildEntity}>` with public `IReadOnlyCollection<{ChildEntity}>` accessor

Namespace: `Product.Template.Core.{MODULE_NAME}.Domain.Entities`

### 2. Repository interface
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/Repositories/I{ENTITY_NAME}Repository.cs`

- Extend `IBaseRepository<{ENTITY_NAME}>`
- Add domain-specific query methods if obvious from the entity (e.g., `GetByEmailAsync`)

Namespace: `Product.Template.Core.{MODULE_NAME}.Domain.Repositories`

### 3. Domain event(s) (if applicable)
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/Events/{ENTITY_NAME}CreatedEvent.cs`

- `sealed record` implementing `IDomainEvent`
- Name in past tense: `{ENTITY_NAME}{PastVerb}Event`
- Immutable payload (init-only properties)
- Include `DateTime OccurredAt { get; init; } = DateTime.UtcNow`

### 4. Value Objects (if applicable)
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/ValueObjects/{VO_NAME}.cs`

- `record` type with init-only properties
- Private constructor + public static `Create(...)` factory with validations
- Throw `DomainException` for violations (not `ArgumentException`)

## Output format

For each file:
```
### File: `{full/path/to/file.cs}`
{complete file content with correct namespaces}
```
