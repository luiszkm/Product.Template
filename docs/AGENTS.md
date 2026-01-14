# Repository Guidelines

## Project Structure & Module Organization
- `src/Api/Api`: ASP.NET Core API (Program, Controllers, Configurations, appsettings*.json, Dockerfile).
- `src/Shared/Kernel.Domain`: Domain entities, exceptions, seedwork.
- `src/Shared/Kernel.Application`: Application layer (behaviors, events, messaging, data, exceptions).
- `src/Shared/Kernel.Infrastructure`: Infrastructure (persistence and adapters).
- `src/Modules/`: Place feature modules here (e.g., `src/Modules/Orders`). Each module should be a Class Library referenced by the API and/or Kernel layers as needed.

## Build, Test, and Development Commands
- `dotnet restore`: Restore NuGet packages for the solution.
- `dotnet build Product.Template.sln -c Debug`: Compile all projects.
- `dotnet run --project src/Api/Api`: Run the API locally on the default Kestrel port.
- `dotnet publish src/Api/Api -c Release -o out`: Produce a self‑contained build artifact for deployment.
- `dotnet test`: Run solution tests (add test projects under `tests/` — see below).

## Coding Style & Naming Conventions
- Indentation: 4 spaces; UTF‑8 files; one type per file.
- Naming: PascalCase for types/methods; camelCase for locals/params; `_camelCase` for private fields; `UPPER_SNAKE_CASE` for constants.
- Namespaces mirror folders (e.g., `src/Shared/Kernel.Domain/SeedWorks` → `Kernel.Domain.SeedWorks`).
- Project naming: `Kernel.*` for shared layers; modules use singular, e.g., `Orders`.
- Formatting: run `dotnet format` before committing. Keep usings sorted, remove dead code.

## Testing Guidelines
- Framework: xUnit recommended. Create projects like `tests/Kernel.Domain.Tests` and `tests/Api.Tests` and reference the target project.
- Test naming: `Method_Should_Expected_When_Context` and place files mirroring source folders.
- Coverage: target ≥80% for new/changed code. Example: `dotnet test /p:CollectCoverage=true` (Coverlet).

## Commit & Pull Request Guidelines
- Commits: imperative, concise, scoped. Examples: `Api: add health endpoint`, `Kernel.Domain: fix value object equality`.
- PRs: include summary, linked issues, validation steps, and samples (e.g., request/response for API changes). Add screenshots/logs when relevant.
- Keep PRs small and focused; include breaking‑change notes and migration steps if applicable.

## Security & Configuration Tips
- Use `appsettings.Development.json` for local overrides; never commit secrets. Prefer environment variables and `dotnet user-secrets` for the API.
- Respect `ASPNETCORE_ENVIRONMENT` for environment‑specific configs.
- Validate external inputs in Controllers and Application layer; avoid throwing raw exceptions across boundaries.

