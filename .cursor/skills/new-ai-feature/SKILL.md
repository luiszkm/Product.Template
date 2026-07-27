---
name: new-ai-feature
version: 1
description: "Add AI capabilities (LLM, OCR, TTS, STT, embeddings, agent tools) following this project's architecture. TRIGGER: \"add AI\", \"LLM\", \"agent loop\", \"ITool\", \"embedding\", \"OCR feature\", \"AI handler\", \"add a tool\", \"new AI use case\". SKIP: generic handlers with no AI dependency (use new-feature or new-command)."
tools: Read, Edit, Write, Glob, Grep
disable-model-invocation: true
---

# Skill: /new-ai-feature

> Implements AI capabilities for this .NET 10 / Clean Architecture repository following the infrastructure pattern — AI SDKs live only in Kernel.Infrastructure; interfaces live in Kernel.Application.

## Arguments

`$ARGUMENTS` format: `{MODULE_NAME} {USE_CASE}`

Examples:
- `/new-ai-feature Documents ExtractInvoiceData`
- `/new-ai-feature Billing SummarizeTransactions`
- `/new-ai-feature Ai AddGetOrdersTool`

## Context — read before generating

- `.cursor/rules/architecture.mdc` — layer dependency rules
- `.cursor/rules/domain.mdc` — domain invariants
- `.cursor/rules/application.mdc` — CQRS patterns
- `.cursor/rules/ai-features.mdc` — AI SDK placement and ITool patterns
- `.agents/checklists/new-feature.md` — completeness checklist
- `src/Core/Ai/` — canonical AI module reference implementation
- `src/Shared/Kernel.Application/Ai/` — AI interfaces
- `src/Shared/Kernel.Infrastructure/Ai/` — AI implementations
- `docs/guides/ai-integration-guide.md` — deep-dive guide with all patterns

## Fundamental Principle: AI is Infrastructure

A LLM is to the domain what a database is: an implementation detail.
The domain **never** knows AI exists. Interfaces live in `Kernel.Application`.

```
Domain  ←  Application  ←  Infrastructure  ←  Api
                ↑                  ↑
         AI interfaces       AI implementations
   (Kernel.Application/Ai/) (Kernel.Infrastructure/Ai/)
```

**FORBIDDEN**: importing `Azure.AI.*`, `OpenAI.*`, or any AI SDK outside `Kernel.Infrastructure`.

## Step 1 — Identify which AI service is needed

| Interface | Responsibility |
|-----------|----------------|
| `ILlmService` | Text generation (complete + stream) with tool calling |
| `IEmbeddingService` | Text vectorization |
| `IOcrService` | Text extraction from images/PDFs |
| `ITextToSpeechService` | Voice synthesis |
| `ISpeechToTextService` | Audio transcription |
| `IAiUsageTracker` | Token + cost tracking per tenant |
| `ITool` | Tool contract for the AgentLoop |

## Step 2 — Choose implementation pattern

| Scenario | Pattern |
|----------|---------|
| Module calls LLM for business use | AI handler in `{Module}.Application/Handlers/Ai/` |
| New tool exposed to the agent | `ITool` in `Ai.Application/Agent/Tools/` |
| New AgentLoop capability | Extend `AgentLoop` in `Ai.Application/Agent/` |

## Step 3 — Generate files

Parse `$ARGUMENTS` as `MODULE_NAME` (first token) and `USE_CASE` (second token).

### Pattern A — AI handler in a business module

#### `{Module}.Application/Handlers/Ai/{UseCase}Command.cs`

```csharp
namespace {Namespace}.Core.{Module}.Application.Handlers.Ai;

public sealed record {UseCase}Command({InputFields}) : ICommand<{UseCase}Output>;
```

#### `{Module}.Application/Handlers/Ai/{UseCase}CommandHandler.cs`

```csharp
namespace {Namespace}.Core.{Module}.Application.Handlers.Ai;

public sealed class {UseCase}CommandHandler(
    I{Entity}Repository {entity}Repository,
    ILlmService llm,                // or IOcrService, ITextToSpeechService, etc.
    IAiUsageTracker tracker,
    ITenantContext tenantContext,
    ILogger<{UseCase}CommandHandler> logger)
    : ICommandHandler<{UseCase}Command, {UseCase}Output>
{
    public async Task<{UseCase}Output> Handle({UseCase}Command request, CancellationToken ct)
    {
        var entity = await {entity}Repository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof({Entity}), request.Id);

        var llmRequest = {UseCase}Prompts.Build{Action}Request(entity);

        var started = DateTime.UtcNow;
        LlmResponse response;
        try
        {
            response = await llm.CompleteAsync(llmRequest, ct);
        }
        finally
        {
            await tracker.TrackAsync(new AiUsageRecord(
                Service: "llm",
                Provider: "azure-openai",
                Model: "gpt-4o",
                Module: "{module}",
                Operation: nameof({UseCase}CommandHandler),
                TenantId: tenantContext.TenantId ?? 0,
                TokensUsed: response?.Usage?.TotalTokens ?? 0,
                Latency: DateTime.UtcNow - started,
                Success: response is not null
            ), ct);
        }

        logger.LogInformation("{UseCase} completed for {EntityId}", nameof({UseCase}), request.Id);
        return new {UseCase}Output(response.Text);
    }
}
```

#### `{Module}.Application/Ai/Prompts/{UseCase}Prompts.cs`

```csharp
namespace {Namespace}.Core.{Module}.Application.Ai.Prompts;

internal static class {UseCase}Prompts
{
    private const string System =
        """
        {Persona and constraints — in the domain language}
        Never invent information not present in the provided context.
        """;

    public static LlmRequest Build{Action}Request({Entity} entity) => new(
        SystemPrompt: System,
        UserPrompt: $"...",   // never inject user-controlled data into SystemPrompt
        Temperature: 0.1f,    // ≤0.2 for extraction; ≤0.8 for generation
        MaxTokens: 500         // always define
    );
}
```

### Pattern B — New ITool for the AgentLoop

#### `Ai.Application/Agent/Tools/{UseCase}Tool.cs`

```csharp
namespace {Namespace}.Core.Ai.Application.Agent.Tools;

public sealed class {UseCase}Tool(IMediator mediator) : ITool
{
    public ToolDefinition Definition { get; } = new(
        Name: "{module_action}",                    // snake_case, unique
        Description: "...(when to use, in English)...",
        InputSchema: new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["{param}"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "..."
                }
            },
            ["required"] = new JsonArray { "{param}" }
        }
    );

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken ct = default)
    {
        // Extract params from call.Parameters
        // Dispatch via IMediator — NEVER access repository directly
        var result = await mediator.Send(new {SomeQuery}(...), ct);
        return JsonSerializer.Serialize(result);
    }
}
```

Register in `Ai.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<ITool, {UseCase}Tool>();
```

## Mandatory rules

### Architecture
1. Never import AI SDK (`Azure.AI.*`, `OpenAI.*`) outside `Kernel.Infrastructure`.
2. Never inject `AzureOpenAIClient` directly into handlers — use `ILlmService`.
3. Prompts belong to Application (business logic) — never to Infrastructure.
4. Tools dispatch via `IMediator` — never access repositories directly.

### Prompts
5. Every `LlmRequest` **must** define `Temperature` explicitly.
6. Every `LlmRequest` **must** define `MaxTokens`.
7. `SystemPrompt` **must** include "Never invent information" (prevents hallucination).
8. User-controlled data **never** interpolated into `SystemPrompt` (prevents prompt injection).

### Observability
9. Every handler calling AI **must** call `_tracker.TrackAsync(...)`.
10. Structured logging required: `Module`, `Operation`, `Provider`, `Model`, `Tokens`, `Latency`, `TenantId`.

### Resilience
11. AI used as enrichment (non-blocking): catch exception, log `Warning`, continue without enrichment.

### DI and configuration
12. Feature flag `FeatureFlags:EnableAI` controls real vs null implementations.
13. Each new tool must be explicitly registered in `Ai.Infrastructure/DependencyInjection.cs`.

### Tests
14. Never call real AI APIs in unit tests — use inline stub `StubLlmService`.
15. Stub: `sealed class StubLlmService(string response) : ILlmService { ... }` — no mocking framework.

## Checklist — new AI feature

- [ ] `{UseCase}Command` + `{UseCase}CommandHandler` in `{Module}.Application/Handlers/Ai/`
- [ ] `{UseCase}CommandValidator` with required field validation
- [ ] `{UseCase}Prompts.cs` with explicit `Temperature` and `MaxTokens`
- [ ] `_tracker.TrackAsync(...)` in finally block
- [ ] Structured logging (entry + success + warning on failure)
- [ ] If enrichment: catch exception and continue without enrichment
- [ ] `{UseCase}Tool.cs` in `Ai.Application/Agent/Tools/` if exposed to agent
- [ ] Tool registered in `Ai.Infrastructure/DependencyInjection.cs`
- [ ] Unit test with inline `StubLlmService` (happy path + AI failure)

## Deep-dive reference

`docs/guides/ai-integration-guide.md` — full guide with implementation examples for each pattern.
