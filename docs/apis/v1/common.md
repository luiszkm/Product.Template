# API v1 - Common

## Base

- Base URL: `/api/v1`
- Header recomendado para autenticacao: `Authorization: Bearer <token>`
- Header de tenant: `X-Tenant: <tenant-key>`

## Paginacao

Queries paginadas retornam:

```json
{
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 42,
  "data": []
}
```

Formato (`PaginatedListOutput<T>`):
- `pageNumber` (int)
- `pageSize` (int)
- `totalCount` (int)
- `data` (array de `T`)

Input base de listagem (`ListInput`):
- `pageNumber` (int, default `1`)
- `pageSize` (int, default `10`)
- `searchTerm` (string|null)
- `sortBy` (string|null)
- `sortDirection` (string|null)

## Errors

Padrao de erro: `ProblemDetails`.

Mapeamentos globais (`ApiGlobalExceptionFilter`):
- `NotFoundException` -> `404 Not Found`
- `BusinessRuleException` -> `400 Bad Request`
- `DomainException` -> `422 Unprocessable Entity`
- Erro nao tratado -> `500 Internal Server Error`

Observacao:
- `401 Unauthorized` e `403 Forbidden` sao retornos de autenticacao/autorizacao.
- Erros de validacao de payload podem retornar `400 Bad Request`.

