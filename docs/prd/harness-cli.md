# PRD: Product Harness CLI

> **Produto:** Product Harness CLI (`product`)  
> **Versão do documento:** 1.0  
> **Data:** 2026-07-27  
> **Status:** Draft — para análise e implementação futura  
> **Autor:** Product.Template Team

---

## 1. Resumo executivo

A **Product Harness CLI** (`product`) é uma ferramenta de linha de comando cross-platform (Go) que orquestra:

1. **Templates** — scaffold de projetos .NET a partir do `Product.Template` (via `dotnet new` e pacotes embarcados).
2. **Harness de agentes** — rules, skills, checklists, hooks e documentação para Cursor e Claude Code.
3. **Workflow de entrega** — pipeline estruturado **PRD → TDD → Spec → Execute → Review**, com gates determinísticos entre fases.

A CLI **não substitui** Cursor ou Claude como executor inteligente. Ela é o **maestro**: gera artefatos, valida gates, monta contexto e sincroniza o harness entre projetos.

---

## 2. Problema

### Situação atual

O `Product.Template` já inclui um ecossistema rico de produtividade com IA:

| Componente | Localização | Função |
|------------|-------------|--------|
| Template .NET | `.template.config/template.json` | `dotnet new product-template` |
| Setup manual | `setup.ps1`, `setup.sh` | Rename, namespaces, Git init |
| Rules | `.cursor/rules/*.mdc` | Contratos por camada |
| Skills | `.cursor/skills/*/SKILL.md` | Playbooks de implementação |
| Plans | `.cursor/plans/_template.md` | Spec de execução |
| Checklists | `.agents/checklists/` | Gates de qualidade |
| Agent docs | `AGENTS.md`, `CLAUDE.md`, `MEMORY.md` | Instruções para agentes |
| Hooks Claude | `.claude/settings.json` | Build no Stop, arch tests no commit |
| Design docs | `docs/templates/module-design-template.md` | TDD de módulos |
| Verify gate | `Makefile` (`make verify`) | Build + arch + unit + format |

### Dores

1. **Fragmentação** — setup, harness e workflow estão em scripts e pastas separadas, sem caminho único documentado.
2. **Duplicação** — `setup.ps1` e `setup.sh` replicam lógica que o template engine já cobre parcialmente.
3. **Workflow implícito** — PRD, design técnico, spec e review existem como artefatos soltos, sem pipeline formal.
4. **Sincronização** — atualizar rules/skills em múltiplos projetos é manual.
5. **Integração frágil** — agentes dependem do humano saber qual skill, rule e checklist usar em cada fase.
6. **Cross-platform** — Windows exige PowerShell; Linux/macOS exige Bash; não há binário único.

### Oportunidade

Uma CLI focada em **harness + templates** unifica a experiência, impõe gates entre fases e posiciona o `Product.Template` como plataforma de produtividade — não apenas como boilerplate .NET.

---

## 3. Objetivos

### Objetivos primários

| # | Objetivo | Métrica de sucesso |
|---|----------|-------------------|
| O1 | Setup de projeto completo em um comando | `product create` gera projeto + harness em < 2 min |
| O2 | Workflow PRD → TDD → Spec → Execute → Review rastreável | Estado persistido em `.product/flows/` |
| O3 | Harness versionado e sincronizável | `product harness sync` atualiza rules/skills entre projetos |
| O4 | Gates determinísticos entre fases | `product flow gate` bloqueia avanço se artefato incompleto |
| O5 | Integração nativa com Cursor e Claude | Artefatos gerados são consumidos diretamente pelos agentes |

### Objetivos secundários

| # | Objetivo |
|---|----------|
| O6 | Substituir `setup.ps1` / `setup.sh` como caminho oficial |
| O7 | Expor MCP server para integração futura com Cursor |
| O8 | Suportar profiles (`full`, `backend`, `harness-only`) |

### Non-goals (fora de escopo v1)

- Substituir Cursor/Claude como agente de codificação headless.
- IDE própria ou editor integrado.
- Gerenciamento de issues (Linear/Jira) — apenas link/referência.
- Hospedagem cloud ou SaaS do harness.
- Geração de código sem passar pelo workflow (bypass de TDD/Spec).

---

## 4. Personas e casos de uso

### P1 — Tech Lead / Arquiteto

> "Quero iniciar um produto novo com harness completo e garantir que o time siga PRD → TDD → Spec antes de codar."

**Casos de uso:**
- Criar projeto a partir do template com harness full.
- Aprovar TDD antes de liberar fase Spec.
- Sincronizar rules/skills atualizadas para todos os repos do time.

### P2 — Desenvolvedor

> "Quero saber em que fase estou, o que falta e qual skill usar agora."

**Casos de uso:**
- Iniciar flow de feature: `product flow start billing-refund`.
- Ver contexto para colar no agente: `product flow context`.
- Avançar fase após gate passar: `product flow advance`.

### P3 — Agente de IA (Cursor / Claude)

> "Preciso de contexto estruturado: PRD, design, spec, rules e checklist da fase atual."

**Casos de uso:**
- Ler `.product/flows/{id}.yaml` para saber a fase.
- Consumir `docs/prd/`, `docs/design/`, `.cursor/plans/`.
- Executar skills indicadas no flow (`new-feature`, `test-writer`, `pr-review`).

### P4 — Mantenedor do template

> "Quero publicar nova versão do harness e dos templates sem quebrar projetos existentes."

**Casos de uso:**
- Empacotar rules/skills em release da CLI.
- Publicar template pack compatível com versão da CLI.
- Changelog de breaking changes no harness.

---

## 5. Workflow oficial: PRD → TDD → Spec → Execute → Review

### 5.1 Diagrama

```text
┌─────┐    ┌─────┐    ┌──────┐    ┌─────────┐    ┌────────┐
│ PRD │ ─► │ TDD │ ─► │ Spec │ ─► │ Execute │ ─► │ Review │
└─────┘    └─────┘    └──────┘    └─────────┘    └────────┘
  │          │           │             │              │
  ▼          ▼           ▼             ▼              ▼
 docs/    docs/      .cursor/      código +      verify +
 prd/     design/    plans/        testes        pr-review
```

### 5.2 Definição das fases

| Fase | Sigla | Pergunta | Artefato principal | Skill sugerida |
|------|-------|----------|-------------------|----------------|
| **PRD** | Product Requirements Document | *O quê* e *por quê*? | `docs/prd/{slug}.md` | `prd-writer` (nova) |
| **TDD** | Technical Design Documentation | *Como* tecnicamente? | `docs/design/{slug}.md` | `tdd-designer` (nova) |
| **Spec** | Execution Specification | *O quê exatamente* implementar? | `.cursor/plans/{date}-{slug}.md` | `spec-planner` (nova) |
| **Execute** | Implementation | Implementar conforme Spec | código + testes | `new-feature`, `new-command`, etc. |
| **Review** | Quality gate | Está correto e completo? | comentários + verify | `pr-review`, `review` |

### 5.3 Gates entre fases

| Transição | Gate (CLI valida) |
|-----------|-------------------|
| PRD → TDD | PRD existe; acceptance criteria definidos; issue linkada (opcional) |
| TDD → Spec | Design completo: módulo, agregados, API, RBAC, persistence, riscos |
| Spec → Execute | Plano com affected files, steps, acceptance command, rollback |
| Execute → Review | `make verify` passa (build + arch + unit + format) |
| Review → Done | Checklist PR completo; review sem blockers Critical/Security |

### 5.4 Estado do flow

Arquivo: `.product/flows/{slug}.yaml`

```yaml
id: billing-refund
title: Billing Partial Refund
phase: tdd                    # prd | tdd | spec | execute | review | done
issue: PT-42                  # opcional — Linear/Jira
created: 2026-07-27
updated: 2026-07-27

artifacts:
  prd: docs/prd/billing-refund.md
  tdd: docs/design/billing-refund.md
  spec: .cursor/plans/2026-07-27-billing-refund.md

gates:
  prd: approved               # draft | in_progress | approved
  tdd: in_progress
  spec: not_started
  execute: not_started
  review: not_started

skills:
  execute: [new-feature]
  review: [pr-review]

acceptance:
  command: make verify
  extra: dotnet test tests/IntegrationTests --filter "FullyQualifiedName~Billing"
```

---

## 6. Escopo funcional da CLI

### 6.1 Comandos — Template (orquestração)

| Comando | Descrição |
|---------|-----------|
| `product create <name> [-o PATH]` | Cria projeto via `dotnet new product-template` + harness |
| `product create <name> --profile full\|backend\|minimal` | Profile de scaffold |
| `product create <name> --verify` | Roda verify após create |
| `product template install` | Instala/atualiza template pack local |
| `product template list` | Lista templates disponíveis |

**Comportamento de `create`:**
1. Verifica .NET SDK ≥ 10.
2. Instala template pack se ausente/desatualizado.
3. Executa `dotnet new product-template -n {name} -o {path}`.
4. Injeta harness conforme profile.
5. Inicializa Git (opcional, default on).
6. Copia `compose.env.example` → `compose.env` (opcional).
7. Roda `dotnet restore` + `make verify` se `--verify`.

**Profiles:**

| Profile | .NET template | Harness |
|---------|---------------|---------|
| `full` | Completo | rules + skills + checklists + AGENTS + hooks |
| `backend` | Completo | rules essenciais + AGENTS.md |
| `minimal` | Completo | Apenas verify hooks |
| `harness-only` | Não aplica | Injeta harness em repo existente |

### 6.2 Comandos — Harness

| Comando | Descrição |
|---------|-----------|
| `product harness init [--profile]` | Instala harness em repo existente |
| `product harness sync [--version]` | Atualiza rules/skills do pacote embarcado |
| `product harness status` | Versão instalada vs disponível |
| `product harness diff` | Diff entre harness local e pacote |
| `product skill new <name>` | Scaffold de `.cursor/skills/{name}/SKILL.md` |
| `product skill list` | Lista skills instaladas |
| `product rules list` | Lista rules instaladas |

**Pacote embarcado (embed):**

```
embed/harness/
├── .cursor/
│   ├── rules/          # todos os .mdc
│   ├── skills/         # todos os SKILL.md
│   └── plans/_template.md
├── .agents/checklists/
├── .agents/patterns/
├── AGENTS.md
├── CLAUDE.md
├── MEMORY.md.template
└── .claude/settings.json
```

### 6.3 Comandos — Workflow (core do produto)

| Comando | Descrição |
|---------|-----------|
| `product flow start <slug> [--issue ID]` | Inicia flow; gera PRD draft |
| `product flow status [slug]` | Fase atual, gates, artefatos |
| `product flow advance [slug]` | Avança fase se gate passar |
| `product flow gate [slug] [--phase]` | Valida gate da fase |
| `product flow context [slug]` | Monta bundle de contexto para agente |
| `product flow approve <phase>` | Marca fase como approved (human gate) |
| `product flow list` | Lista flows ativos |
| `product flow done <slug>` | Marca flow como concluído |

**`product flow context` output (exemplo):**

```markdown
# Context Bundle — billing-refund (phase: execute)

## Flow
- Phase: execute
- Issue: PT-42
- Skills: new-feature

## Read first
1. docs/prd/billing-refund.md
2. docs/design/billing-refund.md
3. .cursor/plans/2026-07-27-billing-refund.md

## Rules (layer)
- .cursor/rules/domain.mdc
- .cursor/rules/application.mdc
- ...

## Checklist
- .agents/checklists/new-feature.md

## Acceptance
make verify
dotnet test tests/IntegrationTests --filter "FullyQualifiedName~Billing"
```

### 6.4 Comandos — Verify e Review

| Comando | Descrição |
|---------|-----------|
| `product verify` | Wrap de `make verify` |
| `product review local` | verify + checklist PR |
| `product review pr <number>` | Prepara contexto para skill `pr-review` |
| `product dev` | `docker compose up` + `dotnet run` (opcional v2) |

### 6.5 Integração Cursor / Claude

| Integração | Mecanismo | Fase |
|------------|-----------|------|
| **Cursor Rules** | Copia `.cursor/rules/*.mdc` | harness init/sync |
| **Cursor Skills** | Copia `.cursor/skills/*/SKILL.md` | harness init/sync |
| **Plan Mode** | Gera `.cursor/plans/{date}-{slug}.md` | Spec |
| **Claude Hooks** | Gera/atualiza `.claude/settings.json` | harness init |
| **Context bundle** | stdout ou `.product/context/{slug}.md` | todas |
| **MCP server** | `product mcp` expõe flow status, gates | v2 |

**Princípio:** integração **file-based** na v1 — sem depender de API headless do Cursor.

---

## 7. Templates orquestrados

### 7.1 Template .NET (existente)

| Item | Valor |
|------|-------|
| Source | Repositório `Product.Template` |
| Engine | `dotnet new` + `.template.config/template.json` |
| shortName | `product-template` |
| sourceName | `Product.Template` |

A CLI **delega** rename/namespace/GUIDs ao template engine — não reimplementa lógica de `setup.sh`.

### 7.2 Templates de documentação (novos — embarcados na CLI)

| Template | Destino | Fase |
|----------|---------|------|
| `prd.md.tmpl` | `docs/prd/{slug}.md` | PRD |
| `design.md.tmpl` | `docs/design/{slug}.md` | TDD — baseado em `docs/templates/module-design-template.md` |
| `plan.md.tmpl` | `.cursor/plans/{date}-{slug}.md` | Spec — baseado em `.cursor/plans/_template.md` |
| `skill.md.tmpl` | `.cursor/skills/{name}/SKILL.md` | harness |
| `flow.yaml.tmpl` | `.product/flows/{slug}.yaml` | workflow |

### 7.3 Skills novas (a criar no harness)

| Skill | Fase | Responsabilidade |
|-------|------|------------------|
| `prd-writer` | PRD | Gera/refina PRD a partir de input do usuário |
| `tdd-designer` | TDD | Produz design técnico completo |
| `spec-planner` | Spec | Deriva plano de execução do TDD |

Skills existentes reutilizadas:

- Execute: `new-feature`, `new-module`, `new-command`, `new-query`, `new-endpoint`, `new-entity`, `new-migration`, `test-writer`
- Review: `pr-review`, `review`, `optimize-query`

---

## 8. Arquitetura técnica

### 8.1 Stack

| Camada | Tecnologia |
|--------|------------|
| Linguagem | Go 1.22+ |
| CLI framework | Cobra |
| Templates | `text/template` + `embed.FS` |
| Config/state | YAML |
| Subprocess | `dotnet`, `git`, `gh`, `docker`, `make` |
| Distribuição | GitHub Releases, `go install`, Scoop/winget |

### 8.2 Estrutura do repositório (proposto)

```text
product-cli/                    # repo separado ou src/Tools/ProductCli/
├── cmd/product/main.go
├── internal/
│   ├── cli/                    # comandos Cobra
│   ├── harness/                # install, sync, embed
│   ├── template/               # dotnet new wrapper
│   ├── flow/                   # state machine PRD→Review
│   ├── gate/                   # validadores por fase
│   ├── context/                # bundle builder
│   └── exec/                   # subprocess runners
├── embed/
│   ├── harness/                # snapshot do Product.Template harness
│   └── templates/              # prd, design, plan, skill, flow
├── docs/
│   └── prd/harness-cli.md      # este documento
├── go.mod
└── .goreleaser.yaml
```

### 8.3 Diagrama de componentes

```text
┌─────────────────────────────────────────────────────────┐
│                    product CLI (Go)                      │
├─────────────┬──────────────┬──────────────┬─────────────┤
│  template   │   harness    │    flow      │   verify    │
│  (dotnet    │  (embed FS   │  (state      │  (make/     │
│   new)      │   sync)      │   machine)   │   dotnet)   │
└──────┬──────┴──────┬───────┴──────┬───────┴──────┬──────┘
       │             │              │              │
       ▼             ▼              ▼              ▼
  Product.Template  .cursor/    .product/flows/  Makefile
  (dotnet new)      AGENTS.md   docs/prd/        dotnet test
                    .agents/    docs/design/
                                .cursor/plans/
       │             │              │
       └─────────────┴──────────────┘
                     │
                     ▼
            Cursor / Claude Code
            (lê artefatos, executa skills)
```

### 8.4 Versionamento

| Artefato | Esquema |
|----------|---------|
| CLI | Semver (`v1.2.3`) |
| Harness pack | `{cli-version}` — embarcado no binário |
| Template pack | Compatível com CLI `>= 1.0.0` |
| Flow schema | `.product/flows/schema.json` v1 |

---

## 9. Requisitos funcionais

### RF-01 — Create project
- **Dado** .NET SDK 10+ instalado  
- **Quando** `product create devserver -o C:\www --profile full`  
- **Então** projeto criado em `C:\www\devserver` com harness completo e Git init  

### RF-02 — Harness init em repo legado
- **Dado** repo .NET existente sem `.cursor/`  
- **Quando** `product harness init --profile backend`  
- **Então** rules, AGENTS.md e checklists mínimos instalados  

### RF-03 — Iniciar workflow
- **Dado** repo com harness instalado  
- **Quando** `product flow start billing-refund --issue PT-42`  
- **Então** cria `docs/prd/billing-refund.md` (draft), `.product/flows/billing-refund.yaml` (phase: prd)  

### RF-04 — Gate TDD
- **Dado** flow em fase TDD com design incompleto  
- **Quando** `product flow advance`  
- **Então** erro listando seções faltantes (API, RBAC, persistence, etc.)  

### RF-05 — Context bundle
- **Dado** flow em fase Execute  
- **Quando** `product flow context billing-refund`  
- **Então** output markdown com PRD + TDD + Spec + rules + checklist + acceptance  

### RF-06 — Harness sync
- **Dado** CLI v1.1.0 com harness pack atualizado  
- **Quando** `product harness sync`  
- **Então** rules/skills atualizados; diff exibido; arquivos customizados preservados (merge strategy)  

### RF-07 — Verify
- **Dado** projeto com Makefile  
- **Quando** `product verify`  
- **Então** executa build + arch + unit + format; exit code != 0 se falhar  

### RF-08 — Deprecar setup scripts
- **Dado** CLI estável v1.0  
- **Então** README documenta `product create` como caminho oficial; setup.ps1/sh marcados deprecated  

---

## 10. Requisitos não-funcionais

| ID | Requisito | Target |
|----|-----------|--------|
| RNF-01 | Binário único, cross-platform | Windows, Linux, macOS (amd64 + arm64) |
| RNF-02 | Startup | < 100ms |
| RNF-03 | Sem runtime externo | Go binary; apenas SDK .NET para create |
| RNF-04 | Offline-first | harness embarcado; sync remoto opcional |
| RNF-05 | Idempotência | `harness init` e `sync` safe to re-run |
| RNF-06 | Exit codes | 0 ok, 1 erro, 2 gate failed |
| RNF-07 | Output | human-readable default; `--json` para CI |
| RNF-08 | Segurança | nunca escrever secrets; respeitar `.gitignore` |

---

## 11. Roadmap

### MVP (v0.1) — 4–6 semanas

- [ ] `product create` (wrap `dotnet new` + harness full)
- [ ] `product harness init` + `sync`
- [ ] `product flow start | status | advance | gate | context`
- [ ] Templates: PRD, TDD (design), Spec (plan), flow.yaml
- [ ] `product verify`
- [ ] Skills novas: `prd-writer`, `tdd-designer`, `spec-planner` (markdown)
- [ ] Deprecate setup.ps1/sh no README

### v1.0 — 8–10 semanas

- [ ] Profiles (`full`, `backend`, `minimal`, `harness-only`)
- [ ] `product harness diff`
- [ ] `product skill new`
- [ ] `product flow approve` (human gate)
- [ ] `product review local`
- [ ] Goreleaser + GitHub Releases
- [ ] Documentação completa

### v1.x — futuro

- [ ] `product review pr` (integração gh)
- [ ] `product dev` (docker + dotnet run)
- [ ] MCP server (`product mcp`)
- [ ] Remote harness registry (GitHub packages)
- [ ] `product flow` integração Linear API
- [ ] Hooks generator por fase para Claude Code

---

## 12. Métricas de sucesso

| Métrica | Baseline | Target v1.0 |
|---------|----------|-------------|
| Tempo setup projeto + harness | ~15 min manual | < 2 min |
| Projetos usando workflow formal | 0% | 80% do time |
| Flows com TDD antes de Spec | N/A | 100% (gate enforced) |
| Verify pass rate pós-Execute | N/A | > 90% first run |
| Adoção vs setup.ps1/sh | 100% scripts | 0% scripts (deprecated) |

---

## 13. Riscos e mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Cursor muda formato de skills/rules | Alto | Versionar harness pack; testes de snapshot |
| Duplicação com setup.ps1/sh | Médio | Deprecar scripts; CLI delega ao template engine |
| Gate too strict — friction | Médio | `--skip-gate` para solo dev; gates configuráveis |
| Harness sync sobrescreve customizações | Alto | Merge strategy; `harness diff` antes de sync |
| Go + .NET — dois stacks | Baixo | CLI isolada em repo próprio |
| Agentes ignoram workflow | Médio | Context bundle + rules enforce Plan Mode |

---

## 14. Dependências

| Dependência | Obrigatória | Uso |
|-------------|-------------|-----|
| .NET SDK 10+ | Sim (create/verify) | template + build + test |
| Git | Recomendado | init, flow tracking |
| gh CLI | Opcional | review pr |
| Docker | Opcional | dev command |
| make | Opcional | verify (fallback para dotnet direto) |
| Cursor / Claude Code | Opcional | execução das fases |

---

## 15. Questões em aberto

| # | Questão | Decisão pendente |
|---|---------|------------------|
| Q1 | Repo separado (`product-cli`) ou `src/Tools/ProductCli/`? | Repo separado recomendado |
| Q2 | Nome final do binário: `product`, `pt`, `product-harness`? | `product` |
| Q3 | Sync merge: overwrite vs merge vs prompt? | Prompt + diff default |
| Q4 | TDD template: reutilizar `module-design-template.md` integral ou versão feature-scoped? | Feature-scoped derivado |
| Q5 | Human approve obrigatório em PRD e TDD? | Sim para teams; `--auto` para solo |
| Q6 | Publicar harness pack separado do binário? | v2 — embed na v1 |

---

## 16. Referências internas

| Documento | Uso na CLI |
|-----------|------------|
| `.template.config/template.json` | Template .NET orquestrado |
| `.cursor/plans/_template.md` | Base do template Spec |
| `docs/templates/module-design-template.md` | Base do template TDD |
| `.cursor/skills/new-feature/SKILL.md` | Skill Execute referência |
| `.cursor/skills/pr-review/SKILL.md` | Skill Review referência |
| `AGENTS.md` | Verification gate + workflow rules |
| `.claude/settings.json` | Hooks Claude |
| `Makefile` | Verify gate |
| `.agents/checklists/new-feature.md` | Checklist Execute |

---

## 17. Aprovação

| Papel | Nome | Data | Status |
|-------|------|------|--------|
| Product Owner | | | Pendente |
| Tech Lead | | | Pendente |
| Harness Maintainer | | | Pendente |

---

## Changelog deste documento

| Versão | Data | Alteração |
|--------|------|-----------|
| 1.0 | 2026-07-27 | Versão inicial — PRD → TDD → Spec → Execute → Review |
