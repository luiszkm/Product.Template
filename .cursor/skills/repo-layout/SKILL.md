---
name: repo-layout
version: 1
description: "Show the repository folder structure, explain where to place new files, or answer where a specific file type belongs. TRIGGER: \"where to put file\", \"folder structure\", \"new module path\", \"scaffold location\", \"where does X go\", \"project layout\", \"directory structure\". SKIP: do not trigger during normal code generation unless file placement is ambiguous."
tools: Read, Glob
disable-model-invocation: true
---

# Skill: /repo-layout

> Reference for repository folder structure — where to place new files and how the project tree is organized.

## Arguments

`$ARGUMENTS` format: optional — `{FILE_TYPE}` or `{MODULE_NAME}`

Examples:
- `/repo-layout` — show full structure
- `/repo-layout handler` — show where handlers go
- `/repo-layout Identity` — show Identity module structure

## Root structure

```
{ProjectName}/
├── .agents/                      # Agent harness (checklists, patterns, examples)
│   ├── checklists/
│   ├── examples/
│   └── patterns/
├── .cursor/                      # Cursor rules and skills
│   ├── rules/
│   └── skills/
├── .github/                      # GitHub workflows, PR templates
├── docs/
│   └── security/
│       └── RBAC_MATRIX.md        # Authorization matrix (enforced by tests)
├── src/
│   ├── Api/                      # ASP.NET Core host
│   ├── Core/                     # Bounded contexts (modules)
│   │   ├── Identity/             # Users + Authentication
│   │   ├── Authorization/        # Roles, Permissions, UserAssignments
│   │   └── Tenants/              # Tenant domain entity + management
│   │       └── {Module}/
│   │           ├── {Module}.Domain/
│   │           ├── {Module}.Application/
│   │           └── {Module}.Infrastructure/
│   ├── Shared/
│   │   ├── Kernel.Domain/        # Base types (Entity, AggregateRoot, SeedWorks)
│   │   ├── Kernel.Application/   # CQRS interfaces, Behaviors, Exceptions
│   │   └── Kernel.Infrastructure/ # EF Core, Security, MultiTenancy, Migrations
│   └── Tools/
│       └── Migrator/             # Database migration tool
├── tests/
│   ├── ArchitectureTests/
│   ├── CommonTests/
│   ├── E2ETests/
│   ├── IntegrationTests/
│   └── UnitTests/
├── .editorconfig
├── Directory.Build.props
├── {ProjectName}.sln
├── README.md
├── CONTRIBUTING.md
└── global.json
```

## Module structure (e.g., Identity)

```
src/Core/Identity/
├── Identity.Domain/
│   ├── Entities/
│   │   ├── User.cs               # AggregateRoot
│   │   └── RefreshToken.cs       # Entity
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   └── Password.cs
│   ├── Events/
│   │   ├── UserRegisteredEvent.cs
│   │   └── UserLoggedInEvent.cs
│   ├── Repositories/
│   │   └── IUserRepository.cs
│   └── Identity.Domain.csproj
│
├── Identity.Application/
│   ├── Handlers/
│   │   └── {Feature}/
│   │       ├── Commands/
│   │       │   └── {Verb}{Noun}Command.cs
│   │       └── {Verb}{Noun}CommandHandler.cs
│   ├── Queries/
│   │   └── {Feature}/
│   │       ├── Commands/
│   │       │   └── {Get|List}{Noun}Query.cs
│   │       ├── {Get|List}{Noun}QueryHandler.cs
│   │       └── {Noun}Output.cs
│   ├── Validators/
│   │   └── {CommandName}Validator.cs
│   ├── Mappers/
│   │   └── {Noun}Mapper.cs
│   └── Identity.Application.csproj
│
└── Identity.Infrastructure/
    ├── Data/
    │   ├── Persistence/
    │   │   └── {Entity}Repository.cs
    │   └── Seeders/
    │       └── {Entity}Seeder.cs
    ├── DependencyInjection.cs
    └── Identity.Infrastructure.csproj
```

## API structure

```
src/Api/
├── Controllers/v1/{Module}Controller.cs
├── Configurations/{Feature}Configuration.cs
├── Middleware/{Purpose}Middleware.cs
├── GlobalFilter/Exceptions/ApiGlobalExceptionFilter.cs
├── HealthChecks/DatabaseHealthCheck.cs
├── ApiModels/Response/
├── Program.cs
└── Api.csproj
```

## Tests structure

```
tests/
├── ArchitectureTests/      # Layer & naming enforcement (NetArchTest)
├── UnitTests/              # Domain, handlers, validators
│   └── {Feature}/
│       ├── {Handler}Tests.cs
│       └── {Validator}Tests.cs
├── IntegrationTests/       # HTTP pipeline, authorization
│   └── {Feature}/
│       └── {Feature}IntegrationTests.cs
├── CommonTests/            # Shared fixtures (BaseFixture, Bogus)
└── E2ETests/               # RBAC matrix ↔ controller alignment
    └── Security/
        └── RbacMatrixConsistencyTests.cs
```

## Quick placement guide

| File type | Location |
|-----------|----------|
| Entity / AggregateRoot | `src/Core/{Module}/{Module}.Domain/Entities/` |
| Value Object | `src/Core/{Module}/{Module}.Domain/ValueObjects/` |
| Domain Event | `src/Core/{Module}/{Module}.Domain/Events/` |
| Repository interface | `src/Core/{Module}/{Module}.Domain/Repositories/` |
| Command | `src/Core/{Module}/{Module}.Application/Handlers/{Feature}/Commands/` |
| Command handler | `src/Core/{Module}/{Module}.Application/Handlers/{Feature}/` |
| Query | `src/Core/{Module}/{Module}.Application/Queries/{Feature}/Commands/` |
| Query handler | `src/Core/{Module}/{Module}.Application/Queries/{Feature}/` |
| Output DTO | `src/Core/{Module}/{Module}.Application/Queries/{Feature}/` |
| Mapper | `src/Core/{Module}/{Module}.Application/Mappers/` |
| Validator | `src/Core/{Module}/{Module}.Application/Validators/` |
| Repository impl | `src/Core/{Module}/{Module}.Infrastructure/Data/Persistence/` |
| EF configuration | `src/Shared/Kernel.Infrastructure/Persistence/Configurations/` |
| Seeder | `src/Core/{Module}/{Module}.Infrastructure/Data/Seeders/` |
| DI registration | `src/Core/{Module}/{Module}.Infrastructure/DependencyInjection.cs` |
| Controller | `src/Api/Controllers/v1/{Module}Controller.cs` |
| API Configuration | `src/Api/Configurations/{Feature}Configuration.cs` |
| Migration | `src/Tools/Migrator/` |
| Unit test | `tests/UnitTests/{Feature}/` |
| Integration test | `tests/IntegrationTests/{Feature}/` |

## Rules for new files

1. Never create a file outside the established structure without justification.
2. New module → create the full `.Domain` / `.Application` / `.Infrastructure` triple.
3. New controller → `src/Api/Controllers/v1/{Module}Controller.cs`.
4. New API configuration → `src/Api/Configurations/{Feature}Configuration.cs`.
5. Authorization entities (Role, Permission, RolePermission, UserAssignment) → `Authorization.Domain`, not `Identity.Domain`.
6. Tenant entity (`Tenant`) → `Tenants.Domain`; does NOT implement `IMultiTenantEntity`.
7. Design-time factory for `AppDbContext` → `src/Api/` (references all Infrastructure assemblies).
