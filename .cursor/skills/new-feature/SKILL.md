---
name: new-feature
version: 1
description: "Scaffold a complete vertical slice — Entity + Command + Query + Handler + Validator + Mapper + Controller endpoint + EF config + Integration tests — in an existing module. TRIGGER: \"add feature\", \"implement feature\", \"new feature for\", \"build the X feature\", \"I need the full stack for\". SKIP: use new-module when you need a new bounded context; use new-command or new-query for partial slices."
tools: Read, Edit, Write, Bash, Glob, Grep
disable-model-invocation: true
---

# Skill: /new-feature

> Generates a complete vertical slice for a new entity within an existing or new module.

## Arguments

`$ARGUMENTS` format: `{MODULE_NAME} {ENTITY_NAME}`

Example: `/new-feature Products Product`

## Context — invariants (rules)

- `.cursor/rules/global.mdc`
- `.cursor/rules/architecture.mdc`
- `.cursor/rules/domain.mdc`
- `.cursor/rules/application.mdc`
- `.cursor/rules/infrastructure.mdc`
- `.cursor/rules/api.mdc`
- `.cursor/rules/tests.mdc`
- `.agents/checklists/new-feature.md`
- `.agents/patterns/domain-aggregate.md`
- `.agents/patterns/domain-value-object.md`
- `.agents/patterns/domain-event.md`
- `.agents/patterns/command-handler.md`
- `.agents/patterns/query-handler.md`
- `.agents/patterns/validator.md`
- `.agents/patterns/repository.md`
- `.agents/patterns/ef-configuration.md`
- `.agents/patterns/controller-endpoint.md`
- `.agents/patterns/unit-test-handler.md`
- `.agents/patterns/integration-test-auth.md`
- `src/Core/Identity/` — canonical reference implementation

## Context — invoke if needed (skills)

- `/naming-conventions` — when naming is ambiguous
- `/repo-layout` — when file placement is unclear

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

After all files, print the checklist from `.agents/checklists/new-feature.md` with completed items ticked.

### Verification

Run before declaring done:

```bash
dotnet build
dotnet test tests/ArchitectureTests
dotnet test tests/UnitTests --filter "FullyQualifiedName~{MODULE_NAME}"
dotnet test tests/IntegrationTests --filter "FullyQualifiedName~{MODULE_NAME}"  # if integration tests written
```
