# Prompt: Create AI Handler

> Use este prompt para adicionar uma feature de IA a um módulo de negócio existente.
> Para adicionar uma tool ao AgentLoop, use `create-ai-tool.md`.

---

## Context

Read these files before starting:
- `.ai/rules/15-ai-features.md` — **regras canónicas de IA** (obrigatório)
- `.ai/rules/00-global.md`
- `.ai/rules/03-application.md`
- `docs/guides/ai-integration-guide.md` — secções 4 e 6
- `src/Core/Ai/Ai.Application/Handlers/ChatCommandHandler.cs` — padrão de tracking

---

## Instruction

Create an AI feature for module **`{MODULE_NAME}`** with use case **`{USE_CASE}`**.

AI capability type (choose one):
- [ ] LLM (text generation / summarization / extraction)
- [ ] OCR (extract text from PDF/image)
- [ ] Embeddings + RAG (semantic search)
- [ ] Text-to-Speech
- [ ] Speech-to-Text

---

### Deliver ALL of the following:

#### 1. Command + Handler (`{Module}.Application/Handlers/Ai/`)

```
Commands/
  {UseCase}Command.cs           ← record implementing ICommand<{UseCase}Output>
{UseCase}CommandHandler.cs      ← ICommandHandler<{UseCase}Command, {UseCase}Output>
{UseCase}CommandValidator.cs    ← AbstractValidator<{UseCase}Command> (obrigatório)
```

**Handler rules:**
- [ ] Injects `I{LlmService|OcrService|...}`, `IAiUsageTracker`, `ITenantContext`, `ILogger<T>`
- [ ] Calls `_tracker.TrackAsync(...)` in `finally` block (always tracks, even on failure)
- [ ] Structured log: Module, Operation, Tokens, Latency, TenantId
- [ ] If AI is enrichment (non-blocking): catch exception, log `Warning`, continue
- [ ] `CancellationToken` in every async call

#### 2. Prompts (`{Module}.Application/Ai/Prompts/`)

```
{UseCase}Prompts.cs
```

**Prompt rules:**
- [ ] `internal static class` (not public — business logic detail)
- [ ] `SystemPrompt` includes `"Nunca invente informações que não estejam no contexto"`
- [ ] `Temperature` set explicitly: ≤0.2 for extraction/classification, ≤0.8 for generation
- [ ] `MaxTokens` set explicitly
- [ ] User data only in `UserPrompt`, never in `SystemPrompt` (prevents prompt injection)

#### 3. Unit Tests (`tests/UnitTests/{Module}/Ai/`)

```
{UseCase}CommandHandlerTests.cs
```

**Test rules:**
- [ ] Happy path: handler returns expected output
- [ ] AI failure path: when `ILlmService` throws, handler either re-throws or continues (per enrichment policy)
- [ ] Tracking called with correct Module and Operation
- [ ] Inline stub `sealed class StubLlmService(string response) : ILlmService { ... }`
- [ ] No mocking frameworks
- [ ] Test naming: `Handle_{Scenario}_{ExpectedResult}`

#### 4. (Optional) ITool for AgentLoop

If this feature should be accessible via natural language chat:
- [ ] Create `{UseCase}Tool.cs` in `Ai.Application/Agent/Tools/` (use `create-ai-tool.md`)
- [ ] Register in `Ai.Infrastructure/DependencyInjection.cs`

---

### Output Format

For each file:
```
### File: `{full/path/to/file.cs}`
{complete file content}
```

---

### Examples

**LLM extraction:**
```csharp
// {Module}.Application/Ai/Prompts/ExtractInvoiceFieldsPrompts.cs
internal static class ExtractInvoiceFieldsPrompts
{
    private const string System =
        """
        Você é um extrator de dados de documentos fiscais.
        Retorne APENAS JSON válido, sem markdown.
        Nunca invente informações que não estejam no contexto fornecido.
        """;

    public static LlmRequest BuildRequest(string rawText) => new(
        SystemPrompt: System,
        UserPrompt: $"Extraia os campos do seguinte texto:\n\n{rawText}",
        Temperature: 0.0f,
        MaxTokens: 500
    );
}
```
