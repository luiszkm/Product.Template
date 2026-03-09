# 05 — API Rules

## Controllers

- Located in `src/Api/Controllers/v{N}/`.
- One controller per module/feature: `{Module}Controller`.
- Inherit from `ControllerBase`.
- Annotated with `[ApiController]`, `[ApiVersion("1.0")]`, `[Route("api/v{version:apiVersion}/[controller]")]`.
- Controllers are **thin dispatchers**: receive request → send to MediatR → return response.
- No business logic in controllers. Maximum ~15-20 lines per action.

## Action Methods

```csharp
[HttpPost]
[Authorize(Policy = SecurityConfiguration.SomePolicyName)]
[ProducesResponseType(typeof(SomeOutput), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<SomeOutput>> CreateSomething(
    [FromBody] CreateSomethingCommand command,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(command, cancellationToken);
    return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1" }, result);
}
```

### Rules
1. Always accept `CancellationToken` as the last parameter.
2. Always declare `[ProducesResponseType]` for every possible status code.
3. Always use `[Authorize(Policy = "...")]` with an **explicit policy name** — never bare `[Authorize]`.
4. Public endpoints use `[AllowAnonymous]`.
5. Return `ActionResult<T>` (not `IActionResult`) for typed responses.
6. Use `CreatedAtAction` for POST that creates a resource.
7. Use `NoContent()` for DELETE and state-change POST/PUT with no body.

## HTTP Conventions

| Operation | Verb | Success Code | Body |
|-----------|------|-------------|------|
| Get by ID | GET | 200 | `{Output}` |
| List (paginated) | GET | 200 | `PaginatedListOutput<{Output}>` |
| Create | POST | 201 | `{Output}` + `Location` header |
| Update | PUT | 200 | `{Output}` |
| Delete | DELETE | 204 | (none) |
| Login / Action | POST | 200 | `{ActionOutput}` |

## Error Responses (ProblemDetails)

All errors are returned as RFC 9457 ProblemDetails by `ApiGlobalExceptionFilter`:

| Exception | Status | `type` |
|-----------|--------|--------|
| `NotFoundException` | 404 | `Not Found` |
| `BusinessRuleException` | 400 | `BusinessRuleViolation` |
| `DomainException` | 422 | `UnProcessableEntity` |
| `ValidationException` | 400 | (FluentValidation errors) |
| `UnauthorizedAccessException` | 401 | — |
| Unhandled | 500 | `UnexpectedError` |

In Development, `StackTrace` is included in the response extensions.

## Versioning

- Use `Asp.Versioning.Mvc`.
- Version in URL: `/api/v1/...`.
- When deprecating, add `[ApiVersion("1.0", Deprecated = true)]` and create a new version.

## Request/Response Models

- **Commands** (`[FromBody]`) ARE the request model — no separate "Request" DTO.
- **Query parameters** use `[FromQuery]` with primitive types or bind to the query record.
- **Responses** are `record` types suffixed with `Output`.

## RBAC Matrix

Every protected endpoint MUST be documented in `docs/security/RBAC_MATRIX.md` in the same PR.

## Middleware Pipeline Order (Program.cs)

```
ResponseCompression → OutputCaching → SerilogRequestLogging
→ RequestLoggingMiddleware → RequestDeduplicationMiddleware
→ TenantResolutionMiddleware → IpWhitelistMiddleware
→ ForwardedHeaders → HttpsRedirection → Routing
→ CORS → Authentication → Authorization → RateLimiting
→ HealthChecks → Documentation → MapControllers
```

