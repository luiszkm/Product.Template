# 09 — Observability Rules

## Structured Logging (Serilog)

### Configuration
- Serilog is configured in `Api/Configurations/SerilogConfiguration.cs`.
- Sinks: Console, File (rolling daily, 10MB limit, 30-day retention), Seq.
- Enrichers: `FromLogContext`, `WithMachineName`, `WithThreadId`, `WithExceptionDetails`.

### Rules

1. Use **structured log templates** — never string concatenation:
   ```csharp
   // ✅ CORRECT
   _logger.LogInformation("User {UserId} registered successfully", user.Id);

   // ❌ WRONG
   _logger.LogInformation($"User {user.Id} registered successfully");
   ```
2. Log at the **right level**:
   - `Debug` — internal state for troubleshooting.
   - `Information` — significant business events (user registered, login succeeded).
   - `Warning` — recoverable issues (login failed, duplicate request, idempotent skip).
   - `Error` — unexpected failures that need attention.
   - `Fatal` — application cannot continue.
3. **Never log sensitive data** — passwords, tokens, secrets, PII beyond user IDs.
4. Include `{UserId}`, `{Email}`, `{TenantId}` in log context where available.

## Correlation ID

- `RequestLoggingMiddleware` generates or reads `X-Correlation-ID` from the request.
- The correlation ID is added to the Serilog `LogContext` and returned in the response header.
- All log entries within a request share the same correlation ID.

## Request Logging

- `RequestLoggingMiddleware` logs:
  - Request: method, path, query, safe headers, remote IP, body (with sensitive field masking).
  - Response: status code, elapsed time, body size.
- Serilog's built-in `UseSerilogRequestLogging()` provides performance-optimized summary logs.

## OpenTelemetry

### Stack de Observabilidade

| Pilar | Tecnologia | URL local | Função |
|-------|-----------|-----------|--------|
| **Traces** | Grafana Tempo | `http://localhost:3200` | Armazena e consulta distributed traces |
| **Métricas** | Prometheus | `http://localhost:9090` | Coleta métricas via scraping `/metrics` |
| **Dashboards** | Grafana | `http://localhost:3000` | Visualização unificada métricas + traces |
| **Logs** | Seq | `http://localhost:5341` | Logs estruturados com filtros |

### Tracing
- Instrumentação automática: `AspNetCore`, `HttpClient`, `Runtime`.
- Exporter de traces: OTLP gRPC → **Grafana Tempo** (`http://localhost:4317`).
- Em Docker: `http://tempo:4317` (variável `OpenTelemetry__OtlpTracesEndpoint`).
- Configurado em `Api/Configurations/OpenTelemetryConfiguration.cs`.

### Metrics
- Exporter: **Prometheus** scraping endpoint `/metrics` (via `AddPrometheusExporter()`).
- Prometheus coleta a cada 15s conforme `infra/prometheus/prometheus.yml`.
- Runtime metrics são coletadas automaticamente (GC, threads, heap).
- Custom metrics: `RbacMetrics` (role assignments, revocations, denials).
- Quando adicionar novas métricas, siga o padrão:
  ```csharp
  private static readonly Meter Meter = new("Product.Template.{Module}", "1.0.0");
  public static readonly Counter<long> MyCounter = Meter.CreateCounter<long>(
      "{module}_{metric_name}_total",
      description: "Description");
  ```

### Custom Spans
- Use `OpenTelemetryConfiguration.ActivitySource.StartActivity("NomeDoSpan")` para spans manuais.
- Adicione tags relevantes: `tenant.id`, `user.id`, `entity.id`.
- Chame `activity?.RecordException(ex)` em catch blocks.

### Service Identity
- `ServiceName`: `Product.Template.Api`
- `ServiceVersion`: `1.0.0` (configurável via `appsettings.json`)

### Configurações disponíveis (`appsettings.json`)

```json
"OpenTelemetry": {
  "ServiceName": "Product.Template.Api",
  "ServiceVersion": "1.0.0",
  "EnableTraces": true,
  "EnableMetrics": true,
  "EnableConsoleExporter": false,
  "EnablePrometheusExporter": true,
  "OtlpEndpoint": "http://localhost:4317",
  "OtlpTracesEndpoint": "http://localhost:4317"
}
```

### Dashboard Grafana
- Dashboard pré-configurado em `infra/grafana/dashboards/api-overview.json`.
- Provisionado automaticamente via `infra/grafana/provisioning/`.
- Acesse: `http://localhost:3000` → admin/admin123 → Dashboards → Product Template — API Overview.

## Health Checks

- EF Core database health check is registered.
- HealthChecks UI is available at `/healthchecks-ui`.
- Custom health checks (e.g., `DatabaseHealthCheck`) are in `Api/HealthChecks/`.
- Every external dependency (Redis, external API) should have a health check.

## Minimum Observability for New Features

When adding a new feature, ensure:

1. **Handler logging**: Log at `Information` level on entry and success; `Warning` on business rule failure.
2. **Correlation ID**: automatically propagated — no extra work needed.
3. **Metrics**: Add counters for significant business operations (if applicable).
4. **Health check**: If the feature depends on a new external service, add a health check.

