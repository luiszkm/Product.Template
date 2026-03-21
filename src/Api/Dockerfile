# =============================================================================
# Product.Template API — Production Dockerfile
# Follows .ai/rules/13-docker.md
#
# Stages:
#   base    → minimal runtime image (Alpine, non-root user)
#   restore → NuGet restore with locked-mode (layer cache)
#   publish → dotnet publish
#   final   → production image (runtime only, no SDK)
# =============================================================================

# ─────────────────────────────────────────────────────────────────────────────
# Stage 1 — base: runtime image + non-root user + env
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

# NOTE: mcr.microsoft.com/dotnet/aspnet:10.0-alpine already ships with
# user/group app:app (UID/GID 1654) — no need to create them.
# ICU libs are required because DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false.
RUN apk add --no-cache icu-libs

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_GC_HEAP_HARD_LIMIT_PERCENT=75

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1

# ─────────────────────────────────────────────────────────────────────────────
# Stage 2 — restore: copy .csproj files only → maximise layer cache
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore
WORKDIR /src

COPY Directory.Build.props ./
COPY global.json           ./

# Shared kernel
COPY src/Shared/Kernel.Domain/Kernel.Domain.csproj                         src/Shared/Kernel.Domain/
COPY src/Shared/Kernel.Domain/packages.lock.json                           src/Shared/Kernel.Domain/
COPY src/Shared/Kernel.Application/Kernel.Application.csproj               src/Shared/Kernel.Application/
COPY src/Shared/Kernel.Application/packages.lock.json                      src/Shared/Kernel.Application/
COPY src/Shared/Kernel.Infrastructure/Kernel.Infrastructure.csproj         src/Shared/Kernel.Infrastructure/
COPY src/Shared/Kernel.Infrastructure/packages.lock.json                   src/Shared/Kernel.Infrastructure/

# Identity module
COPY src/Core/Identity/Identity.Domain/Identity.Domain.csproj              src/Core/Identity/Identity.Domain/
COPY src/Core/Identity/Identity.Domain/packages.lock.json                  src/Core/Identity/Identity.Domain/
COPY src/Core/Identity/Identity.Application/Identity.Application.csproj    src/Core/Identity/Identity.Application/
COPY src/Core/Identity/Identity.Application/packages.lock.json             src/Core/Identity/Identity.Application/
COPY src/Core/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj src/Core/Identity/Identity.Infrastructure/
COPY src/Core/Identity/Identity.Infrastructure/packages.lock.json          src/Core/Identity/Identity.Infrastructure/

# Authorization module
COPY src/Core/Authorization/Authorization.Domain/Authorization.Domain.csproj           src/Core/Authorization/Authorization.Domain/
COPY src/Core/Authorization/Authorization.Domain/packages.lock.json                    src/Core/Authorization/Authorization.Domain/
COPY src/Core/Authorization/Authorization.Application/Authorization.Application.csproj src/Core/Authorization/Authorization.Application/
COPY src/Core/Authorization/Authorization.Application/packages.lock.json               src/Core/Authorization/Authorization.Application/
COPY src/Core/Authorization/Authorization.Infrastructure/Authorization.Infrastructure.csproj src/Core/Authorization/Authorization.Infrastructure/
COPY src/Core/Authorization/Authorization.Infrastructure/packages.lock.json            src/Core/Authorization/Authorization.Infrastructure/

# Tenants module
COPY src/Core/Tenants/Tenants.Domain/Tenants.Domain.csproj                 src/Core/Tenants/Tenants.Domain/
COPY src/Core/Tenants/Tenants.Domain/packages.lock.json                    src/Core/Tenants/Tenants.Domain/
COPY src/Core/Tenants/Tenants.Application/Tenants.Application.csproj       src/Core/Tenants/Tenants.Application/
COPY src/Core/Tenants/Tenants.Application/packages.lock.json               src/Core/Tenants/Tenants.Application/
COPY src/Core/Tenants/Tenants.Infrastructure/Tenants.Infrastructure.csproj src/Core/Tenants/Tenants.Infrastructure/
COPY src/Core/Tenants/Tenants.Infrastructure/packages.lock.json            src/Core/Tenants/Tenants.Infrastructure/

# API entry-point
COPY src/Api/Api.csproj        src/Api/
COPY src/Api/packages.lock.json src/Api/

RUN dotnet restore "src/Api/Api.csproj" --locked-mode

# ─────────────────────────────────────────────────────────────────────────────
# Stage 3 — publish
# ─────────────────────────────────────────────────────────────────────────────
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

# ─────────────────────────────────────────────────────────────────────────────
# Stage 4 — final: production image (runtime only)
# ─────────────────────────────────────────────────────────────────────────────
FROM base AS final

ARG VERSION=1.0.0
ARG VCS_REF=unknown
ARG BUILD_DATE=unknown

# OCI image labels for traceability
LABEL org.opencontainers.image.title="Product.Template API" \
      org.opencontainers.image.version="$VERSION" \
      org.opencontainers.image.revision="$VCS_REF" \
      org.opencontainers.image.created="$BUILD_DATE" \
      org.opencontainers.image.source="https://github.com/neuraptor/product-template" \
      org.opencontainers.image.vendor="Neuraptor" \
      org.opencontainers.image.licenses="MIT"

WORKDIR /app
COPY --from=publish --chown=app:app /app/publish .

USER app

ENTRYPOINT ["dotnet", "Api.dll"]
