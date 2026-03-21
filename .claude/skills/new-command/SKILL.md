---
description: Scaffold a CQRS Command + Handler + Validator + Unit Tests
tools: Read, Edit, Write, Glob, Grep
disable-model-invocation: true
---

# Skill: /new-command

> Creates a complete CQRS command slice: command record, handler, FluentValidation validator, and unit tests.

## Arguments

`$ARGUMENTS` format: `{MODULE_NAME} {COMMAND_NAME}`

Example: `/new-command Identity RegisterUser`

Where `{COMMAND_NAME}` is the verb+noun without the `Command` suffix (e.g., `RegisterUser` → generates `RegisterUserCommand`).

## Context — read these files before generating any code

- `.ai/rules/03-application.md`
- `.ai/rules/11-naming.md`
- `.ai/rules/12-folder-structure.md`
- `.ai/prompts/create-command.md`
- `src/Core/Identity/Identity.Application/` — canonical reference

## Instruction

Parse `$ARGUMENTS` as `MODULE_NAME` (first token) and `COMMAND_NAME` (second token, without `Command` suffix).

Create these files:

### 1. Command record
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Handlers/{COMMAND_NAME}/Commands/{COMMAND_NAME}Command.cs`

- `record` type implementing `ICommand` (void) or `ICommand<{OUTPUT_TYPE}>` (with return)
- Use file-scoped namespace: `Product.Template.Core.{MODULE_NAME}.Application.Handlers.{COMMAND_NAME}.Commands`

### 2. Command Handler
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Handlers/{COMMAND_NAME}/{COMMAND_NAME}CommandHandler.cs`

- Implement `ICommandHandler<{COMMAND_NAME}Command>` or `ICommandHandler<{COMMAND_NAME}Command, {OUTPUT_TYPE}>`
- Inject: repository, `IUnitOfWork`, `ILogger<{COMMAND_NAME}CommandHandler>`
- Call `await _unitOfWork.Commit(cancellationToken)` after all mutations
- Throw `NotFoundException` for missing entities
- Throw `BusinessRuleException` for business rule violations
- Log: `Information` for success, `Warning` for business failures
- Never use string interpolation in log templates

### 3. Validator
**Path:** `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Validators/{COMMAND_NAME}CommandValidator.cs`

- Extend `AbstractValidator<{COMMAND_NAME}Command>`
- Validate: required fields (`NotEmpty`), max lengths (`MaximumLength`), formats
- Do NOT validate business rules (uniqueness, existence) — that is the handler's job

### 4. Unit Tests — Handler
**Path:** `tests/UnitTests/{COMMAND_NAME}/{COMMAND_NAME}CommandHandlerTests.cs`

- Test naming: `Handle_{Scenario}_{ExpectedResult}`
- At minimum: happy path, not-found (throws `NotFoundException`), business rule violation
- No mocking frameworks — use inline fakes/stubs (sealed classes at bottom of file)
- Use `NullLogger<T>.Instance` for loggers

### 5. Unit Tests — Validator
**Path:** `tests/UnitTests/{COMMAND_NAME}/{COMMAND_NAME}CommandValidatorTests.cs`

- Test that required fields produce validation errors when empty
- Test that valid input passes

## Output format

For each file:
```
### File: `{full/path/to/file.cs}`
{complete file content with correct namespaces}
```
