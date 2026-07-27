# Examples

> **Note:** Copy-pastable pattern docs live in `.agents/patterns/` (11/11 published). This folder is an **index** to the live Identity reference — not a duplicate of patterns.

## Purpose

Point agents and developers to the canonical reference implementation when a pattern doc needs a concrete file to compare against.

## Prefer patterns first

1. Read `.agents/patterns/{pattern}.md` for structure and invariants.
2. Use the table below only for drift checks or when the pattern doc says "see live reference".
3. Validate against `.agents/checklists/`.

The **Identity module** (`src/Core/Identity/`) is the canonical reference implementation for this template. Before creating examples here, study these files:

| Pattern | Reference File |
|---------|---------------|
| Aggregate Root entity | `src/Core/Identity/Identity.Domain/Entities/User.cs` |
| Child entity | `src/Core/Identity/Identity.Domain/Entities/UserRole.cs` |
| Value Object | `src/Core/Identity/Identity.Domain/ValueObjects/Email.cs` |
| Domain Event | `src/Core/Identity/Identity.Domain/Events/UserRegisteredEvent.cs` |
| Repository interface | `src/Core/Identity/Identity.Domain/Repositories/IUserRepository.cs` |
| Command + Handler | `src/Core/Identity/Identity.Application/Handlers/User/Commands/RegisterUserCommand.cs` + `RegisterUserCommandHandler.cs` |
| Query + Handler | `src/Core/Identity/Identity.Application/Queries/User/Commands/ListUserQuery.cs` + `ListUserQueryHandler.cs` |
| Validator | `src/Core/Identity/Identity.Application/Validators/RegisterUserCommandValidator.cs` |
| Output DTO | `src/Core/Identity/Identity.Application/Queries/User/UserOuput.cs` |
| Mapper | `src/Core/Identity/Identity.Application/Mappers/UserMapper.cs` |
| Repository implementation | `src/Core/Identity/Identity.Infrastructure/Data/Persistence/UserRepository.cs` |
| EF Configuration | `src/Shared/Kernel.Infrastructure/Persistence/Configurations/UserConfigurations.cs` |
| Seeder | `src/Core/Identity/Identity.Infrastructure/Data/Seeders/RoleSeeder.cs` |
| Controller | `src/Api/Controllers/v1/IdentityController.cs` |
| DI registration | `src/Core/Identity/Identity.Infrastructure/DependencyInjection.cs` |
| Auth handler test | `tests/UnitTests/Security/RbacRoleManagementHandlerTests.cs` |
| RBAC matrix test | `tests/E2ETests/Security/RbacMatrixConsistencyTests.cs` |
| Integration test | `tests/IntegrationTests/Authorization/RbacHttpAuthorizationIntegrationTests.cs` |
| RBAC matrix | `docs/security/RBAC_MATRIX.md` |

## How to Choose a Feature as Example

A good example feature should:

1. **Be complete** — cover all layers (Domain → Application → Infrastructure → Api → Tests).
2. **Be simple** — not require understanding of unrelated modules.
3. **Show all patterns** — CRUD, pagination, authorization, validation, events.
4. **Be real** — not a contrived "Foo/Bar" example.

The Identity module meets all these criteria.

## How Agents Should Use This Folder

1. Read the relevant **rules** from `.cursor/rules/`.
2. Read the matching **pattern** from `.agents/patterns/`.
3. Use the **reference files** table above only when you need a live drift check.
4. Validate against the **checklist** from `.agents/checklists/`.

Future standalone example write-ups (e.g. Dapper read service) may be added here; until then, patterns + Identity cover all workflows.

