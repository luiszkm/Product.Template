---
name: docker-setup
version: 1
description: "Generate or update Dockerfiles, .dockerignore, and docker-compose files for this .NET 10 repository. TRIGGER: \"Dockerfile\", \"docker compose\", \"containerize\", \"HEALTHCHECK\", \"multi-stage build\", \"docker image\", \"write a Dockerfile\". SKIP: CI/CD pipeline workflows (use setup-cicd)."
tools: Read, Edit, Write, Glob
disable-model-invocation: true
---

# Skill: /docker-setup

> Generates and maintains Dockerfiles, `.dockerignore`, and docker-compose files for this .NET 10 / Clean Architecture repository.

## Arguments

`$ARGUMENTS` format: `{TARGET}` (optional)

Examples:
- `/docker-setup dockerfile`
- `/docker-setup compose`
- `/docker-setup dockerignore`
- `/docker-setup` (generates all)

## Context — read before generating

- `.cursor/rules/docker.mdc`
- `src/Api/Dockerfile` — existing Dockerfile (if any)
- `compose.yaml` — existing compose (if any)
- `.dockerignore` — existing ignore rules

## Core Principles

1. **Multi-stage build required** — never copy the SDK into the final image.
2. **Minimal base image** — always use `mcr.microsoft.com/dotnet/aspnet:{version}-alpine` in the `final` stage unless `bookworm-slim` is justified.
3. **Non-root user required** — process must run as `app` (UID 1654), never as `root`.
4. **Reproducible image** — `dotnet restore` uses `--locked-mode` (requires committed `packages.lock.json`).
5. **Efficient layer cache** — copy only `.csproj` / `.props` before `restore`; copy source code after.
6. **No secrets in image** — tokens and keys are never `ARG`/`ENV` at build-time; passed at runtime.
7. **ENTRYPOINT + CMD** — use exec form: `ENTRYPOINT ["dotnet", "Api.dll"]`.
8. **OCI Labels** — add `org.opencontainers.image.*` traceability labels.
9. **Native HEALTHCHECK** — declare `HEALTHCHECK` pointing to `/health/live`.
10. **`.dockerignore` required** — exclude `bin/`, `obj/`, `.git/`, `tests/`, `docs/`.

## Canonical Dockerfile (4-stage)

```dockerfile
# ─────────────────────────────────────────────
# Stage 1 — base runtime (minimal image)
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

# Non-root user (Alpine uses addgroup/adduser)
RUN addgroup --system --gid 1654 appgroup \
 && adduser  --system --uid 1654 --ingroup appgroup --no-create-home app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_GC_HEAP_HARD_LIMIT_PERCENT=75

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1

# ─────────────────────────────────────────────
# Stage 2 — restore (dependency cache)
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore
WORKDIR /src

# Copy ONLY project and props files to maximize cache
COPY Directory.Build.props ./
COPY global.json ./
COPY src/Api/Api.csproj                                                         src/Api/
COPY src/Shared/Kernel.Domain/Kernel.Domain.csproj                              src/Shared/Kernel.Domain/
COPY src/Shared/Kernel.Application/Kernel.Application.csproj                    src/Shared/Kernel.Application/
COPY src/Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj              src/Shared/Kernel.Infrastructure/
COPY src/Core/Identity/Identity.Domain/Identity.Domain.csproj                   src/Core/Identity/Identity.Domain/
COPY src/Core/Identity/Identity.Application/Identity.Application.csproj         src/Core/Identity/Identity.Application/
COPY src/Core/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj   src/Core/Identity/Identity.Infrastructure/
# → Add new modules following the same pattern

RUN dotnet restore "src/Api/Api.csproj" --locked-mode

# ─────────────────────────────────────────────
# Stage 3 — build & publish
# ─────────────────────────────────────────────
FROM restore AS publish
ARG BUILD_CONFIGURATION=Release
ARG VERSION=1.0.0

COPY . .

RUN dotnet publish "src/Api/Api.csproj" \
      --no-restore \
      -c $BUILD_CONFIGURATION \
      -p:Version=$VERSION \
      -p:UseAppHost=false \
      -o /app/publish

# ─────────────────────────────────────────────
# Stage 4 — final image (runtime only)
# ─────────────────────────────────────────────
FROM base AS final

ARG VERSION=1.0.0
ARG VCS_REF=unknown
ARG BUILD_DATE=unknown
LABEL org.opencontainers.image.title="Product.Template API" \
      org.opencontainers.image.version="$VERSION" \
      org.opencontainers.image.revision="$VCS_REF" \
      org.opencontainers.image.created="$BUILD_DATE" \
      org.opencontainers.image.source="https://github.com/org/repo" \
      org.opencontainers.image.vendor="YourOrg"

WORKDIR /app
COPY --from=publish --chown=app:appgroup /app/publish .

USER app

ENTRYPOINT ["dotnet", "Api.dll"]
```

## Canonical `.dockerignore`

```
# Build artifacts
**/bin/
**/obj/

# Source control
.git/
.gitignore

# IDE / editor
.vs/
.vscode/
.idea/
*.user
*.suo

# Tests
tests/

# Docs / tools
docs/
prompts/
.agents/
.github/
setup.ps1
setup.sh
*.md
*.http
*.txt

# Secrets & local config
*.pfx
*.p12
appsettings.Development.json
appsettings.Local.json
.env
.env.*
```

## Base image selection

| Criteria | Image |
|----------|-------|
| Default runtime | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` |
| Needs ICU/globalization | `mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim` |
| SDK (build stages only) | `mcr.microsoft.com/dotnet/sdk:10.0-alpine` |
| NEVER in final stage | `sdk:*` |

Always pin to explicit minor version (`10.0`, not `latest`). In production pipelines, use SHA256 digest.

## Required ENV vars

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_GC_HEAP_HARD_LIMIT_PERCENT=75
```

## Build ARGs

```dockerfile
ARG BUILD_CONFIGURATION=Release   # Release | Debug
ARG VERSION=1.0.0                 # Injected by CI (git tag)
ARG VCS_REF=unknown               # Short git SHA
ARG BUILD_DATE=unknown            # ISO 8601
```

Never use `ARG` for secrets — use `--mount=type=secret` (BuildKit).

## HEALTHCHECK

```dockerfile
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1
```

- `/health/live` → liveness (process alive)
- `/health/ready` → readiness (dependencies ready) — checked by orchestrator separately

## BuildKit cache (optional, speeds local builds ~40%)

```dockerfile
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "src/Api/Api.csproj" --locked-mode
```

## docker-compose (local development)

```yaml
services:
  api:
    build:
      context: .
      dockerfile: src/Api/Dockerfile
      target: final
      args:
        BUILD_CONFIGURATION: Release
        VERSION: "1.0.0-local"
    image: product-template-api:local
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Default=Host=db;Database=product_template;Username=postgres;Password=postgres
    depends_on:
      db:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "wget", "-qO-", "http://localhost:8080/health/live"]
      interval: 30s
      timeout: 5s
      retries: 3

  db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: product_template
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
```

## Image size target

- Final image < **200 MB** (Alpine).
- Verify: `docker image inspect <image> --format='{{.Size}}'`

## Never do

1. Use `FROM mcr.microsoft.com/dotnet/sdk:*` in the final image.
2. Run as `root` at runtime.
3. Copy all source code before `restore` (breaks layer cache).
4. Use `CMD dotnet Api.dll` (shell form) — always exec form.
5. Omit `HEALTHCHECK`.
6. Pass connection strings or tokens as `ARG` or `ENV`.
7. Use `latest` as base image tag in production.
8. Omit `.dockerignore` or leave it incomplete.
9. Do `COPY . .` without first restoring `.csproj` files separately.
10. Expose multiple ports without justification.
