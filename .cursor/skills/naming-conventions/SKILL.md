---
name: naming-conventions
version: 1
description: "Look up naming conventions for any file, type, method, or database element in this project. TRIGGER: \"how to name\", \"naming convention\", \"what to call\", \"name for handler\", \"name for entity\", \"name for validator\", \"namespace for\", \"test method name\". SKIP: do not trigger during normal code generation — inline into generation tasks only when naming is explicitly ambiguous."
tools: Read
disable-model-invocation: true
---

# Skill: /naming-conventions

> Reference for all naming conventions in this repository.

## Arguments

`$ARGUMENTS` format: optional — `{ELEMENT_TYPE}` to filter

Examples:
- `/naming-conventions` — show all conventions
- `/naming-conventions handler` — show handler naming
- `/naming-conventions database` — show DB naming

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

Pattern: `{RootNamespace}.{Layer}.{Module}.{Folder}`

```
{RootNamespace}.Kernel.Domain.SeedWorks
{RootNamespace}.Kernel.Domain.MultiTenancy
{RootNamespace}.Kernel.Application.Messaging.Interfaces
{RootNamespace}.Kernel.Application.Security
{RootNamespace}.Kernel.Application.Behaviors
{RootNamespace}.Kernel.Infrastructure.Persistence
{RootNamespace}.Kernel.Infrastructure.Security
{RootNamespace}.Core.Identity.Domain.Entities
{RootNamespace}.Core.Identity.Domain.ValueObjects
{RootNamespace}.Core.Identity.Domain.Events
{RootNamespace}.Core.Identity.Domain.Repositories
{RootNamespace}.Core.Identity.Application.Handlers.User
{RootNamespace}.Core.Identity.Application.Handlers.User.Commands
{RootNamespace}.Core.Identity.Application.Queries.User
{RootNamespace}.Core.Identity.Application.Validators
{RootNamespace}.Core.Identity.Infrastructure.Data.Persistence
{RootNamespace}.Core.Identity.Infrastructure.Data.Seeders
{RootNamespace}.Api.Controllers.v1
{RootNamespace}.Api.Configurations
{RootNamespace}.Api.Middleware
```

`{RootNamespace}` = the project root namespace (e.g., `MyCompany.MyProduct`).

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

## C# member naming (quick reference)

| Element | Convention | Example |
|---------|-----------|---------|
| Class / Record / Struct | PascalCase | `RegisterUserCommand` |
| Interface | `I` + PascalCase | `IUserRepository` |
| Method | PascalCase | `GetByEmailAsync` |
| Property | PascalCase | `FirstName` |
| Private field | `_camelCase` | `_userRepository` |
| Local variable | camelCase | `userOutput` |
| Constant | PascalCase | `MaxRetryCount` |
| Enum value | PascalCase | `TenantIsolationMode.SharedDb` |
| Type parameter | `T` + PascalCase | `TResponse` |
| Async method | Suffix `Async` | `GetByIdAsync` |
