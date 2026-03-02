# RBAC Matrix (Prioridade 1)

Matriz inicial de autorização por endpoint para eliminar `[Authorize]` genérico e explicitar policy/role esperada.

## Policies ativas
- `Authenticated`
- `UserOnly` (roles: `User`, `Admin`, `Manager`)
- `AdminOnly` (role: `Admin`)
- `UsersRead` (role `Admin` **ou** claim `permission=users.read`)
- `UsersManage` (role `Admin` **ou** claim `permission=users.manage`)

## API v1 - IdentityController

| Método | Rota | Acesso | Policy | Observação |
|---|---|---|---|---|
| GET | `/api/v1/identity/providers` | Público | - | Descoberta de providers |
| POST | `/api/v1/identity/login` | Público | - | Login JWT |
| POST | `/api/v1/identity/register` | Público | - | Registro |
| POST | `/api/v1/identity/external-login` | Público | - | Auth externa |
| GET | `/api/v1/identity` | Protegido | `UsersRead` | Listagem de usuários por role/permission |
| GET | `/api/v1/identity/roles` | Protegido | `UsersRead` | Listagem de roles |
| GET | `/api/v1/identity/roles/{roleId}` | Protegido | `UsersRead` | Detalhe de role |
| POST | `/api/v1/identity/roles` | Protegido | `UsersManage` | Criação de role |
| PUT | `/api/v1/identity/roles/{roleId}` | Protegido | `UsersManage` | Atualização de role |
| DELETE | `/api/v1/identity/roles/{roleId}` | Protegido | `UsersManage` | Remoção de role |
| GET | `/api/v1/identity/{id}` | Protegido | `UserOnly` | Leitura de usuário |
| PUT | `/api/v1/identity/{id}` | Protegido | `UserOnly` | Atualização de usuário |
| DELETE | `/api/v1/identity/{id}` | Protegido | `UsersManage` | Exclusão de usuário |
| GET | `/api/v1/identity/{id}/roles` | Protegido | `UsersManage` | Listar roles do usuário |
| POST | `/api/v1/identity/{id}/roles` | Protegido | `UsersManage` | Adicionar role ao usuário |
| DELETE | `/api/v1/identity/{id}/roles/{roleName}` | Protegido | `UsersManage` | Remover role do usuário |

## Regras de revisão (gate)
1. Não introduzir endpoint protegido com `[Authorize]` sem `Policy` explícita.
2. Todo endpoint novo deve entrar nesta matriz no mesmo PR.
3. Mudanças de policy em endpoint exigem atualização da matriz.
