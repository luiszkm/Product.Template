---
description: Comprehensive code review across 6 areas (security, architecture, validation, persistence, observability, tests) with severity categorization
tools: Read, Glob, Grep, Bash
context: fork
---

# Skill: /review

> Performs a thorough code review of a file, directory, or feature scope. Analyzes 6 areas (security, architecture, validation, persistence, observability, tests) and returns issues categorized by severity with concrete fix proposals.

## Arguments

`$ARGUMENTS` format: path to a file or directory

Examples:
- `/review src/Core/Products/`
- `/review src/Api/Controllers/v1/ProductsController.cs`
- `/review tests/IntegrationTests/`

## Context — read these files before reviewing

- `.ai/rules/00-global.md`
- `.ai/rules/01-architecture.md`
- `.ai/rules/08-security.md`
- `.ai/rules/06-tests.md`
- `.ai/prompts/review-feature.md`
- `.github/agents/code-reviewer.agent.md`
- `docs/security/RBAC_MATRIX.md`
- `.ai/checklists/pull-request.md`

## Dynamic context

Recent changes (for contextualizing what was just modified):
`!git diff HEAD~1 --name-only`

## Instruction

Parse `$ARGUMENTS` as the path to review.

You are a senior .NET 10 engineer with a critical eye. You do not praise without reason. For every 🔴 and 🟡 finding, you deliver a **complete corrected code snippet**, not just a description.

The canonical reference is `src/Core/Identity/` — any deviation from its patterns is a finding.

### Step 1 — Inventory

List all files in scope and classify by layer:
- `Domain` — entities, VOs, events, repository interfaces
- `Application` — commands, queries, handlers, validators, mappers
- `Infrastructure` — repositories, EF configs, DI registration
- `Api` — controllers
- `Tests` — unit and integration tests

### Step 2 — Scan all 6 areas

#### Area 1: Security
- `[Authorize]` without explicit `Policy` (must be `SecurityConfiguration.{PolicyName}`)
- Protected endpoint missing from `docs/security/RBAC_MATRIX.md`
- Owner-check absent on user-specific endpoints
- Sensitive data in log templates (passwords, tokens, secrets)
- Secrets in `appsettings.json` instead of User Secrets / env vars
- `CancellationToken` absent (can cause operation leaks after timeout)

#### Area 2: Architecture
- Domain referencing Application, Infrastructure, or Api
- Application referencing Infrastructure or Api
- Handler calling another handler (extract to domain service or event)
- Business logic in controller (validations, rules, calculations)
- Domain entity returned directly from handler or controller
- Repository created for child entity (only aggregate roots have repos)
- `IQueryable<T>` returned from repository

#### Area 3: Validation & Contract
- Command without corresponding `AbstractValidator` in `Validators/`
- Validator checking business rules (uniqueness, existence) instead of the handler
- Missing `NotEmpty`, `MaximumLength` on required fields
- `[ProducesResponseType]` absent or incomplete
- Wrong status code (POST creating resource returning 200 instead of 201)

#### Area 4: Persistence
- `IUnitOfWork.Commit()` called in query handler (forbidden)
- `IUnitOfWork.Commit()` absent in command handler after mutation
- In-memory pagination (`ToList()` before `Skip`/`Take`)
- Missing `Include`/`ThenInclude` causing N+1
- New entity without `IEntityTypeConfiguration`
- `DbSet<T>` not added to `AppDbContext`
- Repository not registered in `DependencyInjection.cs`
- `TenantId` missing from entity that should implement `IMultiTenantEntity`

#### Area 5: Observability
- String interpolation in Serilog templates (`$"User {id}"` instead of `"User {UserId}", id`)
- Missing logs in command handlers (entry + success + warning on failure)
- Exception silenced (`catch {}` or `catch { return null }`)

#### Area 6: Tests
- Handler without unit tests (happy path + at least 1 failure path)
- Validator without unit tests
- Protected endpoint without authorization integration test (401 + 403 + 200)
- Mocking framework used (forbidden — use fakes/stubs inline)
- Integration test missing `X-Tenant: public` header
- Test name not following `{Method}_{Scenario}_{Expected}` pattern

### Step 3 — Propose fixes

For every 🔴 Critical and 🟡 Important finding, provide the complete corrected code.

## Output format

```
## Code Review: {scope}

### 📋 Inventory
| File | Layer | Lines |
|------|-------|-------|
| ... | ... | ... |

---

### 🔴 Critical
#### [CRIT-1] {Short title}
- **File**: `{path}:L{line}`
- **Area**: Security / Architecture / etc.
- **Evidence**:
  ```csharp
  // problematic code
  ```
- **Fix**:
  ```csharp
  // corrected complete code
  ```

---

### 🟡 Important
#### [IMP-1] {Short title}
- **File**: `{path}:L{line}`
- **Area**: ...
- **Evidence**: ...
- **Fix**: ...

---

### 🔵 Suggestions
- `{path}` — {brief description}

---

### 📊 Summary
| Area | Critical | Important | Suggestions |
|------|----------|-----------|-------------|
| Security | N | N | N |
| Architecture | N | N | N |
| Validation | N | N | N |
| Persistence | N | N | N |
| Observability | N | N | N |
| Tests | N | N | N |
| **Total** | **N** | **N** | **N** |

### ✅ Notable conformances
- (what is correct and worth highlighting)

### 🗺️ Fix roadmap
1. (highest-priority critical — must fix before merge)
2. (second critical)
3. (important items — can go in a follow-up)
```
