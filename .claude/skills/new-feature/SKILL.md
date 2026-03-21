---
description: Scaffold a complete vertical slice (Domain → Application → Infrastructure → API → Tests) for a new feature
tools: Read, Edit, Write, Bash, Glob, Grep
disable-model-invocation: true
---

# Skill: /new-feature

> Generates a complete vertical slice for a new entity within an existing or new module.

## Arguments

`$ARGUMENTS` format: `{MODULE_NAME} {ENTITY_NAME}`

Example: `/new-feature Products Product`

## Context — read these files before generating any code

- `.ai/rules/00-global.md`
- `.ai/rules/01-architecture.md`
- `.ai/rules/02-domain.md`
- `.ai/rules/03-application.md`
- `.ai/rules/04-infrastructure.md`
- `.ai/rules/05-api.md`
- `.ai/rules/06-tests.md`
- `.ai/rules/11-naming.md`
- `.ai/rules/12-folder-structure.md`
- `.ai/checklists/new-feature.md`
- `.ai/prompts/create-feature.md`
- `src/Core/Identity/` — canonical reference implementation

## Dynamic context

Existing modules (for naming consistency and cross-module awareness):
`!ls src/Core`

## Instruction

Parse `$ARGUMENTS` as `MODULE_NAME` (first token) and `ENTITY_NAME` (second token).

Create a complete feature for module **`{MODULE_NAME}`** with entity **`{ENTITY_NAME}`**, delivering ALL of the following files:

### 1. Domain Layer — `src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/`

- `Entities/{ENTITY_NAME}.cs`
  - Inherit `AggregateRoot` (or `Entity` if child)
  - Implement `IMultiTenantEntity` (`public long TenantId { get; set; }`)
  - Private parameterless constructor (EF Core) + private constructor for factory
  - Public static `Create(...)` factory — validates invariants, raises domain event
  - Properties with `private set`
  - Behavior methods (`Activate`, `Deactivate`, `Update…`)
- `Repositories/I{ENTITY_NAME}Repository.cs`
- `Events/{ENTITY_NAME}CreatedEvent.cs` (and others as needed)
- `ValueObjects/*.cs` (if applicable)

### 2. Application Layer — `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/`

- `Handlers/{ENTITY_NAME}/Commands/Create{ENTITY_NAME}Command.cs`
- `Handlers/{ENTITY_NAME}/Create{ENTITY_NAME}CommandHandler.cs`
- `Handlers/{ENTITY_NAME}/Commands/Update{ENTITY_NAME}Command.cs`
- `Handlers/{ENTITY_NAME}/Update{ENTITY_NAME}CommandHandler.cs`
- `Handlers/{ENTITY_NAME}/Commands/Delete{ENTITY_NAME}Command.cs`
- `Handlers/{ENTITY_NAME}/Delete{ENTITY_NAME}CommandHandler.cs`
- `Queries/{ENTITY_NAME}/Queries/Get{ENTITY_NAME}ByIdQuery.cs`
- `Queries/{ENTITY_NAME}/Get{ENTITY_NAME}ByIdQueryHandler.cs`
- `Queries/{ENTITY_NAME}/Queries/List{ENTITY_NAME}Query.cs`
- `Queries/{ENTITY_NAME}/List{ENTITY_NAME}QueryHandler.cs`
- `Queries/{ENTITY_NAME}/{ENTITY_NAME}Output.cs` (record)
- `Queries/{ENTITY_NAME}/{ENTITY_NAME}Mapper.cs`
- `Validators/Create{ENTITY_NAME}CommandValidator.cs`
- `Validators/Update{ENTITY_NAME}CommandValidator.cs`

### 3. Infrastructure Layer — `src/Core/{MODULE_NAME}/{MODULE_NAME}.Infrastructure/`

- `Data/Repositories/{ENTITY_NAME}Repository.cs`
- `Data/Persistence/Configurations/{ENTITY_NAME}Configurations.cs`
- Update `DependencyInjection.cs` to register repository

### 4. API Layer — `src/Api/`

- `Controllers/v1/{MODULE_NAME}Controller.cs` (create if not exists)
  - All CRUD actions with correct `[Authorize(Policy = SecurityConfiguration.{Policy})]`
  - `[ProducesResponseType]` for every status code
  - `CancellationToken` as last parameter
- Update `docs/security/RBAC_MATRIX.md` with new endpoint rows

### 5. Tests

- `tests/UnitTests/{ENTITY_NAME}/Create{ENTITY_NAME}CommandHandlerTests.cs`
- `tests/UnitTests/{ENTITY_NAME}/Create{ENTITY_NAME}CommandValidatorTests.cs`
- `tests/IntegrationTests/Authorization/{MODULE_NAME}AuthorizationTests.cs`

## Output format

For each file:
```
### File: `{full/path/to/file.cs}`
{complete file content with correct namespaces}
```

After all files, print the checklist from `.ai/checklists/new-feature.md` with completed items ticked.
