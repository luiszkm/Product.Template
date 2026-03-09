# Prompt: Create Command

> Scaffold a CQRS command with handler, validator, and test.

---

## Context Files
- `.ai/rules/03-application.md`
- `.ai/rules/11-naming.md`
- `.ai/rules/12-folder-structure.md`
- Reference: `src/Core/Identity/Identity.Application/Handlers/User/`

## Instruction

Create a command **`{COMMAND_NAME}`** in module **`{MODULE_NAME}`**.

### Files to Create

#### 1. Command Definition
- Path: `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Handlers/{Feature}/Commands/{COMMAND_NAME}.cs`
- Must be a `record` implementing `ICommand` (no return) or `ICommand<{OUTPUT_TYPE}>` (with return).

#### 2. Command Handler
- Path: `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Handlers/{Feature}/{COMMAND_NAME}Handler.cs`
- Implement `ICommandHandler<{COMMAND_NAME}>` or `ICommandHandler<{COMMAND_NAME}, {OUTPUT_TYPE}>`.
- Inject repository + `IUnitOfWork` + `ILogger<>`.
- Call `_unitOfWork.Commit(cancellationToken)` after state changes.
- Throw `NotFoundException` for missing entities.
- Throw `BusinessRuleException` for business rule violations.
- Log at `Information` for success, `Warning` for failures.

#### 3. Validator
- Path: `src/Core/{MODULE_NAME}/{MODULE_NAME}.Application/Validators/{COMMAND_NAME}Validator.cs`
- Extend `AbstractValidator<{COMMAND_NAME}>`.
- Validate shape: required fields, max lengths, formats.
- Do NOT validate business rules (uniqueness, existence) — that's the handler's job.

#### 4. Unit Tests
- Path: `tests/UnitTests/{Feature}/{COMMAND_NAME}HandlerTests.cs`
- Test at least:
  - Happy path (success).
  - Not found (throws `NotFoundException`).
  - Business rule violation (throws `BusinessRuleException`).
- Path: `tests/UnitTests/{Feature}/{COMMAND_NAME}ValidatorTests.cs`
  - Test required fields return validation errors.
  - Test valid input passes.

## Output Format
Provide complete files with correct namespaces.

