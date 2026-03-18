# Frontend Integration Guide

> Guia de integração para o frontend do Product.Template.
> Versão da API: **v1**. Base URL: `https://<host>/api/v1`

---

## Índice

1. [Visão geral da aplicação](#1-visão-geral-da-aplicação)
2. [Headers obrigatórios](#2-headers-obrigatórios)
3. [Autenticação e tokens](#3-autenticação-e-tokens)
4. [Contratos da API](#4-contratos-da-api)
   - 4.1 [Identity](#41-identity)
   - 4.2 [Authorization](#42-authorization)
   - 4.3 [Tenants](#43-tenants)
5. [Tratamento de erros](#5-tratamento-de-erros)
6. [RBAC no frontend](#6-rbac-no-frontend)
7. [Checklist de implementação](#7-checklist-de-implementação)

---

## 1. Visão geral da aplicação

Product.Template é um backend **multi-tenant SaaS** com Clean Architecture. O frontend precisa conhecer três conceitos centrais:

| Conceito | O que significa para o frontend |
|----------|--------------------------------|
| **Multi-tenant** | Toda requisição autenticada precisa informar o tenant via header `X-Tenant`. |
| **JWT + Refresh Token** | O access token expira em 60 min; use o refresh token para renová-lo sem novo login. |
| **RBAC por permissão** | Botões/rotas são habilitados/desabilitados com base nas permissões (`permission` claims) contidas no token JWT. |

### Módulos disponíveis

| Módulo | Responsabilidade |
|--------|-----------------|
| **Identity** | Autenticação (login, registro, OAuth), gestão de usuários |
| **Authorization** | Roles, Permissions, atribuição de roles a usuários |
| **Tenants** | Provisionamento e gestão de tenants (painel admin) |

---

## 2. Headers obrigatórios

Toda requisição (autenticada ou não) deve incluir:

```http
X-Tenant: <tenant-key>          # Ex: "public", "acme", "demo"
Content-Type: application/json
Accept: application/json
```

Requisições autenticadas adicionam:

```http
Authorization: Bearer <access_token>
```

> **Nota:** requisições sem `X-Tenant` recebem **400 Bad Request** do `TenantGuardMiddleware`.
> Endpoints marcados como públicos (login, register, confirm-email, etc.) ainda exigem o header.

---

## 3. Autenticação e tokens

### 3.1 Fluxo de login local

```
POST /api/v1/identity/login
→ { accessToken, tokenType, expiresIn, refreshToken, user }

Salvar: accessToken (memória/sessionStorage), refreshToken (httpOnly cookie ou localStorage)
```

### 3.2 Estrutura do token JWT

O access token é um JWT assinado. Claims relevantes para o frontend:

| Claim | Tipo | Descrição |
|-------|------|-----------|
| `sub` | `string (Guid)` | UserId |
| `email` | `string` | E-mail do usuário |
| `given_name` | `string` | Primeiro nome |
| `role` | `string[]` | Roles do usuário (ex: `["Admin"]`) |
| `permission` | `string[]` | Permissões canônicas (ex: `["identity.user.read"]`) |
| `security_stamp` | `string` | Versão de segurança — se mudar, o token é inválido |
| `exp` | `number` | Unix timestamp de expiração |

**Parse recomendado (TypeScript):**

```typescript
interface JwtPayload {
  sub: string;          // userId (Guid)
  email: string;
  given_name: string;
  role: string | string[];
  permission: string | string[];
  security_stamp: string;
  exp: number;
}

function parseToken(token: string): JwtPayload {
  const payload = token.split('.')[1];
  return JSON.parse(atob(payload));
}

// Normalize claims que podem ser string ou array
function toClaims(value: string | string[]): string[] {
  return Array.isArray(value) ? value : [value];
}
```

### 3.3 Refresh token rotation

O refresh token é **one-time use** — cada uso gera um novo par. Implemente interceptor HTTP:

```typescript
// Pseudo-código — adaptar para axios/fetch/etc.
async function refreshIfNeeded(request: Request): Promise<Response> {
  const response = await fetch(request);

  if (response.status === 401 && !isRefreshRequest(request)) {
    const newTokens = await callRefresh(getStoredRefreshToken());

    if (newTokens) {
      storeTokens(newTokens);
      return fetch(request.clone(), {
        headers: { Authorization: `Bearer ${newTokens.accessToken}` }
      });
    }

    redirectToLogin();
  }
  return response;
}
```

### 3.4 Login via provider externo (OAuth)

```
GET /api/v1/identity/providers
→ { providers: ["microsoft"], count: 1 }

POST /api/v1/identity/external-login
Body: { provider: "microsoft", code: "<oauth_code>", redirectUri: "<uri>" }
→ AuthTokenOutput (mesmo formato do login local)
```

**Fluxo OAuth típico:**
1. Frontend redireciona o usuário para `https://login.microsoftonline.com/...`
2. Microsoft redireciona de volta com `?code=...`
3. Frontend envia `code` + `redirectUri` para `POST /external-login`
4. Backend troca o code por tokens internos e retorna `AuthTokenOutput`

---

## 4. Contratos da API

### Tipos base

```typescript
interface PaginatedList<T> {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  data: T[];
}

interface ProblemDetails {     // RFC 9457 — retornado em todos os erros
  type: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;  // validação: campo → mensagens
}
```

---

### 4.1 Identity

#### `GET /api/v1/identity/providers` — público

```
Response 200:
{
  providers: string[],   // ex: ["microsoft"]
  count: number
}
```

#### `POST /api/v1/identity/login` — público

```typescript
// Request
{ email: string; password: string }

// Response 200 — AuthTokenOutput
interface AuthTokenOutput {
  accessToken: string;
  tokenType: string;       // "Bearer"
  expiresIn: number;       // segundos (3600 = 60 min)
  refreshToken: string;
  user: {
    id: string;            // Guid
    email: string;
    firstName: string;
    lastLoginAt: string | null;   // ISO 8601 UTC
    roles: string[];
  };
}

// Errors: 400 (validação), 401 (credenciais inválidas)
```

#### `POST /api/v1/identity/refresh` — público

```typescript
// Request
{ refreshToken: string }

// Response 200 — AuthTokenOutput (mesmo formato acima)
// Errors: 400 (token inválido/expirado), 401 (token revogado)
```

#### `POST /api/v1/identity/register` — público

```typescript
// Request
{
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

// Response 201 — UserOutput
interface UserOutput {
  id: string;              // Guid
  email: string;
  firstName: string;
  lastName: string;
  emailConfirmed: boolean;
  createdAt: string;       // ISO 8601 UTC
  lastLoginAt: string | null;
}

// Errors: 400 (validação), 409 (e-mail já cadastrado)
```

#### `POST /api/v1/identity/external-login` — público

```typescript
// Request
{ provider: string; code: string; redirectUri?: string }

// Response 200 — AuthTokenOutput
// Errors: 400, 401
```

#### `GET /api/v1/identity` — requer `identity.user.read`

```typescript
// Query params
?pageNumber=1&pageSize=10&searchTerm=&sortBy=&sortDirection=asc

// Response 200 — PaginatedList<UserOutput>
```

#### `GET /api/v1/identity/{id}` — requer `identity.user.read` ou ser o próprio usuário

```typescript
// Response 200 — UserOutput
// Errors: 401, 403, 404
```

#### `GET /api/v1/identity/{id}/roles` — requer `identity.user.manage`

```typescript
// Response 200 — string[]   (nomes dos roles)
// Errors: 401, 403
```

#### `PUT /api/v1/identity/{id}` — requer `identity.user.manage` ou ser o próprio usuário

```typescript
// Request  (id na URL deve bater com userId no body)
{ userId: string; firstName: string; lastName: string }

// Response 200 — UserOutput
// Errors: 400, 401, 403, 404
```

#### `POST /api/v1/identity/{id}/confirm-email` — público

```
// Response 204 No Content
// Errors: 404
```

#### `DELETE /api/v1/identity/{id}` — requer `identity.user.manage`

```
// Response 204 No Content
// Errors: 401, 403, 404
```

---

### 4.2 Authorization

#### Roles

##### `GET /api/v1/authorization/roles` — requer `authorization.role.read`

```typescript
// Query: ?pageNumber=1&pageSize=10
// Response 200 — PaginatedList<RoleOutput>
interface RoleOutput {
  id: string;          // Guid
  name: string;
  description: string;
  createdAt: string;
}
```

##### `GET /api/v1/authorization/roles/{id}` — requer `authorization.role.read`

```typescript
// Response 200 — RoleWithPermissionsOutput
interface RoleWithPermissionsOutput {
  id: string;
  name: string;
  description: string;
  createdAt: string;
  permissions: PermissionOutput[];
}
```

##### `POST /api/v1/authorization/roles` — requer `authorization.role.manage`

```typescript
// Request
{ name: string; description: string }
// Response 201 — RoleOutput
// Errors: 400, 401, 403
```

##### `PUT /api/v1/authorization/roles/{id}` — requer `authorization.role.manage`

```typescript
// Request  (roleId no body deve bater com id na URL)
{ roleId: string; name: string; description: string }
// Response 200 — RoleOutput
// Errors: 400, 401, 403, 404
```

##### `DELETE /api/v1/authorization/roles/{id}` — requer `authorization.role.manage`

```
// Response 204
// Errors: 401, 403, 404
```

#### Role → Permissions

##### `GET /api/v1/authorization/roles/{id}/permissions` — requer `authorization.role.read`

```typescript
// Response 200 — RoleWithPermissionsOutput
```

##### `POST /api/v1/authorization/roles/{id}/permissions` — requer `authorization.role.manage`

```typescript
// Request
{ permissionId: string }   // Guid da permission a associar
// Response 204
// Errors: 400, 401, 403
```

##### `DELETE /api/v1/authorization/roles/{id}/permissions/{permissionId}` — requer `authorization.role.manage`

```
// Response 204
// Errors: 401, 403, 404
```

#### Permissions

##### `GET /api/v1/authorization/permissions` — requer `authorization.permission.read`

```typescript
// Query: ?pageNumber=1&pageSize=10
// Response 200 — PaginatedList<PermissionOutput>
interface PermissionOutput {
  id: string;          // Guid
  name: string;        // código canônico: "module.resource.action"
  description: string;
  createdAt: string;
}
```

##### `POST /api/v1/authorization/permissions` — requer `authorization.permission.manage`

```typescript
// Request
{ name: string; description: string }
// Response 201 — PermissionOutput
// Errors: 400, 401, 403
```

##### `PUT /api/v1/authorization/permissions/{id}` — requer `authorization.permission.manage`

```typescript
// Request
{ permissionId: string; name: string; description: string }
// Response 200 — PermissionOutput
// Errors: 400, 401, 403, 404
```

##### `DELETE /api/v1/authorization/permissions/{id}` — requer `authorization.permission.manage`

```
// Response 204
// Errors: 401, 403, 404
```

#### User Assignments

##### `GET /api/v1/authorization/users/{userId}/roles` — requer `authorization.role.read`

```typescript
// Response 200 — RoleOutput[]
```

##### `POST /api/v1/authorization/users/{userId}/roles` — requer `authorization.role.manage`

```typescript
// Request
{ roleId: string }
// Response 204
// Errors: 400, 401, 403
```

##### `DELETE /api/v1/authorization/users/{userId}/roles/{roleId}` — requer `authorization.role.manage`

```
// Response 204
// Errors: 401, 403, 404
```

---

### 4.3 Tenants

> Endpoints de tenants são tipicamente usados por um **painel administrativo** (superadmin).
> Em aplicações produto, o tenant já está resolvido via `X-Tenant` header; o usuário normal não interage com esses endpoints.

#### `GET /api/v1/tenants` — requer `tenants.read`

```typescript
// Query: ?pageNumber=1&pageSize=10
// Response 200 — PaginatedList<TenantOutput>
interface TenantOutput {
  tenantId: number;         // long
  tenantKey: string;        // ex: "acme"
  displayName: string;      // ex: "Acme Corp"
  contactEmail: string | null;
  isActive: boolean;
  isolationMode: "SharedDb" | "SchemaPerTenant" | "DedicatedDb";
  createdAt: string;
}
```

#### `GET /api/v1/tenants/{id}` — requer `tenants.read`

```
// Response 200 — TenantOutput
// Errors: 401, 403, 404
```

#### `POST /api/v1/tenants` — requer `tenants.manage`

```typescript
// Request
{
  tenantId: number;
  tenantKey: string;
  displayName: string;
  contactEmail?: string;
  isolationMode: "SharedDb" | "SchemaPerTenant" | "DedicatedDb";
}
// Response 201 — TenantOutput
// Errors: 400, 401, 403
```

#### `PUT /api/v1/tenants/{id}` — requer `tenants.manage`

```typescript
// Request  (tenantId no body deve bater com id na URL)
{ tenantId: number; displayName: string; contactEmail?: string }
// Response 200 — TenantOutput
// Errors: 400, 401, 403, 404
```

#### `DELETE /api/v1/tenants/{id}` — requer `tenants.manage`

```
// Soft-deactivate (isActive = false). NÃO remove dados.
// Response 204
// Errors: 401, 403, 404
```

---

## 5. Tratamento de erros

Todos os erros seguem **RFC 9457 ProblemDetails**:

```typescript
interface ProblemDetails {
  type: string;      // URI de referência
  title: string;     // mensagem curta (inglês)
  status: number;    // HTTP status code
  detail?: string;   // mensagem detalhada para o usuário
  errors?: Record<string, string[]>;  // erros de validação por campo
}
```

### Mapeamento de status → UX

| Status | Causa | Ação recomendada no frontend |
|--------|-------|------------------------------|
| `400` | Validação ou regra de negócio | Exibir `errors` por campo; ou `detail` em toast |
| `401` | Token expirado / inválido | Tentar refresh; se falhar, redirecionar para login |
| `403` | Permissão insuficiente | Exibir "Acesso negado" — não esconder o motivo |
| `404` | Recurso não encontrado | Exibir mensagem e oferecer voltar |
| `409` | Conflito (ex: e-mail duplicado) | Exibir `detail` em campo correspondente |
| `422` | Violação de invariante de domínio | Exibir `detail` — geralmente erro de lógica de negócio |
| `429` | Rate limit excedido | Exibir "Muitas requisições, aguarde" com retry-after |
| `500` | Erro interno | Log + toast genérico; não expor `detail` ao usuário final |

### Exemplo de handler global (TypeScript)

```typescript
async function handleApiError(response: Response): Promise<never> {
  const problem: ProblemDetails = await response.json();

  switch (response.status) {
    case 400:
      if (problem.errors) throw new ValidationError(problem.errors);
      throw new BusinessRuleError(problem.detail ?? problem.title);
    case 401:
      await tryRefreshOrLogout();
      break;
    case 403:
      throw new ForbiddenError(problem.title);
    case 404:
      throw new NotFoundError(problem.detail ?? "Recurso não encontrado");
    default:
      throw new ApiError(problem.title, response.status);
  }
  throw new ApiError("Erro inesperado", response.status);
}
```

---

## 6. RBAC no frontend

### 6.1 Como checar permissões

Após o login, extraia as permissões do JWT e armazene em contexto/store:

```typescript
const payload = parseToken(accessToken);
const permissions = new Set(toClaims(payload.permission));
const roles = new Set(toClaims(payload.role));

function can(permission: string): boolean {
  return permissions.has(permission);
}

function hasRole(role: string): boolean {
  return roles.has(role);
}
```

### 6.2 Permissões canônicas disponíveis

| Permissão | Descrição | Usado em |
|-----------|-----------|----------|
| `identity.user.read` | Listar e visualizar usuários | Lista de usuários, painel admin |
| `identity.user.manage` | Criar, editar, deletar usuários | Formulários de gestão |
| `authorization.role.read` | Listar e visualizar roles | Painel de roles |
| `authorization.role.manage` | CRUD de roles + atribuição | Gestão de roles |
| `authorization.permission.read` | Listar permissões | Painel de permissões |
| `authorization.permission.manage` | CRUD de permissões | Gestão de permissões |
| `tenants.read` | Listar e visualizar tenants | Painel superadmin |
| `tenants.manage` | CRUD de tenants | Gestão de tenants |

### 6.3 Proteção de rotas (exemplo React)

```tsx
// PermissionGuard.tsx
function PermissionGuard({ permission, children }: { permission: string; children: ReactNode }) {
  const { can } = useAuth();
  if (!can(permission)) return <Navigate to="/forbidden" replace />;
  return <>{children}</>;
}

// Router
<Route path="/users" element={
  <PermissionGuard permission="identity.user.read">
    <UsersPage />
  </PermissionGuard>
} />

// Componente — esconder botão sem permissão
function UserActions({ userId }: { userId: string }) {
  const { can } = useAuth();
  return (
    <>
      {can("identity.user.manage") && (
        <Button onClick={() => deleteUser(userId)}>Excluir</Button>
      )}
    </>
  );
}
```

### 6.4 Owner-check (UserReadOrSelf / UserManageOrSelf)

Os endpoints `GET /identity/{id}` e `PUT /identity/{id}` permitem que o **próprio usuário** acesse seus dados sem precisar ter a permissão `identity.user.read`/`identity.user.manage`. O backend valida isso automaticamente. No frontend:

```typescript
function canViewUser(targetUserId: string): boolean {
  return can("identity.user.read") || currentUserId === targetUserId;
}
```

---

## 7. Checklist de implementação

### HTTP Client

- [ ] Configurar base URL (`https://<host>/api/v1`)
- [ ] Interceptor que injeta `X-Tenant` header em toda requisição
- [ ] Interceptor que injeta `Authorization: Bearer <token>` quando autenticado
- [ ] Interceptor de resposta que trata `401` com refresh automático
- [ ] Handler global de `ProblemDetails` mapeado para erros tipados
- [ ] Retry com backoff para `429 Too Many Requests`

### Autenticação

- [ ] Tela de login (email + password) → `POST /identity/login`
- [ ] Armazenamento seguro do access token (memória ou sessionStorage)
- [ ] Armazenamento seguro do refresh token (preferência: httpOnly cookie)
- [ ] Lógica de refresh token rotation (`POST /identity/refresh`)
- [ ] Logout: limpar tokens + redirecionar para login
- [ ] Parse do JWT payload para extrair `userId`, `email`, `roles`, `permissions`
- [ ] Detecção de expiração do token (`exp` claim) para refresh proativo
- [ ] Tela/fluxo de registro (`POST /identity/register`)
- [ ] Tela de confirmação de e-mail (`POST /identity/{id}/confirm-email`)
- [ ] Descoberta e botão de login externo (`GET /identity/providers`)
- [ ] Callback OAuth → `POST /identity/external-login`

### RBAC / Autorização

- [ ] Context/Store com permissões e roles do usuário logado
- [ ] Hook/helper `can(permission: string): boolean`
- [ ] Hook/helper `hasRole(role: string): boolean`
- [ ] `PermissionGuard` para proteger rotas
- [ ] Esconder/desabilitar botões e ações sem permissão
- [ ] Página `/forbidden` para acesso negado (403)
- [ ] Refresh automático do token ao detectar mudança de permissões (`security_stamp` mudou → 401)

### Módulo Identity — Usuários

- [ ] Lista paginada de usuários com busca e ordenação (`GET /identity`)
  - [ ] Paginação com `pageNumber`, `pageSize`, `totalCount`
  - [ ] Campo de busca (`searchTerm`)
- [ ] Detalhe do usuário (`GET /identity/{id}`)
- [ ] Formulário de edição do perfil (`PUT /identity/{id}`)
  - [ ] Validação: firstName e lastName obrigatórios
  - [ ] `userId` no body deve bater com `id` na URL
- [ ] Ação de excluir usuário (`DELETE /identity/{id}`)
  - [ ] Confirmação antes de deletar
  - [ ] Exibir que é soft-delete (conta desativada, não apagada)
- [ ] Visualizar roles do usuário (`GET /identity/{id}/roles`)

### Módulo Authorization — Roles

- [ ] Lista paginada de roles (`GET /authorization/roles`)
- [ ] Detalhe do role com permissões vinculadas (`GET /authorization/roles/{id}`)
- [ ] Formulário de criação de role (`POST /authorization/roles`)
  - [ ] Validação: name obrigatório e único
- [ ] Formulário de edição de role (`PUT /authorization/roles/{id}`)
- [ ] Ação de excluir role (`DELETE /authorization/roles/{id}`)
- [ ] Tela de gestão de permissões do role:
  - [ ] Listar permissões atuais (`GET /authorization/roles/{id}/permissions`)
  - [ ] Adicionar permissão ao role (`POST /authorization/roles/{id}/permissions`)
    - [ ] Body: `{ permissionId: string }`
  - [ ] Remover permissão do role (`DELETE /authorization/roles/{id}/permissions/{permissionId}`)

### Módulo Authorization — Permissions

- [ ] Lista paginada de permissões (`GET /authorization/permissions`)
- [ ] Formulário de criação (`POST /authorization/permissions`)
  - [ ] Validação de nome no formato `module.resource.action`
- [ ] Formulário de edição (`PUT /authorization/permissions/{id}`)
- [ ] Ação de excluir (`DELETE /authorization/permissions/{id}`)

### Módulo Authorization — Atribuição de Roles a Usuários

- [ ] Listar roles do usuário (`GET /authorization/users/{userId}/roles`)
- [ ] Atribuir role ao usuário (`POST /authorization/users/{userId}/roles`)
  - [ ] Body: `{ roleId: string }`
- [ ] Revogar role do usuário (`DELETE /authorization/users/{userId}/roles/{roleId}`)

### Módulo Tenants (painel superadmin)

- [ ] Lista paginada de tenants (`GET /tenants`)
- [ ] Detalhe do tenant (`GET /tenants/{id}`)
- [ ] Formulário de criação de tenant (`POST /tenants`)
  - [ ] Campos: `tenantId` (number), `tenantKey`, `displayName`, `contactEmail?`, `isolationMode`
  - [ ] Dropdown de `isolationMode`: SharedDb / SchemaPerTenant / DedicatedDb
- [ ] Formulário de edição (`PUT /tenants/{id}`)
  - [ ] Somente `displayName` e `contactEmail` são editáveis
- [ ] Ação de desativar tenant (`DELETE /tenants/{id}`)
  - [ ] Deixar claro que é desativação (não exclusão definitiva)
  - [ ] Exibir `isActive: false` na lista após desativação

### Multi-tenancy

- [ ] Mecanismo para selecionar/resolver o tenant ativo:
  - [ ] Via subdomínio (`acme.myapp.com` → `X-Tenant: acme`)
  - [ ] Via configuração estática no ambiente (ex: `PUBLIC_TENANT=public`)
  - [ ] Via seleção de tenant no login (para superadmin com acesso a múltiplos tenants)
- [ ] `X-Tenant` injetado em **todas** as requisições sem exceção

### Tratamento de erros e UX

- [ ] Toast/notificação para erros `400` (mensagem de `detail`)
- [ ] Exibição de erros por campo para `400` com `errors` (validação)
- [ ] Redirecionamento automático para login em `401` após falha de refresh
- [ ] Página de "Acesso Negado" para `403`
- [ ] Página de "Não Encontrado" para `404`
- [ ] Mensagem genérica para `500` (sem expor detalhes técnicos)
- [ ] Loading state em todas as chamadas assíncronas
- [ ] Estado de erro recuperável (retry) em listas e páginas de detalhe

### Testes do frontend

- [ ] Testes unitários de `can()` e `hasRole()` com diferentes payloads JWT
- [ ] Testes do interceptor de refresh token (token expirado → refresh → retry)
- [ ] Testes do handler de `ProblemDetails` para cada status code
- [ ] Testes do `PermissionGuard` (rota acessível vs. redirecionada)
- [ ] Testes de integração (MSW ou similar) para fluxo de login completo
- [ ] Testes de integração para fluxo de CRUD de roles/permissions

---

## Referências

- `docs/security/RBAC_MATRIX.md` — todos os endpoints com policies e permissões
- `docs/ENTERPRISE_ANALYSIS.md` — features enterprise (refresh token, audit, soft delete)
- `docs/Adrs/ADR-004_catalogo_permissoes_rbac.md` — formato canônico de permissões
- `docs/Adrs/ADR-003_isolamento_dados_multitenant.md` — estratégia de multi-tenancy
