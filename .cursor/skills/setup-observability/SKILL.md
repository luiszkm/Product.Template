---
name: setup-observability
version: 1
description: "Configure OpenTelemetry, Prometheus, Grafana, Seq, or add health checks / custom metrics to this project. TRIGGER: \"add observability\", \"configure OTel\", \"add health check\", \"add metrics\", \"set up Grafana\", \"add custom span\", \"configure Seq\". SKIP: adding log statements to a handler (that is standard logging, not observability setup)."
tools: Read, Edit, Write, Glob
disable-model-invocation: true
---

# Skill: /setup-observability

> Configure the full observability stack for this .NET 10 / Clean Architecture repository: structured logging (Serilog), distributed tracing (OpenTelemetry → Grafana Tempo), metrics (Prometheus → Grafana), and health checks.

## Arguments

`$ARGUMENTS` format: `{TARGET}` (optional)

Examples:
- `/setup-observability otel` — configure OpenTelemetry
- `/setup-observability healthcheck` — add a health check
- `/setup-observability metrics` — add custom metrics
- `/setup-observability` — full setup overview

## Context — read before configuring

- `src/Api/Configurations/OpenTelemetryConfiguration.cs` — existing OTel config
- `src/Api/Configurations/SerilogConfiguration.cs` — existing Serilog config
- `infra/prometheus/prometheus.yml.template` — Prometheus config
- `infra/grafana/` — Grafana provisioning
- `.cursor/rules/observability.mdc` — logging invariants

## Observability Stack

| Pillar | Technology | Local URL | Role |
|--------|-----------|-----------|------|
| Traces | Grafana Tempo | `http://localhost:3200` | Distributed trace storage |
| Metrics | Prometheus | `http://localhost:9090` | Metrics scraping via `/metrics` |
| Dashboards | Grafana | `http://localhost:3000` | Unified view (metrics + traces) |
| Logs | Seq | `http://localhost:5341` | Structured log UI |

## OpenTelemetry Configuration

Located in `src/Api/Configurations/OpenTelemetryConfiguration.cs`.

### appsettings.json schema

```json
"OpenTelemetry": {
  "ServiceName": "{ProjectName}.Api",
  "ServiceVersion": "1.0.0",
  "EnableTraces": true,
  "EnableMetrics": true,
  "EnableConsoleExporter": false,
  "EnablePrometheusExporter": true,
  "OtlpEndpoint": "http://localhost:4317",
  "OtlpTracesEndpoint": "http://localhost:4317"
}
```

### Tracing

- Auto-instrumentation: `AspNetCore`, `HttpClient`, `Runtime`.
- Exporter: OTLP gRPC → Grafana Tempo.
- In Docker: `http://tempo:4317` (set via `OpenTelemetry__OtlpTracesEndpoint`).

### Metrics

- Exporter: Prometheus scraping endpoint `/metrics` (`AddPrometheusExporter()`).
- Prometheus scrapes every 15s (see `infra/prometheus/prometheus.yml`).
- Runtime metrics auto-collected (GC, threads, heap).

## Adding Custom Metrics

```csharp
// In the class that tracks the metric (e.g., in Application or Infrastructure)
private static readonly Meter Meter = new("{Namespace}.{Module}", "1.0.0");

public static readonly Counter<long> MyOperationCounter = Meter.CreateCounter<long>(
    "{module}_{operation}_total",
    description: "Number of {operation} performed");

// Usage:
MyOperationCounter.Add(1, new TagList { { "tenant_id", tenantId } });
```

Register the meter in `OpenTelemetryConfiguration.cs`:

```csharp
.WithMetrics(metrics => metrics
    .AddMeter("{Namespace}.{Module}")   // ← add this line
    ...
```

## Adding Custom Spans

```csharp
using var activity = OpenTelemetryConfiguration.ActivitySource
    .StartActivity("DescriptiveName");

activity?.SetTag("tenant.id", tenantId);
activity?.SetTag("entity.id", entityId);

try
{
    // ... work ...
}
catch (Exception ex)
{
    activity?.RecordException(ex);
    throw;
}
```

## Adding Health Checks

### 1. Create the health check class

```csharp
// src/Api/HealthChecks/{Service}HealthCheck.cs
public sealed class {Service}HealthCheck(I{Service}Client client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await client.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message, ex);
        }
    }
}
```

### 2. Register in `HealthCheckConfiguration.cs`

```csharp
services.AddHealthChecks()
    .AddCheck<{Service}HealthCheck>("{service-name}", tags: ["ready"]);
```

### 3. Rules

- `/health/live` → liveness (no dependencies — just that process is alive).
- `/health/ready` → readiness (all `ready`-tagged checks must pass).
- Every external dependency (Redis, external API, AI service) must have a health check.

## Grafana Dashboard

Pre-configured dashboard: `infra/grafana/dashboards/api-overview.json`.
Auto-provisioned via `infra/grafana/provisioning/`.

Access: `http://localhost:3000` → admin/admin123 → Dashboards → Product Template — API Overview.

## Minimum observability for new features

When adding a new feature:

1. **Handler logging** — `Information` on entry and success; `Warning` on business rule failure.
2. **Correlation ID** — automatically propagated by `RequestLoggingMiddleware`.
3. **Metrics** — add a counter for significant business operations if relevant.
4. **Health check** — required if the feature introduces a new external dependency.
