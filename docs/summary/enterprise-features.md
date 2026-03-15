# Enterprise Features — Implemented

> Funcionalidades enterprise-grade implementadas no Product.Template

---

## ✅ 1. Refresh Token (Token Rotation)

### O que é
Tokens de longa duração que permitem renovar o access token sem exigir novo login.

### Como funciona
- **Login** → retorna `accessToken` (60 min) + `refreshToken` (30 dias)
- **Token expira** → chamar `POST /identity/refresh` com o refresh token
- **Rotation automático** → cada uso do refresh token o invalida e gera um novo

### Endpoint
```http
POST /api/v1/identity/refresh
Content-Type: application/json

{
  "refreshToken": "xMzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response:**
```json
{
  "accessToken": "novo-access-token...",
  "refreshToken": "novo-refresh-token...",
  "expiresIn": 3600,
  "user": { ... }
}
```

### Tabela no banco
`RefreshTokens` com colunas:
- `Token`, `UserId`, `ExpiresAt`, `IsRevoked`, `ReplacedByToken`, `CreatedByIp`

### Segurança
- Token rotation previne reutilização de tokens roubados
- Refresh tokens revogados não podem ser usados
- IP de criação/revogação registrado para auditoria

---

## ✅ 2. Audit Log

### O que é
Registro imutável de todas as mudanças de estado no sistema para compliance, debugging e segurança.

### Como funciona
Interceptor EF Core (`AuditLogInterceptor`) captura automaticamente:
- `Added` → "Created"
- `Modified` → "Updated" + JSON com (old → new)
- `Deleted` → "Deleted"

### Tabela no banco
`AuditLogs`:
```
Id, TenantId, Actor, EntityType, EntityId, Action, Changes, Metadata, OccurredAt
```

### Exemplo de entry
```json
{
  "id": "...",
  "actor": "admin@example.com",
  "entityType": "User",
  "entityId": "3fa85f64-...",
  "action": "Updated",
  "changes": "{\"Email\":{\"Old\":\"old@x.com\",\"New\":\"new@x.com\"}}",
  "occurredAt": "2026-03-15T14:30:00Z"
}
```

### Uso
- **Compliance**: LGPD, SOC2, GDPR
- **Debugging**: "quem mudou X quando?"
- **Segurança**: detectar ações suspeitas

---

## ✅ 3. Tenant Isolation (3 modos)

### Modos disponíveis

| Modo | Banco | Schema | Quando usar |
|------|-------|--------|-------------|
| `SharedDb` | Único | Único | SaaS multi-tenant padrão (+ eficiente) |
| `SchemaPerTenant` | Único | Um por tenant | Isolamento médio (PostgreSQL only) |
| `DedicatedDb` | Um por tenant | Um | Máxima isolação (clientes enterprise) |

### Proteções implementadas
- ✅ `IMultiTenantEntity` → todas as entidades têm `TenantId`
- ✅ Query filter global → `WHERE TenantId = @currentTenant`
- ✅ Save interceptor → `TenantId` preenchido automaticamente
- ✅ Header `X-Tenant` obrigatório em toda requisição

### Configuração
`appsettings.json`:
```json
{
  "MultiTenancy": {
    "Provider": "Sqlite",
    "HeaderName": "X-Tenant",
    "AllowPublicFallback": false,
    "PublicTenantKey": "public"
  }
}
```

---

## ✅ 4. Soft Delete Global

### O que é
Entidades marcadas como deletadas permanecem no banco (invisíveis para queries normais).

### Como funciona
- `Entity` implementa `ISoftDeletableEntity` → tem `DeletedAt`, `DeletedBy`
- Query filter global: `WHERE DeletedAt IS NULL`
- Método `entity.SoftDelete(deletedBy)` marca como deletada
- Método `entity.Restore(restoredBy)` restaura

### Campos audit de delete
```csharp
DateTime? DeletedAt
string? DeletedBy
DateTime? RestoredAt
string? RestoredBy
```

### Vantagens
- Recuperação de dados acidental
- Auditoria completa (quem deletou, quando)
- Compliance (LGPD direito ao esquecimento com retenção temporária)

---

## ✅ 5. Observabilidade (Serilog + OpenTelemetry)

### Logging (Serilog)
- Structured logging: `_logger.LogInformation("User {UserId} created", user.Id)`
- Sinks: Console + File (rolling) + Seq
- Enrichers: `CorrelationId`, `MachineName`, `ThreadId`
- Middleware: `RequestLoggingMiddleware` adiciona `X-Correlation-ID` em toda resposta

### Tracing (OpenTelemetry)
- Instrumentação automática: ASP.NET Core, HttpClient, EF Core
- Exporter: OTLP (Jaeger, Zipkin, Azure Monitor)
- ServiceName: `Product.Template.Api`

### Metrics (OpenTelemetry)
- Runtime metrics: CPU, memory, GC
- Custom meters por módulo: `Product.Template.Identity`
- Pattern: `{module}_{operation}_total`

### Health Checks
- `/health/live` → processo vivo (usado pelo Docker HEALTHCHECK)
- `/health/ready` → dependências prontas (DB, serviços externos)

---

## ✅ 6. Idempotency (Request Deduplication)

### O que é
Previne processamento duplicado de requisições POST/PUT/PATCH.

### Como funciona
- Middleware `RequestDeduplicationMiddleware`
- Header opcional: `X-Idempotency-Key`
- Se ausente, gera hash SHA256 do (método + path + body)
- Cache por 5 minutos em `IMemoryCache`
- Requisição duplicada → `409 Conflict`

### Uso no front-end
```http
POST /api/v1/identity/register
X-Idempotency-Key: uuid-gerado-pelo-cliente
```

### Limitação atual
- Usa `IMemoryCache` → não funciona em múltiplas réplicas
- **TODO enterprise**: substituir por Redis distribuído

---

## ✅ 7. Rate Limiting

### Implementação atual
- Global por IP: 100 req/min, 1000 req/hora
- Policy: `fixed-by-ip`
- Resposta: `429 Too Many Requests` + header `Retry-After`

### Configuração
```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60
  }
}
```

### Limitação atual
- Apenas por IP — **não considera tenant**
- **TODO enterprise**: rate limit por tenant para SaaS justo

---

## Próximos passos enterprise

| Feature | Status | Prioridade |
|---------|--------|-----------|
| Idempotency distribuída (Redis) | ⚠️ Planejado | Alta |
| Rate limit por tenant | ⚠️ Planejado | Alta |
| Audit log query API | ⚠️ Planejado | Média |
| Soft delete permanente job | ⚠️ Planejado | Média |
| Refresh token blacklist distribuída | ⚠️ Planejado | Baixa |

---

## Checklist de deploy

Antes de ir para produção, garanta:

- [ ] `Jwt:RefreshTokenExpirationDays` configurado (padrão: 30)
- [ ] Tabelas `RefreshTokens` e `AuditLogs` criadas (via migration)
- [ ] `AuditLogInterceptor` registrado no `AddInterceptors`
- [ ] Seq ou OTLP endpoint configurado para logs/traces
- [ ] Health checks `/health/ready` verificando todas as dependências
- [ ] Soft delete query filter testado (entidades deletadas invisíveis)
- [ ] Tenant isolation validado (um tenant nunca vê dados de outro)
- [ ] Rate limiting ajustado por carga esperada

