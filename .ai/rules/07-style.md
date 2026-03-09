# 07 — Code Style

## General

- **Language version**: `latest` (set in `Directory.Build.props`).
- **Nullable reference types**: enabled globally.
- **Implicit usings**: enabled globally.
- **File-scoped namespaces**: preferred (`namespace X.Y.Z;`).
- **One type per file** (exception: small related records in the same file, e.g., command + output).
- Enforced by `.editorconfig` — do not override.

## Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Class / Record / Struct | PascalCase | `RegisterUserCommand` |
| Interface | `I` + PascalCase | `IUserRepository` |
| Method | PascalCase | `GetByEmailAsync` |
| Property | PascalCase | `FirstName` |
| Private field | `_camelCase` | `_userRepository` |
| Local variable | camelCase | `userOutput` |
| Constant | PascalCase | `MaxRetryCount` |
| Enum value | PascalCase | `TenantIsolationMode.SharedDb` |
| Type parameter | `T` + PascalCase | `TResponse` |
| Async method | Suffix `Async` | `GetByIdAsync` |

## Async/Await

- Every async method returns `Task` or `Task<T>`.
- Every async method accepts `CancellationToken cancellationToken` as the **last parameter**.
- Always forward `cancellationToken` to downstream calls.
- Never use `.Result` or `.Wait()` — always `await`.
- Use `ValueTask<T>` only when benchmarks justify it.

## Records & DTOs

- All DTOs are `record` types (positional syntax preferred).
- Command records: `public record {Verb}{Noun}Command(...) : ICommand<{Output}>`.
- Query records: `public record {Get|List}{Noun}Query(...) : IQuery<{Output}>`.
- Output records: `public record {Noun}Output(...)`.

## Classes

- Prefer `sealed` for classes that are not designed for inheritance.
- Maximum ~200 lines per class (handlers, repos). Split if larger.
- Dependency injection via **constructor injection** only.
- Primary constructors are acceptable for simple classes (e.g., middleware).

## Comments

- **No trivial comments** (e.g., `// Get user` above `GetUser()`).
- **Do** add XML doc comments (`///`) on public API controller actions for OpenAPI generation.
- **Do** add comments for non-obvious business rules in domain entities.
- **Use** `// TODO:` for known technical debt — include a brief description.

## Error Handling

- Never swallow exceptions silently.
- Throw specific exceptions (`NotFoundException`, `BusinessRuleException`, `DomainException`).
- Never catch `Exception` in handlers — let the global filter handle it.
- Use guard clauses at method entry: `ArgumentNullException.ThrowIfNull(...)`.

## LINQ

- Prefer method syntax over query syntax.
- Avoid `Count() > 0` — use `Any()`.
- Avoid multiple enumerations — materialize with `ToList()` or `ToArray()` when needed.

## Strings

- Use `string.IsNullOrWhiteSpace()` for validation (not `IsNullOrEmpty`).
- Use string interpolation `$"..."` for log message templates only when Serilog structured logging is not applicable.
- For Serilog, use **structured templates**: `_logger.LogInformation("User {UserId} created", userId)`.

