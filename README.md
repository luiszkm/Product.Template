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
| Serilog | 10.x | Structured logging |
| OpenTelemetry | 1.14+ | Traces & metrics |
| JWT Bearer | — | Authentication |
| Scalar | — | API documentation (OpenAPI) |
| xUnit + Bogus | — | Testing |
| Docker | Linux | Containerization |

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
- **Observability** — Serilog + OpenTelemetry + correlation IDs + health checks
- **Resilience** — Polly retry, circuit breaker, timeout, rate limiting
- **API documentation** — Scalar (modern OpenAPI UI)
- **Request deduplication** — idempotency key middleware
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
| `README.md` | This file |
| `CONTRIBUTING.md` | How to contribute and use AI tools |
| `.github/copilot-instructions.md` | Persistent Copilot rules (auto-loaded) |
| `.github/instructions/` | Layer-specific instructions for Copilot |
| `.github/agents/` | Specialized agents for Copilot Chat |
| `prompts/` | Reusable prompt templates |
| `.ai/rules/` | Detailed architectural rules by layer |
| `.ai/checklists/` | Verification checklists for features and PRs |
| `docs/security/RBAC_MATRIX.md` | Authorization matrix |

## License

See [LICENSE](LICENSE).

