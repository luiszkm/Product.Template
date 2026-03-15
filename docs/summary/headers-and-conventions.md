# Headers & Conventions — Frontend

> Headers obrigatórios, multi-tenancy, CORS, rate limiting e convenções gerais de comunicação com a API.

---

## Headers obrigatórios em toda requisição

| Header | Obrigatório | Exemplo | Descrição |
|--------|:-----------:|---------|-----------|
| `X-Tenant` | ✅ Sim | `X-Tenant: acme` | Identifica o tenant (inquilino) da requisição |
| `Authorization` | Em rotas protegidas | `Authorization: Bearer eyJ...` | Token JWT |
| `Content-Type` | Em POST/PUT | `Content-Type: application/json` | Tipo do body |

> ⚠️ Requisições sem o header `X-Tenant` serão rejeitadas pela API.

---

## Multi-tenancy — `X-Tenant`

Este sistema é **multi-tenant**: cada instância da aplicação serve múltiplos clientes (tenants) isolados.

### Como funciona

A API identifica o tenant de duas formas:
1. **Header HTTP** (padrão): `X-Tenant: {slug-do-tenant}`
2. **Subdomínio** (alternativo): `{slug}.api.exemplo.com`

O front-end deve sempre enviar o header `X-Tenant` em **todas** as requisições.

### Como determinar o slug do tenant

```ts
// Opção 1 — variável de ambiente (para deploys dedicados por tenant)
const tenantSlug = import.meta.env.VITE_TENANT_SLUG; // ex: "acme"

// Opção 2 — baseado no subdomínio atual (para SaaS multi-tenant)
function getTenantFromSubdomain(): string {
  const hostname = window.location.hostname;
  const parts = hostname.split('.');
  // acme.app.exemplo.com → "acme"
  return parts.length > 2 ? parts[0] : 'public';
}

// Opção 3 — tenant público (para desenvolvimento/testes)
const tenantSlug = 'public';
```

### Tenant público

O sistema pode ter um tenant `public` para acesso sem isolamento. Disponível apenas se `AllowPublicFallback: true` no backend.

```ts
// Em testes de integração e desenvolvimento local, use:
headers['X-Tenant'] = 'public';
```

---

## CORS — Origens permitidas

Em desenvolvimento, a API aceita requisições de qualquer origem (`*`).

Em produção, as origens são configuradas em `appsettings.json → Cors.AllowedOrigins`. Certifique-se de solicitar ao time de infraestrutura que sua URL de front-end esteja na lista.

**Headers expostos pela API:**
- `X-Correlation-ID` — ID de correlação para rastreamento de erros
- `X-Pagination` — metadados de paginação (quando aplicável)

---

## Rate Limiting

A API limita requisições por IP:

| Janela | Limite |
|--------|--------|
| 1 minuto | 100 requisições |
| 1 hora | 1.000 requisições |

Quando o limite é atingido, a API retorna `429 Too Many Requests`.

### Como tratar no front-end

```ts
if (err.isRateLimit) {
  // Verificar o header Retry-After (se presente)
  const retryAfter = res.headers.get('Retry-After');
  const seconds = retryAfter ? parseInt(retryAfter) : 60;

  toast.warning(`Limite de requisições atingido. Aguarde ${seconds} segundos.`);

  // Opcional: implementar exponential backoff automático
  await sleep(seconds * 1000);
  return retry();
}
```

---

## Headers de resposta relevantes

| Header | Quando presente | Valor de exemplo | Uso |
|--------|----------------|-----------------|-----|
| `X-Correlation-ID` | Sempre | `abc123-def456` | Incluir em reports de erro ao suporte |
| `Retry-After` | Em `429` | `60` | Segundos para aguardar antes de retry |
| `Location` | Em `201 Created` | `/api/v1/identity/{id}` | URL do recurso criado |

---

## Versionamento da API

A API usa versionamento por URL: `/api/v{version}/{controller}`.

**Versão atual:** `v1`

Ao deprecar uma versão, a API retorna o header:
```
Deprecation: true
Sunset: {date}
```

---

## Configuração de ambiente (variáveis)

Configure as seguintes variáveis no seu projeto front-end:

```env
# .env.development
VITE_API_URL=http://localhost:8080/api/v1
VITE_TENANT_SLUG=public
VITE_OAUTH_REDIRECT_URI=http://localhost:3000/auth/callback

# .env.staging
VITE_API_URL=https://staging.api.exemplo.com/api/v1
VITE_TENANT_SLUG=acme
VITE_OAUTH_REDIRECT_URI=https://staging.app.exemplo.com/auth/callback

# .env.production
VITE_API_URL=https://api.exemplo.com/api/v1
VITE_TENANT_SLUG=acme
VITE_OAUTH_REDIRECT_URI=https://app.exemplo.com/auth/callback
```

---

## Configuração do cliente HTTP (exemplo completo)

```ts
// src/lib/api-client.ts
import type { ProblemDetails } from '../types/api-error';
import { ApiError } from '../types/api-error';

const BASE_URL = import.meta.env.VITE_API_URL;

function getTenantSlug(): string {
  return import.meta.env.VITE_TENANT_SLUG
    ?? window.location.hostname.split('.')[0]
    ?? 'public';
}

function getStoredToken(): string | null {
  return localStorage.getItem('access_token');
}

function buildHeaders(extra?: HeadersInit): HeadersInit {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'X-Tenant': getTenantSlug(),
  };

  const token = getStoredToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  return { ...headers, ...extra };
}

export async function apiGet<T>(path: string): Promise<T> {
  return apiRequest<T>(path, { method: 'GET' });
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  return apiRequest<T>(path, { method: 'POST', body: JSON.stringify(body) });
}

export async function apiPut<T>(path: string, body: unknown): Promise<T> {
  return apiRequest<T>(path, { method: 'PUT', body: JSON.stringify(body) });
}

export async function apiDelete(path: string): Promise<void> {
  await apiRequest<void>(path, { method: 'DELETE' });
}

async function apiRequest<T>(path: string, init: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: buildHeaders(init.headers),
  });

  if (res.status === 401) {
    localStorage.removeItem('access_token');
    window.location.href = '/login';
    throw new ApiError({ title: 'Não autorizado', status: 401 }, 401);
  }

  if (!res.ok) {
    const problem: ProblemDetails = await res.json().catch(() => ({
      title: 'Erro inesperado',
      status: res.status,
    }));
    throw new ApiError(problem, res.status);
  }

  if (res.status === 204) return undefined as T;

  return res.json() as Promise<T>;
}
```

---

## Feature Flags

A API suporta feature flags que podem alterar comportamentos. Flags atuais:

| Flag | Estado padrão | Impacto no front-end |
|------|:-------------:|---------------------|
| `EnableCaching` | `true` | Respostas podem ser cacheadas |
| `EnableRequestDeduplication` | `true` | Requisições duplicadas são ignoradas (idempotency) |
| `EnableAdvancedLogging` | `true` | Mais detalhes em logs de debug |
| `EnableExperimentalFeatures` | `false` | Features em desenvolvimento — não disponíveis |

> O front-end não precisa gerenciar feature flags diretamente — o comportamento já é controlado pelo backend.

---

## Saúde da API — Health Checks

| Endpoint | Propósito |
|----------|-----------|
| `GET /health/live` | Verifica se o processo está rodando |
| `GET /health/ready` | Verifica se a API e suas dependências estão prontas |
| `GET /healthchecks-ui` | Dashboard visual de saúde (apenas para administradores) |

Use `/health/live` para monitorar a disponibilidade da API no front-end:

```ts
async function checkApiHealth(): Promise<boolean> {
  try {
    const res = await fetch(`${BASE_URL.replace('/api/v1', '')}/health/live`);
    return res.ok;
  } catch {
    return false;
  }
}
```

