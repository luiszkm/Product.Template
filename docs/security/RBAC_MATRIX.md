# RBAC Matrix (Prioridade 1)

Matriz inicial de autorização por endpoint para eliminar `[Authorize]` genérico e explicitar policy/role esperada.

## Policies ativas
- `Authenticated`
- `UserOnly` (roles: `User`, `Admin`, `Manager`)
- `AdminOnly` (role: `Admin`)
- `UsersRead` (role `Admin` **ou** claim `permission=identity.user.read`)
- `UsersManage` (role `Admin` **ou** claim `permission=identity.user.manage`)
- `AuthorizationRolesRead` (claim `permission=authorization.role.read`)
- `AuthorizationRolesManage` (claim `permission=authorization.role.manage`)
- `AuthorizationPermissionsRead` (claim `permission=authorization.permission.read`)
- `AuthorizationPermissionsManage` (claim `permission=authorization.permission.manage`)
- `TenantsRead` (claim `permission=tenants.read`)
- `TenantsManage` (claim `permission=tenants.manage`)

## API v1 - IdentityController

| Método | Rota | Acesso | Policy atual | Permissão canônica | Observação |
|---|---|---|---|---|---|
| GET | `/api/v1/identity/providers` | Público | - | - | Descoberta de providers |
| POST | `/api/v1/identity/login` | Público | - | - | Login JWT |
| POST | `/api/v1/identity/register` | Público | - | - | Registro |
| POST | `/api/v1/identity/external-login` | Público | - | - | Auth externa |
| GET | `/api/v1/identity` | Protegido | `UsersRead` | `identity.user.read` | Alinhado |
| GET | `/api/v1/identity/{id}` | Protegido | `UserReadOrSelf` | `identity.user.read` | Owner-check via requirement |
| PUT | `/api/v1/identity/{id}` | Protegido | `UserManageOrSelf` | `identity.user.manage` | Owner-check via requirement |
| DELETE | `/api/v1/identity/{id}` | Protegido | `UsersManage` | `identity.user.manage` | |
| GET | `/api/v1/identity/{id}/roles` | Protegido | `UsersManage` | `identity.user.manage` | |

## API v1 - AuthorizationController

| Método | Rota | Acesso | Policy | Permissão canônica | Observação |
|---|---|---|---|---|---|
| GET | `/api/v1/authorization/roles` | Protegido | `AuthorizationRolesRead` | `authorization.role.read` | |
| GET | `/api/v1/authorization/roles/{id}` | Protegido | `AuthorizationRolesRead` | `authorization.role.read` | |
| POST | `/api/v1/authorization/roles` | Protegido | `AuthorizationRolesManage` | `authorization.role.manage` | |
| PUT | `/api/v1/authorization/roles/{id}` | Protegido | `AuthorizationRolesManage` | `authorization.role.manage` | |
| DELETE | `/api/v1/authorization/roles/{id}` | Protegido | `AuthorizationRolesManage` | `authorization.role.manage` | |
| GET | `/api/v1/authorization/roles/{id}/permissions` | Protegido | `AuthorizationRolesRead` | `authorization.role.read` | |
| POST | `/api/v1/authorization/roles/{id}/permissions` | Protegido | `AuthorizationRolesManage` | `authorization.role.manage` | |
| DELETE | `/api/v1/authorization/roles/{id}/permissions/{code}` | Protegido | `AuthorizationRolesManage` | `authorization.role.manage` | |
| GET | `/api/v1/authorization/permissions` | Protegido | `AuthorizationPermissionsRead` | `authorization.permission.read` | |
| POST | `/api/v1/authorization/permissions` | Protegido | `AuthorizationPermissionsManage` | `authorization.permission.manage` | |
| PUT | `/api/v1/authorization/permissions/{id}` | Protegido | `AuthorizationPermissionsManage` | `authorization.permission.manage` | |
| DELETE | `/api/v1/authorization/permissions/{id}` | Protegido | `AuthorizationPermissionsManage` | `authorization.permission.manage` | |
| GET | `/api/v1/authorization/users/{userId}/roles` | Protegido | `AuthorizationRolesRead` | `authorization.role.read` | |
| POST | `/api/v1/authorization/users/{userId}/roles` | Protegido | `AuthorizationRolesManage` | `authorization.role.manage` | |
| DELETE | `/api/v1/authorization/users/{userId}/roles/{roleId}` | Protegido | `AuthorizationRolesManage` | `authorization.role.manage` | |

## API v1 - TenantsController

| Método | Rota | Acesso | Policy | Permissão canônica | Observação |
|---|---|---|---|---|---|
| GET | `/api/v1/tenants` | Protegido | `TenantsRead` | `tenants.read` | |
| GET | `/api/v1/tenants/{id}` | Protegido | `TenantsRead` | `tenants.read` | |
| POST | `/api/v1/tenants` | Protegido | `TenantsManage` | `tenants.manage` | Provisiona tenant |
| PUT | `/api/v1/tenants/{id}` | Protegido | `TenantsManage` | `tenants.manage` | |
| DELETE | `/api/v1/tenants/{id}` | Protegido | `TenantsManage` | `tenants.manage` | Soft-deactivate |

## Regras de revisão (gate)
1. Não introduzir endpoint protegido com `[Authorize]` sem `Policy` explícita.
2. Todo endpoint novo deve entrar nesta matriz no mesmo PR.
3. Mudanças de policy em endpoint exigem atualização da matriz.
4. Policies futuras devem derivar de permissões canônicas `{module}.{resource}.{action}` registradas no catálogo.
