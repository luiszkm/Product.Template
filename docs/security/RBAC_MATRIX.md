# RBAC Matrix (Prioridade 1)

Matriz inicial de autorização por endpoint para eliminar `[Authorize]` genérico e explicitar policy/role esperada.

## Policies ativas
- `Authenticated`
- `UserOnly` (roles: `User`, `Admin`, `Manager`)
- `AdminOnly` (role: `Admin`)
- `UsersRead` (role `Admin` **ou** claim `permission=identity.user.read`)
- `UsersManage` (role `Admin` **ou** claim `permission=identity.user.manage`)

## API v1 - IdentityController

| Método | Rota | Acesso | Policy atual | Permissão canônica | Observação |
|---|---|---|---|---|---|
| GET | `/api/v1/identity/providers` | Público | - | - | Descoberta de providers |
| POST | `/api/v1/identity/login` | Público | - | - | Login JWT |
| POST | `/api/v1/identity/register` | Público | - | - | Registro |
| POST | `/api/v1/identity/external-login` | Público | - | - | Auth externa |
| GET | `/api/v1/identity` | Protegido | `UsersRead` | `identity.user.read` | Alinhado |
| GET | `/api/v1/identity/roles` | Protegido | `UsersRead` | `identity.role.read` | |
| GET | `/api/v1/identity/roles/{roleId}` | Protegido | `UsersRead` | `identity.role.read` | |
| POST | `/api/v1/identity/roles` | Protegido | `UsersManage` | `identity.role.manage` | |
| PUT | `/api/v1/identity/roles/{roleId}` | Protegido | `UsersManage` | `identity.role.manage` | |
| DELETE | `/api/v1/identity/roles/{roleId}` | Protegido | `UsersManage` | `identity.role.manage` | |
| GET | `/api/v1/identity/{id}` | Protegido | `UserReadOrSelf` | `identity.user.read` | Owner-check via requirement |
| PUT | `/api/v1/identity/{id}` | Protegido | `UserManageOrSelf` | `identity.user.manage` | Owner-check via requirement |
| DELETE | `/api/v1/identity/{id}` | Protegido | `UsersManage` | `identity.user.manage` | |
| GET | `/api/v1/identity/{id}/roles` | Protegido | `UsersManage` | `identity.user.manage` | |
| POST | `/api/v1/identity/{id}/roles` | Protegido | `UsersManage` | `identity.user.manage` | |
| DELETE | `/api/v1/identity/{id}/roles/{roleName}` | Protegido | `UsersManage` | `identity.user.manage` | |

## Regras de revisão (gate)
1. Não introduzir endpoint protegido com `[Authorize]` sem `Policy` explícita.
2. Todo endpoint novo deve entrar nesta matriz no mesmo PR.
3. Mudanças de policy em endpoint exigem atualização da matriz.
4. Policies futuras devem derivar de permissões canônicas `{module}.{resource}.{action}` registradas no catálogo.
