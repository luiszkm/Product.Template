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

### Tracing
- Instrumentation: `AspNetCore`, `HttpClient`, `Runtime`.
- Exporter: OTLP (`http://localhost:4317`) or Console (dev).
- Configured in `Api/Configurations/OpenTelemetryConfiguration.cs`.

### Metrics
- Runtime metrics are collected automatically.
- Custom metrics: `RbacMetrics` (role assignments, revocations, denials).
- When adding new metrics, follow the pattern:
  ```csharp
  private static readonly Meter Meter = new("Product.Template.{Module}", "1.0.0");
  public static readonly Counter<long> MyCounter = Meter.CreateCounter<long>(
      "{module}_{metric_name}_total",
      description: "Description");
  ```

### Service Identity
- `ServiceName`: `Product.Template.Api`
- `ServiceVersion`: from assembly version.

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

