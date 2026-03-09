# Product.Template

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

This template is designed to work seamlessly with LLMs and coding agents.

### For Agents: Before You Start

Read these files in order:
1. `.ai/rules/00-global.md` — Stack, principles, universal rules
2. `.ai/rules/10-agent-behavior.md` — How to behave as an agent in this repo
3. `.ai/rules/12-folder-structure.md` — Where to place files
4. `.ai/rules/11-naming.md` — Naming conventions

### For Agents: Creating a New Feature

1. Read `.ai/prompts/create-feature.md`
2. Follow `.ai/checklists/new-feature.md`
3. Use the Identity module as reference (`src/Core/Identity/`)

### `.ai/` Folder Structure

```
.ai/
├── rules/          → 13 rule files covering every architectural concern
├── prompts/        → 9 reusable prompts for common tasks
├── checklists/     → 4 verification checklists
└── examples/       → Reference implementation guide
```

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
2. Follow `.ai/prompts/create-feature.md`
3. Use `.ai/checklists/new-feature.md` for validation
4. See `CONTRIBUTING.md` for full guidelines

## Documentation

| Document | Purpose |
|----------|---------|
| `README.md` | This file |
| `CONTRIBUTING.md` | How to contribute |
| `.ai/rules/` | Architectural rules for humans and agents |
| `.ai/prompts/` | Reusable prompts for AI-assisted development |
| `.ai/checklists/` | Verification checklists |
| `docs/security/RBAC_MATRIX.md` | Authorization matrix |

## License

See [LICENSE](LICENSE).

