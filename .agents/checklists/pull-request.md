# Checklist: Pull Request

> Use this checklist before submitting or reviewing a PR.

## Architecture Compliance

- [ ] No layer dependency violations (Domain → App → Infra → Api)
- [ ] No circular references between modules
- [ ] New files are in the correct folder per `.ai/rules/12-folder-structure.md`
- [ ] Naming follows `.ai/rules/11-naming.md`

## Code Quality

- [ ] No compiler warnings (treat warnings as errors in Release)
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes all tests
- [ ] No `TODO` comments introduced without tracking
- [ ] No commented-out code
- [ ] No unused `using` directives
- [ ] `.editorconfig` formatting respected

## Domain Layer

- [ ] Entities use private constructors + `Create()` factories
- [ ] Invariants enforced in the domain
- [ ] No infrastructure dependencies (EF, Http, etc.)

## Application Layer

- [ ] Every command has a FluentValidation validator
- [ ] Handlers return DTOs, not entities
- [ ] `CancellationToken` forwarded in all async calls
- [ ] No handler calls another handler

## API Layer

- [ ] No business logic in controllers
- [ ] All protected endpoints use `[Authorize(Policy = "...")]`
- [ ] `[ProducesResponseType]` declared for all status codes
- [ ] `RBAC_MATRIX.md` updated for new/changed endpoints

## Infrastructure Layer

- [ ] New services registered in `DependencyInjection.cs`
- [ ] EF configurations exist for new entities
- [ ] Repositories don't return `IQueryable`

## Security

- [ ] No secrets in source code
- [ ] No sensitive data in logs
- [ ] Input validated before processing
- [ ] Authorization policies are explicit

## Tests

- [ ] Unit tests for new handlers (happy + failure paths)
- [ ] Validator tests for new validators
- [ ] Integration tests for new protected endpoints
- [ ] Architecture tests pass
- [ ] Test naming follows `{Method}_{Scenario}_{Expected}` convention

## Documentation

- [ ] `RBAC_MATRIX.md` updated (if endpoints changed)
- [ ] XML doc comments on public API actions
- [ ] `.ai/rules/12-folder-structure.md` updated (if new folder patterns introduced)

## Observability

- [ ] Structured logging in new handlers
- [ ] No string interpolation in log templates
- [ ] Metrics added for significant business operations (if applicable)

