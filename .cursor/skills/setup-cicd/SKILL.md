---
name: setup-cicd
version: 1
description: "Generate or update CI/CD pipeline workflows for this .NET 10 repository. TRIGGER: \"add workflow\", \"CI/CD\", \"GitHub Actions\", \"Azure Pipelines\", \"Trivy gate\", \"setup pipeline\", \"add deploy workflow\", \"build and push\". SKIP: application code changes, Docker image authoring (use docker-setup)."
tools: Read, Edit, Write, Glob
disable-model-invocation: true
---

# Skill: /setup-cicd

> Generates and maintains CI/CD pipeline workflows for this .NET 10 / Clean Architecture repository.
> Coverage: **GitHub Actions** (default) and **Azure DevOps Pipelines** (alternative).

## Arguments

`$ARGUMENTS` format: `{PLATFORM} {WORKFLOW_TYPE}`

Examples:
- `/setup-cicd github ci`
- `/setup-cicd github build-push`
- `/setup-cicd azdo ci`
- `/setup-cicd github all`

Platforms: `github` | `azdo`
Workflow types: `ci` | `build-push` | `deploy` | `security-scan` | `all`

## Context — read before generating

- `.cursor/rules/global-security.mdc` — secrets and credential rules
- `.github/workflows/` — existing workflows (check before creating new ones)

## Core Principles

1. **Pipeline as code** — all workflows live in `.github/workflows/` (GHA) or `azure-pipelines/` (AzDO); never configure via UI.
2. **Fail fast, fail loud** — build, lint, and fast tests run first; Docker and deploy run after quality gates pass.
3. **No secrets in code** — use GitHub Secrets / Azure Key Vault + Variable Groups; never hardcode.
4. **Idempotence** — deploy can run multiple times without unwanted side effects.
5. **Protected environments** — `staging` and `production` require manual approval before deploy.
6. **Immutable image** — the same image is promoted across all environments; never rebuild on deploy.
7. **Semantic versioning** — image tag = `v{MAJOR}.{MINOR}.{PATCH}[-{sha}]`; never use `latest` alone in production.
8. **Pipeline observability** — generate coverage reports, SBOM, and vulnerability scan reports as artifacts.
9. **.NET matrix** — do not test against multiple runtimes; target is exclusively `.NET 10`.
10. **Mandatory cache** — cache NuGet packages and Docker layers to reduce execution time.

## Workflow Structure

```
.github/
└── workflows/
    ├── ci.yml                → PR validation (build + test + lint + format)
    ├── build-push.yml        → Docker build + push to registry (push on main/develop)
    ├── deploy-staging.yml    → Automatic deploy to staging (after push to develop)
    ├── deploy-production.yml → Manual/approved deploy to production (after push to main)
    ├── security-scan.yml     → Trivy + dotnet audit (scheduled daily)
    └── validate-template.yml → validates the .NET template
```

## Required global env vars (every workflow)

```yaml
env:
  DOTNET_VERSION: "10.0.x"
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  NUGET_PACKAGES: ${{ github.workspace }}/.nuget/packages
```

## Required timeout-minutes

```yaml
build-and-test:   timeout-minutes: 20
build-push:       timeout-minutes: 30
deploy:           timeout-minutes: 15
security-scan:    timeout-minutes: 10
```

## Minimum permissions (every workflow)

```yaml
permissions:
  contents: read        # checkout
  packages: write       # only in build-push.yml
  id-token: write       # only for cosign/OIDC
  pull-requests: write  # only for PR comments
```

**Never use `permissions: write-all`.**

## NuGet Cache (every workflow that runs dotnet)

```yaml
- uses: actions/cache@v4
  with:
    path: ${{ env.NUGET_PACKAGES }}
    key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/packages.lock.json') }}
    restore-keys: |
      nuget-${{ runner.os }}-
```

## Canonical Templates

### `ci.yml`

```yaml
name: CI

on:
  pull_request:
    branches: [main, develop]
  push:
    branches: [main, develop]
  workflow_dispatch:

permissions:
  contents: read
  pull-requests: write

env:
  DOTNET_VERSION: "10.0.x"
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  NUGET_PACKAGES: ${{ github.workspace }}/.nuget/packages

jobs:
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest
    timeout-minutes: 20
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - uses: actions/cache@v4
        with:
          path: ${{ env.NUGET_PACKAGES }}
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/packages.lock.json') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Restore
        run: dotnet restore --locked-mode

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Unit Tests
        run: |
          dotnet test tests/UnitTests/UnitTests.csproj \
            --no-build -c Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage \
            --logger "trx;LogFileName=unit-tests.trx"

      - name: Architecture Tests
        run: |
          dotnet test tests/ArchitectureTests/ArchitectureTests.csproj \
            --no-build -c Release \
            --logger "trx;LogFileName=arch-tests.trx"

      - name: Integration Tests
        run: |
          dotnet test tests/IntegrationTests/IntegrationTests.csproj \
            --no-build -c Release \
            --logger "trx;LogFileName=integration-tests.trx"
        env:
          ASPNETCORE_ENVIRONMENT: Test

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: "**/*.trx"

      - name: Upload Coverage
        uses: actions/upload-artifact@v4
        with:
          name: coverage
          path: coverage/

  format-check:
    name: Format Check
    runs-on: ubuntu-latest
    timeout-minutes: 10
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - run: dotnet restore --locked-mode
      - run: dotnet format --verify-no-changes --verbosity diagnostic

  security-audit:
    name: Security Audit (NuGet)
    runs-on: ubuntu-latest
    timeout-minutes: 10
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
      - run: dotnet restore --locked-mode
      - name: Check for vulnerable packages
        run: |
          dotnet list package --vulnerable --include-transitive 2>&1 | tee vuln.txt
          if grep -q "has the following vulnerable packages" vuln.txt; then
            echo "::error::Vulnerable NuGet packages found!"
            cat vuln.txt
            exit 1
          fi
```

### `build-push.yml`

```yaml
name: Build & Push Docker

on:
  push:
    branches: [main, develop]
    tags: ["v*.*.*"]
  workflow_dispatch:
    inputs:
      push_image:
        description: "Push image to registry?"
        type: boolean
        default: false

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository_owner }}/product-template-api

permissions:
  contents: read
  packages: write
  id-token: write
  attestations: write

jobs:
  build-push:
    name: Build & Push
    runs-on: ubuntu-latest
    timeout-minutes: 30
    outputs:
      image-digest: ${{ steps.build.outputs.digest }}

    steps:
      - uses: actions/checkout@v4

      - name: Log in to GHCR
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Extract Docker metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=ref,event=branch
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=sha,prefix=sha-,format=short

      - name: Build (no push — for scan)
        uses: docker/build-push-action@v6
        with:
          context: .
          file: src/Api/Dockerfile
          push: false
          load: true
          tags: product-template-api:scan
          cache-from: type=gha
          cache-to: type=gha,mode=max
          build-args: |
            VERSION=${{ steps.meta.outputs.version }}
            VCS_REF=${{ github.sha }}
            BUILD_DATE=${{ github.event.repository.updated_at }}

      - name: Trivy vulnerability scan
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: product-template-api:scan
          format: sarif
          output: trivy-results.sarif
          severity: HIGH,CRITICAL
          exit-code: "1"

      - name: Upload Trivy SARIF
        uses: github/codeql-action/upload-sarif@v3
        if: always()
        with:
          sarif_file: trivy-results.sarif

      - name: Build & Push
        id: build
        uses: docker/build-push-action@v6
        with:
          context: .
          file: src/Api/Dockerfile
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
          provenance: true
          sbom: true

      - name: Install cosign
        if: github.ref == 'refs/heads/main'
        uses: sigstore/cosign-installer@v3

      - name: Sign image
        if: github.ref == 'refs/heads/main'
        run: |
          cosign sign --yes \
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}@${{ steps.build.outputs.digest }}
        env:
          COSIGN_EXPERIMENTAL: "true"
```

### `deploy-staging.yml`

```yaml
name: Deploy — Staging

on:
  workflow_run:
    workflows: ["Build & Push Docker"]
    types: [completed]
    branches: [develop]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository_owner }}/product-template-api

jobs:
  migrate:
    name: Run EF Migrations
    runs-on: ubuntu-latest
    environment: staging
    timeout-minutes: 10
    if: ${{ github.event.workflow_run.conclusion == 'success' }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - name: Restore tools
        run: dotnet tool restore
      - name: Run Migrations
        run: |
          dotnet ef database update \
            --project src/Tools/Migrator/Migrator.csproj \
            --no-build
        env:
          ConnectionStrings__Default: ${{ secrets.STAGING_CONNECTION_STRING }}

  deploy:
    name: Deploy API
    needs: migrate
    runs-on: ubuntu-latest
    environment: staging
    timeout-minutes: 15
    steps:
      - name: Deploy to staging
        run: echo "Deploying ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:develop"
        # Replace with orchestrator: K8s, Azure Container Apps, ECS, etc.
```

## Secrets required

| Secret | Description |
|--------|-------------|
| `STAGING_CONNECTION_STRING` | Staging database connection string |
| `PRODUCTION_CONNECTION_STRING` | Production database connection string |
| `JWT_SECRET_KEY` | JWT key (one per environment) |
| `SEQ_SERVER_URL` | Seq URL for structured logs |

Secrets rules:
- **Never** declare secrets in workflow-level `env:` — pass only to specific steps.
- Prefix step variables: `APP_`, `DB_`, `JWT_`.
- Use `gh secret set` for programmatic provisioning.

## Branch naming

| Branch | Purpose | Deploy |
|--------|---------|--------|
| `main` | Production | Manual approval |
| `develop` | Staging | Automatic |
| `feature/*` | Feature branches | No |
| `hotfix/*` | Urgent fixes | No |

## Image versioning

```
sha-{short_sha}          → every push (immutable, traceability)
develop                  → push to develop (floating)
v{major}.{minor}.{patch} → semantic tag
latest                   → ONLY on develop; NEVER on main/production
```

## New Workflow Checklist

- [ ] Minimum `permissions:` declared explicitly
- [ ] `timeout-minutes:` defined on all jobs
- [ ] NuGet cache configured with lock file hash
- [ ] Test artifacts published with `if: always()`
- [ ] Secrets referenced via `${{ secrets.NAME }}`, never hardcoded
- [ ] `DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true` and `DOTNET_CLI_TELEMETRY_OPTOUT: true`
- [ ] `.NET 10.0.x` as target (never `latest`)
- [ ] `dotnet restore --locked-mode`
- [ ] Trivy scan before Docker push
- [ ] Production deploy with manual approval via `environment:`

## Never do

1. Use `dotnet-version: 'latest'` — always pin to `10.0.x`.
2. Use `permissions: write-all`.
3. Create workflows without `timeout-minutes`.
4. Push Docker image without vulnerability scan first.
5. Use `latest` as image tag in production.
6. Put connection strings or JWT secrets directly in YAML.
7. Omit NuGet package cache.
8. Rebuild Docker image in deploy stage.
9. Create deploy job without `environment:`.
10. Use `continue-on-error: true` on critical steps.
