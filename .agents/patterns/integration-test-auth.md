# Pattern: Integration Test (Authorization)

> HTTP-level tests verifying RBAC on protected endpoints: 401, 403, and 200.

## When to use

- Every new protected endpoint (same PR as controller + RBAC matrix row)
- After adding or changing authorization policies

Unit handler tests (`unit-test-handler.md`) do not replace these — they test different layers.

## File location

```
tests/IntegrationTests/{Module}/{Module}AuthorizationTests.cs
```

E2E tests with full factory setup also exist in `tests/E2ETests/Security/` for cross-module RBAC coverage.

## Invariants (non-negotiable)

- ✅ Always send `X-Tenant: public` header (or appropriate tenant)
- ✅ Use `TestAuthHandler` scheme — `Authorization: Test token`
- ✅ Inject roles via `X-Test-Roles`
- ✅ Inject permissions via `X-Test-Permissions`
- ✅ Optionally set user via `X-Test-UserId`
- ❌ No real JWT generation in integration tests
- ✅ Assert HTTP status codes first; body assertions secondary

## Minimum scenarios per protected endpoint

| Scenario | Expected | Headers |
|---|---|---|
| No auth | `401 Unauthorized` | (none) |
| Wrong role/permission | `403 Forbidden` | `Authorization: Test token` + insufficient roles |
| Correct role/permission | `200 OK` or `201 Created` | `Authorization: Test token` + required role/permission |

Public endpoints (`[AllowAnonymous]`) test success without auth and optionally rate-limit behavior.

## Annotated template

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests.{Module};

public class {Module}AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public {Module}AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant", "public");
    }

    [Fact]
    public async Task {Action}_ShouldReturn401_WhenNoTokenIsProvided()
    {
        var response = await _client.GetAsync("/api/v1/{module}/{route}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task {Action}_ShouldReturn403_WhenUserLacksPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/{module}/{route}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "User");  // role without required permission

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task {Action}_ShouldReturn200_WhenUserHasRequiredPermission()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/{module}/{route}");
        request.Headers.Add("Authorization", "Test token");
        request.Headers.Add("X-Test-Roles", "Manager");
        request.Headers.Add("X-Test-Permissions", "{Module}Permissions.{Permission}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

## POST / create endpoint variant

```csharp
[Fact]
public async Task Create{Entity}_ShouldReturn201_WhenUserHasManagePermission()
{
    using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/{module}");
    request.Headers.Add("Authorization", "Test token");
    request.Headers.Add("X-Test-Roles", "Admin");
    request.Headers.Add("X-Test-Permissions", "{Module}Permissions.{Entity}Manage");
    request.Content = JsonContent.Create(new { /* valid body */ });

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

## Same-PR checklist

When adding a protected endpoint:

1. `[Authorize(Policy = SecurityConfiguration.{Policy})]` on action
2. Row in `docs/security/RBAC_MATRIX.md`
3. Integration test file (or extend existing) with 401 + 403 + 200/201
4. Run `dotnet test tests/IntegrationTests --filter "{Module}Authorization"`

## RBAC matrix consistency

Automated check: `tests/E2ETests/Security/RbacMatrixConsistencyTests.cs` verifies matrix ↔ controller alignment.

## Reference

- Live: `tests/E2ETests/Security/RbacHttpAuthorizationIntegrationTests.cs`
- Rules: `.cursor/rules/tests.mdc`, `security.mdc`, `api.mdc`
- Skills: `/new-endpoint`, `/test-writer`
- Checklist: `.agents/checklists/api-endpoint.md`, `pull-request.md` § Tests
