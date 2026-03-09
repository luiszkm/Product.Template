# Contributing to Product.Template

## Before You Start

1. Read `README.md` for project overview and AI-first setup.
2. Read `.github/copilot-instructions.md` — loaded automatically by Copilot, contains all conventions.
3. Read `.ai/rules/01-architecture.md` for layer boundaries.
4. Read `.ai/rules/12-folder-structure.md` for file placement.

## Development Workflow

### 1. Pick a Task

- Check the issue tracker for open issues.
- For new features, create an issue first describing the scope.

### 2. Create a Branch

```bash
git checkout -b feature/{short-description}
# or
git checkout -b fix/{short-description}
```

### 3. Implement

Use the right GitHub Copilot agent for the task (see [Using AI Agents](#using-ai-agents)), or follow the `.ai/rules/` for the relevant layer:

| Layer | Rule File |
|-------|-----------|
| Domain | `.ai/rules/02-domain.md` |
| Application | `.ai/rules/03-application.md` |
| Infrastructure | `.ai/rules/04-infrastructure.md` |
| API | `.ai/rules/05-api.md` |
| Tests | `.ai/rules/06-tests.md` |
| Security | `.ai/rules/08-security.md` |

### 4. Validate

Run through the appropriate checklist in `.ai/checklists/`:

- New feature → `.ai/checklists/new-feature.md`
- New endpoint → `.ai/checklists/api-endpoint.md`
- Database changes → `.ai/checklists/persistence.md`

### 5. Review (antes do PR)

Use `@code-reviewer` no Copilot Chat ou o prompt `prompts/code-review.prompt.md` para fazer um levantamento completo de brechas antes de abrir o PR:

```
Revise o código da feature {FEATURE} no módulo {MODULE}.
Escopo: src/Core/{Module}/ e src/Api/Controllers/v1/{Module}Controller.cs
```

### 6. Test

```bash
dotnet build
dotnet test
```

All tests must pass. Architecture tests enforce layer boundaries automatically.

### 7. Submit PR

Use `.ai/checklists/pull-request.md` as the review checklist.

---

## Using AI Agents

### GitHub Copilot Agents (Copilot Chat)

Este repositório tem agents especializados configurados em `.github/agents/`. Use-os com `@nome-do-agent` no Copilot Chat:

| Agent | Como usar | Quando usar |
|-------|-----------|-------------|
| `@backend-architect` | `@backend-architect revise o módulo Catalog` | Validar arquitetura, detectar derive, revisar novo módulo |
| `@feature-builder` | `@feature-builder crie feature Product no módulo Catalog` | Scaffold de feature completa (entity → endpoint → tests) |
| `@query-optimizer` | `@query-optimizer analise ListProductsQueryHandler` | Diagnosticar N+1, over-fetching, propor Dapper read service |
| `@code-reviewer` | `@code-reviewer revise src/Core/Catalog/` | Revisão profunda: brechas de segurança, arquitetura, testes — com código corrigido |

### Reusable Prompts (`prompts/`)

Arquivos em `prompts/` são templates prontos para copiar no Copilot Chat. Abra o arquivo, preencha os placeholders e cole:

| Prompt | Arquivo | Uso |
|--------|---------|-----|
| Criar feature | `prompts/create-feature.prompt.md` | Scaffold completo com checklist e formato de resposta esperado |
| Revisar arquitetura | `prompts/review-feature.prompt.md` | Revisão por camada (Domain → Api) com critérios explícitos |
| Otimizar query | `prompts/optimize-query.prompt.md` | Diagnóstico de gargalo + proposta EF Core ou Dapper |
| Revisão de código | `prompts/code-review.prompt.md` | Levantamento de brechas (segurança, arq., persistência, testes) + correções |

### Prompts avançados (`.ai/prompts/`)

Para tarefas mais granulares, use os prompts em `.ai/prompts/` com o master-prompt como sistema:

```
Sistema: conteúdo de .ai/prompts/master-prompt.md
```

| Task | Prompt |
|------|--------|
| New entity only | `.ai/prompts/create-entity.md` |
| New endpoint only | `.ai/prompts/create-endpoint.md` |
| New command only | `.ai/prompts/create-command.md` |
| New query only | `.ai/prompts/create-query.md` |
| Create migration | `.ai/prompts/create-migration.md` |

### O que agentes devem ler antes de gerar código

O `copilot-instructions.md` é carregado automaticamente pelo Copilot. Para outros modelos/agentes, instrua-os a ler:

1. `.github/copilot-instructions.md` — regras globais
2. `.github/instructions/backend.instructions.md` — padrão de handlers, validators, DTOs
3. `.github/instructions/api.instructions.md` — padrão de controllers, RBAC
4. `.github/instructions/infrastructure.instructions.md` — persistência, DI
5. A regra específica para a camada sendo modificada (`.ai/rules/02` a `09`)

---

## Code Style

- Enforced by `.editorconfig` — do not override.
- File-scoped namespaces.
- Private fields: `_camelCase`.
- All public members: PascalCase.
- Interfaces: `I` prefix.
- See `.ai/rules/07-style.md` for complete guidelines.

## Commit Messages

Use conventional commits:

```
feat: add Catalog module with CRUD operations
fix: resolve N+1 query in ListUsersQueryHandler
test: add authorization tests for CatalogController
docs: update RBAC matrix for Catalog endpoints
refactor: extract shared pagination logic to base repository
```

## Adding a New Module

1. Create the project structure:
   ```
   src/Core/{Module}/
   ├── {Module}.Domain/{Module}.Domain.csproj
   ├── {Module}.Application/{Module}.Application.csproj
   └── {Module}.Infrastructure/{Module}.Infrastructure.csproj
   ```

2. Set project references per `.ai/rules/01-architecture.md`.

3. Add projects to `Product.Template.sln`.

4. Register DI in `Api/Configurations/CoreConfiguration.cs`.

5. Register MediatR assembly in `Api/Configurations/KernelConfigurations.cs`.

6. Use `@feature-builder` or `prompts/create-feature.prompt.md` to scaffold the first feature.

7. Follow `.ai/checklists/new-feature.md` for completeness.

8. Run `@code-reviewer` before opening the PR.

## RBAC Matrix

Every protected endpoint must be documented in `docs/security/RBAC_MATRIX.md`.

This is enforced by `RbacMatrixConsistencyTests` — PRs that add endpoints without updating the matrix will fail CI.

## Questions?

If unsure about a pattern, look at the **Identity module** (`src/Core/Identity/`) — it's the canonical reference implementation.

For code review and gap analysis, use `@code-reviewer` in Copilot Chat.
