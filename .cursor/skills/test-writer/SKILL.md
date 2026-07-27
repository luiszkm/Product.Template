---
name: test-writer
version: 2
description: "Scaffold missing tests for an existing handler, entity, or feature — unit tests, integration tests, or both. TRIGGER: \"write tests for\", \"add tests\", \"missing tests\", \"test coverage\", \"scaffold tests\", \"generate tests for\". SKIP: creating new features (use new-feature), creating new commands (use new-command), creating new queries (use new-query)."
tools: Read, Edit, Write, Glob, Grep
---

# Skill: /test-writer

> Writes unit and/or integration tests for existing code following the project's testing conventions (xUnit + Bogus, no mocking frameworks, inline fakes/stubs).

## Arguments

`$ARGUMENTS` format: path to a handler, entity, controller, or module directory

Examples:
- `/test-writer src/Core/Identity/Identity.Application/Commands/RegisterUser/`
- `/test-writer src/Core/Authorization/Authorization.Application/`
- `/test-writer src/Api/Controllers/UsersController.cs`

## Context — invariants (rules)

- `.cursor/rules/tests.mdc` — naming, forbidden patterns, minimum coverage
- `.cursor/rules/application.mdc` — CQRS patterns (to understand what to test)

## Context — reference

- `src/Core/Identity/` — canonical reference for test patterns

## Test projects

| Project | Purpose | Framework |
|---------|---------|-----------|
| `tests/UnitTests` | Domain logic, handlers, validators, security attribute coverage | xUnit + Bogus |
| `tests/IntegrationTests` | HTTP pipeline, authorization, middleware | xUnit + `WebApplicationFactory` |
| `tests/ArchitectureTests` | Layer dependencies, naming conventions, CQRS completeness | xUnit + NetArchTest |
| `tests/CommonTests` | Shared fixtures and helpers | Bogus |
| `tests/E2ETests` | Full end-to-end against running instance | xUnit (future) |

## What to test by type

| Subject | Scope |
|---------|-------|
| Domain entities | Factory methods, behavior methods, invariant enforcement |
| Command handlers | Happy path, NotFoundException, BusinessRuleException, domain events |
| Query handlers | Happy path returning correct Output, NotFoundException |
| Validators | Required fields empty → error; max lengths exceeded; valid input passes |
| Controllers | Authorization (401 no auth, 403 wrong role, 200 correct role) |
| Architecture | Layer dependency violations, naming conventions, CQRS completeness |

## Folder structure

```
tests/
├── UnitTests/
│   ├── {Feature}/
│   │   ├── {HandlerName}Tests.cs
│   │   └── {ValidatorName}Tests.cs
│   ├── Security/
│   │   ├── AuthorizationPolicyCoverageTests.cs
│   │   └── RbacMatrixConsistencyTests.cs
│   └── MultiTenancy/
├── IntegrationTests/
│   └── {Module}/
│       └── {Module}AuthorizationTests.cs
└── ArchitectureTests/
    ├── LayerDependencyTests.cs
    ├── NamingConventionTests.cs
    └── CqrsConventionTests.cs
```

## Steps

### Step 1 — Discover existing tests

Search `tests/` for any existing tests covering the target path. List what's missing.

### Step 2 — Read the implementation

Read the target handler/entity/controller. Identify:
- Happy path inputs/outputs
- Failure branches (`NotFoundException`, `BusinessRuleException`, `ValidationException`)
- Domain event assertions (if aggregate)
- Authorization requirements (if controller)

### Step 3 — Write unit tests (always)

Location: `tests/UnitTests/{Feature}/`

Naming: `{Method}_{Scenario}_{ExpectedResult}`

Patterns:
- No mocking frameworks — inline fakes/stubs as `sealed class` at bottom of test file
- Use `NullLogger<T>.Instance` for loggers
- Use `Bogus` for test data (`Faker<T>` or `new Faker()`)
- One `Assert` concept per test (multiple asserts on same concept OK)
- `FluentAssertions` for readable assertions
- Use `BaseFixture` from `CommonTests` for shared random data generation

Fake/stub template:
```csharp
// bottom of test file
private sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _data = [];
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_data.FirstOrDefault(u => u.Id == id));
    public Task AddAsync(User entity, CancellationToken ct) { _data.Add(entity); return Task.CompletedTask; }
}
```

### Step 4 — Write integration tests (if controller or endpoint with auth)

Location: `tests/IntegrationTests/{Module}/`

Patterns:
- Use `WebApplicationFactory<Program>` + `TestAuthHandler`
- **Always** send `X-Tenant: public` header
- Inject roles/permissions via `X-Test-Roles` / `X-Test-Permissions` / `X-Test-UserId` headers
- Assert HTTP status codes: `401` (no auth), `403` (wrong role), `200`/`201` (correct role)
- Assert on status codes for auth tests — response body assertions are secondary

### Step 5 — Write architecture tests (if new module added)

Check `tests/ArchitectureTests/LayerDependencyTests.cs` for the pattern and add equivalent tests for the new module's assemblies.

### Step 6 — Verify

```bash
dotnet test tests/UnitTests
dotnet test tests/IntegrationTests   # if integration tests written
dotnet test tests/ArchitectureTests  # if architecture tests written or modified
```

All tests must pass. No skipped tests.

## Output format

```
## Test Coverage: {scope}

### Tests written
- tests/UnitTests/{Feature}/... ({N} tests)
- tests/IntegrationTests/{Module}/... ({N} tests, if applicable)

### Scenarios covered
| Scenario | Type | Test method |
|----------|------|-------------|
| Happy path | Unit | Handle_ShouldReturn..._When... |
| Not found | Unit | Handle_ShouldThrowNotFoundException_When... |
| Unauthorized | Integration | Endpoint_ShouldReturn401_WhenNoToken |

### Verification
dotnet test result: All passed
```
