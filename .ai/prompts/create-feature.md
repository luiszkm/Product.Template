# Prompt: Create Feature

> Use this prompt to scaffold a complete vertical slice (entity → handler → endpoint → test).

---

## Context

Read these files before starting:
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

## Instruction

Create a complete feature for module **`{MODULE_NAME}`** with entity **`{ENTITY_NAME}`**.

### Deliver ALL of the following:

#### 1. Domain Layer (`src/Core/{MODULE_NAME}/{MODULE_NAME}.Domain/`)
- [ ] Entity class inheriting from `Entity` or `AggregateRoot`
- [ ] Implement `IMultiTenantEntity`
- [ ] Private constructor + static `Create(...)` factory
- [ ] Behavior methods for state changes
- [ ] Value Objects (if applicable)
- [ ] Domain Events (if applicable)
- [ ] Repository interface: `I{ENTITY_NAME}Repository : IBaseRepository<{ENTITY_NAME}>`

#### 2. Application Layer (`src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/`)
- [ ] `Create{ENTITY_NAME}Command` + `Create{ENTITY_NAME}CommandHandler`
- [ ] `Update{ENTITY_NAME}Command` + `Update{ENTITY_NAME}CommandHandler`
- [ ] `Delete{ENTITY_NAME}Command` + `Delete{ENTITY_NAME}CommandHandler`
- [ ] `Get{ENTITY_NAME}ByIdQuery` + `Get{ENTITY_NAME}ByIdQueryHandler`
- [ ] `List{ENTITY_NAME}Query` + `List{ENTITY_NAME}QueryHandler`
- [ ] `{ENTITY_NAME}Output` record
- [ ] `{ENTITY_NAME}Mapper` extension methods
- [ ] FluentValidation validators for each command

#### 3. Infrastructure Layer (`src/Core/{MODULE_NAME}/{MODULE_NAME}.Infrastructure/`)
- [ ] `{ENTITY_NAME}Repository` implementation
- [ ] EF Core `{ENTITY_NAME}Configurations`
- [ ] Add `DbSet<{ENTITY_NAME}>` to `AppDbContext`
- [ ] `{ENTITY_NAME}Seeder` (if applicable)
- [ ] `DependencyInjection.cs` registering the repository

#### 4. API Layer (`src/Api/`)
- [ ] `{MODULE_NAME}Controller` in `Controllers/v1/`
- [ ] CRUD endpoints with correct policies
- [ ] Wire DI in `CoreConfiguration.cs`
- [ ] Register assembly in `KernelConfigurations.cs`

#### 5. Tests
- [ ] Unit tests for command handlers
- [ ] Unit tests for validators
- [ ] Integration tests for authorization
- [ ] Update `docs/security/RBAC_MATRIX.md`

### Output Format

For each file, provide:
```
### File: `{full/path/to/file.cs}`
{complete file content}
```

