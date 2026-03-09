# Examples

## Purpose

This folder is the place for concrete, copy-pastable examples that demonstrate how to implement common patterns in this template. Examples are more effective than rules alone because they show the **exact output** an agent or developer should produce.

## Reference Implementation

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
| Integration test | `tests/IntegrationTests/Authorization/RbacHttpAuthorizationIntegrationTests.cs` |
| RBAC matrix | `docs/security/RBAC_MATRIX.md` |

## How to Choose a Feature as Example

A good example feature should:

1. **Be complete** — cover all layers (Domain → Application → Infrastructure → Api → Tests).
2. **Be simple** — not require understanding of unrelated modules.
3. **Show all patterns** — CRUD, pagination, authorization, validation, events.
4. **Be real** — not a contrived "Foo/Bar" example.

The Identity module meets all these criteria.

## Suggested Examples to Add

When the template grows, consider adding example files here for:

| Example | Description |
|---------|-------------|
| `new-module-scaffold.md` | Step-by-step creation of a new module (e.g., `Catalog`) |
| `dapper-read-service.md` | Optimized read query using Dapper |
| `domain-event-handler.md` | Reacting to a domain event in another module |
| `feature-flag-endpoint.md` | Using `FeatureGateAttribute` to conditionally enable an endpoint |
| `custom-health-check.md` | Adding a health check for an external dependency |

## How Agents Should Use Examples

1. Read the relevant **prompt** from `.ai/prompts/`.
2. Read the relevant **rules** from `.ai/rules/`.
3. Look at the **reference files** listed above for the specific pattern.
4. Generate code that follows the same structure, naming, and conventions.
5. Validate against the **checklist** from `.ai/checklists/`.

