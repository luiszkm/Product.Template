# Pattern: Controller Endpoint

> Thin API action: authorize, dispatch MediatR, return typed HTTP result. No business logic.

## When to use

- Exposing a command or query via HTTP
- Every protected action needs explicit RBAC policy + RBAC matrix entry

## File location

```
src/Api/Controllers/v1/{Module}Controller.cs
```

## Structure checklist

- [ ] Controller: `[ApiController]`, `[ApiVersion("1.0")]`, `[Route("api/v{version:apiVersion}/[controller]")]`
- [ ] Injects `IMediator` + `ILogger<{Controller}>`
- [ ] Action ≤ ~20 lines — dispatch only
- [ ] `[Authorize(Policy = SecurityConfiguration.{Policy})]` — never bare `[Authorize]`
- [ ] Public endpoints: `[AllowAnonymous]` with documented justification
- [ ] `[ProducesResponseType]` for **every** possible status code
- [ ] `CancellationToken` as last parameter
- [ ] Commands bound with `[FromBody]`; list queries with `[FromQuery]`
- [ ] POST create → `201 CreatedAtAction`; DELETE → `204 NoContent`
- [ ] Entry added to `docs/security/RBAC_MATRIX.md` (same PR)
- [ ] Authorization integration test (401 / 403 / 200)

## HTTP status conventions

| Operation | Verb | Success | Return |
|-----------|------|---------|--------|
| Get by ID | GET | 200 | `{Noun}Output` |
| List | GET | 200 | `PaginatedListOutput<{Noun}Output>` |
| Create | POST | 201 | `{Noun}Output` + `Location` |
| Update | PUT | 200 | `{Noun}Output` |
| Delete | DELETE | 204 | (empty) |
| Action (login) | POST | 200 | `{Action}Output` |

## Annotated templates

### Protected GET

```csharp
[HttpGet("{id:guid}", Name = nameof(GetById))]
[Authorize(Policy = SecurityConfiguration.{Noun}ReadPolicy)]
[ProducesResponseType(typeof({Noun}Output), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<{Noun}Output>> GetById(Guid id, CancellationToken cancellationToken)
{
    _logger.LogInformation("Fetching {Noun} {EntityId}", nameof({Noun}), id);
    var result = await _mediator.Send(new Get{Noun}ByIdQuery(id), cancellationToken);
    return Ok(result);
}
```

### Public POST (create)

```csharp
[HttpPost]
[AllowAnonymous]  // document why in RBAC matrix
[ProducesResponseType(typeof({Noun}Output), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<ActionResult<{Noun}Output>> Create(
    [FromBody] {Verb}{Noun}Command command,
    CancellationToken cancellationToken)
{
    _logger.LogInformation("Creating {Noun}", nameof({Noun}));
    var result = await _mediator.Send(command, cancellationToken);
    return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1.0" }, result);
}
```

### Protected DELETE

```csharp
[HttpDelete("{id:guid}")]
[Authorize(Policy = SecurityConfiguration.{Noun}ManagePolicy)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
{
    _logger.LogInformation("Deleting {Noun} {EntityId}", nameof({Noun}), id);
    await _mediator.Send(new Delete{Noun}Command(id), cancellationToken);
    return NoContent();
}
```

## RBAC wiring (same PR)

1. Add policy constant in `src/Api/Configurations/SecurityConfiguration.cs`
2. Register requirement in `AddAuthorization` block
3. Add row to `docs/security/RBAC_MATRIX.md`
4. Add integration test in `tests/IntegrationTests/{Module}/`

## Anti-patterns

- ❌ Business validation or calculations in the action
- ❌ Direct repository injection in controller
- ❌ Returning domain entities
- ❌ `[Authorize]` without `Policy =`
- ❌ POST returning 200 instead of 201 for resource creation

## Reference

- Live: `src/Api/Controllers/v1/IdentityController.cs` — `Register`, `GetById`, `DeleteUser`
- Rules: `.cursor/rules/api.mdc`, `security.mdc`, `openapi-contracts.mdc`
- Skills: `/new-endpoint`
- Checklist: `.agents/checklists/api-endpoint.md`
