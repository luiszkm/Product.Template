# RBAC Matrix (Prioridade 1)

Matriz inicial de autorização por endpoint para eliminar `[Authorize]` genérico e explicitar policy/role esperada.

## Policies ativas
- `Authenticated`
- `UserOnly` (roles: `User`, `Admin`, `Manager`)
- `AdminOnly` (role: `Admin`)
- `UsersRead` (role `Admin` **ou** claim `permission=users.read`)
- `UsersManage` (role `Admin` **ou** claim `permission=users.manage`)

## API v1 - IdentityController

| Método | Rota | Acesso | Policy atual | Permissão canônica (alvo) | Observação |
|---|---|---|---|---|---|
| GET | `/api/v1/identity/providers` | Público | - | - | Descoberta de providers |
| POST | `/api/v1/identity/login` | Público | - | - | Login JWT |
| POST | `/api/v1/identity/register` | Público | - | - | Registro |
| POST | `/api/v1/identity/external-login` | Público | - | - | Auth externa |
| GET | `/api/v1/identity` | Protegido | `UsersRead` | `identity.user.read` | Alvo: alinhar policy ao catálogo |
| GET | `/api/v1/identity/roles` | Protegido | `UsersRead` | `identity.role.read` | Alvo: alinhar policy ao catálogo |
| GET | `/api/v1/identity/roles/{roleId}` | Protegido | `UsersRead` | `identity.role.read` | Alvo: alinhar policy ao catálogo |
| POST | `/api/v1/identity/roles` | Protegido | `UsersManage` | `identity.role.manage` | Alvo: alinhar policy ao catálogo |
| PUT | `/api/v1/identity/roles/{roleId}` | Protegido | `UsersManage` | `identity.role.manage` | Alvo: alinhar policy ao catálogo |
| DELETE | `/api/v1/identity/roles/{roleId}` | Protegido | `UsersManage` | `identity.role.manage` | Alvo: alinhar policy ao catálogo |
| GET | `/api/v1/identity/{id}` | Protegido | `UserReadOrSelf` | `identity.user.read` | Alvo: substituir lógica contextual por policy canônica |
| PUT | `/api/v1/identity/{id}` | Protegido | `UserManageOrSelf` | `identity.user.manage` | Alvo: alinhar policy ao catálogo |
| DELETE | `/api/v1/identity/{id}` | Protegido | `UsersManage` | `identity.user.manage` | Alvo: alinhar policy ao catálogo |
| GET | `/api/v1/identity/{id}/roles` | Protegido | `UsersManage` | `identity.user.manage` | Alvo: alinhar policy ao catálogo |
| POST | `/api/v1/identity/{id}/roles` | Protegido | `UsersManage` | `identity.user.manage` | Alvo: alinhar policy ao catálogo |
| DELETE | `/api/v1/identity/{id}/roles/{roleName}` | Protegido | `UsersManage` | `identity.user.manage` | Alvo: alinhar policy ao catálogo |

## Regras de revisão (gate)
1. Não introduzir endpoint protegido com `[Authorize]` sem `Policy` explícita.
2. Todo endpoint novo deve entrar nesta matriz no mesmo PR.
3. Mudanças de policy em endpoint exigem atualização da matriz.
4. Policies futuras devem derivar de permissões canônicas `{module}.{resource}.{action}` registradas no catálogo.
