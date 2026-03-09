# 12 — Folder Structure

## Root

```
Product.Template/
├── .ai/                          # AI-first documentation (rules, prompts, checklists)
│   ├── rules/
│   ├── prompts/
│   ├── checklists/
│   └── examples/
├── .github/                      # GitHub workflows, PR templates
├── docs/
│   └── security/
│       └── RBAC_MATRIX.md        # Authorization matrix (enforced by tests)
├── src/
│   ├── Api/                      # ASP.NET Core host
│   ├── Core/                     # Bounded contexts (modules)
│   │   └── {Module}/
│   │       ├── {Module}.Domain/
│   │       ├── {Module}.Application/
│   │       └── {Module}.Infrastructure/
│   ├── Shared/
│   │   ├── Kernel.Domain/        # Base types (Entity, AggregateRoot, SeedWorks)
│   │   ├── Kernel.Application/   # CQRS interfaces, Behaviors, Exceptions
│   │   └── Kernel.Infrastructure/ # EF Core, Security, MultiTenancy
│   └── Tools/
│       └── Migrator/             # Database migration tool
├── tests/
│   ├── ArchitectureTests/        # Layer & convention enforcement
│   ├── CommonTests/              # Shared fixtures (Bogus)
│   ├── E2ETests/                 # End-to-end tests
│   ├── IntegrationTests/         # HTTP-level tests
│   └── UnitTests/                # Domain, handler, validator tests
├── .editorconfig
├── Directory.Build.props
├── Product.Template.sln
├── README.md
├── CONTRIBUTING.md
└── global.json
```

## Module Structure (e.g., Identity)

```
src/Core/Identity/
├── Identity.Domain/
│   ├── Entities/
│   │   ├── User.cs               # AggregateRoot
│   │   ├── Role.cs               # Entity
│   │   ├── Permission.cs         # Entity
│   │   ├── UserRole.cs           # Join entity
│   │   └── RolePermission.cs     # Join entity
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   └── Password.cs
│   ├── Events/
│   │   ├── UserRegisteredEvent.cs
│   │   └── UserLoggedInEvent.cs
│   ├── Repositories/
│   │   ├── IUserRepository.cs
│   │   └── IRoleRepository.cs
│   └── Identity.Domain.csproj
│
├── Identity.Application/
│   ├── Handlers/
│   │   ├── Auth/
│   │   │   ├── Commands/
│   │   │   │   ├── LoginCommand.cs
│   │   │   │   └── ExternalLoginCommand.cs
│   │   │   ├── LoginCommandHandler.cs
│   │   │   ├── ExternalLoginCommandHandler.cs
│   │   │   └── AuthTokenOutput.cs
│   │   ├── Role/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateRoleCommand.cs
│   │   │   │   ├── UpdateRoleCommand.cs
│   │   │   │   └── DeleteRoleCommand.cs
│   │   │   ├── CreateRoleCommandHandler.cs
│   │   │   ├── UpdateRoleCommandHandler.cs
│   │   │   └── DeleteRoleCommandHandler.cs
│   │   └── User/
│   │       ├── Commands/
│   │       │   ├── RegisterUserCommand.cs
│   │       │   ├── UpdateUserCommand.cs
│   │       │   ├── DeleteUserCommand.cs
│   │       │   ├── AddUserRoleCommand.cs
│   │       │   └── RemoveUserRoleCommand.cs
│   │       ├── RegisterUserCommandHandler.cs
│   │       ├── UpdateUserCommandHandler.cs
│   │       ├── DeleteUserCommandHandler.cs
│   │       ├── AddUserRoleCommandHandler.cs
│   │       └── RemoveUserRoleCommandHandler.cs
│   ├── Queries/
│   │   ├── Role/
│   │   │   ├── Commands/        # (query definitions)
│   │   │   │   ├── GetRoleByIdQuery.cs
│   │   │   │   └── ListRolesQuery.cs
│   │   │   ├── GetRoleByIdQueryHandler.cs
│   │   │   ├── ListRolesQueryHandler.cs
│   │   │   └── RoleOutput.cs
│   │   └── User/
│   │       ├── Commands/
│   │       │   ├── GetUserByIdQuery.cs
│   │       │   ├── ListUserQuery.cs
│   │       │   └── GetUserRolesQuery.cs
│   │       ├── GetUserByIdQueryHandler.cs
│   │       ├── ListUserQueryHandler.cs
│   │       ├── GetUserRolesQueryHandler.cs
│   │       └── UserOutput.cs
│   ├── Validators/
│   │   ├── LoginCommandValidator.cs
│   │   └── RegisterUserCommandValidator.cs
│   ├── Mappers/
│   │   └── UserMapper.cs
│   ├── Security/
│   │   └── RbacMetrics.cs
│   └── Identity.Application.csproj
│
└── Identity.Infrastructure/
    ├── Data/
    │   ├── DatabaseConfiguration.cs
    │   ├── Persistence/
    │   │   ├── UserRepository.cs
    │   │   └── RoleRepository.cs
    │   └── Seeders/
    │       ├── RoleSeeder.cs
    │       ├── PermissionSeeder.cs
    │       └── UserSeeder.cs
    ├── DependencyInjection.cs
    └── Identity.Infrastructure.csproj
```

## API Structure

```
src/Api/
├── Controllers/
│   └── v1/
│       └── IdentityController.cs
├── Configurations/
│   ├── ApiVersioningConfiguration.cs
│   ├── CachingConfiguration.cs
│   ├── CompressionConfiguration.cs
│   ├── ConnectionsConfigurations.cs
│   ├── ControllersConfigurations.cs
│   ├── CoreConfiguration.cs
│   ├── FeatureFlagsConfiguration.cs
│   ├── HealthCheckConfiguration.cs
│   ├── KernelConfigurations.cs
│   ├── OpenTelemetryConfiguration.cs
│   ├── RateLimitingConfiguration.cs
│   ├── ResilienceConfiguration.cs
│   ├── SecurityConfiguration.cs
│   └── SerilogConfiguration.cs
├── Middleware/
│   ├── IpWhitelistMiddleware.cs
│   ├── RequestDeduplicationMiddleware.cs
│   └── RequestLoggingMiddleware.cs
├── GlobalFilter/
│   └── Exceptions/
│       └── ApiGlobalExceptionFilter.cs
├── HealthChecks/
│   └── DatabaseHealthCheck.cs
├── ApiModels/
│   └── Response/
│       ├── ApiResponse.cs
│       ├── ApiResponseList.cs
│       └── ApiResponseListMeta.cs
├── Attributes/
│   └── FeatureGateAttribute.cs
├── Program.cs
└── Api.csproj
```

## Test Structure

```
tests/
├── ArchitectureTests/
│   ├── LayerDependencyTests.cs
│   ├── NamingConventionTests.cs
│   └── CqrsConventionTests.cs
├── UnitTests/
│   ├── Security/
│   │   ├── AuthorizationPolicyCoverageTests.cs
│   │   ├── IdentityControllerAuthorizationTests.cs
│   │   ├── RbacMatrixConsistencyTests.cs
│   │   └── RbacRoleManagementHandlerTests.cs
│   └── MultiTenancy/
│       ├── TenantResolverTests.cs
│       ├── SharedDbFilterTests.cs
│       └── TenantConnectionRoutingTests.cs
├── IntegrationTests/
│   └── Authorization/
│       ├── RbacEndpointAuthorizationIntegrationTests.cs
│       ├── RbacHttpAuthorizationIntegrationTests.cs
│       └── TestAuthHandler.cs
├── CommonTests/
│   └── Common/
│       └── BaseFixture.cs
└── E2ETests/
```

## Rules for New Files

1. **Never create a file outside the structure above** without updating this document.
2. **New module** → create the full `{Module}.Domain` / `{Module}.Application` / `{Module}.Infrastructure` triple.
3. **New controller** → `src/Api/Controllers/v1/{Module}Controller.cs`.
4. **New configuration** → `src/Api/Configurations/{Feature}Configuration.cs`.
5. **New test** → place in the correct test project and subfolder matching the feature.

