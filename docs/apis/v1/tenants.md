# Tenants Controller

Controller: `TenantsController`

Base route: `/api/v1/tenants`

## GET `/api/v1/tenants`
- Policy: `TenantsRead`
- Query: `pageNumber` (default `1`), `pageSize` (default `20`)
- Response 200: `PaginatedListOutput<TenantOutput>`
- Status: `200`, `401`, `403`

`TenantOutput`:
```json
{
  "tenantId": 1,
  "tenantKey": "string",
  "displayName": "string",
  "contactEmail": "string|null",
  "isActive": true,
  "isolationMode": "SharedDb",
  "createdAt": "datetime"
}
```

## GET `/api/v1/tenants/{id}`
- Policy: `TenantsRead`
- Path: `id` (long)
- Response 200: `TenantOutput`
- Status: `200`, `401`, `403`, `404`

## POST `/api/v1/tenants`
- Policy: `TenantsManage`
- Status: `201`, `400`, `401`, `403`

Request body (`CreateTenantCommand`):
```json
{
  "tenantId": 1,
  "tenantKey": "string",
  "displayName": "string",
  "contactEmail": "string|null",
  "isolationMode": "SharedDb"
}
```

`isolationMode` valores:
- `SharedDb`
- `SchemaPerTenant`
- `DedicatedDb`

Response 201: `TenantOutput`

## PUT `/api/v1/tenants/{id}`
- Policy: `TenantsManage`
- Status: `200`, `400`, `401`, `403`, `404`
- Regra de contrato: `id` da rota deve ser igual a `tenantId` no body

Request body (`UpdateTenantCommand`):
```json
{
  "tenantId": 1,
  "displayName": "string",
  "contactEmail": "string|null"
}
```

Response 200: `TenantOutput`

## DELETE `/api/v1/tenants/{id}`
- Policy: `TenantsManage`
- Path: `id` (long)
- Response: sem body
- Status: `204`, `401`, `403`, `404`

