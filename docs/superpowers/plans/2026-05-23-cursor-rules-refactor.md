# Cursor Rules Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert all 16 `.cursor/rules/` files to proper `.mdc` format with frontmatter so Cursor auto-applies rules based on file context.

**Architecture:** Each file gets a YAML frontmatter block prepended. Rule type determines which frontmatter fields are used: `alwaysApply: true` for style, `globs:` for layer/infra files, `description:` for cross-cutting rules. Old numeric-prefixed files are deleted after new descriptive-slug files are created.

**Tech Stack:** Bash, Cursor `.mdc` rule format

---

## File Map

| Old | New | Type |
|-----|-----|------|
| `00-global.mdc` | `global.mdc` | Agent Requested |
| `01-architecture.md` | `architecture.mdc` | Agent Requested |
| `02-domain.md` | `domain.mdc` | Auto Attached |
| `03-application.md` | `application.mdc` | Auto Attached |
| `04-infrastructure.md` | `infrastructure.mdc` | Auto Attached |
| `05-api.md` | `api.mdc` | Auto Attached |
| `06-tests.md` | `tests.mdc` | Auto Attached |
| `07-style.md` | `style.mdc` | Always |
| `08-security.md` | `security.mdc` | Agent Requested |
| `09-observability.md` | `observability.mdc` | Agent Requested |
| `10-agent-behavior.md` | `agent-behavior.mdc` | Agent Requested |
| `11-naming.md` | `naming.mdc` | Agent Requested |
| `12-folder-structure.md` | `folder-structure.mdc` | Agent Requested |
| `13-docker.md` | `docker.mdc` | Auto Attached |
| `14-cicd.md` | `cicd.mdc` | Auto Attached |
| `15-ai-features.md` | `ai-features.mdc` | Auto Attached |

---

### Task 1: style.mdc — Always rule

**Files:**
- Create: `.cursor/rules/style.mdc`
- Delete: `.cursor/rules/07-style.md`

- [ ] **Step 1: Create style.mdc with alwaysApply frontmatter**

```bash
{
  printf -- '---\nalwaysApply: true\n---\n'
  cat .cursor/rules/07-style.md
} > .cursor/rules/style.mdc
```

- [ ] **Step 2: Verify frontmatter is present**

```bash
head -4 .cursor/rules/style.mdc
```

Expected output:
```
---
alwaysApply: true
---

```

- [ ] **Step 3: Delete old file**

```bash
rm .cursor/rules/07-style.md
```

- [ ] **Step 4: Commit**

```bash
git add .cursor/rules/style.mdc .cursor/rules/07-style.md
git commit -m "refactor(rules): convert style.mdc to alwaysApply"
```

---

### Task 2: Auto Attached — layer rules

**Files:**
- Create: `.cursor/rules/domain.mdc`, `application.mdc`, `infrastructure.mdc`, `api.mdc`
- Delete: `.cursor/rules/02-domain.md`, `03-application.md`, `04-infrastructure.md`, `05-api.md`

- [ ] **Step 1: Create domain.mdc**

```bash
{
  printf -- '---\nglobs: src/Core/**/*.Domain/**, src/Shared/Kernel.Domain/**\nalwaysApply: false\n---\n'
  cat .cursor/rules/02-domain.md
} > .cursor/rules/domain.mdc
```

- [ ] **Step 2: Create application.mdc**

```bash
{
  printf -- '---\nglobs: src/Core/**/*.Application/**, src/Shared/Kernel.Application/**\nalwaysApply: false\n---\n'
  cat .cursor/rules/03-application.md
} > .cursor/rules/application.mdc
```

- [ ] **Step 3: Create infrastructure.mdc**

```bash
{
  printf -- '---\nglobs: src/Core/**/*.Infrastructure/**, src/Shared/Kernel.Infrastructure/**\nalwaysApply: false\n---\n'
  cat .cursor/rules/04-infrastructure.md
} > .cursor/rules/infrastructure.mdc
```

- [ ] **Step 4: Create api.mdc**

```bash
{
  printf -- '---\nglobs: src/Api/**\nalwaysApply: false\n---\n'
  cat .cursor/rules/05-api.md
} > .cursor/rules/api.mdc
```

- [ ] **Step 5: Verify all four have globs frontmatter**

```bash
for f in domain application infrastructure api; do
  echo "=== $f.mdc ===" && head -4 .cursor/rules/$f.mdc
done
```

- [ ] **Step 6: Delete old files**

```bash
rm .cursor/rules/02-domain.md .cursor/rules/03-application.md \
   .cursor/rules/04-infrastructure.md .cursor/rules/05-api.md
```

- [ ] **Step 7: Commit**

```bash
git add .cursor/rules/
git commit -m "refactor(rules): add globs to layer rules (domain, application, infrastructure, api)"
```

---

### Task 3: Auto Attached — infra rules

**Files:**
- Create: `.cursor/rules/tests.mdc`, `docker.mdc`, `cicd.mdc`, `ai-features.mdc`
- Delete: `.cursor/rules/06-tests.md`, `13-docker.md`, `14-cicd.md`, `15-ai-features.md`

- [ ] **Step 1: Create tests.mdc**

```bash
{
  printf -- '---\nglobs: tests/**\nalwaysApply: false\n---\n'
  cat .cursor/rules/06-tests.md
} > .cursor/rules/tests.mdc
```

- [ ] **Step 2: Create docker.mdc**

```bash
{
  printf -- '---\nglobs: Dockerfile*, docker-compose*, .dockerignore\nalwaysApply: false\n---\n'
  cat .cursor/rules/13-docker.md
} > .cursor/rules/docker.mdc
```

- [ ] **Step 3: Create cicd.mdc**

```bash
{
  printf -- '---\nglobs: .github/**, azure-pipelines*\nalwaysApply: false\n---\n'
  cat .cursor/rules/14-cicd.md
} > .cursor/rules/cicd.mdc
```

- [ ] **Step 4: Create ai-features.mdc**

```bash
{
  printf -- '---\nglobs: src/**/Ai/**\nalwaysApply: false\n---\n'
  cat .cursor/rules/15-ai-features.md
} > .cursor/rules/ai-features.mdc
```

- [ ] **Step 5: Verify all four have globs frontmatter**

```bash
for f in tests docker cicd ai-features; do
  echo "=== $f.mdc ===" && head -4 .cursor/rules/$f.mdc
done
```

- [ ] **Step 6: Delete old files**

```bash
rm .cursor/rules/06-tests.md .cursor/rules/13-docker.md \
   .cursor/rules/14-cicd.md .cursor/rules/15-ai-features.md
```

- [ ] **Step 7: Commit**

```bash
git add .cursor/rules/
git commit -m "refactor(rules): add globs to infra rules (tests, docker, cicd, ai-features)"
```

---

### Task 4: Agent Requested — cross-cutting rules

**Files:**
- Create: `.cursor/rules/architecture.mdc`, `security.mdc`, `observability.mdc`, `naming.mdc`, `agent-behavior.mdc`, `folder-structure.mdc`
- Delete: `.cursor/rules/01-architecture.md`, `08-security.md`, `09-observability.md`, `10-agent-behavior.md`, `11-naming.md`, `12-folder-structure.md`

- [ ] **Step 1: Create architecture.mdc**

```bash
{
  printf -- '---\ndescription: Clean Architecture layer dependencies, forbidden references, module structure, patterns (CQRS, Repository, UoW, Domain Events)\nalwaysApply: false\n---\n'
  cat .cursor/rules/01-architecture.md
} > .cursor/rules/architecture.mdc
```

- [ ] **Step 2: Create security.mdc**

```bash
{
  printf -- '---\ndescription: Security rules — authentication, RBAC policies, input validation, secrets handling, tenant isolation\nalwaysApply: false\n---\n'
  cat .cursor/rules/08-security.md
} > .cursor/rules/security.mdc
```

- [ ] **Step 3: Create observability.mdc**

```bash
{
  printf -- '---\ndescription: Structured logging (Serilog), OpenTelemetry, correlation IDs, health checks, metrics\nalwaysApply: false\n---\n'
  cat .cursor/rules/09-observability.md
} > .cursor/rules/observability.mdc
```

- [ ] **Step 4: Create naming.mdc**

```bash
{
  printf -- '---\ndescription: Naming conventions for commands, queries, handlers, validators, DTOs, repositories, EF configs, domain events\nalwaysApply: false\n---\n'
  cat .cursor/rules/11-naming.md
} > .cursor/rules/naming.mdc
```

- [ ] **Step 5: Create agent-behavior.mdc**

```bash
{
  printf -- '---\ndescription: How AI agents should operate in this repo — reading rules, NuGet policy, file creation, testability requirements\nalwaysApply: false\n---\n'
  cat .cursor/rules/10-agent-behavior.md
} > .cursor/rules/agent-behavior.mdc
```

- [ ] **Step 6: Create folder-structure.mdc**

```bash
{
  printf -- '---\ndescription: Repository folder structure for modules, tests, and shared kernel\nalwaysApply: false\n---\n'
  cat .cursor/rules/12-folder-structure.md
} > .cursor/rules/folder-structure.mdc
```

- [ ] **Step 7: Verify all six have description frontmatter**

```bash
for f in architecture security observability naming agent-behavior folder-structure; do
  echo "=== $f.mdc ===" && head -3 .cursor/rules/$f.mdc
done
```

- [ ] **Step 8: Delete old files**

```bash
rm .cursor/rules/01-architecture.md .cursor/rules/08-security.md \
   .cursor/rules/09-observability.md .cursor/rules/10-agent-behavior.md \
   .cursor/rules/11-naming.md .cursor/rules/12-folder-structure.md
```

- [ ] **Step 9: Commit**

```bash
git add .cursor/rules/
git commit -m "refactor(rules): add description to agent-requested rules"
```

---

### Task 5: global.mdc — rename + frontmatter + content fix

**Files:**
- Create: `.cursor/rules/global.mdc`
- Delete: `.cursor/rules/00-global.mdc`

This task also fixes the dead reference on line 43 of `00-global.mdc`: `.ai/rules/` → `.cursor/rules/`.

- [ ] **Step 1: Create global.mdc with frontmatter and fixed content**

```bash
{
  printf -- '---\ndescription: Stack overview, core principles, universal rules for all agents working in this repo\nalwaysApply: false\n---\n'
  sed 's|\.ai/rules/|.cursor/rules/|g' .cursor/rules/00-global.mdc
} > .cursor/rules/global.mdc
```

- [ ] **Step 2: Verify frontmatter and content fix**

```bash
head -5 .cursor/rules/global.mdc
grep -n "cursor/rules" .cursor/rules/global.mdc
```

Expected: frontmatter on lines 1-4, then `Read \`.cursor/rules/\`` on line ~47. No remaining `\.ai/rules/` references.

- [ ] **Step 3: Confirm no dead .ai/rules reference remains**

```bash
grep -n "\.ai/rules" .cursor/rules/global.mdc
```

Expected: no output.

- [ ] **Step 4: Delete old file**

```bash
rm .cursor/rules/00-global.mdc
```

- [ ] **Step 5: Commit**

```bash
git add .cursor/rules/global.mdc .cursor/rules/00-global.mdc
git commit -m "refactor(rules): rename global rule, add frontmatter, fix dead .ai/rules reference"
```

---

### Task 6: Final verification

- [ ] **Step 1: Confirm no old .md files remain**

```bash
ls .cursor/rules/*.md 2>&1
```

Expected: `ls: cannot access '.cursor/rules/*.md': No such file or directory` (or similar — no files listed).

- [ ] **Step 2: Confirm all 16 .mdc files exist**

```bash
ls .cursor/rules/*.mdc | sort
```

Expected (16 files):
```
.cursor/rules/agent-behavior.mdc
.cursor/rules/ai-features.mdc
.cursor/rules/api.mdc
.cursor/rules/application.mdc
.cursor/rules/architecture.mdc
.cursor/rules/cicd.mdc
.cursor/rules/docker.mdc
.cursor/rules/domain.mdc
.cursor/rules/folder-structure.mdc
.cursor/rules/global.mdc
.cursor/rules/infrastructure.mdc
.cursor/rules/naming.mdc
.cursor/rules/observability.mdc
.cursor/rules/security.mdc
.cursor/rules/style.mdc
.cursor/rules/tests.mdc
```

- [ ] **Step 3: Confirm style.mdc is the only alwaysApply rule**

```bash
grep -l "alwaysApply: true" .cursor/rules/*.mdc
```

Expected: `.cursor/rules/style.mdc` only.

- [ ] **Step 4: Confirm 8 auto-attached rules have globs**

```bash
grep -l "^globs:" .cursor/rules/*.mdc | sort
```

Expected (8 files):
```
.cursor/rules/ai-features.mdc
.cursor/rules/api.mdc
.cursor/rules/application.mdc
.cursor/rules/cicd.mdc
.cursor/rules/docker.mdc
.cursor/rules/domain.mdc
.cursor/rules/infrastructure.mdc
.cursor/rules/tests.mdc
```

- [ ] **Step 5: Confirm 7 agent-requested rules have description**

```bash
grep -l "^description:" .cursor/rules/*.mdc | sort
```

Expected (7 files):
```
.cursor/rules/agent-behavior.mdc
.cursor/rules/architecture.mdc
.cursor/rules/folder-structure.mdc
.cursor/rules/global.mdc
.cursor/rules/naming.mdc
.cursor/rules/observability.mdc
.cursor/rules/security.mdc
```
