---
name: new-endpoint
version: 1
description: "Add a controller endpoint with the correct RBAC policy, ProducesResponseType, and CancellationToken following api.mdc. TRIGGER: \"add endpoint\", \"new route\", \"add controller action\", \"create API endpoint\", \"add a GET/POST/PUT/DELETE route\". SKIP: generating the command/query behind the endpoint (use new-command or new-query for that)."
tools: Read, Edit, Write, Glob, Grep
disable-model-invocation: true
---

# Skill: /new-endpoint

> Adds a new action to an existing (or new) controller, updates the RBAC matrix, and creates an authorization integration test.

## Arguments

`$ARGUMENTS` format: `{MODULE_NAME} {HTTP_METHOD} {ROUTE}`

Example: `/new-endpoint Products GET /products/{id}`

## Context — invariants (rules)

- `.cursor/rules/api.mdc`
- `.cursor/rules/security.mdc`
- `src/Api/Controllers/v1/IdentityController.cs` — canonical reference
- `src/Api/Configurations/SecurityConfiguration.cs` — existing policies
- `docs/security/RBAC_MATRIX.md` — existing RBAC matrix

## Context — invoke if needed (skills)

- `/naming-conventions` — when naming a new policy constant or action method

## Instruction

Parse `$ARGUMENTS` as:
- `MODULE_NAME` — first token
- `HTTP_METHOD` — second token (`GET`, `POST`, `PUT`, `DELETE`, `PATCH`)
- `ROUTE` — remaining tokens (the route template)

### Step 1 — Read the existing controller

Read `src/Api/Controllers/v1/{MODULE_NAME}Controller.cs` if it exists. If it does not exist, create the full controller scaffold first.

**New controller requirements:**
- `[ApiController]`
- `[ApiVersion("1.0")]`
- `[Route("api/v{version:apiVersion}/[controller]")]`
- Constructor injecting `IMediator` and `ILogger<{MODULE_NAME}Controller>`

### Step 2 — Add the action method

Requirements:
- Correct HTTP attribute: `[HttpGet("{id}")]`, `[HttpPost]`, etc.
- `[Authorize(Policy = SecurityConfiguration.{POLICY_CONSTANT})]` — never bare `[Authorize]`
  - Use `[AllowAnonymous]` only for explicitly public endpoints
- `[ProducesResponseType(typeof({OutputType}), StatusCodes.Status200OK)]` (or 201 for POST creating a resource)
- `[ProducesResponseType(StatusCodes.Status400BadRequest)]` for commands
- `[ProducesResponseType(StatusCodes.Status401Unauthorized)]`
- `[ProducesResponseType(StatusCodes.Status403Forbidden)]`
- `[ProducesResponseType(StatusCodes.Status404NotFound)]` for GET by ID
- `CancellationToken cancellationToken` as last parameter
- Dispatch to `_mediator.Send(...)` — zero business logic in the controller

### Step 3 — Update RBAC_MATRIX.md

Add a row to `docs/security/RBAC_MATRIX.md`:
```
| {HTTP_METHOD} {ROUTE} | {Policy} | {Roles that have access} |
```

### Step 4 — Security policy

If the required policy does not exist in `SecurityConfiguration.cs`, add:
```csharp
public const string {PolicyName} = "{policy-name}";
```

### Step 5 — Integration test

Create (or update) `tests/IntegrationTests/Authorization/{MODULE_NAME}AuthorizationTests.cs`:
- Always send `X-Tenant: public` header
- Test 401 (no auth token)
- Test 403 (authenticated but wrong role)
- Test 200/201/204 (authenticated with correct role via `X-Test-Roles` header)
- No mocking frameworks — use `WebApplicationFactory<Program>` + `TestAuthHandler`

## Output format

```
### File: `src/Api/Controllers/v1/{MODULE_NAME}Controller.cs`
(full updated or new controller)

### File: `docs/security/RBAC_MATRIX.md`
(new row added)

### File: `tests/IntegrationTests/Authorization/{MODULE_NAME}AuthorizationTests.cs`
(integration test)
```
