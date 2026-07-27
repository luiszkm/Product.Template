# Patterns

> Canonical pattern documentation for AI agents and developers.
> Prefer these docs over reading live code in the reference module (see `project-facts.md`).

## Purpose

Each file describes **one pattern** with structure, invariants, and an annotated snippet.
Skills and checklists reference this folder instead of pointing to specific source files.

## Published patterns (11/11)

| Pattern | File | Layer |
|---------|------|-------|
| Aggregate Root | `domain-aggregate.md` | Domain |
| Value Object | `domain-value-object.md` | Domain |
| Domain Event | `domain-event.md` | Domain |
| Command Handler | `command-handler.md` | Application |
| Query Handler | `query-handler.md` | Application |
| Validator | `validator.md` | Application |
| Repository | `repository.md` | Infrastructure |
| EF Configuration | `ef-configuration.md` | Infrastructure |
| Controller Endpoint | `controller-endpoint.md` | API |
| Unit Test (Handler) | `unit-test-handler.md` | Tests |
| Integration Test (Auth) | `integration-test-auth.md` | Tests |

## How agents should use patterns

1. Read the relevant `.cursor/rules/{layer}.mdc` for constraints.
2. Read the matching pattern from this folder for structure and examples.
3. Validate output against `.agents/checklists/`.
4. Use the reference module (see `project-facts.md`) only when the pattern doc is insufficient or for drift checks.

## Vertical slice reading order

For a full feature (`/new-feature`), read in this order:

```
domain-aggregate → domain-value-object → domain-event
→ command-handler → query-handler → validator
→ repository → ef-configuration
→ controller-endpoint
→ unit-test-handler → integration-test-auth
```
