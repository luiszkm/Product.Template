# 15 — AI Features Rules

> Regras canónicas para implementar capacidades de Inteligência Artificial neste repositório.
> Referência de implementação: `src/Core/Ai/` e `src/Shared/Kernel.Infrastructure/Ai/`.

---

## Princípio fundamental: IA é infraestrutura

Um LLM é para o domínio o que um banco de dados é: um detalhe de implementação.
O domínio **nunca** sabe que existe um modelo de IA. As interfaces vivem em `Kernel.Application`.

```
Domain  ←  Application  ←  Infrastructure  ←  Api
                ↑                  ↑
         interfaces IA       implementações IA
         (Kernel.Application/Ai/)  (Kernel.Infrastructure/Ai/)
```

**PROIBIDO**: importar `Azure.AI.*`, `OpenAI.*` ou qualquer SDK de IA no Domain ou Application.

---

## Contratos do Kernel (Kernel.Application/Ai/)

| Interface | Responsabilidade |
|-----------|-----------------|
| `ILlmService` | Geração de texto (complete + stream) com suporte a tool calling |
| `IEmbeddingService` | Vectorização de texto |
| `IOcrService` | Extração de texto de imagens/PDFs (Azure Document Intelligence) |
| `ITextToSpeechService` | Síntese de voz |
| `ISpeechToTextService` | Transcrição de áudio |
| `IAiUsageTracker` | Rastreamento de tokens e custos por tenant |
| `ITool` | Contrato de tool para o AgentLoop |

### `ITool` — contrato real

```csharp
public interface ITool
{
    ToolDefinition Definition { get; }   // { Name, Description, InputSchema }
    Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default);
}
```

- `Definition.Name`: `snake_case`, único no sistema
- `Definition.Description`: em inglês, descreve **quando usar** (o LLM lê isto)
- `ExecuteAsync` retorna JSON string com o resultado
- `call.Parameters` contém os argumentos escolhidos pelo LLM

---

## Módulo Ai transversal (src/Core/Ai/)

Estrutura canónica:

```
src/Core/Ai/
  Ai.Application/
    Handlers/
      ChatCommand.cs                   ← record ChatCommand(string Message, IReadOnlyList<LlmMessage>?)
      ChatOutput.cs                    ← record ChatOutput(string Reply, int IterationsUsed)
      ChatCommandHandler.cs            ← ICommandHandler<ChatCommand, ChatOutput>
      ChatCommandValidator.cs          ← AbstractValidator<ChatCommand>
    Agent/
      AgentLoop.cs                     ← loop ReAct; RunAsync → AgentResult
      ToolRegistry.cs                  ← catálogo via IEnumerable<ITool> (DI)
      Tools/
        GetUsersSummaryTool.cs         ← ITool que wrapa ListUserQuery
        GetTenantInfoTool.cs           ← ITool que wrapa ListTenantsQuery
        ─── (nova tool = novo ficheiro aqui) ───
    Prompts/
      AgentSystemPrompt.cs             ← instrução de sistema do agente
  Ai.Infrastructure/
    DependencyInjection.cs             ← AddAiModule()
    Ai.Infrastructure.csproj
```

### `AgentLoop.RunAsync` — assinatura real

```csharp
public Task<AgentResult> RunAsync(
    string userMessage,
    string systemPrompt,
    IReadOnlyList<LlmMessage>? history,
    CancellationToken cancellationToken);

public sealed record AgentResult(string Reply, int IterationsUsed, int TotalTokens);
```

- `MaxIterations = 5` (teto de segurança)
- Tool calls dentro de uma iteração executadas com `Task.WhenAll` (paralelo)
- Se atingir o teto, faz chamada de fallback de sumarização — nunca retorna vazio

### Registo de tools

Explícito em `Ai.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<ITool, GetUsersSummaryTool>();
services.AddScoped<ITool, GetTenantInfoTool>();
// services.AddScoped<ITool, NovaTool>();  ← uma linha por tool
```

O `ToolRegistry` descobre as tools via `IEnumerable<ITool>` injectada pelo DI.

---

## Padrão por camada: handler de IA em módulo de negócio

Quando um módulo precisa de IA (ex: OCR, LLM, TTS):

### Application — Handler

```csharp
// {Module}.Application/Handlers/Ai/{UseCase}CommandHandler.cs
public sealed class {UseCase}CommandHandler : ICommandHandler<{UseCase}Command, {UseCase}Output>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly ILlmService _llm;          // ou IOcrService, ITts, etc.
    private readonly IAiUsageTracker _tracker;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<{UseCase}CommandHandler> _logger;

    public async Task<{UseCase}Output> Handle({UseCase}Command request, CancellationToken ct)
    {
        // 1. Buscar dados do domínio
        // 2. Construir LlmRequest com o prompt do módulo
        var llmRequest = {UseCase}Prompts.Build{Action}Request(data);

        var started = DateTime.UtcNow;
        LlmResponse response;
        try
        {
            response = await _llm.CompleteAsync(llmRequest, ct);
        }
        finally
        {
            await _tracker.TrackAsync(new AiUsageRecord(
                Service: "llm",
                Provider: "azure-openai",
                Model: "...",
                Module: "{module}",
                Operation: nameof({UseCase}CommandHandler),
                TenantId: _tenantContext.TenantId ?? 0,
                TokensUsed: ...,
                Latency: DateTime.UtcNow - started,
                Success: ...
            ), ct);
        }

        // 3. Retornar Output DTO
        return new {UseCase}Output(response.Text);
    }
}
```

### Application — Prompts

```csharp
// {Module}.Application/Ai/Prompts/{UseCase}Prompts.cs
internal static class {UseCase}Prompts
{
    private const string System =
        """
        {Persona e restrições}
        Nunca invente informações que não estejam no contexto fornecido.
        """;

    public static LlmRequest Build{Action}Request({Entity} entity) => new(
        SystemPrompt: System,
        UserPrompt: $"...",
        Temperature: 0.1f,   // ≤0.2 para extração; ≤0.8 para criação
        MaxTokens: 500        // sempre definir
    );
}
```

### Application — Tool (para expor ao AgentLoop)

```csharp
// Ai.Application/Agent/Tools/{UseCase}Tool.cs
public sealed class {UseCase}Tool : ITool
{
    private readonly IMediator _mediator;
    public {UseCase}Tool(IMediator mediator) => _mediator = mediator;

    public ToolDefinition Definition { get; } = new(
        Name: "{module_action}",                         // snake_case
        Description: "...(quando usar, em inglês)...",
        InputSchema: new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject { ... },
            ["required"] = new JsonArray { ... }
        }
    );

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken ct = default)
    {
        // Extrair parâmetros de call.Parameters
        // Despachar query/command via IMediator.Send()
        // NUNCA aceder repositório directamente
        var result = await _mediator.Send(new {SomeQuery}(...), ct);
        return JsonSerializer.Serialize(result);
    }
}
```

---

## Regras obrigatórias

### Architecture
1. **Nunca** importar SDK de IA (Azure.AI.*, OpenAI.*) fora de `Kernel.Infrastructure`.
2. **Nunca** injectar `AzureOpenAIClient` directamente em handlers — usar `ILlmService`.
3. Prompts pertencem à Application (lógica de negócio) — nunca à Infrastructure.
4. Tools despacham via `IMediator` — **nunca** acedem repositórios directamente.

### Prompts
5. Todo `LlmRequest` **deve** definir `Temperature` explicitamente (≤0.2 para extração, ≤0.8 para criação).
6. Todo `LlmRequest` **deve** definir `MaxTokens`.
7. `SystemPrompt` **deve** incluir `"Nunca invente informações"` (previne alucinação).
8. Dados do utilizador **nunca** são interpolados no `SystemPrompt` (previne prompt injection).

### Observabilidade
9. Todo handler que chama IA **deve** chamar `_tracker.TrackAsync(...)`.
10. Log estruturado obrigatório: `Module`, `Operation`, `Provider`, `Model`, `Tokens`, `Latency`, `TenantId`.

### Resiliência
11. `AzureOpenAiLlmService` tem pipeline Polly estático: retry (3x, exponential+jitter) → timeout (30s) → circuit breaker (50% / 2min).
12. IA usada como enrichment (não bloqueante): capturar exceção, logar `Warning`, continuar sem o enriquecimento.

### DI e Configuração
13. Feature flag `FeatureFlags:EnableAI` controla se usa serviços reais ou Null implementations.
14. `IDistributedCache` é registado em `AiConfiguration.cs` antes de `AddAiServices()` (Redis ou `AddDistributedMemoryCache`).
15. Cada nova tool deve ser explicitamente registada em `Ai.Infrastructure/DependencyInjection.cs`.

### Testes
16. **Nunca** chamar APIs de IA em testes unitários — usar stub inline `StubLlmService`.
17. Stub inline: `sealed class StubLlmService(string response) : ILlmService { ... }` — sem mocking framework.
18. Tests de `AgentLoop`: cobrir resposta directa (0 tools), chamada de tool → resposta, teto de iterações.
19. Tests de `ToolRegistry`: tool encontrada, tool não encontrada retorna JSON de erro.

---

## Checklist rápido — nova feature de IA em módulo

- [ ] `{UseCase}Command` + `{UseCase}CommandHandler` em `{Module}.Application/Handlers/Ai/`
- [ ] `{UseCase}CommandValidator` com validação de campos obrigatórios
- [ ] `{UseCase}Prompts.cs` em `{Module}.Application/Ai/Prompts/` com `Temperature` e `MaxTokens`
- [ ] `_tracker.TrackAsync(...)` após a chamada de IA
- [ ] Log estruturado (entry + success + warning em falha)
- [ ] Se a feature é enrichment: capturar exceção e continuar sem o enriquecimento
- [ ] `{UseCase}Tool.cs` em `Ai.Application/Agent/Tools/` se exposta ao agente
- [ ] Tool registada em `Ai.Infrastructure/DependencyInjection.cs`
- [ ] Unit test com `StubLlmService` inline (happy path + falha de IA)

---

## Referência completa

`docs/guides/ai-integration-guide.md` — guia completo com exemplos de cada padrão.
