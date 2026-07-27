# Patterns

> Canonical pattern documentation for AI agents and developers.
> Prefer these docs over reading live code in `src/Core/Identity/`.

## Purpose

Each file describes **one pattern** with structure, invariants, and an annotated snippet.
Skills and checklists reference this folder instead of pointing to specific source files.

## Planned patterns

| Pattern | File | Status |
|---------|------|--------|
| Aggregate Root | `domain-aggregate.md` | Planned |
| Value Object | `domain-value-object.md` | Planned |
| Domain Event | `domain-event.md` | Planned |
| Command Handler | `command-handler.md` | Planned |
| Query Handler | `query-handler.md` | Planned |
| Validator | `validator.md` | Planned |
| Repository | `repository.md` | Planned |
| EF Configuration | `ef-configuration.md` | Planned |
| Controller Endpoint | `controller-endpoint.md` | Planned |
| Unit Test (Handler) | `unit-test-handler.md` | Planned |
| Integration Test (Auth) | `integration-test-auth.md` | Planned |

## How agents should use patterns

1. Read the relevant `.cursor/rules/{layer}.mdc` for constraints.
2. Read the matching pattern from this folder for structure and examples.
3. Validate output against `.agents/checklists/`.
4. Use `src/Core/Identity/` only when the pattern doc is insufficient or for drift checks.
