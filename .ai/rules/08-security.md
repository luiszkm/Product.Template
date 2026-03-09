# 08 — Security Rules

## Authentication

- **JWT Bearer** is the primary authentication scheme.
- Configured in `Api/Configurations/SecurityConfiguration.cs`.
- Token generation: `JwtTokenService` using `JsonWebTokenHandler` with HMAC-SHA256.
- Secret: `Jwt:Secret` in `appsettings.json` (use User Secrets or env vars in production).
- External providers (Microsoft OAuth2) are optional and configured via `AuthenticationConfiguration`.

## Authorization (RBAC)

### Policies

| Policy Name | Constant | Rule |
|-------------|----------|------|
| `Authenticated` | `SecurityConfiguration.AuthenticatedPolicy` | Any authenticated user. |
| `AdminOnly` | `SecurityConfiguration.AdminOnlyPolicy` | Role = `Admin`. |
| `UserOnly` | `SecurityConfiguration.UserOnlyPolicy` | Role ∈ {`User`, `Admin`, `Manager`}. |
| `UsersRead` | `SecurityConfiguration.UsersReadPolicy` | Role = `Admin` OR claim `permission=users.read`. |
| `UsersManage` | `SecurityConfiguration.UsersManagePolicy` | Role = `Admin` OR claim `permission=users.manage`. |

### Rules

1. **Every protected endpoint** uses `[Authorize(Policy = "...")]` with an explicit policy — never bare `[Authorize]`.
2. **Public endpoints** use `[AllowAnonymous]`.
3. When adding a new policy, define it in `SecurityConfiguration.cs` and add a `public const string` for it.
4. **Permission claims** use the claim type `permission` (from `AuthorizationClaimTypes.Permission`).
5. Permissions are stored in the DB (`Permission` entity) and assigned to roles via `RolePermission`.
6. The `LoginCommandHandler` populates both role claims AND permission claims in the JWT.

### RBAC Matrix

Every protected endpoint must be documented in `docs/security/RBAC_MATRIX.md`.
The test `RbacMatrixConsistencyTests` enforces this automatically.

## Input Validation

- All user input is validated by FluentValidation before reaching the handler.
- Never trust client input — validate even `Guid` route parameters.
- Use `[FromBody]`, `[FromQuery]`, `[FromRoute]` explicitly.

## Secrets Management

- Never commit secrets to source control.
- Use `appsettings.json` for **structure only** (with placeholder values).
- Use **User Secrets** (`dotnet user-secrets`) in development.
- Use **environment variables** or a vault (Azure Key Vault, AWS Secrets Manager) in production.
- JWT secret must be ≥32 characters.

## Data Protection

- Passwords are hashed with **PBKDF2-SHA256** (100K iterations) via `HashServices`.
- Never log passwords, tokens, or secrets.
- `RequestLoggingMiddleware` masks sensitive headers (`Authorization`, `Cookie`, `X-Api-Key`) and fields (`password`, `token`, `secret`).

## Tenant Isolation

- Every data query is filtered by `TenantId` via EF global query filters.
- The `AddUserRoleCommandHandler` and `RemoveUserRoleCommandHandler` enforce cross-tenant checks.
- The `TenantResolutionMiddleware` rejects requests without a valid tenant.

## IP Security

- `IpWhitelistMiddleware` supports configurable whitelist/blacklist.
- Disabled by default (`IpSecurity:EnableWhitelist = false`).

## Rate Limiting

- ASP.NET Core rate limiting is enabled per configuration.
- Login endpoints should have stricter rate limits to prevent brute-force.

## Headers

- `ForwardedHeaders` is enabled for `X-Forwarded-For` and `X-Forwarded-Proto`.
- HTTPS redirection is enforced.
- CORS origins are configurable — default is `*` (restrict in production).

