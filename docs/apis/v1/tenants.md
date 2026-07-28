# Tenants Controller

Controller: `TenantsController`

Base route: `/api/v1/tenants`

## Identificador do tenant

| Conceito | Tipo | Uso |
|----------|------|-----|
| `tenantId` | `uuid` (Guid) | Identificador estável na API, Host DB e coluna `TenantId` em entidades multi-tenant (`IMultiTenantEntity`) |
| `tenantKey` | `string` | Slug resolvido no runtime via header `X-Tenant` (ex.: `public`, `acme`) |

O servidor **gera** `tenantId` na criação (`POST`). O cliente não envia ID numérico sequencial.

Tenant seed padrão (desenvolvimento): `WellKnownTenants.Public` = `00000000-0000-0000-0000-000000000001`, `tenantKey` = `public`.

Detalhes de arquitetura: [Tenant identifiers](../../guides/tenant-identifiers.md) e [Multi-tenancy architecture](../../guides/multi-tenancy-architecture.md) (resolução, cache, provisionamento, isolamento).

## GET `/api/v1/tenants`

- Policy: `TenantsRead`
- Query (`ListTenantsQuery` / `ListInput`):
  - `pageNumber` (default `1`)
  - `pageSize` (default `20`)
  - `searchTerm` (opcional) — filtra `displayName`, `tenantKey`, `contactEmail`
  - `sortBy` (opcional) — `tenantKey`, `key`, `displayName`, `name`, `contactEmail`, `description`, `createdAt`, `isActive`
  - `sortDirection` (opcional) — `asc` ou `desc` (default da listagem: `createdAt` desc)
- Response 200: `PaginatedListOutput<TenantOutput>`
- Status: `200`, `401`, `403`

`TenantOutput`:

```json
{
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantKey": "acme",
  "displayName": "Acme Corp",
  "contactEmail": "admin@acme.com",
  "isActive": true,
  "isolationMode": "SharedDb",
  "createdAt": "2026-05-22T12:00:00Z"
}
```

## GET `/api/v1/tenants/{id}`

- Policy: `TenantsRead`
- Path: `id` (`uuid`)
- Response 200: `TenantOutput`
- Status: `200`, `401`, `403`, `404`

## POST `/api/v1/tenants`

- Policy: `TenantsManage`
- Status: `201`, `400`, `401`, `403`

Request body (`CreateTenantCommand`):

```json
{
  "tenantKey": "acme",
  "displayName": "Acme Corp",
  "contactEmail": "admin@acme.com",
  "isolationMode": "SharedDb"
}
```

`isolationMode` valores:

- `SharedDb`
- `SchemaPerTenant`
- `DedicatedDb`

Response 201: `TenantOutput` (inclui `tenantId` gerado)

## PUT `/api/v1/tenants/{id}`

- Policy: `TenantsManage`
- Path: `id` (`uuid`)
- Status: `200`, `400`, `401`, `403`, `404`
- Regra: `id` da rota deve ser igual a `tenantId` no body

Request body (`UpdateTenantCommand`):

```json
{
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "displayName": "Acme Corp",
  "contactEmail": "admin@acme.com"
}
```

Response 200: `TenantOutput`

## DELETE `/api/v1/tenants/{id}`

- Policy: `TenantsManage`
- Path: `id` (`uuid`)
- Comportamento: desativa o tenant (`isActive = false`); não apaga dados
- Response: sem body
- Status: `204`, `401`, `403`, `404`
