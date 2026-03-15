# 14 — CI/CD Rules

> Regras para geração e manutenção de pipelines de CI/CD para este repositório .NET 10.
> Cobertura: **GitHub Actions** (padrão) e **Azure DevOps Pipelines** (alternativo).

---

## Princípios Fundamentais

1. **Pipeline como código** — todo workflow vive em `.github/workflows/` (GHA) ou `azure-pipelines/` (AzDO); nunca configuração pela UI.
2. **Fail fast, fail loud** — build, lint e testes rápidos executam primeiro; Docker e deploy executam após aprovação de qualidade.
3. **Nenhum segredo em código** — usar GitHub Secrets / Azure Key Vault + Variable Groups; nunca hardcoded.
4. **Idempotência** — deploy pode ser executado múltiplas vezes sem efeito colateral indesejado.
5. **Ambientes protegidos** — `staging` e `production` requerem aprovação manual antes do deploy.
6. **Imagem imutável** — a mesma imagem promovida por todos os ambientes; nunca rebuildar no deploy.
7. **Versionamento semântico** — tag de imagem = `v{MAJOR}.{MINOR}.{PATCH}[-{sha}]`; nunca usar apenas `latest` para produção.
8. **Observabilidade de pipeline** — gerar relatórios de cobertura, SBOM e scan de vulnerabilidades como artefatos.
9. **Matriz de .NET** — não testar em múltiplos runtimes; o target é exclusivamente `.NET 10`.
10. **Cache obrigatório** — cachear NuGet packages e layers Docker para reduzir tempo de execução.

---

## GitHub Actions — Estrutura de Workflows

```
.github/
└── workflows/
    ├── ci.yml                → PR validation (build + test + lint + format)
    ├── build-push.yml        → Build Docker + push para registry (push em main/develop)
    ├── deploy-staging.yml    → Deploy automático em staging (após push em develop)
    ├── deploy-production.yml → Deploy manual/aprovado em production (após push em main)
    ├── security-scan.yml     → Trivy + dotnet audit (agendado diariamente)
    └── validate-template.yml → (existente) valida o template .NET
```

---

## Workflow: CI (`ci.yml`)

### Gatilhos

```yaml
on:
  pull_request:
    branches: [main, develop]
  push:
    branches: [main, develop]
  workflow_dispatch:
```

### Jobs obrigatórios

```
build-and-test  ──► integration-tests ──► arch-tests + security-audit
format-check    ─┘
```

### Template canônico `ci.yml`

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

---

## Workflow: Build & Push Docker (`build-push.yml`)

### Regras obrigatórias

- Usar **`docker/build-push-action`** com BuildKit habilitado.
- Gerar `SBOM` e `provenance` attestation (Supply Chain Security).
- Assinar a imagem com `cosign` (Sigstore) em push para `main`.
- Tags: `develop` (em develop), `v{version}` e SHA curto para `main`.
- Scanear com `trivy` antes de fazer push — falhar em `HIGH`/`CRITICAL`.

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

      - name: Build (sem push — para scan)
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
          build-args: |
            VERSION=${{ steps.meta.outputs.version }}
            VCS_REF=${{ github.sha }}
            BUILD_DATE=${{ github.event.repository.updated_at }}

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

---

## Workflow: Deploy (`deploy-staging.yml` / `deploy-production.yml`)

### Princípios

- Deploy usa a **imagem já construída** (pelo digest) — nunca rebuilda.
- `staging`: deploy automático após `build-push` em `develop`.
- `production`: deploy após `build-push` em `main` + **aprovação manual** via `environment`.
- Migrations EF Core executam como **job separado antes** do deploy da API.

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
        # Substituir pelo orquestrador: K8s, Azure Container Apps, ECS, etc.
        run: |
          echo "Deploying ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:develop"
          # Ex: kubectl set image deployment/api api=$IMAGE:$TAG -n staging
```

---

## Segredos e Variáveis de Ambiente

### GitHub Secrets obrigatórios

| Secret | Descrição |
|--------|-----------|
| `STAGING_CONNECTION_STRING` | Connection string do banco de staging |
| `PRODUCTION_CONNECTION_STRING` | Connection string do banco de produção |
| `JWT_SECRET_KEY` | Chave JWT (uma por ambiente) |
| `SEQ_SERVER_URL` | URL do Seq para logs estruturados |
| `REGISTRY_TOKEN` | Token para registry privado (se não usar GHCR) |

### Regras de Segredos

- **Nunca** declarar segredos em `env:` de nível de workflow — passar somente para steps específicos.
- Prefixar variáveis de step: `APP_`, `DB_`, `JWT_`.
- Rotacionar segredos a cada 90 dias.
- Usar `gh secret set` via CLI para provisionamento programático.

---

## Regras de Qualidade de Pipeline

### Nomes

- Jobs: `kebab-case` descritivo (`build-and-test`, `security-audit`, `deploy-staging`).
- Steps: frase imperativa curta em inglês (`Run unit tests`, `Upload coverage report`).

### `permissions` mínimas — obrigatórias em todo workflow

```yaml
permissions:
  contents: read        # checkout
  packages: write       # apenas em build-push.yml
  id-token: write       # apenas para cosign/OIDC
  pull-requests: write  # apenas para comentários em PR
```

**Nunca usar `permissions: write-all`.**

### Timeouts obrigatórios

```yaml
jobs:
  build-and-test:
    timeout-minutes: 20
  build-push:
    timeout-minutes: 30
  deploy:
    timeout-minutes: 15
```

### Variáveis de ambiente globais obrigatórias

```yaml
env:
  DOTNET_VERSION: "10.0.x"
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  NUGET_PACKAGES: ${{ github.workspace }}/.nuget/packages
```

### Cache de NuGet

```yaml
- uses: actions/cache@v4
  with:
    path: ${{ env.NUGET_PACKAGES }}
    key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj', '**/packages.lock.json') }}
    restore-keys: |
      nuget-${{ runner.os }}-
```

Sempre incluir `packages.lock.json` no hash para cache determinístico.

### Artefatos

- Resultados de teste (`.trx`) → sempre publicados, mesmo em falha (`if: always()`).
- Relatório de cobertura → publicado como artefato e comentado no PR.
- SBOM → publicado como artefato e anexado ao release.
- SARIF (Trivy/CodeQL) → enviado ao GitHub Security tab.

---

## Azure DevOps Pipelines (alternativo)

### Estrutura de arquivos

```
azure-pipelines/
├── ci.yml               → PR validation
├── build-push.yml       → Docker build + push para ACR
├── deploy-staging.yml   → Deploy staging
└── deploy-prod.yml      → Deploy produção (aprovação manual)
```

### Template canônico `azure-pipelines/ci.yml`

```yaml
trigger:
  branches:
    include: [main, develop]
  paths:
    exclude: [docs/**, "*.md"]

pr:
  branches:
    include: [main, develop]

pool:
  vmImage: ubuntu-latest

variables:
  DOTNET_VERSION: "10.0.x"
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  buildConfiguration: Release
  nugetCacheDir: $(Pipeline.Workspace)/.nuget/packages

stages:
  - stage: CI
    displayName: Build & Test
    jobs:
      - job: BuildAndTest
        timeoutInMinutes: 25
        steps:
          - task: UseDotNet@2
            inputs:
              packageType: sdk
              version: $(DOTNET_VERSION)
            displayName: Setup .NET SDK

          - task: Cache@2
            inputs:
              key: 'nuget | "$(Agent.OS)" | **/packages.lock.json'
              restoreKeys: 'nuget | "$(Agent.OS)"'
              path: $(nugetCacheDir)
            displayName: Cache NuGet packages

          - script: dotnet restore --locked-mode
            displayName: Restore

          - script: dotnet build --no-restore -c $(buildConfiguration)
            displayName: Build

          - script: |
              dotnet test tests/UnitTests/UnitTests.csproj \
                --no-build -c $(buildConfiguration) \
                --collect:"XPlat Code Coverage" \
                --results-directory $(Agent.TempDirectory)/coverage \
                --logger trx
            displayName: Unit Tests

          - script: |
              dotnet test tests/ArchitectureTests/ArchitectureTests.csproj \
                --no-build -c $(buildConfiguration) --logger trx
            displayName: Architecture Tests

          - script: |
              dotnet test tests/IntegrationTests/IntegrationTests.csproj \
                --no-build -c $(buildConfiguration) --logger trx
            displayName: Integration Tests

          - script: dotnet format --verify-no-changes --verbosity diagnostic
            displayName: Format Check

          - script: |
              dotnet list package --vulnerable --include-transitive 2>&1 | tee vuln.txt
              if grep -q "has the following vulnerable packages" vuln.txt; then
                echo "##vso[task.logissue type=error]Vulnerable NuGet packages found!"
                exit 1
              fi
            displayName: NuGet Vulnerability Audit

          - task: PublishTestResults@2
            condition: always()
            inputs:
              testResultsFormat: VSTest
              testResultsFiles: "**/*.trx"
            displayName: Publish Test Results

          - task: PublishCodeCoverageResults@2
            inputs:
              summaryFileLocation: "$(Agent.TempDirectory)/coverage/**/coverage.cobertura.xml"
            displayName: Publish Coverage

  - stage: DockerBuild
    displayName: Docker Build & Scan
    dependsOn: CI
    condition: succeeded()
    jobs:
      - job: DockerBuildScan
        timeoutInMinutes: 30
        steps:
          - task: Docker@2
            displayName: Build image
            inputs:
              command: build
              dockerfile: src/Api/Dockerfile
              buildContext: .
              tags: product-template-api:$(Build.BuildId)
              arguments: >
                --build-arg VERSION=$(Build.BuildNumber)
                --build-arg VCS_REF=$(Build.SourceVersion)

          - script: |
              curl -sfL https://raw.githubusercontent.com/aquasecurity/trivy/main/contrib/install.sh | sh -s -- -b /usr/local/bin
              trivy image --severity HIGH,CRITICAL --exit-code 1 \
                product-template-api:$(Build.BuildId)
            displayName: Trivy Vulnerability Scan

          - task: Docker@2
            displayName: Push to ACR
            condition: and(succeeded(), in(variables['Build.SourceBranchName'], 'main', 'develop'))
            inputs:
              command: push
              containerRegistry: $(ACR_SERVICE_CONNECTION)
              repository: product-template-api
              tags: |
                $(Build.BuildId)
                $(Build.SourceBranchName)
```

### Azure DevOps — Segredos e Variáveis

- Usar **Variable Groups** no Azure DevOps Library (nunca `variables:` inline para segredos).
- Linkar Variable Group ao **Azure Key Vault** para rotação automática.
- Referenciar com `$(SECRET_NAME)` — mascarados automaticamente nos logs.
- Ambientes de produção: adicionar **Approval Gates** + **Business Hours** checks no Environment.

---

## Regras de Nomenclatura de Branches

| Branch | Finalidade | Deploy automático |
|--------|-----------|-------------------|
| `main` | Produção | Não (aprovação manual) |
| `develop` | Staging | Sim |
| `feature/*` | Feature branches | Não |
| `hotfix/*` | Correções urgentes | Não |
| `release/*` | Release candidates | Staging (manual) |

---

## Versionamento de Imagem Docker no CI

```
Padrão: {REGISTRY}/{OWNER}/{NAME}:{TAG}

Tags geradas automaticamente:
  - sha-{short_sha}          → todo push (imutável, rastreabilidade)
  - develop                  → push em develop (floating)
  - v{major}.{minor}.{patch} → tag semântica (push de tag git)
  - v{major}.{minor}         → floating minor
  - latest                   → APENAS em develop; NUNCA em main/production
```

**Imagem de produção NUNCA usa `latest`** — sempre digest SHA256 ou tag semântica fixa.

---

## Checklist de Novo Workflow

- [ ] `permissions:` mínimas declaradas explicitamente
- [ ] `timeout-minutes:` definido em todos os jobs
- [ ] Cache de NuGet configurado com hash de lock files
- [ ] Artefatos de teste publicados com `if: always()`
- [ ] Segredos referenciados via `${{ secrets.NAME }}`, nunca hardcoded
- [ ] `DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true` e `DOTNET_CLI_TELEMETRY_OPTOUT: true`
- [ ] `.NET 10.0.x` como versão alvo (nunca `latest`)
- [ ] `dotnet restore --locked-mode` (requer `packages.lock.json`)
- [ ] Scan de segurança (Trivy) antes de push Docker
- [ ] Deploy em produção com aprovação manual via `environment:`

---

## O que o Copilot NÃO deve fazer

1. Usar `dotnet-version: 'latest'` — sempre pin para `10.0.x`.
2. Usar `permissions: write-all` — sempre declarar permissões mínimas.
3. Criar workflows sem `timeout-minutes`.
4. Fazer push de imagem Docker sem scan de vulnerabilidades antes.
5. Usar `latest` como tag de imagem em ambiente de produção.
6. Colocar connection strings, JWT secrets ou API keys diretamente no YAML.
7. Omitir cache de NuGet packages.
8. Fazer build Docker na stage de deploy (imagem deve ser promovida, não reconstruída).
9. Criar job de deploy sem `environment:` configurado (para ambientes protegidos).
10. Omitir publicação de resultados de teste.
11. Usar `continue-on-error: true` em steps críticos (build, test, security scan).
12. Omitir `dotnet restore --locked-mode` (garantia de build determinístico).
