# Project facts

> Single source of truth for facts specific to *this* instantiation of the template.
> Rules and skills under `.cursor/` must not hardcode these — they should point here instead.
> When bootstrapping a new project from this template, this is the one file to update.

| Fact | Value |
|------|-------|
| Reference module (canonical pattern source) | `src/Core/Identity/` |
| Solution / project name | `Product.Template` |
| Docker image name | `product-template-api` |
| Database name | `product_template` |
| RBAC matrix | `docs/security/RBAC_MATRIX.md` |

## Test projects

| Project | Path |
|---------|------|
| Unit | `tests/UnitTests` |
| Integration | `tests/IntegrationTests` |
| Architecture | `tests/ArchitectureTests` |
| E2E | `tests/E2ETests` |

**Convention for tooling:** the architecture-test project directory must contain
`Architecture` (case-insensitive) in its name under `tests/` — the commit hook in
`.claude/settings.json` discovers it by this convention rather than a hardcoded path.

## Reference module

`src/Core/Identity/` is the living implementation agents check against for drift
(structure, naming, layering). Prefer `.agents/patterns/` (11/11 published) and
`.agents/examples/` for agent context; fall back to reading the reference module
directly only when those docs are insufficient.

To change the reference module for a new project: update the row above, then
`.agents/examples/README.md`'s reference table.
