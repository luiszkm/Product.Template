# MEMORY.md
Last updated: 2026-05-24
Next review: 2026-06-07

## Module status

| Module | Status | Next step |
|--------|--------|-----------|
| Identity | ✅ Done (canonical reference module) | — |
| Authorization | ✅ Done | — |
| Tenants | ✅ Done | — |

## Technical decisions

| Date | Decision | Reason |
|------|----------|--------|
| 2026-05-23 | Rules migrated from `.ai/rules/NN-foo.md` to `.cursor/rules/{slug}.mdc` | Refactor commits aba7b9e / 419dc41 |
| 2026-05-23 | No mocking frameworks — inline fakes/stubs only | Prior mock/prod divergence caused undetected migration failure |
| 2026-05-23 | Identity is the canonical reference module | First complete module with all patterns implemented |

## Active blockers

(none)

## Session notes

- 2026-05-23: Evaluated current enterprise architecture to derive a Minimal API pattern for MVP/POC use cases. No code changes or architectural decisions applied to repository.
- 2026-05-24: Full harness audit completed. Score: 84.5% → targeting ≥90%. Created: `global-security.mdc`, `openapi-contracts.mdc`, `csharp-patterns.mdc`, `test-writer/SKILL.md`. Updated: `AGENTS.md` (subagent coordination section). Translated MEMORY.md to English.
