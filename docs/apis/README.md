# API Docs

Documentacao dos controllers da API (v1), focada em:
- rotas
- payloads de request
- payloads de response
- status codes
- politica de autorizacao

Sem exemplos ou codigo de frontend.

## Estrutura

- `docs/apis/v1/common.md`
- `docs/apis/v1/identity.md`
- `docs/apis/v1/authorization.md`
- `docs/apis/v1/tenants.md`
- `docs/apis/v1/ai.md`

## Convencoes gerais

- Base URL: `/api/v1`
- Content-Type: `application/json`
- Endpoints protegidos usam policy explicita (`[Authorize(Policy = ...)]`)
- Multi-tenancy: enviar header `X-Tenant` quando aplicavel
- Paginacao usa `PaginatedListOutput<T>`

