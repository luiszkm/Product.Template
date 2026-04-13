# Authorization Controller

Controller: `AuthorizationController`

Base route: `/api/v1/authorization`

## Roles

### GET `/api/v1/authorization/roles`
- Policy: `AuthorizationRolesRead`
- Query: `pageNumber` (default `1`), `pageSize` (default `10`)
- Response 200: `PaginatedListOutput<RoleOutput>`
- Status: `200`, `401`, `403`

`RoleOutput`:
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "createdAt": "datetime"
}
```

### GET `/api/v1/authorization/roles/{id}`
- Policy: `AuthorizationRolesRead`
- Path: `id` (guid)
- Response 200: `RoleWithPermissionsOutput`
- Status: `200`, `401`, `403`, `404`

`RoleWithPermissionsOutput`:
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "createdAt": "datetime",
  "permissions": [
    {
      "id": "guid",
      "name": "string",
      "description": "string",
      "createdAt": "datetime"
    }
  ]
}
```

### POST `/api/v1/authorization/roles`
- Policy: `AuthorizationRolesManage`
- Status: `201`, `400`, `401`, `403`

Request body (`CreateRoleCommand`):
```json
{
  "name": "string",
  "description": "string"
}
```

Response 201: `RoleOutput`

### PUT `/api/v1/authorization/roles/{id}`
- Policy: `AuthorizationRolesManage`
- Status: `200`, `400`, `401`, `403`, `404`
- Regra de contrato: `id` da rota deve ser igual a `roleId` no body

Request body (`UpdateRoleCommand`):
```json
{
  "roleId": "guid",
  "name": "string",
  "description": "string"
}
```

Response 200: `RoleOutput`

### DELETE `/api/v1/authorization/roles/{id}`
- Policy: `AuthorizationRolesManage`
- Path: `id` (guid)
- Response: sem body
- Status: `204`, `401`, `403`, `404`

## Role Permissions

### GET `/api/v1/authorization/roles/{id}/permissions`
- Policy: `AuthorizationRolesRead`
- Path: `id` (guid)
- Response 200: `RoleWithPermissionsOutput`
- Status: `200`, `401`, `403`, `404`

### POST `/api/v1/authorization/roles/{id}/permissions`
- Policy: `AuthorizationRolesManage`
- Path: `id` (guid)
- Status: `204`, `400`, `401`, `403`

Request body:
```json
{
  "permissionId": "guid"
}
```

Response: sem body

### DELETE `/api/v1/authorization/roles/{id}/permissions/{permissionId}`
- Policy: `AuthorizationRolesManage`
- Path: `id` (guid), `permissionId` (guid)
- Response: sem body
- Status: `204`, `401`, `403`, `404`

## Permissions

### GET `/api/v1/authorization/permissions`
- Policy: `AuthorizationPermissionsRead`
- Query: `pageNumber` (default `1`), `pageSize` (default `50`)
- Response 200: `PaginatedListOutput<PermissionOutput>`
- Status: `200`, `401`, `403`

`PermissionOutput`:
```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "createdAt": "datetime"
}
```

### POST `/api/v1/authorization/permissions`
- Policy: `AuthorizationPermissionsManage`
- Status: `201`, `400`, `401`, `403`

Request body (`CreatePermissionCommand`):
```json
{
  "name": "string",
  "description": "string"
}
```

Response 201: `PermissionOutput`

### PUT `/api/v1/authorization/permissions/{id}`
- Policy: `AuthorizationPermissionsManage`
- Status: `200`, `400`, `401`, `403`, `404`
- Regra de contrato: `id` da rota deve ser igual a `permissionId` no body

Request body (`UpdatePermissionCommand`):
```json
{
  "permissionId": "guid",
  "name": "string",
  "description": "string"
}
```

Response 200: `PermissionOutput`

### DELETE `/api/v1/authorization/permissions/{id}`
- Policy: `AuthorizationPermissionsManage`
- Path: `id` (guid)
- Response: sem body
- Status: `204`, `401`, `403`, `404`

## User Assignments

### GET `/api/v1/authorization/users/{userId}/roles`
- Policy: `AuthorizationRolesRead`
- Path: `userId` (guid)
- Response 200: `IReadOnlyList<RoleOutput>`
- Status: `200`, `401`, `403`

### POST `/api/v1/authorization/users/{userId}/roles`
- Policy: `AuthorizationRolesManage`
- Path: `userId` (guid)
- Status: `204`, `400`, `401`, `403`

Request body:
```json
{
  "roleId": "guid"
}
```

Response: sem body

### DELETE `/api/v1/authorization/users/{userId}/roles/{roleId}`
- Policy: `AuthorizationRolesManage`
- Path: `userId` (guid), `roleId` (guid)
- Response: sem body
- Status: `204`, `401`, `403`, `404`

