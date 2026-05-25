---
name: test-writer
version: 1
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

## Context — read these files before writing

- `.cursor/rules/tests.mdc`
- `.cursor/rules/application.mdc`
- `src/Core/Identity/` — canonical reference for test patterns

## Steps

### Step 1 — Discover existing tests

Search `tests/` for any existing tests covering the target path. List what's missing.

### Step 2 — Read the implementation

Read the target handler/entity/controller. Identify:
- Happy path inputs/outputs
- Failure branches (NotFoundException, BusinessRuleException, ValidationException)
- Domain event assertions (if aggregate)
- Authorization requirements (if controller)

### Step 3 — Write unit tests (always)

Location: `tests/UnitTests/Core/{Module}/{Layer}/`

```
{Method}_{Scenario}_{Result}
```

Rules:
- No mocking frameworks — inline fakes/stubs as `sealed class` at bottom of test file
- Use `NullLogger<T>.Instance` for loggers
- Use `Bogus` for test data (`Faker<T>` or `new Faker()`)
- One `Assert` concept per test (multiple asserts on same concept OK)
- `FluentAssertions` for readable assertions

### Step 4 — Write integration tests (if controller or command with side effects)

Location: `tests/IntegrationTests/Core/{Module}/`

Rules:
- Use `WebApplicationFactory<Program>` + `TestAuthHandler`
- Always send `X-Tenant: public` header
- Inject roles/permissions via `X-Test-Roles` / `X-Test-Permissions` headers
- Assert HTTP status code + response body shape
- Use `dotnet test tests/IntegrationTests --filter "FullyQualifiedName~{ClassName}"` to verify

### Step 5 — Verify

```bash
dotnet test tests/UnitTests
dotnet test tests/IntegrationTests  # if integration tests written
```

All tests must pass. No skipped tests.

## Output format

```
## Test Coverage: {scope}

### Tests written
- tests/UnitTests/Core/{Module}/... ({N} tests)
- tests/IntegrationTests/Core/{Module}/... ({N} tests, if applicable)

### Scenarios covered
| Scenario | Type | Test method |
|----------|------|-------------|
| Happy path | Unit | Handle_ShouldReturn..._When... |
| Not found | Unit | Handle_ShouldThrowNotFoundException_When... |
| Validation | Unit | Handle_ShouldThrowValidationException_When... |

### Verification
dotnet test result: ✅ All passed
```
