# 11 — Naming Conventions

## Files & Types

| Element | Pattern | Example |
|---------|---------|---------|
| **Entity** | `{Noun}` | `User.cs`, `Role.cs`, `Permission.cs` |
| **Aggregate Root** | `{Noun}` (same as entity) | `User.cs` (inherits `AggregateRoot`) |
| **Value Object** | `{Noun}` | `Email.cs`, `Password.cs` |
| **Domain Event** | `{Noun}{PastVerb}Event` | `UserRegisteredEvent.cs`, `UserLoggedInEvent.cs` |
| **Repository Interface** | `I{AggregateRoot}Repository` | `IUserRepository.cs` |
| **Repository Impl** | `{AggregateRoot}Repository` | `UserRepository.cs` |
| **Command** | `{Verb}{Noun}Command` | `RegisterUserCommand.cs`, `DeleteRoleCommand.cs` |
| **Command Handler** | `{Verb}{Noun}CommandHandler` | `RegisterUserCommandHandler.cs` |
| **Query** | `{Get\|List}{Noun}Query` | `GetUserByIdQuery.cs`, `ListRolesQuery.cs` |
| **Query Handler** | `{Get\|List}{Noun}QueryHandler` | `GetUserByIdQueryHandler.cs` |
| **Validator** | `{CommandName}Validator` | `RegisterUserCommandValidator.cs` |
| **Output DTO** | `{Noun}Output` | `UserOutput.cs`, `RoleOutput.cs`, `AuthTokenOutput.cs` |
| **Mapper** | `{Noun}Mapper` | `UserMapper.cs` (extension methods) |
| **Controller** | `{Module}Controller` | `IdentityController.cs` |
| **EF Configuration** | `{Entity}Configurations` | `UserConfigurations.cs`, `RoleConfigurations.cs` |
| **Seeder** | `{Entity}Seeder` | `UserSeeder.cs`, `RoleSeeder.cs` |
| **Middleware** | `{Purpose}Middleware` | `TenantResolutionMiddleware.cs` |
| **Interceptor** | `{Purpose}Interceptor` | `AuditableEntityInterceptor.cs` |
| **Configuration** | `{Feature}Configuration` | `SecurityConfiguration.cs`, `CachingConfiguration.cs` |
| **Behavior** | `{Purpose}Behavior` | `ValidationBehavior.cs`, `LoggingBehavior.cs` |
| **Test Class** | `{SystemUnderTest}Tests` | `LoginCommandHandlerTests.cs` |

## Namespaces

Follow folder structure → namespace mapping:

```
Product.Template.Kernel.Domain.SeedWorks
Product.Template.Kernel.Domain.MultiTenancy
Product.Template.Kernel.Application.Messaging.Interfaces
Product.Template.Kernel.Application.Security
Product.Template.Kernel.Application.Behaviors
Product.Template.Kernel.Infrastructure.Persistence
Product.Template.Kernel.Infrastructure.Security
Product.Template.Core.Identity.Domain.Entities
Product.Template.Core.Identity.Domain.ValueObjects
Product.Template.Core.Identity.Domain.Events
Product.Template.Core.Identity.Domain.Repositories
Product.Template.Core.Identity.Application.Handlers.User
Product.Template.Core.Identity.Application.Handlers.User.Commands
Product.Template.Core.Identity.Application.Queries.User
Product.Template.Core.Identity.Application.Validators
Product.Template.Core.Identity.Infrastructure.Data.Persistence
Product.Template.Core.Identity.Infrastructure.Data.Seeders
Product.Template.Api.Controllers.v1
Product.Template.Api.Configurations
Product.Template.Api.Middleware
```

## Database

| Element | Convention | Example |
|---------|-----------|---------|
| Table name | Plural PascalCase | `Users`, `Roles`, `RolePermissions` |
| Column name | PascalCase (matches property) | `FirstName`, `PasswordHash` |
| FK column | `{ReferencedEntity}Id` | `UserId`, `RoleId` |
| Index | Composite: `{TenantId, ...}` | — |

## Test Methods

```
{Method}_{Scenario}_{ExpectedBehavior}
```

Examples:
- `Handle_ShouldCreateUser_WhenInputIsValid`
- `Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist`
- `GetById_ShouldReturnForbid_WhenUserIsNotOwnerAndNotAdmin`

## Constants & Policies

- Authorization policy names: `PascalCase` string constants in `SecurityConfiguration`.
- Claim types: lowercase dot-separated in `AuthorizationClaimTypes` (e.g., `"permission"`).
- Permission names: lowercase dot-separated (e.g., `"users.read"`, `"users.manage"`).

