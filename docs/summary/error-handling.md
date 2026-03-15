# Error Handling — Frontend

> Formato dos erros retornados pela API e como tratá-los no front-end.

---

## Formato padrão: ProblemDetails (RFC 9457)

Todos os erros seguem o padrão RFC 9457 (`ProblemDetails`):

```json
{
  "title": "Not Found",
  "status": 404,
  "type": "Not Found",
  "detail": "User with ID '3fa85f64-...' was not found."
}
```

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `title` | `string` | Título curto do erro |
| `status` | `number` | HTTP status code |
| `type` | `string` | Identificador do tipo de erro |
| `detail` | `string` | Mensagem detalhada legível para o usuário |

> ℹ️ Em ambiente de **desenvolvimento**, a API adiciona o campo `extensions.StackTrace` na resposta. Nunca exibir ao usuário final.

---

## Erros de validação (FluentValidation)

Quando o payload de entrada falha na validação, a API retorna `400 Bad Request` com lista de erros por campo:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "type": "ValidationError",
  "errors": {
    "email": ["'Email' is not a valid email address."],
    "password": [
      "Password must be at least 8 characters.",
      "Password must contain at least one uppercase letter."
    ]
  }
}
```

> Use o campo `errors` para mapear erros diretamente nos campos do formulário.

---

## Tabela completa de erros por tipo

| `type` | Status | Quando ocorre | Ação recomendada no front |
|--------|--------|---------------|--------------------------|
| `Not Found` | `404` | Recurso (usuário, role) não existe | Exibir mensagem "Não encontrado" |
| `BusinessRuleViolation` | `400` | Regra de negócio violada (ex: email duplicado) | Exibir `detail` ao usuário |
| `UnProcessableEntity` | `422` | Invariante de domínio violada | Exibir `detail` ao usuário |
| `ValidationError` | `400` | Campos inválidos (FluentValidation) | Mapear `errors` nos campos do form |
| `UnexpectedError` | `500` | Erro interno inesperado | Exibir mensagem genérica + notificar suporte |

---

## Erros por endpoint

### `POST /identity/login`
| Status | `type` | O que fazer |
|--------|--------|-------------|
| `400` | `ValidationError` | Exibir erros nos campos |
| `401` | — | "Email ou senha incorretos" |
| `429` | — | "Muitas tentativas. Aguarde antes de tentar novamente." |

### `POST /identity/register`
| Status | `type` | O que fazer |
|--------|--------|-------------|
| `400` | `ValidationError` | Exibir erros nos campos |
| `409` | `BusinessRuleViolation` | "Este email já está cadastrado" |

### `PUT /identity/{id}`
| Status | O que fazer |
|--------|-------------|
| `400` | ID da URL diferente do body — erro de programação |
| `403` | Usuário sem permissão — exibir "Acesso negado" |
| `404` | Usuário não encontrado |

---

## Implementação do handler global de erros

### TypeScript (fetch + tipo utilitário)

```ts
// types/api-error.ts
export interface ProblemDetails {
  title: string;
  status: number;
  type?: string;
  detail?: string;
  errors?: Record<string, string[]>; // validação FluentValidation
}

export class ApiError extends Error {
  constructor(
    public readonly problem: ProblemDetails,
    public readonly httpStatus: number
  ) {
    super(problem.detail ?? problem.title);
  }

  get isNotFound() { return this.httpStatus === 404; }
  get isUnauthorized() { return this.httpStatus === 401; }
  get isForbidden() { return this.httpStatus === 403; }
  get isValidationError() { return this.httpStatus === 400 && !!this.problem.errors; }
  get isConflict() { return this.httpStatus === 409; }
  get isRateLimit() { return this.httpStatus === 429; }
}

// api.ts — dentro do wrapper de fetch
async function apiFetch(path: string, init: RequestInit = {}) {
  const res = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: buildHeaders(init.headers),
  });

  if (!res.ok) {
    const problem: ProblemDetails = await res.json().catch(() => ({
      title: 'Erro inesperado',
      status: res.status,
    }));
    throw new ApiError(problem, res.status);
  }

  return res.status === 204 ? null : res.json();
}
```

### Tratando erros de formulário (React Hook Form)

```ts
// Exemplo no formulário de registro
async function onSubmit(data: RegisterFormData) {
  try {
    await register(data);
    navigate('/login');
  } catch (err) {
    if (err instanceof ApiError) {
      if (err.isValidationError && err.problem.errors) {
        // Mapear erros diretamente nos campos
        Object.entries(err.problem.errors).forEach(([field, messages]) => {
          form.setError(field as keyof RegisterFormData, {
            message: messages[0],
          });
        });
        return;
      }

      if (err.isConflict) {
        form.setError('email', { message: 'Este email já está cadastrado' });
        return;
      }

      // Outros erros — toast global
      toast.error(err.problem.detail ?? 'Erro ao criar conta');
    }
  }
}
```

### Handler global com Axios

```ts
// axios.config.ts
api.interceptors.response.use(
  res => res,
  (err: AxiosError<ProblemDetails>) => {
    const status = err.response?.status;
    const problem = err.response?.data;

    switch (status) {
      case 401:
        clearToken();
        window.location.href = '/login';
        break;
      case 403:
        toast.error('Você não tem permissão para realizar esta ação');
        break;
      case 429:
        toast.error('Muitas requisições. Aguarde alguns instantes.');
        break;
      case 500:
        toast.error('Erro interno. Nossa equipe foi notificada.');
        Sentry.captureException(err); // ou outro tracker
        break;
    }

    return Promise.reject(new ApiError(problem ?? { title: 'Erro', status: status ?? 0 }, status ?? 0));
  }
);
```

---

## Mensagens recomendadas para o usuário final

| Cenário | Mensagem sugerida (PT-BR) |
|---------|--------------------------|
| `401` no login | "Email ou senha incorretos." |
| `401` em rota protegida | Redirecionar para login silenciosamente |
| `403` | "Você não tem permissão para acessar este recurso." |
| `404` | "Item não encontrado." |
| `409` no cadastro | "Este email já está cadastrado. Faça login ou use outro email." |
| `422` | Exibir o campo `detail` do ProblemDetails |
| `429` | "Muitas tentativas. Por favor, aguarde alguns minutos." |
| `500` | "Ocorreu um erro inesperado. Tente novamente mais tarde." |

---

## Headers de correlação

Cada resposta da API inclui o header `X-Correlation-ID` com o ID único da requisição. Ao reportar um erro ao suporte, inclua este valor:

```ts
const correlationId = res.headers.get('X-Correlation-ID');
toast.error(`Erro inesperado (ID: ${correlationId}). Contate o suporte.`);
```

