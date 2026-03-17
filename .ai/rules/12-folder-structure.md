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
│   │   └── RefreshToken.cs       # Entity
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   └── Password.cs
│   ├── Events/
│   │   ├── UserRegisteredEvent.cs
│   │   └── UserLoggedInEvent.cs
│   ├── Repositories/
│   │   ├── IUserRepository.cs
│   │   └── IRefreshTokenRepository.cs
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
│   │   └── User/
│   │       ├── Commands/
│   │       │   ├── RegisterUserCommand.cs
│   │       │   ├── UpdateUserCommand.cs
│   │       │   └── DeleteUserCommand.cs
│   │       ├── RegisterUserCommandHandler.cs
│   │       ├── UpdateUserCommandHandler.cs
│   │       └── DeleteUserCommandHandler.cs
│   ├── Queries/
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
│   ├── Permissions/
│   │   ├── IdentityPermissions.cs
│   │   └── IdentityPermissionCatalogSeeder.cs
│   └── Identity.Application.csproj
│
└── Identity.Infrastructure/
    ├── Data/
    │   ├── DatabaseConfiguration.cs
    │   ├── Persistence/
    │   │   └── UserRepository.cs
    │   └── Seeders/
    │       └── UserSeeder.cs
    ├── DependencyInjection.cs
    └── Identity.Infrastructure.csproj
```

## Authorization Module Structure

```
src/Core/Authorization/
├── Authorization.Domain/
│   ├── Entities/
│   │   ├── Role.cs               # AggregateRoot
│   │   ├── Permission.cs         # Entity
│   │   ├── UserAssignment.cs     # Join entity (UserId Guid — no User nav ref)
│   │   └── RolePermission.cs     # Join entity
│   ├── Events/
│   │   ├── RoleCreatedEvent.cs
│   │   └── UserAssignedToRoleEvent.cs
│   ├── Repositories/
│   │   ├── IRoleRepository.cs
│   │   └── IPermissionRepository.cs
│   └── Authorization.Domain.csproj
│
├── Authorization.Application/
│   ├── Handlers/
│   │   ├── Role/         # CreateRole, UpdateRole, DeleteRole, AssignPermission, etc.
│   │   ├── Permission/   # CreatePermission, UpdatePermission, DeletePermission
│   │   └── UserAssignment/ # AssignUserToRole, RevokeUserFromRole
│   ├── Queries/
│   │   ├── Role/         # GetRoleById, ListRoles
│   │   ├── Permission/   # ListPermissions
│   │   └── UserAssignment/ # GetUserAssignments
│   ├── Permissions/
│   │   ├── AuthorizationPermissions.cs
│   │   └── AuthorizationPermissionCatalogSeeder.cs
│   └── Authorization.Application.csproj
│
└── Authorization.Infrastructure/
    ├── Data/
    │   ├── Persistence/
    │   │   ├── RoleRepository.cs
    │   │   ├── PermissionRepository.cs
    │   │   └── UserAssignmentRepository.cs
    │   └── Configurations/
    │       ├── RoleConfigurations.cs
    │       ├── PermissionConfigurations.cs
    │       ├── RolePermissionConfigurations.cs
    │       └── UserAssignmentConfigurations.cs
    ├── DependencyInjection.cs
    └── Authorization.Infrastructure.csproj
```

## Tenants Module Structure

```
src/Core/Tenants/
├── Tenants.Domain/
│   ├── Entities/
│   │   └── Tenant.cs             # AggregateRoot (NOT IMultiTenantEntity)
│   ├── Events/
│   │   ├── TenantCreatedEvent.cs
│   │   └── TenantDeactivatedEvent.cs
│   ├── Repositories/
│   │   └── ITenantRepository.cs
│   └── Tenants.Domain.csproj
│
├── Tenants.Application/
│   ├── Handlers/
│   │   └── Tenant/   # CreateTenant, UpdateTenant, ActivateTenant, DeactivateTenant
│   ├── Queries/
│   │   └── Tenant/   # GetTenantById, GetTenantByKey, ListTenants
│   ├── Permissions/
│   │   ├── TenantsPermissions.cs
│   │   └── TenantsPermissionCatalogSeeder.cs
│   └── Tenants.Application.csproj
│
└── Tenants.Infrastructure/
    ├── Data/
    │   └── Persistence/
    │       └── TenantRepository.cs   # Maps TenantConfig ↔ Tenant (HostDbContext)
    ├── DependencyInjection.cs
    └── Tenants.Infrastructure.csproj
```

## API Structure

```
src/Api/
├── Controllers/
│   └── v1/
│       ├── IdentityController.cs
│       ├── AuthorizationController.cs
│       └── TenantsController.cs
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
├── AppDbContextDesignTimeFactory.cs  # EF design-time factory (all module assemblies)
├── Program.cs
└── Api.csproj
```

## Test Structure

```
tests/
├── ArchitectureTests/
│   ├── LayerDependencyTests.cs
│   ├── NamingConventionTests.cs
│   ├── TenancyInvariantTests.cs
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
6. **Authorization entities** (Role, Permission, RolePermission, UserAssignment) live in `Authorization.Domain`, not Identity.
7. **Tenant domain entity** (`Tenant`) lives in `Tenants.Domain`; does NOT implement `IMultiTenantEntity`.
8. **Design-time factory** for `AppDbContext` lives in `src/Api/` so it can reference all module Infrastructure assemblies.
