# 00 — Global Rules

> Canonical reference for every agent, LLM, or human working in this repository.

## Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 10 |
| Web Framework | ASP.NET Core | 10 |
| Language | C# | latest (LangVersion) |
| ORM (write) | Entity Framework Core | 10.x |
| ORM (read) | Dapper | latest (when optimised reads needed) |
| CQRS / Mediator | MediatR | 14.x |
| Validation | FluentValidation | 12.x |
| Logging | Serilog | 10.x |
| Observability | OpenTelemetry | 1.14+ |
| Auth | JWT Bearer + RBAC + External OAuth2 | — |
| Multi-tenancy | Custom (header/subdomain + schema-per-tenant) | — |
| Tests | xUnit + Bogus + WebApplicationFactory | — |
| API Docs | Scalar (OpenAPI) | — |
| Container | Docker (Linux / Alpine) | — |
| CI/CD | GitHub Actions / Azure DevOps | — |
| Image Registry | GHCR / ACR | — |
| Security Scan | Trivy | latest |
| Image Signing | Sigstore cosign | latest |

## Principles

1. **Clean Architecture** — dependencies point inward: Domain ← Application ← Infrastructure ← Api.
2. **DDD Tactical Patterns** — Entity, AggregateRoot, ValueObject, DomainEvent, Repository interface in Domain.
3. **CQRS** — Commands mutate state; Queries read state. Never mix.
4. **Explicit over implicit** — no magic strings, no service-locator, no ambient context outside `ITenantContext`.
5. **Fail fast** — validate at the boundary (FluentValidation) and at the domain (invariants).
6. **Immutable outputs** — all DTOs and query results are `record` types.
7. **CancellationToken everywhere** — every async method accepts and forwards `CancellationToken`.
8. **No business logic in controllers** — controllers are thin dispatchers to MediatR.
9. **ProblemDetails for all errors** — `ApiGlobalExceptionFilter` maps exceptions to RFC 9457.
10. **Multi-tenant by default** — every entity implements `IMultiTenantEntity`; every query is tenant-scoped.

## Universal Rules for Agents

- **Read `.ai/rules/` before generating code.** Start with this file, then the rule file for the layer you are modifying.
- **Never add a NuGet package** without checking `Directory.Build.props` and existing `.csproj` files first.
- **Never create a file outside the established folder structure** (see `12-folder-structure.md`).
- **Always provide the full file path** relative to the repository root when creating or modifying files.
- **Always run `dotnet build` mentally** — ensure `using` directives, namespaces, and project references are correct.
- **Preserve the existing code style** defined in `.editorconfig`.
- **Use English for code, types, and file names.** Comments and user-facing docs may be bilingual (PT-BR / EN).
- **Every change must be testable.** If you create a handler, create its test. If you create an endpoint, update `RBAC_MATRIX.md`.
- **Docker**: follow `13-docker.md` — multi-stage build, non-root user, HEALTHCHECK, no secrets in image.
- **CI/CD**: follow `14-cicd.md` — explicit permissions, timeouts, NuGet cache, Trivy scan before push, locked-mode restore.

