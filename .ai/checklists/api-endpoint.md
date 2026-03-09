# Checklist: API Endpoint

> Use this checklist when adding or modifying an API endpoint.

## Controller

- [ ] Action is in the correct controller: `src/Api/Controllers/v1/{Module}Controller.cs`
- [ ] Correct HTTP verb attribute (`[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`)
- [ ] Route template is RESTful and consistent with existing endpoints
- [ ] `CancellationToken cancellationToken` is the last parameter
- [ ] Returns `ActionResult<T>` (not `IActionResult`) for typed responses
- [ ] Uses `CreatedAtAction` for POST that creates a resource (201)
- [ ] Uses `NoContent()` for DELETE (204)
- [ ] No business logic — only dispatch to MediatR

## Authorization

- [ ] `[Authorize(Policy = SecurityConfiguration.{PolicyName})]` with **explicit** policy
- [ ] OR `[AllowAnonymous]` for public endpoints
- [ ] Never bare `[Authorize]` without a Policy
- [ ] Owner-check logic (if applicable) uses `ICurrentUserService`

## Documentation

- [ ] XML doc comment (`///`) on the action method
- [ ] `[ProducesResponseType(typeof(T), StatusCodes.Status200OK)]` for success
- [ ] `[ProducesResponseType(StatusCodes.Status400BadRequest)]` if validation can fail
- [ ] `[ProducesResponseType(StatusCodes.Status401Unauthorized)]` if protected
- [ ] `[ProducesResponseType(StatusCodes.Status403Forbidden)]` if role-restricted
- [ ] `[ProducesResponseType(StatusCodes.Status404NotFound)]` if entity lookup involved

## RBAC Matrix

- [ ] New row added to `docs/security/RBAC_MATRIX.md`
- [ ] Row includes: HTTP method, route, access level, policy name, observation
- [ ] `RbacMatrixConsistencyTests` passes

## Testing

- [ ] Integration test verifying **401** when no token
- [ ] Integration test verifying **403** when insufficient role/permissions
- [ ] Integration test verifying **200/201/204** when authorized
- [ ] All tests use `X-Tenant: public` header

## Status Code Reference

| Operation | Success | Common Errors |
|-----------|---------|---------------|
| GET by ID | 200 | 401, 403, 404 |
| GET list | 200 | 401, 403 |
| POST create | 201 | 400, 401, 403, 409 |
| PUT update | 200 | 400, 401, 403, 404 |
| DELETE | 204 | 401, 403, 404 |
| POST action | 200 | 400, 401, 429 |

