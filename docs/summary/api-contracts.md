# API Contracts — Identity

> Referência completa de todos os endpoints disponíveis na v1 da API.
> Base URL: `/api/v1/identity`

---

## Sumário de endpoints

| Método | Rota | Acesso | Descrição |
|--------|------|--------|-----------|
| GET | `/identity/providers` | Público | Lista provedores de autenticação ativos |
| POST | `/identity/login` | Público | Login com email e senha → JWT |
| POST | `/identity/register` | Público | Cadastro de novo usuário |
| POST | `/identity/external-login` | Público | Login via OAuth2 (Microsoft, Google) |
| GET | `/identity` | `UsersRead` | Lista usuários paginada |
| GET | `/identity/{id}` | `UserOnly` | Busca usuário por ID |
| PUT | `/identity/{id}` | `UserOnly` | Atualiza perfil de usuário |
| DELETE | `/identity/{id}` | `UsersManage` | Remove usuário |
| GET | `/identity/roles` | `UsersRead` | Lista roles paginada |
| GET | `/identity/roles/{roleId}` | `UsersRead` | Busca role por ID |
| POST | `/identity/roles` | `UsersManage` | Cria nova role |
| PUT | `/identity/roles/{roleId}` | `UsersManage` | Atualiza role |
| DELETE | `/identity/roles/{roleId}` | `UsersManage` | Remove role |
| GET | `/identity/{id}/roles` | `UsersManage` | Lista roles de um usuário |
| POST | `/identity/{id}/roles` | `UsersManage` | Adiciona role a um usuário |
| DELETE | `/identity/{id}/roles/{roleName}` | `UsersManage` | Remove role de um usuário |

> Legenda de acesso: veja [rbac-guide.md](./rbac-guide.md)

---

## Endpoints públicos (sem autenticação)

---

### `GET /identity/providers`

Lista quais provedores de autenticação estão ativos na instância.

**Request:** sem body

**Response `200 OK`:**
```json
{
  "providers": ["jwt", "microsoft"],
  "count": 2
}
```

> Use este endpoint na tela de login para decidir quais botões exibir (ex: "Entrar com Microsoft").

---

### `POST /identity/login`

Autentica o usuário com email e senha. Retorna o token JWT.

**Request body:**
```json
{
  "email": "usuario@exemplo.com",
  "password": "SenhaSegura123!"
}
```

**Validações:**
- `email`: obrigatório, formato válido
- `password`: obrigatório, mínimo 8 caracteres

**Response `200 OK`:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "usuario@exemplo.com",
    "firstName": "João",
    "lastLoginAt": "2026-03-14T18:30:00Z",
    "roles": ["User"]
  }
}
```

**Erros possíveis:**
| Status | Quando ocorre |
|--------|---------------|
| `400` | Email ou senha em formato inválido |
| `401` | Credenciais incorretas |
| `429` | Muitas tentativas — aguardar antes de tentar novamente |

---

### `POST /identity/register`

Cria um novo usuário no sistema.

**Request body:**
```json
{
  "email": "novousuario@exemplo.com",
  "password": "SenhaSegura123!",
  "firstName": "Maria",
  "lastName": "Santos"
}
```

**Regras de validação:**
- `email`: obrigatório, formato válido, **deve ser único no sistema**
- `password`: obrigatório, mínimo 8 caracteres, deve conter maiúsculas, minúsculas, números e caracteres especiais
- `firstName`: obrigatório
- `lastName`: obrigatório

**Response `201 Created`:**
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "email": "novousuario@exemplo.com",
  "firstName": "Maria",
  "lastName": "Santos",
  "emailConfirmed": false,
  "createdAt": "2026-03-15T14:30:00Z",
  "lastLoginAt": null,
  "roles": []
}
```

**Erros possíveis:**
| Status | Quando ocorre |
|--------|---------------|
| `400` | Campos inválidos (validação FluentValidation) |
| `409` | Email já cadastrado no sistema |

> ℹ️ Após o registro, o campo `emailConfirmed` vem como `false`. O front-end pode exibir um aviso para o usuário confirmar o email.

---

### `POST /identity/external-login`

Autentica via OAuth2 (Microsoft Entra ID / Azure AD). Requer que o front-end já tenha realizado o OAuth flow e obtido o `code`.

**Request body:**
```json
{
  "provider": "microsoft",
  "code": "0.AX0A...",
  "redirectUri": "https://app.exemplo.com/auth/callback"
}
```

**Campos:**
- `provider`: `"microsoft"` (único suportado atualmente; `"google"` em desenvolvimento)
- `code`: código de autorização recebido do provedor OAuth2
- `redirectUri`: URI de callback configurada no Azure AD (deve ser exata)

**Response `200 OK`:** mesmo formato de `/login`

**Erros possíveis:**
| Status | Quando ocorre |
|--------|---------------|
| `400` | Provider não suportado, código inválido ou redirectUri incorreto |
| `401` | Falha na validação do token com o provedor externo |

> ℹ️ Veja o fluxo completo em [auth-guide.md](./auth-guide.md#autenticação-externa-microsoft)

---

## Endpoints protegidos — Usuários

> Todos requerem `Authorization: Bearer {token}` no header.

---

### `GET /identity?pageNumber=1&pageSize=10`

Lista todos os usuários com paginação.

**Query params:**
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `pageNumber` | `int` | `1` | Página atual (começa em 1) |
| `pageSize` | `int` | `10` | Itens por página |

**Policy requerida:** `UsersRead` (role `Admin` ou permission `users.read`)

**Response `200 OK`:**
```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 45,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "usuario1@exemplo.com",
      "firstName": "João",
      "lastName": "Silva",
      "emailConfirmed": true,
      "createdAt": "2026-01-14T10:30:00Z",
      "lastLoginAt": "2026-03-14T18:30:00Z",
      "roles": ["User"]
    }
  ]
}
```

---

### `GET /identity/{id}`

Busca um usuário pelo ID.

**Policy requerida:** `UserOnly` — **regra owner-check:** somente o próprio usuário ou um `Admin` pode acessar.

**Response `200 OK`:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "usuario@exemplo.com",
  "firstName": "João",
  "lastName": "Silva",
  "emailConfirmed": true,
  "createdAt": "2026-01-14T10:30:00Z",
  "lastLoginAt": "2026-03-14T18:30:00Z",
  "roles": ["User"]
}
```

**Erros possíveis:**
| Status | Quando ocorre |
|--------|---------------|
| `401` | Token ausente ou inválido |
| `403` | Usuário tentando acessar dados de outro usuário |
| `404` | ID não encontrado |

---

### `PUT /identity/{id}`

Atualiza o perfil do usuário (nome).

**Policy requerida:** `UserOnly` — **regra owner-check:** somente o próprio usuário ou um `Admin`.

**⚠️ Importante:** o `userId` no body deve ser igual ao `{id}` da URL.

**Request body:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "João Atualizado",
  "lastName": "Silva"
}
```

**Response `200 OK`:** mesmo formato de `GET /identity/{id}`

**Erros possíveis:**
| Status | Quando ocorre |
|--------|---------------|
| `400` | `userId` do body diferente do `id` da URL |
| `403` | Tentativa de editar outro usuário |
| `404` | Usuário não encontrado |

---

### `DELETE /identity/{id}`

Remove um usuário do sistema. **Ação irreversível.**

**Policy requerida:** `UsersManage` (role `Admin` ou permission `users.manage`)

**Response `204 No Content`** — sem body.

---

## Endpoints protegidos — Roles

---

### `GET /identity/roles?pageNumber=1&pageSize=10`

Lista todas as roles com paginação.

**Policy requerida:** `UsersRead`

**Response `200 OK`:**
```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 3,
  "data": [
    {
      "id": "a1b2c3d4-...",
      "name": "Admin",
      "description": "Administrador com acesso total",
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

---

### `GET /identity/roles/{roleId}`

Busca uma role por ID.

**Policy requerida:** `UsersRead`

**Response `200 OK`:**
```json
{
  "id": "a1b2c3d4-...",
  "name": "Admin",
  "description": "Administrador com acesso total",
  "createdAt": "2026-01-01T00:00:00Z"
}
```

---

### `POST /identity/roles`

Cria uma nova role.

**Policy requerida:** `UsersManage`

**Request body:**
```json
{
  "name": "Manager",
  "description": "Gerente com acesso de leitura e gestão de usuários"
}
```

**Response `201 Created`:** mesmo formato de `GET /identity/roles/{roleId}`

---

### `PUT /identity/roles/{roleId}`

Atualiza nome ou descrição de uma role.

**Policy requerida:** `UsersManage`

**⚠️ Importante:** o `roleId` no body deve ser igual ao `{roleId}` da URL.

**Request body:**
```json
{
  "roleId": "a1b2c3d4-...",
  "name": "Manager",
  "description": "Descrição atualizada"
}
```

**Response `200 OK`:** mesmo formato de `GET /identity/roles/{roleId}`

---

### `DELETE /identity/roles/{roleId}`

Remove uma role.

**Policy requerida:** `UsersManage`

**Response `204 No Content`** — sem body.

---

### `GET /identity/{id}/roles`

Lista as roles atribuídas a um usuário.

**Policy requerida:** `UsersManage`

**Response `200 OK`:**
```json
["Admin", "User"]
```

---

### `POST /identity/{id}/roles`

Adiciona uma role a um usuário.

**Policy requerida:** `UsersManage`

**Request body:**
```json
{
  "roleName": "Manager"
}
```

**Response `204 No Content`** — sem body.

---

### `DELETE /identity/{id}/roles/{roleName}`

Remove uma role de um usuário.

**Policy requerida:** `UsersManage`

**Response `204 No Content`** — sem body.

---

## Convenções gerais

### Paginação

Todos os endpoints de listagem retornam o envelope:
```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 45,
  "data": [ ... ]
}
```

Para calcular o número total de páginas no front-end:
```js
const totalPages = Math.ceil(response.totalCount / response.pageSize);
```

### IDs

Todos os IDs são **GUID** (UUID v4), formato: `"3fa85f64-5717-4562-b3fc-2c963f66afa6"`.

### Datas

Todas as datas são em **UTC**, formato ISO 8601: `"2026-03-15T14:30:00Z"`. Converter para timezone local no front-end.

