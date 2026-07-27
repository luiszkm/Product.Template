---
name: new-module
version: 1
description: "DDD design session for a new bounded context — produces module structure, entity list, and aggregate boundaries. Design-only, no code generated. TRIGGER: \"new module\", \"new bounded context\", \"add a module for\", \"design module\", \"I need a new domain for\". SKIP: use new-feature to scaffold code inside an existing module; use new-entity for a single entity."
tools: Read, Glob, Grep
disable-model-invocation: true
---

# Skill: /new-module

> Conducts a mini-event storming session and delivers a complete DDD bounded context design document for a new module. No code is written — the output is a specification ready for `/new-feature` or `/new-entity`.

## Arguments

`$ARGUMENTS` format: `{MODULE_NAME}`

Example: `/new-module Orders`

## Context — invariants (rules)

- `.cursor/rules/domain.mdc`
- `.cursor/rules/application.mdc`
- `.cursor/rules/architecture.mdc`
- `.cursor/rules/folder-structure.mdc`
- `docs/templates/module-design-template.md` — output structure
- `docs/templates/module-design-example-orders.md` — filled example
- `docs/guides/module-designer-quickstart.md` — invocation guide
- `src/Core/Identity/` — canonical reference implementation

## Context — invoke if needed (skills)

- `/repo-layout` — when verifying folder/project structure for the new module

## Instruction

Parse `$ARGUMENTS` as `MODULE_NAME`.

You are a Domain-Driven Design expert. You do **not** write code — you design, document, and validate the module before a single line is implemented.

Follow this 5-phase process:

### Phase 1 — Domain Discovery (Simplified Event Storming)

Identify:
- **Domain Events** (past-tense: `{Noun}{PastVerb}Event`) — "What happens in this context?"
- **Commands** (`{Verb}{Noun}Command`) — "What do users/systems do?"
- **Queries** (`{Get|List}{Noun}Query`) — "What information is queried?"
- **Policies** (`When{Event}Then{Action}Policy`) — "When X happens, what triggers automatically?"

### Phase 2 — Bounded Context Canvas

Produce the full canvas:
- Module name, purpose, ubiquitous language
- Responsibilities, aggregates, entities, value objects
- Dependencies on other contexts (upstream/downstream)
- Published and consumed events

### Phase 3 — Aggregate Design

For each aggregate:
- Identify the aggregate root
- List invariants (rules that NEVER can be violated)
- Child entities (with cardinality)
- Value objects
- Domain methods (factory + business methods)
- Events raised

### Phase 4 — Entity & Value Object Templates

For each entity: type, properties, invariants, factory method, business methods.
For each VO: purpose, immutability, validations.

### Phase 5 — Events & Policies

For each domain event: trigger, owner, payload, consumers.
For each policy: type (domain vs application), trigger event, action pseudocode.

## Validation checklist (before delivering)

- [ ] Aggregate root identified with clear invariants
- [ ] No direct references to other aggregate entities (only IDs)
- [ ] All entities implement `IMultiTenantEntity` and `ISoftDeletableEntity`
- [ ] All VOs are immutable (record with init-only)
- [ ] Domain event names are past-tense
- [ ] Ubiquitous language documented and consistent

## Output format

Deliver a structured design document following `docs/templates/module-design-template.md`, including:

1. Bounded Context Canvas
2. Aggregates (with invariants, methods, events)
3. Entities
4. Value Objects
5. Domain Events
6. Policies (event handlers)
7. Commands & Queries table
8. Folder structure
9. Implementation checklist
10. Next steps

Also include:
- **Mermaid class diagram** of aggregates
- **Mermaid sequence diagram** of the main command flow
