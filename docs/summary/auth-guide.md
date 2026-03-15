# Authentication Guide — Frontend

> Como autenticar, gerenciar tokens e implementar cada fluxo de login no front-end.

---

## Fluxo 1 — Login com email e senha (JWT)

```
[Frontend]                          [API]
    │                                 │
    │── POST /identity/login ─────────►│
    │   { email, password }            │
    │                                 │── valida credenciais
    │                                 │── gera JWT (HS256)
    │◄── 200 OK ──────────────────────│
    │   { accessToken, expiresIn,     │
    │     tokenType, user }            │
    │                                 │
    │── armazena token ───────────────►(local/session storage ou cookie)
    │── define Authorization header ──►Bearer {accessToken}
```

### Implementação recomendada

```ts
// auth.service.ts
async function login(email: string, password: string) {
  const res = await fetch('/api/v1/identity/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Tenant': getTenantSlug(),  // obrigatório — ver headers-and-conventions.md
    },
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) throw await res.json(); // ProblemDetails

  const data = await res.json();

  // Armazene o token de forma segura
  storeToken(data.accessToken, data.expiresIn);
  setCurrentUser(data.user);

  return data;
}
```

### O que armazenar

| Campo | Onde armazenar | Por quê |
|-------|---------------|---------|
| `accessToken` | `localStorage` ou cookie `HttpOnly` | Necessário para todas as chamadas |
| `expiresIn` | Junto ao token | Para calcular expiração |
| `user.id` | State global | Para owner-checks no front-end |
| `user.roles` | State global | Para controle de navegação/UI |

> ⚠️ **Segurança**: em aplicações que precisam de proteção máxima contra XSS, prefira cookie `HttpOnly` para o token em vez de `localStorage`.

### Expiração do token

O token expira em **60 minutos** por padrão (`expiresIn: 3600`).

Estratégias para lidar com a expiração:
1. **Silent refresh**: antes de cada chamada, verifique se o token expira em < 5 minutos e realize novo login silencioso (se o produto suportar)
2. **Redirect**: ao receber `401`, redirecionar para a tela de login
3. **Decodificar o JWT**: o payload contém `exp` (Unix timestamp)

```ts
function isTokenExpired(token: string): boolean {
  const payload = JSON.parse(atob(token.split('.')[1]));
  return Date.now() >= payload.exp * 1000;
}
```

---

## Fluxo 2 — Cadastro de novo usuário

```
[Frontend]                          [API]
    │── POST /identity/register ────►│
    │   { email, password,           │
    │     firstName, lastName }       │── valida campos
    │                                │── cria usuário
    │◄── 201 Created ────────────────│
    │   { id, email, firstName, ...} │
    │                                │
    │── redirecionar para login ─────►(ou realizar login automático)
```

### Após o registro

Duas opções para a UX:

**Opção A — Login automático:**
```ts
const user = await register(data);
const auth = await login(data.email, data.password); // login imediato
```

**Opção B — Redirecionar para login:**
```ts
const user = await register(data);
navigate('/login', { state: { message: 'Conta criada! Faça login.' } });
```

> ℹ️ O campo `emailConfirmed: false` indica que o e-mail ainda não foi verificado. Exiba um banner/aviso enquanto estiver `false`.

---

## Fluxo 3 — Autenticação externa (Microsoft)

### Pré-requisitos
1. Aplicativo registrado no Azure AD/Entra ID
2. `redirectUri` configurada tanto no Azure quanto no `appsettings.json → MicrosoftAuth`
3. Verificar que o provider `"microsoft"` está ativo via `GET /identity/providers`

### Fluxo completo PKCE (Authorization Code)

```
[Frontend]                    [Azure AD]                    [API]
    │                              │                           │
    │── 1. Redirecionar ──────────►│
    │   authorize?client_id=...    │
    │   &response_type=code        │
    │   &redirect_uri=...          │
    │   &scope=openid profile email│
    │                              │
    │◄── 2. code=0.AX0A... ────────│ (redirect back para o front)
    │                              │
    │── 3. POST /identity/external-login ─────────────────────►│
    │   { provider: "microsoft",   │                           │── valida code
    │     code: "0.AX0A...",       │                           │── troca por token Microsoft
    │     redirectUri: "..." }     │                           │── cria/atualiza usuário
    │                              │                           │── gera JWT interno
    │◄── 4. { accessToken, ... } ─────────────────────────────│
```

### URL de autorização do Azure AD

```
https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize
  ?client_id={clientId}
  &response_type=code
  &redirect_uri={redirectUri}     ← deve ser EXATAMENTE igual ao cadastrado no Azure
  &scope=openid%20profile%20email
  &response_mode=query
```

### Implementação da callback

```ts
// pages/auth/callback.ts (executado quando Azure redireciona de volta)
async function handleOAuthCallback() {
  const params = new URLSearchParams(window.location.search);
  const code = params.get('code');
  const error = params.get('error');

  if (error || !code) {
    navigate('/login', { state: { error: 'Autenticação cancelada' } });
    return;
  }

  const auth = await fetch('/api/v1/identity/external-login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Tenant': getTenantSlug(),
    },
    body: JSON.stringify({
      provider: 'microsoft',
      code,
      redirectUri: import.meta.env.VITE_OAUTH_REDIRECT_URI,
    }),
  }).then(r => r.json());

  storeToken(auth.accessToken, auth.expiresIn);
  setCurrentUser(auth.user);
  navigate('/dashboard');
}
```

---

## Configurar o Authorization header globalmente

### Fetch nativo (interceptor simples)

```ts
// api.ts
const BASE_URL = import.meta.env.VITE_API_URL;

export async function apiFetch(path: string, init: RequestInit = {}) {
  const token = getStoredToken();
  const tenant = getTenantSlug();

  const headers = new Headers(init.headers);
  headers.set('Content-Type', 'application/json');
  headers.set('X-Tenant', tenant);

  if (token && !isTokenExpired(token)) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const res = await fetch(`${BASE_URL}${path}`, { ...init, headers });

  if (res.status === 401) {
    clearToken();
    window.location.href = '/login';
    return;
  }

  return res;
}
```

### Axios (interceptors)

```ts
// axios.config.ts
import axios from 'axios';

const api = axios.create({ baseURL: import.meta.env.VITE_API_URL });

api.interceptors.request.use(config => {
  const token = getStoredToken();
  const tenant = getTenantSlug();

  config.headers['X-Tenant'] = tenant;
  if (token) config.headers['Authorization'] = `Bearer ${token}`;

  return config;
});

api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      clearToken();
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

export default api;
```

---

## Estrutura do JWT (payload decodificado)

```json
{
  "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "usuario@exemplo.com",
  "role": ["User"],
  "permission": ["users.read"],
  "iss": "Product.Template.Api",
  "aud": "Product.Template.Api",
  "exp": 1742070600,
  "iat": 1742067000
}
```

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `sub` | `string` (GUID) | ID do usuário — use para owner-checks |
| `email` | `string` | Email do usuário |
| `role` | `string[]` | Roles atribuídas |
| `permission` | `string[]` | Permissões granulares |
| `exp` | `number` | Expiração (Unix timestamp) |

> O front-end pode decodificar o JWT localmente (não é necessário chamar a API) para ler roles e permissions sem fazer uma nova requisição.

---

## Logout

A API não tem endpoint de logout (stateless JWT). O logout é feito no front-end:

```ts
function logout() {
  // 1. Remover token do storage
  localStorage.removeItem('access_token');

  // 2. Limpar state do usuário
  clearCurrentUser();

  // 3. Redirecionar para login
  window.location.href = '/login';
}
```

> Se houver necessidade de invalidar tokens (ex: logout de todos os dispositivos), implemente uma blacklist no backend — entre em contato com o time de backend.

