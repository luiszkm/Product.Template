# Cursor Rules Refactor — Design Spec

**Date:** 2026-05-23  
**Scope:** `.cursor/rules/` — convert 16 rule files to proper `.mdc` format with frontmatter

---

## Problem

All 16 rules in `.cursor/rules/` are either `.md` or lack frontmatter. Without `.mdc` + frontmatter, Cursor cannot automatically apply rules — every rule is effectively **Manual** (must be `@`-mentioned). Layer-specific rules (domain, api, tests) never auto-attach to relevant files.

Additional issue: `00-global.mdc` references `Read .ai/rules/` but those files are deleted from the repo.

---

## Goals

1. Convert all files to `.mdc` with correct frontmatter.
2. Assign each rule the right **application type**: Always, Auto Attached, or Agent Requested.
3. Rename files to descriptive slugs (remove numeric prefix).
4. Fix dead internal reference in `global.mdc`.

---

## Non-Goals

- Consolidating or merging rules.
- Changing rule content beyond the one dead reference fix.
- Subdirectory organization.

---

## Rule Typing Strategy

### Always (1 file)

| File | Reason |
|------|--------|
| `style.mdc` | Code style applies to every file edited |

Frontmatter:
```yaml
---
alwaysApply: true
---
```

### Auto Attached (8 files)

Attached automatically when matching files are open in context.

| File | Globs |
|------|-------|
| `domain.mdc` | `src/Core/**/*.Domain/**, src/Shared/Kernel.Domain/**` |
| `application.mdc` | `src/Core/**/*.Application/**, src/Shared/Kernel.Application/**` |
| `infrastructure.mdc` | `src/Core/**/*.Infrastructure/**, src/Shared/Kernel.Infrastructure/**` |
| `api.mdc` | `src/Api/**` |
| `tests.mdc` | `tests/**` |
| `docker.mdc` | `Dockerfile*, docker-compose*, .dockerignore` |
| `cicd.mdc` | `.github/**, azure-pipelines*` |
| `ai-features.mdc` | `src/**/Ai/**` |

Frontmatter pattern:
```yaml
---
globs: <pattern>
alwaysApply: false
---
```

### Agent Requested (7 files)

Agent reads description and decides whether the rule is relevant.

| File | Description field |
|------|------------------|
| `global.mdc` | Stack overview, core principles, universal rules for all agents working in this repo |
| `architecture.mdc` | Clean Architecture layer dependencies, forbidden references, module structure, patterns (CQRS, Repository, UoW, Domain Events) |
| `naming.mdc` | Naming conventions for commands, queries, handlers, validators, DTOs, repositories, EF configs, domain events |
| `agent-behavior.mdc` | How AI agents should operate in this repo — reading rules, NuGet policy, file creation, testability requirements |
| `security.mdc` | Security rules — authentication, RBAC policies, input validation, secrets handling, tenant isolation |
| `observability.mdc` | Structured logging (Serilog), OpenTelemetry, correlation IDs, health checks, metrics |
| `folder-structure.mdc` | Repository folder structure for modules, tests, and shared kernel |

Frontmatter pattern:
```yaml
---
description: <description>
alwaysApply: false
---
```

---

## File Rename Map

| Old filename | New filename |
|---|---|
| `00-global.mdc` | `global.mdc` |
| `01-architecture.md` | `architecture.mdc` |
| `02-domain.md` | `domain.mdc` |
| `03-application.md` | `application.mdc` |
| `04-infrastructure.md` | `infrastructure.mdc` |
| `05-api.md` | `api.mdc` |
| `06-tests.md` | `tests.mdc` |
| `07-style.md` | `style.mdc` |
| `08-security.md` | `security.mdc` |
| `09-observability.md` | `observability.mdc` |
| `10-agent-behavior.md` | `agent-behavior.mdc` |
| `11-naming.md` | `naming.mdc` |
| `12-folder-structure.md` | `folder-structure.mdc` |
| `13-docker.md` | `docker.mdc` |
| `14-cicd.md` | `cicd.mdc` |
| `15-ai-features.md` | `ai-features.mdc` |

---

## Content Changes

Only one content change (beyond adding frontmatter):

**File:** `global.mdc`  
**Line 43:** `Read .ai/rules/ before generating code.`  
**Fix:** Change to `Read .cursor/rules/ before generating code.`

**Reason:** `.ai/rules/` was deleted from the repo (confirmed in git status). Reference is dead.

---

## Implementation Steps

1. For each file: create new `.mdc` file with frontmatter prepended to existing content.
2. Delete old `.md` files.
3. Rename `00-global.mdc` → `global.mdc` (already `.mdc`, just needs frontmatter + content fix).
4. Fix dead reference in `global.mdc`.
5. Verify all 16 new `.mdc` files exist and old files are removed.
