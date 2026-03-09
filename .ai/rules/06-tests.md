# 06 — Test Rules

## Test Projects

| Project | Purpose | Framework |
|---------|---------|-----------|
| `tests/UnitTests` | Domain logic, handlers, validators, security attribute coverage | xUnit + Bogus |
| `tests/IntegrationTests` | HTTP pipeline, authorization, middleware | xUnit + `WebApplicationFactory` |
| `tests/ArchitectureTests` | Layer dependencies, naming conventions, CQRS completeness | xUnit + NetArchTest |
| `tests/CommonTests` | Shared fixtures and helpers | Bogus |
| `tests/E2ETests` | Full end-to-end against running instance | xUnit (future) |

## Naming Conventions

```
{SystemUnderTest}_{Scenario}_{ExpectedResult}
```

Examples:
- `GetById_ShouldReturnForbid_WhenUserIsNotOwnerAndNotAdmin`
- `AddUserRole_ShouldBeIdempotent_WhenUserAlreadyHasRole`
- `ProtectedActions_ShouldUseExplicitPolicy_WhenAuthorizeIsPresent`

## Unit Tests

### What to test
- **Domain entities**: factory methods, behavior methods, invariant enforcement.
- **Handlers**: command/query handler logic with fake repositories.
- **Validators**: FluentValidation rules with valid/invalid inputs.
- **Security attributes**: reflection tests ensuring correct `[Authorize(Policy)]` on controller actions.

### Patterns
- Use **fakes/stubs** (in-test sealed classes) instead of mocking frameworks for repositories and services.
- Use `NullLogger<T>.Instance` for logger dependencies.
- Group fakes at the bottom of the test class or in a shared inner class.
- Use `BaseFixture` from `CommonTests` for random data generation.

### Structure
```
tests/UnitTests/
├── {Feature}/
│   ├── {HandlerName}Tests.cs
│   └── {ValidatorName}Tests.cs
├── Security/
│   ├── AuthorizationPolicyCoverageTests.cs
│   ├── IdentityControllerAuthorizationTests.cs
│   └── RbacMatrixConsistencyTests.cs
└── MultiTenancy/
    └── ...
```

## Integration Tests

### What to test
- **Authorization enforcement** (401/403 for missing/wrong roles).
- **Request pipeline** (middleware behavior).
- **Full HTTP round-trip** for critical endpoints.

### Patterns
- Use `WebApplicationFactory<Program>` with a test authentication scheme (`TestAuthHandler`).
- Inject roles/permissions via custom headers (`X-Test-Roles`, `X-Test-Permissions`, `X-Test-UserId`).
- Always send `X-Tenant: public` header.
- Assert on HTTP status codes, not response bodies (for auth tests).

## Architecture Tests

### What to test
- **Layer dependency violations** (Domain must not reference Infrastructure).
- **Naming conventions** (Handlers end with `Handler`, Validators end with `Validator`).
- **CQRS completeness** (every `ICommand`/`IQuery` has a corresponding handler).
- **Entities inherit from `Entity` or `AggregateRoot`**.

## RBAC Matrix Consistency Tests

The test `RbacMatrixConsistencyTests` reads `docs/security/RBAC_MATRIX.md` and verifies:
- Every protected endpoint in the controller is present in the matrix.
- The policy in the matrix matches the `[Authorize(Policy)]` attribute.
- This test MUST pass in CI. Any new endpoint requires an update to the matrix.

## Minimum Coverage Requirements

- Every **command handler** must have at least one happy-path and one failure test.
- Every **validator** must have tests for required fields and edge cases.
- Every **protected endpoint** must be covered by an authorization integration test.
- Every **new module** must have architecture tests verifying layer boundaries.

