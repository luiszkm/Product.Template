# AI Controller

Controller: `AiController`

Base route: `/api/v1/ai`

## POST `/api/v1/ai/chat`

- Policy: `Authenticated`
- Feature flag: `FeatureFlags:EnableAI` (desligado → `404`)
- Status: `200`, `400`, `401`, `404`

### Request body (`ChatCommand`)

```json
{
  "message": "string",
  "history": [
    {
      "role": "string",
      "content": "string",
      "toolCallId": "string|null"
    }
  ]
}
```

Campos:
- `message` (string, obrigatorio)
- `history` (array de `LlmMessage`, opcional)
  - `role` (string)
  - `content` (string)
  - `toolCallId` (string|null)

### Response 200 (`ChatOutput`)

```json
{
  "reply": "string",
  "iterationsUsed": 1
}
```

Campos:
- `reply` (string)
- `iterationsUsed` (int)

## Ferramentas do agente (`ITool`)

O agente pode chamar ferramentas registradas durante a conversa. Cada ferramenta valida sua própria permissão via `ToolAuthorization.EnsurePermission` (role `Admin` **ou** a claim de permissão indicada) — independente da policy `Authenticated` do endpoint. Falha lança `UnauthorizedAccessException`.

| Ferramenta | Permissão exigida |
|------------|--------------------|
| `get_tenant_info` | `TenantsPermissions.Read` (`tenants.read`) |
| `get_users_summary` | `IdentityPermissions.UserRead` (`identity.user.read`) |

Ou seja: um usuário autenticado sem a permissão do módulo consegue conversar com `/chat`, mas o modelo recebe erro ao tentar invocar a ferramenta correspondente — não há dados vazados de tenants/usuários fora do RBAC do usuário.

