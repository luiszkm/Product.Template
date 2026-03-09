# Master Prompt

> Use this prompt to instruct an LLM/agent before it begins ANY work on this repository.

---

You are working on the **Product.Template** backend repository — a .NET 10 Clean Architecture template using DDD, CQRS, MediatR, FluentValidation, EF Core, Serilog, and OpenTelemetry.

## Mandatory Reading

Before generating any code, read and internalize these files in order:

1. `.ai/rules/00-global.md` — Stack, principles, universal rules.
2. `.ai/rules/01-architecture.md` — Layer boundaries and dependency rules.
3. `.ai/rules/12-folder-structure.md` — Where every file must go.
4. `.ai/rules/11-naming.md` — Naming conventions for every type.
5. `.ai/rules/10-agent-behavior.md` — How you must behave as an agent.
6. The **specific rule file** for the layer you are modifying (02–09).

## Reference Implementation

The **Identity module** (`src/Core/Identity/`) is the canonical example. When unsure about a pattern, look at how Identity implements it.

## Response Requirements

For every change you make:
1. Provide the **full file path** relative to the repository root.
2. Show the **complete file content** for new files.
3. For modifications, show only the changed sections with enough context.
4. List all files created and modified.
5. If you created an endpoint, show the RBAC_MATRIX.md update.
6. If you created a handler, show the corresponding test.

## Constraints

- Never violate the dependency rules in `01-architecture.md`.
- Never put business logic in controllers.
- Never return domain entities from API responses.
- Never use bare `[Authorize]` without a Policy.
- Always pass `CancellationToken`.
- Always create FluentValidation validators for commands.
- Always register new services in DependencyInjection.cs.

