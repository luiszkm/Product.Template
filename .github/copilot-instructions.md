# Copilot Instructions — Product.Template

> Instruções persistentes para o GitHub Copilot. Lidas automaticamente em todo contexto deste repositório.

## O que é este repositório

Template backend .NET 10 seguindo Clean Architecture, DDD tático, CQRS com MediatR, multi-tenancy, RBAC granular com JWT e observabilidade via Serilog + OpenTelemetry.

## Stack real

| Camada | Tecnologia |
|--------|-----------|
| Runtime | .NET 10 / ASP.NET Core 10 / C# latest |
| CQRS | MediatR 14.x (`ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`) |
| Validação | FluentValidation 12.x (`AbstractValidator<TCommand>`) |
| ORM (escrita) | EF Core 10.x (`AppDbContext`, Interceptors, Configurations) |
| Logging | Serilog (Console + File + Seq) |
| Telemetria | OpenTelemetry (OTLP traces + metrics) |
| Auth | JWT Bearer + RBAC (roles + permission claims) |
| Multi-tenancy | Header `X-Tenant` / subdomain → `ITenantContext` |
| API Docs | Scalar (OpenAPI) |
| Testes | xUnit + Bogus + WebApplicationFactory + NetArchTest |

## Arquitetura — Regra de dependência

```
Domain ← Application ← Infrastructure ← Api
```

| Projeto | Pode referenciar |
|---------|-----------------|
| `Kernel.Domain` | Nada |
| `{Module}.Domain` | `Kernel.Domain` |
| `Kernel.Application` | `Kernel.Domain` |
| `{Module}.Application` | `{Module}.Domain`, `Kernel.Application` |
| `Kernel.Infrastructure` | `Kernel.Application`, `{Module}.Domain` (para EF configs) |
| `{Module}.Infrastructure` | `{Module}.Application`, `Kernel.Infrastructure` |
| `Api` | Todos os acima |

**PROIBIDO**: Domain referenciar Application/Infrastructure/Api. Application referenciar Infrastructure/Api.

## Estrutura de módulos

Cada bounded context vive em `src/Core/{Module}/` com 3 projetos:

```
src/Core/{Module}/
├── {Module}.Domain/           → Entities, VOs, Events, Repository interfaces
├── {Module}.Application/      → Commands, Queries, Handlers, Validators, Mappers, DTOs
└── {Module}.Infrastructure/   → Repositories EF, Seeders, DependencyInjection.cs
```

O módulo **Identity** (`src/Core/Identity/`) é a implementação de referência canônica.

## Convenções que o Copilot DEVE seguir

### Tipos e nomes
- **Command**: `{Verbo}{Substantivo}Command` → `RegisterUserCommand`, `DeleteRoleCommand`
- **Query**: `{Get|List}{Substantivo}Query` → `GetUserByIdQuery`, `ListRolesQuery`
- **Handler de command**: `{Verbo}{Substantivo}CommandHandler` → `RegisterUserCommandHandler`
- **Handler de query**: `{Get|List}{Substantivo}QueryHandler` → `GetUserByIdQueryHandler`
- **Validator**: `{CommandName}Validator` → `RegisterUserCommandValidator`
- **Output DTO**: `{Substantivo}Output` → `UserOutput`, `RoleOutput` (sempre `record`)
- **Mapper**: `{Substantivo}Mapper` → `UserMapper` (extension methods: `entity.ToOutput()`)
- **Repository interface**: `I{AggregateRoot}Repository` → `IUserRepository`
- **Repository impl**: `{AggregateRoot}Repository` → `UserRepository`
- **Controller**: `{Module}Controller` → `IdentityController`
- **EF Config**: `{Entity}Configurations` → `UserConfigurations`

### Commands e Queries
- Commands e queries são `record` implementando `ICommand<TOutput>`, `ICommand`, `IQuery<TOutput>`.
- Commands chamam `IUnitOfWork.Commit(cancellationToken)` após mutação.
- Queries NUNCA chamam `Commit()`.
- Handlers retornam DTOs `record` — NUNCA entidades de domínio.
- Todo command DEVE ter um validator FluentValidation correspondente.

### Controllers
- Thin dispatchers para MediatR — ZERO lógica de negócio.
- `[ApiController]`, `[ApiVersion("1.0")]`, `[Route("api/v{version:apiVersion}/[controller]")]`
- Todo endpoint protegido usa `[Authorize(Policy = SecurityConfiguration.{PolicyName})]` — NUNCA `[Authorize]` sem Policy.
- `CancellationToken cancellationToken` como último parâmetro de toda action async.
- `[ProducesResponseType]` para cada status code possível.
- XML doc comments (`///`) com exemplos em cada action pública.

### Entidades de domínio
- Herdam de `Entity` ou `AggregateRoot` (de `Kernel.Domain.SeedWorks`).
- Implementam `IMultiTenantEntity` (`TenantId long`).
- Construtor privado + factory `static Create(...)` com validação de invariantes.
- Properties com `private set`.
- Mudança de estado via métodos explícitos (ex: `Deactivate()`, `UpdateLastLogin()`).
- Domain events disparados via `AddDomainEvent(...)` dentro do aggregate.

### Persistência
- `AppDbContext` único em `Kernel.Infrastructure/Persistence/`.
- Configurações EF: `{Entity}Configurations : IEntityTypeConfiguration<{Entity}>` em `Kernel.Infrastructure/Persistence/Configurations/`.
- Repositories em `{Module}.Infrastructure/Data/Persistence/`.
- `Include`/`ThenInclude` para eager loading de filhos do aggregate.
- Paginação: queries herdam `ListInput` → retornam `PaginatedListOutput<T>`.
- DI: `{Module}.Infrastructure/DependencyInjection.cs` → wired em `Api/Configurations/CoreConfiguration.cs`.

### Async e CancellationToken
- Todo método async aceita `CancellationToken` como último parâmetro.
- Sempre `await` — nunca `.Result` ou `.Wait()`.
- Sempre propagar o token para chamadas downstream.

### Tratamento de erros
- `NotFoundException` → 404 Not Found
- `BusinessRuleException` → 400 Bad Request
- `DomainException` → 422 Unprocessable Entity
- `ValidationException` (FluentValidation) → 400 Bad Request
- `UnauthorizedAccessException` → 401 Unauthorized
- Mapeamento via `ApiGlobalExceptionFilter` → `ProblemDetails` (RFC 9457).

### Segurança (RBAC)
- Policies definidas em `SecurityConfiguration.cs` como `public const string`.
- Policies ativas: `Authenticated`, `AdminOnly`, `UserOnly`, `UsersRead`, `UsersManage`.
- Permission claims: tipo `"permission"` com valores como `"users.read"`, `"users.manage"`.
- Todo endpoint protegido novo DEVE ser adicionado a `docs/security/RBAC_MATRIX.md`.

### Logging
- Serilog structured logging: `_logger.LogInformation("User {UserId} created", user.Id)`.
- NUNCA string interpolation em templates de log.
- `RequestLoggingMiddleware` adiciona `CorrelationId` e mascara dados sensíveis.

### Testes
- Naming: `{Método}_{Cenário}_{Resultado}` → `Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist`.
- Fakes/stubs inline (sealed classes no fim do test file) — sem framework de mock.
- `NullLogger<T>.Instance` para loggers.
- Integration tests: `WebApplicationFactory<Program>` + `TestAuthHandler` com headers `X-Test-Roles`, `X-Test-Permissions`.
- Sempre enviar header `X-Tenant: public` nos integration tests.

## O que o Copilot NÃO deve fazer

1. Colocar lógica de negócio em controllers.
2. Retornar entidades de domínio em responses de API.
3. Criar `[Authorize]` sem `Policy` explícita.
4. Esquecer `CancellationToken` em métodos async.
5. Criar handler que chama outro handler.
6. Retornar `IQueryable<T>` de repositories.
7. Criar arquivos fora da estrutura de pastas estabelecida.
8. Criar repository para entidade filha (apenas aggregate roots).
9. Adicionar endpoint protegido sem atualizar `RBAC_MATRIX.md`.
10. Esquecer de registrar serviço em `DependencyInjection.cs`.
11. Usar mocking frameworks — usar fakes/stubs inline.
12. Usar string interpolation em log templates do Serilog.

## Docker e CI/CD

### Dockerfile
- Multi-stage build obrigatório: `restore` → `publish` → `final` (apenas runtime Alpine).
- Imagem final roda como non-root (`app`, UID 1654) — nunca como `root`.
- `HEALTHCHECK` apontando para `/health/live` obrigatório.
- `ARG`s para `VERSION`, `VCS_REF`, `BUILD_DATE` + labels OCI.
- Regras completas: `.ai/rules/13-docker.md`

### CI/CD (GitHub Actions / Azure DevOps)
- Todo workflow declarado em `.github/workflows/` ou `azure-pipelines/`.
- `permissions:` mínimas explícitas em todo workflow — nunca `write-all`.
- `timeout-minutes:` obrigatório em todos os jobs.
- Cache de NuGet usando hash de `packages.lock.json`.
- Scan Trivy (HIGH/CRITICAL) antes de push de imagem Docker.
- Deploy em produção requer `environment:` com aprovação manual.
- `dotnet restore --locked-mode` — nunca omitir.
- Imagem de produção nunca usa tag `latest` — sempre semver ou SHA digest.
- Regras completas: `.ai/rules/14-cicd.md`

## Referências adicionais

- Regras detalhadas por camada: `.ai/rules/`
- Prompts reutilizáveis: `prompts/`
- Agents especializados: `.github/agents/`
  - `backend-architect` — consistência arquitetural (Clean Architecture, DDD, CQRS)
  - `code-reviewer` — revisão de código, segurança e contratos
  - `feature-builder` — criação de features completas
  - `query-optimizer` — otimização de queries EF Core / Dapper
  - `deploy-observer` — Docker, CI/CD e observabilidade ← **novo**
- Instruções por camada: `.github/instructions/`
- RBAC Matrix: `docs/security/RBAC_MATRIX.md`
- Implementação de referência: `src/Core/Identity/`
