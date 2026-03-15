# 13 — Docker Rules

> Regras para geração e manutenção de Dockerfiles de qualidade para este repositório .NET 10 / Clean Architecture.

---

## Princípios Fundamentais

1. **Multi-stage build obrigatório** — nunca copiar o SDK para a imagem final.
2. **Imagem base mínima** — sempre usar `mcr.microsoft.com/dotnet/aspnet:{version}-alpine` na stage `final` salvo necessidade justificada de `bookworm-slim`.
3. **Non-root user obrigatório** — o processo deve rodar como `app` (UID 1654), nunca como `root`.
4. **Imagem reproduzível** — `dotnet restore` usa `--locked-mode` (requer `packages.lock.json` commitado) para garantir build determinístico.
5. **Layer cache eficiente** — copiar apenas `.csproj` / `.props` antes do `restore`; copiar o código-fonte depois.
6. **Nenhum segredo na imagem** — tokens, connection strings e chaves nunca são `ARG`/`ENV` em build-time; passados em runtime via `docker secret` ou variável de ambiente do orquestrador.
7. **ENTRYPOINT + CMD** — usar `ENTRYPOINT ["dotnet", "Api.dll"]` + `CMD []`; nunca `CMD dotnet Api.dll` (shell form).
8. **Labels OCI** — adicionar labels de rastreabilidade (`org.opencontainers.image.*`).
9. **HEALTHCHECK nativa** — declarar `HEALTHCHECK` apontando para `/health/live`.
10. **`.dockerignore` obrigatório** — excluir `bin/`, `obj/`, `.git/`, `tests/`, `docs/` e arquivos de IDE.

---

## Estrutura Canônica do Dockerfile

```dockerfile
# ─────────────────────────────────────────────
# Stage 1 — base runtime (imagem mínima)
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

# Non-root user (Alpine usa addgroup/adduser)
RUN addgroup --system --gid 1654 appgroup \
 && adduser  --system --uid 1654 --ingroup appgroup --no-create-home app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1

# ─────────────────────────────────────────────
# Stage 2 — restore (cache de dependências)
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore
WORKDIR /src

# Copiar SOMENTE arquivos de projeto e props para maximizar cache
COPY Directory.Build.props ./
COPY global.json ./
COPY src/Api/Api.csproj                                       src/Api/
COPY src/Shared/Kernel.Domain/Kernel.Domain.csproj            src/Shared/Kernel.Domain/
COPY src/Shared/Kernel.Application/Kernel.Application.csproj  src/Shared/Kernel.Application/
COPY src/Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj src/Shared/Kernel.Infrastructure/
COPY src/Core/Identity/Identity.Domain/Identity.Domain.csproj            src/Core/Identity/Identity.Domain/
COPY src/Core/Identity/Identity.Application/Identity.Application.csproj  src/Core/Identity/Identity.Application/
COPY src/Core/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj src/Core/Identity/Identity.Infrastructure/
# → Adicione novos módulos seguindo o mesmo padrão

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
# Stage 4 — imagem final (apenas runtime)
# ─────────────────────────────────────────────
FROM base AS final

# Labels OCI de rastreabilidade
ARG VERSION=1.0.0
ARG VCS_REF=unknown
ARG BUILD_DATE=unknown
LABEL org.opencontainers.image.title="Product.Template API" \
      org.opencontainers.image.version="$VERSION" \
      org.opencontainers.image.revision="$VCS_REF" \
      org.opencontainers.image.created="$BUILD_DATE" \
      org.opencontainers.image.source="https://github.com/neuraptor/product-template" \
      org.opencontainers.image.vendor="Neuraptor"

WORKDIR /app
COPY --from=publish --chown=app:appgroup /app/publish .

USER app

ENTRYPOINT ["dotnet", "Api.dll"]
```

---

## `.dockerignore` Canônico

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
.ai/
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

---

## Regras Detalhadas

### Imagem base
| Critério | Regra |
|----------|-------|
| Runtime default | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` |
| Necessita ICU (globalization) | `mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim` com `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` |
| SDK (build only) | `mcr.microsoft.com/dotnet/sdk:10.0-alpine` |
| NUNCA em produção | `sdk:*` na imagem final |

> Sempre pin com minor version **explícita** (`10.0`, não `latest`). Em pipelines de produção, usar digest SHA256.

### Variáveis de Ambiente Obrigatórias
```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false   # se usar datas/locale
ENV DOTNET_GC_HEAP_HARD_LIMIT_PERCENT=75          # previne OOM em containers com limite de memória
```

### Porta
- Expor **apenas porta 8080** (HTTP). TLS é terminado no ingress/load balancer.
- `EXPOSE 443` apenas se houver mTLS direto entre serviços.

### Tamanho da imagem
- Meta: imagem final < **200 MB** (Alpine).
- Verificar com `docker image inspect <image> --format='{{.Size}}'` e reportar no CI.
- Remover ferramentas de diagnóstico desnecessárias do runtime.

### Segurança
- Rodar como non-root (`USER app`) — obrigatório.
- Não instalar `curl`, `wget` ou outros utilitários na imagem final (apenas para `HEALTHCHECK`, usar `wget` já presente no Alpine).
- Varrer com `trivy image` no CI — falhar se houver CVE `HIGH` ou `CRITICAL` sem exceção justificada.
- Nunca usar `--privileged` ou `cap_add` sem aprovação de segurança.

### Build Arguments (ARGs)
```dockerfile
ARG BUILD_CONFIGURATION=Release   # Release | Debug
ARG VERSION=1.0.0                 # Injetado pelo CI (ex: git tag)
ARG VCS_REF=unknown               # git SHA curto
ARG BUILD_DATE=unknown            # ISO 8601
```
Nunca usar `ARG` para segredos — usar `--mount=type=secret` (BuildKit).

### Secrets em Build-Time (BuildKit)
Se precisar de credenciais para `dotnet restore` (feed privado NuGet):
```dockerfile
RUN --mount=type=secret,id=nuget_token \
    dotnet restore "src/Api/Api.csproj" \
      --configfile NuGet.Config \
      /p:NuGetToken=$(cat /run/secrets/nuget_token) \
      --locked-mode
```

### Cache BuildKit (`--mount=type=cache`)
```dockerfile
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "src/Api/Api.csproj" --locked-mode
```
Reduz tempo de build em ~40% em builds locais repetidos.

### HEALTHCHECK
```dockerfile
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1
```
- `/health/live` → liveness (processo vivo)
- `/health/ready` → readiness (dependências prontas) — verificado pelo orquestrador, não pelo HEALTHCHECK nativo

---

## docker-compose (desenvolvimento local)

```yaml
# docker-compose.yml
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
      - X-Tenant=public
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

---

## O que o Copilot NÃO deve fazer

1. Usar `FROM mcr.microsoft.com/dotnet/sdk:*` na imagem final.
2. Usar `root` como usuário em runtime.
3. Copiar todo o source code antes do `restore` (quebra o cache).
4. Usar `CMD dotnet Api.dll` (shell form) — sempre usar exec form.
5. Omitir `HEALTHCHECK`.
6. Passar connection strings ou tokens como `ARG` ou `ENV` no Dockerfile.
7. Usar `latest` como tag de imagem base em produção.
8. Omitir `.dockerignore` ou deixá-lo incompleto.
9. Fazer `COPY . .` sem antes copiar e restaurar os `.csproj` separadamente.
10. Criar mais de uma porta EXPOSE sem justificativa.

