# Prompt: Create AI Tool (ITool for AgentLoop)

> Use este prompt para expor uma operação de módulo ao AgentLoop via `ITool`.
> O agente de chat usará a tool automaticamente quando a pergunta do utilizador for relevante.

---

## Context

Read these files before starting:
- `.ai/rules/15-ai-features.md` — secção "Módulo Ai transversal" e "Regras obrigatórias"
- `src/Core/Ai/Ai.Application/Agent/Tools/GetUsersSummaryTool.cs` — referência canónica
- `src/Core/Ai/Ai.Application/Agent/Tools/GetTenantInfoTool.cs` — referência canónica
- `src/Shared/Kernel.Application/Ai/ITool.cs` — contrato

---

## Instruction

Create an `ITool` implementation that exposes **`{OPERATION_DESCRIPTION}`** from module **`{MODULE_NAME}`** to the AgentLoop.

The tool should dispatch **`{QueryOrCommandName}`** via `IMediator`.

---

### Deliver the following:

#### 1. Tool class (`Ai.Application/Agent/Tools/`)

```
{ModuleAction}Tool.cs
```

**Rules:**
- [ ] `public sealed class {ModuleAction}Tool : ITool`
- [ ] Constructor receives `IMediator mediator` only
- [ ] `ToolDefinition Definition { get; } = new(Name, Description, InputSchema)` — property initializer, not computed
- [ ] `Name` in `snake_case`, globally unique (ex: `get_users_summary`, `get_revenue_by_month`)
- [ ] `Description` in English, explains **when to use it** (not just what it does) — the LLM reads this
- [ ] `InputSchema`: valid JSON Schema with `type`, `properties`, `required`
- [ ] `ExecuteAsync(ToolCall call, CancellationToken ct)`:
  - Extracts params from `call.Parameters["{param}"]?.GetValue<T>()`
  - Dispatches via `await _mediator.Send(new {Query}(...), ct)`
  - Returns `JsonSerializer.Serialize(anonymousResultObject)`
  - **NEVER** accesses repository directly
- [ ] Math.Clamp for any numeric parameters with upper bounds

#### 2. DI Registration (modify existing file)

```
Ai.Infrastructure/DependencyInjection.cs
```

Add:
```csharp
services.AddScoped<ITool, {ModuleAction}Tool>();
```

#### 3. Unit Tests (`tests/UnitTests/Ai/`)

Add test cases to `ToolRegistryTests.cs` or create `{ModuleAction}ToolTests.cs`:
- [ ] `ExecuteAsync_ShouldDispatchCorrectQuery_WhenCalled`
- [ ] `Definition_ShouldHaveCorrectName_AndSchema`
- [ ] Inline stub `IMediator` (no mocking framework)

---

### Output Format

```
### File: `src/Core/Ai/Ai.Application/Agent/Tools/{ModuleAction}Tool.cs`
{complete file}

### File: `Ai.Infrastructure/DependencyInjection.cs` (modified section)
{relevant diff or full updated method}
```

---

### Reference — canonical tool pattern

```csharp
public sealed class GetUsersSummaryTool : ITool
{
    private readonly IMediator _mediator;
    public GetUsersSummaryTool(IMediator mediator) => _mediator = mediator;

    public ToolDefinition Definition { get; } = new(
        Name: "get_users_summary",
        Description: "Returns a summary of registered users. Use when the user asks about " +
                     "number of users, registrations, or user growth.",
        InputSchema: new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["page_size"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "Number of users to retrieve (default: 10, max: 50)"
                }
            },
            ["required"] = new JsonArray()
        }
    );

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(call.Parameters["page_size"]?.GetValue<int>() ?? 10, 1, 50);
        var result = await _mediator.Send(new ListUserQuery { PageSize = pageSize }, ct);
        return JsonSerializer.Serialize(new
        {
            total_count = result.TotalCount,
            users = result.Data.Select(u => new { u.Id, u.Email, ... })
        });
    }
}
```
