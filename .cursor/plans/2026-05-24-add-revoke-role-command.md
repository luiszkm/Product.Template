# Plan: add-revoke-role-command
Date: 2026-05-24
Status: done

## Scope

Authorization module — Application + Infrastructure + API layers.

## Trigger

User requested: "add an endpoint to revoke a role from a user."

## Affected files

```
src/Core/Authorization/Authorization.Application/Commands/RevokeRole/RevokeRoleCommand.cs
src/Core/Authorization/Authorization.Application/Commands/RevokeRole/RevokeRoleCommandHandler.cs
src/Core/Authorization/Authorization.Application/Commands/RevokeRole/RevokeRoleCommandValidator.cs
src/Api/Controllers/AuthorizationController.cs  (new action)
docs/security/RBAC_MATRIX.md  (new endpoint entry)
tests/UnitTests/Core/Authorization/Application/Commands/RevokeRoleCommandHandlerTests.cs
tests/IntegrationTests/Core/Authorization/RevokeRoleEndpointTests.cs
```

## Input

- `UserId` (Guid) — target user
- `RoleId` (Guid) — role to revoke
- Caller must have `authorization:roles:revoke` permission

## Expected output

- `DELETE /api/authorization/users/{userId}/roles/{roleId}` returns `204 No Content`
- `UserRoleAssignment` removed from DB
- `UserRoleRevokedEvent` dispatched
- Unit tests: happy path + user not found + assignment not found
- Integration tests: 204 success + 404 not found + 403 forbidden

## Acceptance command

```bash
dotnet build
dotnet test tests/ArchitectureTests
dotnet test tests/UnitTests --filter "FullyQualifiedName~RevokeRole"
dotnet test tests/IntegrationTests --filter "FullyQualifiedName~RevokeRole"
dotnet format --verify-no-changes
```

## Rollback

Delete the 7 files listed above. Revert `AuthorizationController.cs` and `RBAC_MATRIX.md` changes.

## Steps

- [x] 1. Create `RevokeRoleCommand` record implementing `ICommand`
- [x] 2. Create `RevokeRoleCommandValidator` (UserId/RoleId not empty)
- [x] 3. Create `RevokeRoleCommandHandler` — load assignment, call `Remove`, `Commit`, dispatch event
- [x] 4. Add `DELETE` action to `AuthorizationController`
- [x] 5. Update `RBAC_MATRIX.md` with new endpoint + required permission
- [x] 6. Write unit tests (3 scenarios)
- [x] 7. Write integration tests (3 scenarios)
- [x] 8. Run acceptance command — all green

## Blockers / notes

(none)
