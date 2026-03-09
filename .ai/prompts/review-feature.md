# Prompt: Review Feature

> Use this prompt for AI-assisted code review of a feature implementation.

---

## Context Files
- `.ai/rules/00-global.md` through `12-folder-structure.md`
- `.ai/checklists/pull-request.md`

## Instruction

Review the following feature implementation for compliance with this template's architecture.

Check each of these areas:

### Architecture
- [ ] Files are in the correct folders per `12-folder-structure.md`.
- [ ] Layer dependencies are correct per `01-architecture.md`.
- [ ] No domain references to Infrastructure or Api projects.

### Domain
- [ ] Entities inherit from `Entity` or `AggregateRoot`.
- [ ] Entities implement `IMultiTenantEntity`.
- [ ] Constructors are private; factory methods are used.
- [ ] Invariants are enforced in the domain, not in handlers.

### Application
- [ ] Commands have validators.
- [ ] Handlers don't call other handlers.
- [ ] Handlers return DTOs, not entities.
- [ ] Queries don't call `IUnitOfWork.Commit()`.
- [ ] `CancellationToken` is passed everywhere.

### API
- [ ] Controllers are thin — no business logic.
- [ ] `[Authorize(Policy = "...")]` with explicit policy on all protected endpoints.
- [ ] `[ProducesResponseType]` declared for all status codes.
- [ ] `RBAC_MATRIX.md` updated for new/changed endpoints.

### Infrastructure
- [ ] Repository doesn't return `IQueryable`.
- [ ] EF configuration exists for new entities.
- [ ] New services registered in `DependencyInjection.cs`.

### Tests
- [ ] Handler has unit tests (happy + failure paths).
- [ ] Validator has unit tests.
- [ ] Protected endpoints have authorization integration tests.

### Style
- [ ] Naming follows `11-naming.md`.
- [ ] File-scoped namespaces used.
- [ ] No magic strings — use constants.
- [ ] Structured logging (no string interpolation in log templates).

## Output Format

```
## Review Summary

### ✅ Compliant
- (list what's correct)

### ⚠️ Warnings
- (list minor issues)

### ❌ Violations
- (list blocking issues with file paths and line references)

### Suggested Fixes
- (concrete code changes to fix violations)
```

