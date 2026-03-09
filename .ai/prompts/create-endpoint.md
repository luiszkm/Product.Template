# Prompt: Create Endpoint

> Add a new API endpoint following the template's controller conventions.

---

## Context Files
- `.ai/rules/05-api.md`
- `.ai/rules/08-security.md`
- `.ai/rules/11-naming.md`
- `.ai/checklists/api-endpoint.md`
- Reference: `src/Api/Controllers/v1/IdentityController.cs`

## Instruction

Add a **`{HTTP_METHOD}`** endpoint at **`{ROUTE}`** in the **`{MODULE_NAME}Controller`**.

Requirements:
1. Controller location: `src/Api/Controllers/v1/{MODULE_NAME}Controller.cs`.
2. If the controller doesn't exist, create it with:
   - `[ApiController]`, `[ApiVersion("1.0")]`, `[Route("api/v{version:apiVersion}/[controller]")]`
   - Constructor injection of `IMediator` and `ILogger<{MODULE_NAME}Controller>`
3. Action method must:
   - Use the correct HTTP attribute (`[HttpGet]`, `[HttpPost]`, etc.).
   - Accept `CancellationToken cancellationToken` as last parameter.
   - Use `[Authorize(Policy = SecurityConfiguration.{POLICY})]` or `[AllowAnonymous]`.
   - Declare `[ProducesResponseType]` for all expected status codes.
   - Send a command/query via `_mediator.Send(...)`.
   - Return `ActionResult<{OutputType}>`.
4. The command/query being dispatched must already exist or be created.

## Also Required
- Update `docs/security/RBAC_MATRIX.md` with the new endpoint row.
- Create an integration test in `tests/IntegrationTests/Authorization/` verifying 401/403/200.
- If a new policy is needed, add it to `SecurityConfiguration.cs`.

## Output Format
```
### File: `src/Api/Controllers/v1/{MODULE_NAME}Controller.cs`
(action method code)

### File: `docs/security/RBAC_MATRIX.md`
(new row in the table)

### File: `tests/IntegrationTests/Authorization/{MODULE_NAME}AuthorizationTests.cs`
(integration test)
```

