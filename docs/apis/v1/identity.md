# Identity Controller

Controller: `IdentityController`

Base route: `/api/v1/identity`

## GET `/api/v1/identity/providers`
- Auth: `AllowAnonymous`
- Status: `200`

Response 200:
```json
{
  "providers": ["string"],
  "count": 1
}
```

## GET `/api/v1/identity/{id}`
- Policy: `UserReadOrSelf`
- Path: `id` (guid)
- Response 200: `UserOutput`
- Status: `200`, `401`, `403`, `404`

`UserOutput`:
```json
{
  "id": "guid",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "emailConfirmed": true,
  "createdAt": "datetime",
  "lastLoginAt": "datetime|null"
}
```

## GET `/api/v1/identity/{id}/roles`
- Policy: `UsersManage`
- Path: `id` (guid)
- Response 200: `IEnumerable<string>`
- Status: `200`, `401`, `403`

## POST `/api/v1/identity/login`
- Auth: `AllowAnonymous`
- Status: `200`, `400`, `401`

Request body (`LoginCommand`):
```json
{
  "email": "string",
  "password": "string"
}
```

Response 200 (`AuthTokenOutput`):
```json
{
  "accessToken": "string",
  "tokenType": "string",
  "expiresIn": 3600,
  "refreshToken": "string",
  "user": {
    "id": "guid",
    "email": "string",
    "firstName": "string",
    "lastLoginAt": "datetime|null",
    "roles": ["string"]
  }
}
```

## POST `/api/v1/identity/refresh`
- Auth: `AllowAnonymous`
- Status: `200`, `400`, `401`

Request body (`RefreshTokenCommand`):
```json
{
  "refreshToken": "string"
}
```

Response 200: `AuthTokenOutput`

## POST `/api/v1/identity/register`
- Auth: `AllowAnonymous`
- Status: `201`, `400`, `409`

Request body (`RegisterUserCommand`):
```json
{
  "email": "string",
  "password": "string",
  "firstName": "string",
  "lastName": "string"
}
```

Response 201: `UserOutput`

## POST `/api/v1/identity/external-login`
- Auth: `AllowAnonymous`
- Status: `200`, `400`, `401`

Request body (`ExternalLoginCommand`):
```json
{
  "provider": "string",
  "code": "string",
  "redirectUri": "string|null"
}
```

Response 200: `AuthTokenOutput`

## GET `/api/v1/identity`
- Policy: `UsersRead`
- Query: `ListInput` (`pageNumber`, `pageSize`, `searchTerm`, `sortBy`, `sortDirection`)
- Response 200: `PaginatedListOutput<UserOutput>`
- Status: `200`, `401`, `403`

## PUT `/api/v1/identity/{id}`
- Policy: `UserManageOrSelf`
- Status: `200`, `400`, `401`, `403`, `404`
- Regra de contrato: `id` da rota deve ser igual a `userId` no body

Request body (`UpdateUserCommand`):
```json
{
  "userId": "guid",
  "firstName": "string",
  "lastName": "string"
}
```

Response 200: `UserOutput`

## POST `/api/v1/identity/{id}/confirm-email`
- Auth: `AllowAnonymous`
- Path: `id` (guid)
- Response: sem body
- Status: `204`, `404`

## DELETE `/api/v1/identity/{id}`
- Policy: `UsersManage`
- Path: `id` (guid)
- Response: sem body
- Status: `204`, `401`, `403`, `404`

