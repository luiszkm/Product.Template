# Deploy & Observability Instructions — Product.Template

> Regras de deploy, build Docker e observabilidade derivadas deste repositório.
> Referência canônica de regras: `.ai/rules/13-docker.md`, `.ai/rules/14-cicd.md`, `.ai/rules/09-observability.md`.

---

## Docker — Onde ficam os arquivos

```
Product.Template/
├── src/Api/Dockerfile          → Dockerfile de produção (multi-stage)
├── .dockerignore               → Exclusões do contexto de build
└── docker-compose.yml          → Ambiente local de desenvolvimento (opcional)
```

---

## Dockerfile — 4 stages obrigatórios

```
Stage 1: base    → mcr.microsoft.com/dotnet/aspnet:10.0-alpine
                   non-root user (app, UID 1654)
                   ENV, EXPOSE 8080, HEALTHCHECK

Stage 2: restore → mcr.microsoft.com/dotnet/sdk:10.0-alpine
                   COPY .csproj files ONLY
                   dotnet restore --locked-mode

Stage 3: publish → FROM restore
                   COPY . .
                   dotnet publish --no-restore

Stage 4: final   → FROM base
                   ARG VERSION, VCS_REF, BUILD_DATE
                   LABEL OCI
                   COPY --from=publish --chown=app:appgroup
                   USER app
                   ENTRYPOINT ["dotnet", "Api.dll"]
```

> **Regra de ouro**: a imagem final NUNCA deve conter o SDK, source code, `bin/`, `obj/` ou secrets.

---

## CI/CD — Onde ficam os workflows

```
.github/workflows/
├── ci.yml                → build + test + format + security-audit (PR e push)
├── build-push.yml        → Docker build + Trivy scan + push + cosign sign
├── deploy-staging.yml    → EF migrations + deploy (automático em develop)
├── deploy-production.yml → EF migrations + deploy (aprovação manual em main)
└── security-scan.yml     → scan diário agendado (Trivy + NuGet audit)

azure-pipelines/           → (alternativo ao GitHub Actions)
├── ci.yml
├── build-push.yml
├── deploy-staging.yml
└── deploy-prod.yml
```

---

## CI/CD — Ordem de execução no CI

```
PR aberto / push
    │
    ├── ci.yml (paralelo)
    │   ├── build-and-test  ──► upload test results + coverage
    │   ├── format-check
    │   └── security-audit  ──► fail se CVE encontrado
    │
    └── (em merge para main/develop)
        │
        └── build-push.yml
            ├── docker build (sem push) ──► Trivy scan ──► fail se HIGH/CRITICAL
            ├── docker build + push ──► SBOM + provenance
            └── cosign sign (apenas main)
                │
                └── deploy-staging.yml (automático em develop)
                    ├── migrate (EF)
                    └── deploy
                        │
                        └── deploy-production.yml (aprovação manual em main)
                            ├── migrate (EF)
                            └── deploy
```

---

## Observabilidade — Onde ficam os arquivos

```
src/Api/
├── Configurations/
│   ├── SerilogConfiguration.cs        → Serilog sinks + enrichers
│   └── OpenTelemetryConfiguration.cs  → Tracing + Metrics (OTLP)
├── HealthChecks/
│   ├── DatabaseHealthCheck.cs         → Verifica conectividade EF
│   └── {Dependency}HealthCheck.cs     → Um por dependência externa
├── Middleware/
│   └── RequestLoggingMiddleware.cs    → CorrelationId + masking
└── Metrics/
    └── {Module}Metrics.cs             → Custom counters/gauges por módulo
```

---

## Observabilidade — Convenções de nome

| Artefato | Padrão | Exemplo |
|----------|--------|---------|
| Custom Meter | `Product.Template.{Module}` | `Product.Template.Identity` |
| Counter | `{module}_{operation}_total` | `identity_user_registrations_total` |
| Health Check | `{Dependency}HealthCheck` | `RedisHealthCheck` |
| CorrelationId header | `X-Correlation-ID` | — |

---

## Variáveis de ambiente obrigatórias no container

| Variável | Valor padrão | Descrição |
|----------|-------------|-----------|
| `ASPNETCORE_URLS` | `http://+:8080` | Porta de escuta |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Ambiente ativo |
| `DOTNET_RUNNING_IN_CONTAINER` | `true` | Habilita otimizações de container |
| `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | `false` | Suporte a locale/datas |
| `DOTNET_GC_HEAP_HARD_LIMIT_PERCENT` | `75` | Evita OOM com memory limits |

---

## Secrets — Onde configurar (nunca no Dockerfile/YAML)

| Ambiente | Mecanismo |
|----------|-----------|
| Local (dev) | `dotnet user-secrets` ou `appsettings.Development.json` (gitignored) |
| CI/CD | GitHub Secrets (`${{ secrets.NAME }}`) ou Azure Key Vault |
| Staging / Production | Variáveis de ambiente do orquestrador (K8s secret, ACA secret) |

---

## Health Checks — Endpoints

| Endpoint | Propósito | Usado por |
|----------|-----------|-----------|
| `/health/live` | Liveness — processo vivo | Docker HEALTHCHECK, K8s livenessProbe |
| `/health/ready` | Readiness — dependências prontas | K8s readinessProbe, load balancer |
| `/healthchecks-ui` | Dashboard visual | Monitoramento interno |

---

## O que o agente deploy-observer valida

Ao usar o agente `deploy-observer`, ele auditará automaticamente:

1. **Dockerfile** — todos os 4 stages, non-root, locked-mode, HEALTHCHECK, OCI labels
2. **`.dockerignore`** — exclusões de `bin/`, `obj/`, `tests/`, secrets
3. **Workflows CI** — permissions, timeouts, NuGet cache, format check, Trivy
4. **Workflows Deploy** — migrations antes do deploy, environment com aprovação, digest em produção
5. **Logging** — structured templates, sem interpolation, sem dados sensíveis
6. **Health Checks** — cobertura de todas as dependências externas
7. **Métricas** — convenções de nome, Meter por módulo

---

## Restrições absolutas

- Nunca usar `FROM mcr.microsoft.com/dotnet/sdk:*` na imagem final.
- Nunca rodar como `root` em produção.
- Nunca usar `latest` como tag de imagem em production.
- Nunca hardcodar secrets no Dockerfile, docker-compose ou YAML de workflow.
- Nunca omitir `dotnet restore --locked-mode`.
- Nunca fazer build Docker na stage de deploy.
- Nunca usar `permissions: write-all` em workflows.
- Nunca logar com `$"User {user.Id} ..."` — sempre template estruturado.

