﻿# Product.Template

A production-ready .NET 10 backend template following Clean Architecture, DDD, and CQRS — designed for **AI-first development** with LLMs and coding agents.

## Stack

| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET | 10 | Runtime |
| ASP.NET Core | 10 | Web framework |
| C# | latest | Language |
| EF Core | 10.x | ORM (write) |
| MediatR | 14.x | CQRS mediator |
| FluentValidation | 12.x | Input validation |
| Serilog | 10.x | Structured logging (Console · File · Seq) |
| OpenTelemetry | 1.14+ | Distributed traces & metrics |
| Grafana Tempo | 2.6.x | Trace storage (OTLP receiver) |
| Prometheus | latest | Metrics scraping & storage |
| Grafana | latest | Dashboards (traces + metrics) |
| Seq | 2025.x | Structured log UI |
| JWT Bearer | — | Authentication |
| Scalar | — | API documentation (OpenAPI) |
| xUnit + Bogus | — | Testing |
| Docker Compose | — | Full local dev environment |

## Architecture

```
┌──────────────────────────────┐
│  Api (Controllers, Middleware)│
├──────────────────────────────┤
│  Application (CQRS Handlers) │
├──────────────────────────────┤
│  Infrastructure (EF, Repos)  │
├──────────────────────────────┤
│  Domain (Entities, VOs, Events)│
└──────────────────────────────┘
```

Dependencies flow **inward**: Api → Infrastructure → Application → Domain.

## Project Structure

```
src/
├── Api/                          → ASP.NET Core host
├── Core/
│   └── Identity/                 → Identity module (reference implementation)
│       ├── Identity.Domain/
│       ├── Identity.Application/
│       └── Identity.Infrastructure/
├── Shared/
│   ├── Kernel.Domain/            → Base types (Entity, AggregateRoot)
│   ├── Kernel.Application/       → CQRS interfaces, Behaviors, Exceptions
│   └── Kernel.Infrastructure/    → EF Core, Security, MultiTenancy
└── Tools/
    └── Migrator/

tests/
├── ArchitectureTests/            → Layer & naming enforcement
├── UnitTests/                    → Domain, handlers, validators
├── IntegrationTests/             → HTTP authorization tests
├── CommonTests/                  → Shared fixtures
└── E2ETests/                     → End-to-end (future)
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (Optional) Docker for containerized execution

### Setup

```bash
# Clone and restore
git clone <repository-url>
cd Product.Template
dotnet restore

# Run the API
cd src/Api
dotnet run
```

The API starts at `https://localhost:7254` with Scalar docs at `/scalar/v1`.

### Default Seed Data

| User | Email | Role |
|------|-------|------|
| Admin | admin@producttemplate.com | Admin |
| Test | user@producttemplate.com | User |

### Required Headers

All API requests require the tenant header:
```
X-Tenant: public
```

---

## Docker (Development)

The `compose.yaml` at the repo root spins up the **complete local development environment** — API, database, and the full observability stack — with a single command.

### Start everything

```bash
docker compose up
```

### Rebuild after code changes

```bash
docker compose up --build
```

### Stop and wipe all volumes

```bash
docker compose down -v
```

### Service map

| Service | Image | Port(s) | Purpose |
|---------|-------|---------|---------|
| `api` | `product-template-api` (built locally) | `8080` | ASP.NET Core API |
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | `1433` | SQL Server (AppDb + HostDb) |
| `seq` | `datalust/seq:2025.x` | `5341` | Structured log UI |
| `tempo` | `grafana/tempo:2.6.1` | `3200` · `4317` · `4318` | Trace storage (OTLP) |
| `prometheus` | `prom/prometheus` | `9090` | Metrics storage (scrapes `/metrics`) |
| `grafana` | `grafana/grafana` | `3000` | Dashboards |

### Startup sequence

```
sqlserver (healthy) ─┐
                     ├──► api ──► prometheus ──► grafana
tempo (started)  ────┘
seq (independent)
```

The `api` waits for SQL Server to pass its healthcheck before starting.

### Default credentials

| Service | URL | Username | Password |
|---------|-----|----------|---------|
| API | http://localhost:8080 | — | — |
| Seq | http://localhost:5341 | `admin` | `admin123` |
| Grafana | http://localhost:3000 | `admin` | `admin123` |
| Prometheus | http://localhost:9090 | — | — |
| SQL Server | `localhost,1433` | `sa` | `YourStrong!Pass123` |

> **Important:** credentials in `compose.yaml` are for **local development only**.  
> In production, supply them via environment variables or a secrets manager — never in source control.

### Dockerfile multi-stage build

```
restore  → copy .csproj + packages.lock.json, dotnet restore --locked-mode
publish  → copy full source, dotnet publish -c Release
final    → aspnet:10.0-alpine + icu-libs, non-root user app:app (UID 1654)
```

The image runs as non-root (`USER app`) and exposes port `8080` only.  
`HEALTHCHECK` points to `/health/live` — a zero-cost liveness probe that always returns `200` as long as the process is alive.

---

## Observability

The stack ships with a complete, pre-wired observability pipeline out of the box.

### Architecture

```
API ──(OTLP gRPC)──► Tempo ──────────────────► Grafana
 │                                              ▲
 └──(/metrics scrape)──► Prometheus ────────────┘
 │
 └──(Serilog sink)──► Seq
```

### Structured Logging — Serilog

Serilog writes to three sinks simultaneously:

| Sink | Where | Notes |
|------|-------|-------|
| Console | stdout | Human-readable in dev, JSON in prod |
| File | `src/Api/logs/log-YYYYMMDD.txt` | Rotates daily, 30-day retention |
| Seq | http://localhost:5341 | Searchable, filterable UI |

Every request is enriched with:
- `CorrelationId` — propagated via `X-Correlation-ID` response header
- `MachineName`, `ThreadId`, `Application`, `Environment`
- Exceptions via `Serilog.Exceptions` (structured, not string-serialised)

Sensitive fields (passwords, tokens) are masked by `RequestLoggingMiddleware` before logging.

**Rule:** never use string interpolation in log templates — always structured arguments:

```csharp
// ✅ correct
_logger.LogInformation("User {UserId} logged in", user.Id);

// ❌ wrong
_logger.LogInformation($"User {user.Id} logged in");
```

### Distributed Tracing — OpenTelemetry → Tempo

Traces are exported via **OTLP gRPC** to Grafana Tempo (`http://tempo:4317`).

Automatically instrumented:
- All incoming HTTP requests (ASP.NET Core)
- All outgoing HTTP calls (HttpClient)
- Runtime metrics (GC, thread pool, exceptions)

Custom spans can be added anywhere:

```csharp
using var activity = OpenTelemetryConfiguration.ActivitySource.StartActivity("my-operation");
activity?.SetTag("order.id", orderId);
```

Configuration (`appsettings.json`):

```json
"OpenTelemetry": {
  "EnableTraces": true,
  "EnableMetrics": true,
  "EnablePrometheusExporter": true,
  "OtlpTracesEndpoint": "http://localhost:4317"
}
```

### Metrics — Prometheus + Grafana

Prometheus scrapes the `/metrics` endpoint every **15 seconds**.

Exposed metrics include:
- ASP.NET Core request duration, active connections, response codes
- .NET runtime: GC collections, heap size, thread pool, exception rate
- HttpClient call durations

Open **Grafana** at http://localhost:3000 to query metrics and traces together.  
Tempo is pre-configured as a data source and linked to Prometheus via span metrics.

### Health Checks

Three purpose-built endpoints:

| Endpoint | Purpose | Used by |
|----------|---------|---------|
| `GET /health/live` | Liveness — is the process alive? | Docker `HEALTHCHECK`, k8s `livenessProbe` |
| `GET /health/ready` | Readiness — AppDb + HostDb reachable? | k8s `readinessProbe`, load balancer |
| `GET /health` | Full diagnostics (all checks + history) | Monitoring, admins — **protected in production** |

Checks registered:

| Check | Tag | Threshold |
|-------|-----|-----------|
| `appdb` — `SELECT 1` on AppDbContext | `ready` | `> 1 000 ms` → Degraded |
| `hostdb` — `CanConnect` on HostDbContext | `ready` | failure → Unhealthy |
| `memory` — GC heap | `system` | `> 1 GB` → Degraded |
| `disk-space` — available free space | `system` | `< 10 %` → Degraded |

In **Development** only, the Health Checks UI is available at:
```
http://localhost:8080/healthchecks-ui
```

In **Production**, `/health` requires `AdminOnly` policy and `/healthchecks-ui` is disabled entirely.

### Correlation IDs

Every request receives a `CorrelationId` (UUID v4) injected by `RequestLoggingMiddleware`.  
It is:
- Logged in every Serilog entry for that request
- Returned in the `X-Correlation-ID` response header
- Propagated to downstream HTTP calls via `HttpClient` headers

Use it to correlate a Seq log entry → Tempo trace → Prometheus metric for the same request.

---

## AI-First Development

This template has a built-in **GitHub Copilot AI-first layer** — persistent instructions, specialized agents, and reusable prompts that eliminate the need to repeat architecture rules in every session.

### How it works

GitHub Copilot reads `.github/copilot-instructions.md` automatically on every interaction in this repo. It contains the complete stack, architecture rules, naming conventions, and a list of what Copilot must never do — all derived from the actual code.

### GitHub Copilot Instructions

```
.github/
├── copilot-instructions.md          → Auto-loaded by Copilot on every session
├── instructions/
│   ├── backend.instructions.md      → Commands, Queries, Handlers, Validators, DTOs
│   ├── api.instructions.md          → Controllers, HTTP contracts, RBAC, error handling
│   └── infrastructure.instructions.md → EF Core, Repositories, Multi-tenancy, DI
└── agents/
    ├── backend-architect.agent.md   → Architectural review & drift detection
    ├── feature-builder.agent.md     → Scaffold complete features following the template
    ├── query-optimizer.agent.md     → EF Core / Dapper read optimisation
    └── code-reviewer.agent.md       → Deep review: security gaps, violations + fix proposals
```

### Specialized Agents (GitHub Copilot Chat)

Use these agents in Copilot Chat with `@` to get context-aware, project-specific assistance:

| Agent | Activate with | Purpose |
|-------|--------------|---------|
| Backend Architect | `@backend-architect` | Validate architecture, detect layer violations, review new modules |
| Feature Builder | `@feature-builder` | Scaffold a full feature (entity → handler → endpoint → tests) |
| Query Optimizer | `@query-optimizer` | Diagnose N+1, over-fetching, missing indexes, propose Dapper read services |
| Code Reviewer | `@code-reviewer` | Deep code review — security gaps, architecture violations, missing tests, with fix proposals |

### Reusable Prompts

```
prompts/
├── create-feature.prompt.md   → Full feature scaffold checklist + expected output format
├── review-feature.prompt.md   → Architectural review criteria by layer
├── optimize-query.prompt.md   → Query diagnosis + optimisation proposal template
└── code-review.prompt.md      → Deep review: security gaps, violations, missing tests + fix proposals
```

**Usage**: Open a prompt file, copy its content into Copilot Chat, and fill in the `{MODULE}`, `{ENTITY}` and other placeholders.

### Deep Rules (`.ai/`)

For more detailed, layer-by-layer rules used by the `.github/instructions/` files:

```
.ai/
├── rules/          → 13 rule files (00-global → 12-folder-structure)
├── checklists/     → new-feature, api-endpoint, persistence, pull-request
└── examples/       → Reference implementation guide (points to Identity module)
```

### Quick start for agents

When starting a new task in Copilot Chat:

1. **Copilot already has context** — `copilot-instructions.md` is loaded automatically.
2. **Use the right agent**:
   - `@feature-builder` — scaffold a new feature end-to-end
   - `@backend-architect` — validate architecture or review a new module
   - `@query-optimizer` — diagnose and fix slow queries
   - `@code-reviewer` — deep review with security gaps, violations, and fix proposals
3. **Use a prompt file** — copy from `prompts/` to get a structured, checklist-driven response.
4. **Reference is always Identity** — any pattern question can be answered by looking at `src/Core/Identity/`.

## Key Features

- **Clean Architecture** with strict layer enforcement (tested by ArchitectureTests)
- **CQRS** via MediatR with validation, logging, and performance behaviors
- **Multi-tenancy** — header/subdomain resolution, shared-DB or schema-per-tenant
- **RBAC** — role + permission-based authorization with JWT
- **Observability** — Serilog (Console · File · Seq) + OpenTelemetry (Traces → Tempo, Metrics → Prometheus) + Grafana dashboards + health checks
- **Health checks** — `/health/live` (liveness), `/health/ready` (readiness, DB-only), `/health` (full diagnostics, admin-only in prod)
- **Resilience** — Polly retry, circuit breaker, timeout, rate limiting
- **API documentation** — Scalar (modern OpenAPI UI)
- **Request deduplication** — idempotency key middleware
- **Correlation IDs** — propagated through logs, traces, and response headers
- **Feature flags** — Microsoft.FeatureManagement
- **Architecture tests** — automated enforcement of layer dependencies and naming

## Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/UnitTests
dotnet test tests/IntegrationTests
dotnet test tests/ArchitectureTests
```

## Adding a New Module

1. Create the project triple under `src/Core/{Module}/`
2. Use `@feature-builder` in Copilot Chat — or copy `prompts/create-feature.prompt.md`
3. Use `.ai/checklists/new-feature.md` to verify completeness
4. See `CONTRIBUTING.md` for full guidelines

## Documentation

| Document | Purpose |
|----------|---------|
| `README.md` | This file — stack, setup, Docker, observability, AI tooling |
| `CONTRIBUTING.md` | How to contribute and use AI tools |
| `compose.yaml` | Local development environment (API + SQL Server + Seq + Tempo + Prometheus + Grafana) |
| `src/Api/Dockerfile` | Multi-stage production build (Alpine + icu-libs, non-root, locked restore) |
| `.github/copilot-instructions.md` | Persistent Copilot rules (auto-loaded) |
| `.github/instructions/` | Layer-specific instructions for Copilot |
| `.github/agents/` | Specialized agents for Copilot Chat |
| `prompts/` | Reusable prompt templates |
| `.ai/rules/` | Detailed architectural rules by layer |
| `.ai/checklists/` | Verification checklists for features and PRs |
| `docs/security/RBAC_MATRIX.md` | Authorization matrix |

## License

See [LICENSE](LICENSE).

