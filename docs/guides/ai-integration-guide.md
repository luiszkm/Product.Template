# AI Integration Guide

> Como adicionar capacidades de Inteligência Artificial ao Product.Template
> mantendo Clean Architecture, testabilidade e governança de custos.

---

## Índice

1. [Princípios fundamentais](#1-princípios-fundamentais)
2. [Onde cada peça de IA vive](#2-onde-cada-peça-de-ia-vive)
3. [Contratos do Kernel](#3-contratos-do-kernel)
4. [Padrões por capacidade](#4-padrões-por-capacidade)
   - 4.1 [LLM (geração de texto)](#41-llm-geração-de-texto)
   - 4.2 [Embeddings + RAG](#42-embeddings--rag)
   - 4.3 [OCR (extração de texto de imagens/PDFs)](#43-ocr-extração-de-texto-de-imagenspdf)
   - 4.4 [Text-to-Speech e Speech-to-Text](#44-text-to-speech-e-speech-to-text)
5. [Agente Cross-Módulo (Chat em Linguagem Natural)](#5-agente-cross-módulo-chat-em-linguagem-natural)
   - 5.1 [Visão geral do padrão](#51-visão-geral-do-padrão)
   - 5.2 [Estrutura do módulo Ai](#52-estrutura-do-módulo-ai)
   - 5.3 [Contrato ITool](#53-contrato-itool)
   - 5.4 [ToolRegistry — catálogo de ferramentas](#54-toolregistry--catálogo-de-ferramentas)
   - 5.5 [AgentLoop — Reason → Act → Observe](#55-agentloop--reason--act--observe)
   - 5.6 [Exemplo de Tool por módulo](#56-exemplo-de-tool-por-módulo)
   - 5.7 [ChatCommandHandler — entry point](#57-chatcommandhandler--entry-point)
   - 5.8 [Registro automático de Tools no DI](#58-registro-automático-de-tools-no-di)
   - 5.9 [Dashboard sem agente (agregação simples)](#59-dashboard-sem-agente-agregação-simples)
6. [Adicionando IA a um módulo](#6-adicionando-ia-a-um-módulo)
7. [Prompt Engineering](#7-prompt-engineering)
8. [Observabilidade e custos](#8-observabilidade-e-custos)
9. [Tratamento de erros e resiliência](#9-tratamento-de-erros-e-resiliência)
10. [Testes](#10-testes)
11. [Checklist de implementação](#11-checklist-de-implementação)

---

## 1. Princípios fundamentais

### IA é infraestrutura — trate como tal

Um LLM é para o domínio o que um banco de dados é: um detalhe de implementação. O domínio nunca deve saber que existe um modelo de IA; ele só conhece interfaces.

```
❌ ERRADO — domínio dependendo de SDK de IA
public class Invoice : AggregateRoot
{
    public async Task ExtractFields(OpenAiClient client) { ... }
}

✅ CERTO — domínio puro, IA na camada de aplicação
public class ExtractInvoiceFieldsCommandHandler : ICommandHandler<...>
{
    public ExtractInvoiceFieldsCommandHandler(IOcrService ocr, ILlmService llm) { ... }
}
```

### Regra de dependência não muda

```
Domain  ←  Application  ←  Infrastructure  ←  Api
               ↑                  ↑
         interfaces IA       implementações IA
         (Kernel.Application)  (Kernel.Infrastructure)
```

### Prompts são código — versione e teste

Templates de prompt pertencem à camada Application (lógica de negócio), não à Infrastructure. Trate-os como código: versione, revise em PR, cubra com testes.

### Custos são um requisito não-funcional

Toda chamada a um LLM ou serviço de visão tem custo. Observabilidade de tokens, caching de respostas e circuit breakers são obrigatórios antes de produção.

---

## 2. Onde cada peça de IA vive

```
src/
├── Shared/
│   ├── Kernel.Application/
│   │   └── Ai/
│   │       ├── ILlmService.cs               # contrato de LLM (com suporte a tool calling)
│   │       ├── IEmbeddingService.cs         # contrato de embeddings
│   │       ├── IOcrService.cs               # contrato de OCR
│   │       ├── ITextToSpeechService.cs      # contrato de TTS
│   │       ├── ISpeechToTextService.cs      # contrato de STT
│   │       ├── IAiUsageTracker.cs           # rastreamento de custos/tokens
│   │       ├── ITool.cs                     # contrato de tool para o agente
│   │       └── Models/
│   │           ├── LlmRequest.cs            # inclui Tools (lista de schemas)
│   │           ├── LlmResponse.cs           # inclui ToolCalls (decisões do modelo)
│   │           ├── ToolCall.cs              # { Id, Name, Parameters }
│   │           ├── ToolDefinition.cs        # { Name, Description, InputSchema }
│   │           ├── EmbeddingResult.cs
│   │           ├── OcrModels.cs             # OcrRequest, OcrResult, OcrPage, OcrTable
│   │           ├── TtsModels.cs             # TtsOptions
│   │           ├── SttModels.cs             # SttOptions, SttResult, SttSegment
│   │           └── AiUsageRecord.cs
│   │
│   └── Kernel.Infrastructure/
│       └── Ai/
│           ├── AzureOpenAiLlmService.cs     # implementação Azure OpenAI (com function calling)
│           ├── AzureOpenAiEmbeddingService.cs
│           ├── AzureOcrService.cs           # Azure Document Intelligence
│           ├── AzureTextToSpeechService.cs
│           ├── AzureSpeechToTextService.cs
│           ├── CachedEmbeddingService.cs    # decorator com IDistributedCache (24h AbsoluteExpiration)
│           ├── AiUsageTracker.cs
│           └── Null/                        # implementações noop para ambientes sem IA
│               ├── NullLlmService.cs
│               ├── NullEmbeddingService.cs
│               ├── NullOcrService.cs
│               ├── NullTextToSpeechService.cs
│               ├── NullSpeechToTextService.cs
│               └── NullAiUsageTracker.cs
│
└── Core/
    ├── Ai/                                  # módulo transversal de agente
    │   ├── Ai.Application/
    │   │   ├── Handlers/
    │   │   │   ├── ChatCommand.cs
    │   │   │   ├── ChatOutput.cs            # { Reply, IterationsUsed }
    │   │   │   ├── ChatCommandHandler.cs    # entry point do chat
    │   │   │   └── ChatCommandValidator.cs
    │   │   ├── Agent/
    │   │   │   ├── AgentLoop.cs             # loop ReAct → retorna AgentResult
    │   │   │   ├── ToolRegistry.cs          # catálogo via IEnumerable<ITool>
    │   │   │   └── Tools/
    │   │   │       ├── GetUsersSummaryTool.cs   # wrapa ListUserQuery (Identity)
    │   │   │       └── GetTenantInfoTool.cs     # wrapa ListTenantsQuery (Tenants)
    │   │   └── Prompts/
    │   │       └── AgentSystemPrompt.cs     # prompt de sistema do agente
    │   └── Ai.Infrastructure/
    │       └── DependencyInjection.cs       # AddAiModule()
    │
    └── {Module}/                            # módulo de negócio normal
        ├── {Module}.Domain/
        │   └── Repositories/
        │       └── I{Entity}VectorRepository.cs  # se o módulo tem vector store
        │
        ├── {Module}.Application/
        │   ├── Handlers/
        │   │   └── Ai/
        │   │       ├── {UseCase}Command.cs        # handler de IA com contexto de negócio
        │   │       └── {UseCase}CommandHandler.cs
        │   └── Ai/
        │       └── Prompts/
        │           └── {UseCase}Prompts.cs        # templates de prompt do módulo
        │
        └── {Module}.Infrastructure/
            └── Ai/
                └── {Entity}VectorRepository.cs    # implementação do vector store
```

### Regra de decisão rápida

| O que você tem | Onde vai |
|----------------|----------|
| Interface `IOcrService` | `Kernel.Application/Ai/` |
| Implementação `AzureOcrService` | `Kernel.Infrastructure/Ai/` |
| Template de prompt de negócio | `{Module}.Application/Ai/Prompts/` |
| Handler que chama OCR + LLM | `{Module}.Application/Handlers/Ai/` |
| Interface `IInvoiceVectorRepository` | `Finance.Domain/Repositories/` |
| Implementação `PineconeInvoiceVectorRepository` | `Finance.Infrastructure/Ai/` |

---

## 3. Contratos do Kernel

### `ILlmService`

```csharp
// Kernel.Application/Ai/ILlmService.cs
namespace Product.Template.Kernel.Application.Ai;

public interface ILlmService
{
    /// <summary>
    /// Envia um prompt e retorna a resposta gerada.
    /// Quando <see cref="LlmRequest.Tools"/> é fornecido, o modelo pode retornar
    /// <see cref="LlmResponse.ToolCalls"/> em vez de (ou além de) texto.
    /// </summary>
    Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streaming de tokens à medida que são gerados (para UX responsiva).
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(LlmRequest request, CancellationToken cancellationToken = default);
}

public sealed record LlmRequest(
    string UserPrompt,
    string? SystemPrompt = null,
    string? Model = null,                              // null = usar padrão configurado
    float Temperature = 0.2f,                         // baixo = mais determinístico
    int? MaxTokens = null,
    IReadOnlyList<LlmMessage>? History = null,        // conversas multi-turn
    IReadOnlyList<ToolDefinition>? Tools = null       // tools disponíveis para o agente
);

public sealed record LlmMessage(
    string Role,       // "user" | "assistant" | "tool"
    string Content,
    string? ToolCallId = null   // preenchido quando Role == "tool" (resultado de uma tool call)
);

public sealed record LlmResponse(
    string Text,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    string Model,
    TimeSpan Latency,
    IReadOnlyList<ToolCall>? ToolCalls = null   // preenchido quando o modelo decide chamar tools
);

// Kernel.Application/Ai/Models/ToolDefinition.cs
public sealed record ToolDefinition(
    string Name,
    string Description,
    JsonObject InputSchema    // JSON Schema dos parâmetros esperados
);

// Kernel.Application/Ai/Models/ToolCall.cs
public sealed record ToolCall(
    string Id,            // ID único gerado pelo modelo (necessário para enviar o resultado de volta)
    string Name,          // nome da tool escolhida pelo modelo
    JsonObject Parameters // argumentos escolhidos pelo modelo
);
```

### `IEmbeddingService`

```csharp
// Kernel.Application/Ai/IEmbeddingService.cs
namespace Product.Template.Kernel.Application.Ai;

public interface IEmbeddingService
{
    Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}

public sealed record EmbeddingResult(
    float[] Vector,
    int Dimensions,
    int TokensUsed,
    string Model
);
```

### `IOcrService`

```csharp
// Kernel.Application/Ai/IOcrService.cs
namespace Product.Template.Kernel.Application.Ai;

public interface IOcrService
{
    Task<OcrResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken = default);
}

public sealed record OcrRequest(
    byte[] FileBytes,
    string MimeType,            // "application/pdf", "image/png", "image/jpeg"
    string? Language = null,    // null = detecção automática
    bool ExtractTables = false,
    bool ExtractKeyValues = false
);

public sealed record OcrResult(
    string FullText,
    IReadOnlyList<OcrPage> Pages,
    IReadOnlyList<OcrTable> Tables,
    IReadOnlyDictionary<string, string> KeyValues,
    float Confidence
);

public sealed record OcrPage(int Number, string Text, float Confidence);
public sealed record OcrTable(int PageNumber, IReadOnlyList<IReadOnlyList<string>> Rows);
```

### `ITextToSpeechService` e `ISpeechToTextService`

```csharp
// Kernel.Application/Ai/ITextToSpeechService.cs
public interface ITextToSpeechService
{
    Task<byte[]> SynthesizeAsync(string text, TtsOptions? options = null, CancellationToken cancellationToken = default);
}

public sealed record TtsOptions(
    string? Voice = null,       // "alloy", "nova", "shimmer" (OpenAI) etc.
    string? Language = null,
    float? Speed = null,        // 0.5 – 2.0
    string OutputFormat = "mp3" // "mp3" | "wav" | "ogg"
);

// Kernel.Application/Ai/ISpeechToTextService.cs
public interface ISpeechToTextService
{
    Task<SttResult> TranscribeAsync(byte[] audioBytes, SttOptions? options = null, CancellationToken cancellationToken = default);
}

public sealed record SttOptions(string? Language = null, bool Timestamps = false);
public sealed record SttResult(string Text, string? Language, IReadOnlyList<SttSegment>? Segments);
public sealed record SttSegment(double Start, double End, string Text);
```

### `IAiUsageTracker`

```csharp
// Kernel.Application/Ai/IAiUsageTracker.cs
namespace Product.Template.Kernel.Application.Ai;

/// <summary>
/// Registra consumo de tokens/requests para observabilidade e controle de custos.
/// </summary>
public interface IAiUsageTracker
{
    Task TrackAsync(AiUsageRecord record, CancellationToken cancellationToken = default);
}

public sealed record AiUsageRecord(
    string Service,           // "llm" | "embedding" | "ocr" | "tts" | "stt"
    string Provider,          // "openai" | "azure-openai" | "azure-cognitive"
    string Model,
    string Module,            // módulo que originou a chamada
    string Operation,         // nome do handler/use case
    long TenantId,
    int? TokensUsed,
    TimeSpan Latency,
    bool Success,
    string? ErrorCode = null
);
```

---

## 4. Padrões por capacidade

### 4.1 LLM (geração de texto)

#### Estrutura do handler

```csharp
// Finance.Application/Handlers/Ai/SummarizeInvoiceCommandHandler.cs
public sealed class SummarizeInvoiceCommandHandler
    : ICommandHandler<SummarizeInvoiceCommand, SummarizeInvoiceOutput>
{
    private readonly IInvoiceRepository _invoices;
    private readonly ILlmService _llm;
    private readonly IAiUsageTracker _tracker;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SummarizeInvoiceCommandHandler> _logger;

    public async Task<SummarizeInvoiceOutput> Handle(
        SummarizeInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException($"Invoice {request.InvoiceId} not found.");

        var llmRequest = InvoicePrompts.BuildSummaryRequest(invoice);

        var sw = Stopwatch.StartNew();
        var response = await _llm.CompleteAsync(llmRequest, cancellationToken);
        sw.Stop();

        await _tracker.TrackAsync(new AiUsageRecord(
            Service: "llm",
            Provider: "openai",
            Model: response.Model,
            Module: "finance",
            Operation: nameof(SummarizeInvoiceCommandHandler),
            TenantId: _tenantContext.Tenant.TenantId,
            TokensUsed: response.TotalTokens,
            Latency: sw.Elapsed,
            Success: true
        ), cancellationToken);

        _logger.LogInformation(
            "Invoice {InvoiceId} summarized. Tokens: {Tokens}, Latency: {Latency}ms",
            request.InvoiceId, response.TotalTokens, sw.ElapsedMilliseconds);

        return new SummarizeInvoiceOutput(response.Text, response.TotalTokens);
    }
}
```

#### Prompt como objeto de valor

```csharp
// Finance.Application/Ai/Prompts/InvoicePrompts.cs
namespace Product.Template.Core.Finance.Application.Ai.Prompts;

internal static class InvoicePrompts
{
    private const string SummarySystem =
        """
        Você é um assistente especializado em análise financeira.
        Responda sempre em português.
        Seja objetivo e destaque: valor total, fornecedor, vencimento e itens principais.
        Nunca invente informações que não estejam no contexto fornecido.
        """;

    public static LlmRequest BuildSummaryRequest(Invoice invoice) => new(
        SystemPrompt: SummarySystem,
        UserPrompt: $"""
            Resuma a seguinte nota fiscal em até 3 linhas:

            Fornecedor: {invoice.Supplier}
            Número: {invoice.Number}
            Data de emissão: {invoice.IssuedAt:dd/MM/yyyy}
            Vencimento: {invoice.DueDate:dd/MM/yyyy}
            Valor total: {invoice.Total:C}
            Itens:
            {string.Join("\n", invoice.Items.Select(i => $"- {i.Description}: {i.Amount:C}"))}
            """,
        Temperature: 0.1f,
        MaxTokens: 300
    );
}
```

---

### 4.2 Embeddings + RAG

O RAG tem 3 fases bem definidas: **indexação**, **retrieval** e **geração**. Cada fase tem seu lugar na arquitetura.

#### Fase 1 — Indexação (command ao persistir um documento)

```csharp
// Documents.Application/Handlers/Ai/IndexDocumentCommandHandler.cs
public sealed class IndexDocumentCommandHandler : ICommandHandler<IndexDocumentCommand>
{
    private readonly IDocumentRepository _documents;
    private readonly IDocumentVectorRepository _vectors;  // interface no Domain
    private readonly IEmbeddingService _embeddings;
    private readonly IUnitOfWork _unitOfWork;

    public async Task Handle(IndexDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(...);

        // 1. Chunking — dividir o texto em partes com overlap
        var chunks = ChunkText(document.Content, chunkSize: 500, overlap: 50);

        // 2. Embedding de cada chunk
        var embeddings = await _embeddings.EmbedBatchAsync(chunks, cancellationToken);

        // 3. Persistir no vector store
        var vectors = chunks.Zip(embeddings, (text, emb) =>
            new DocumentVector(document.Id, text, emb.Vector));

        await _vectors.UpsertAsync(vectors, cancellationToken);
        await _unitOfWork.Commit(cancellationToken);

        _logger.LogInformation(
            "Document {DocumentId} indexed: {ChunkCount} chunks", request.DocumentId, chunks.Count);
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var words = text.Split(' ');
        for (int i = 0; i < words.Length; i += chunkSize - overlap)
        {
            var chunk = string.Join(" ", words.Skip(i).Take(chunkSize));
            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);
        }
        return chunks;
    }
}
```

#### Fase 2 e 3 — Retrieval + Geração (query RAG)

```csharp
// Documents.Application/Handlers/Ai/SearchDocumentsQueryHandler.cs
public sealed class SearchDocumentsQueryHandler
    : IQueryHandler<SearchDocumentsQuery, RagAnswer>
{
    private readonly IDocumentVectorRepository _vectors;
    private readonly IEmbeddingService _embeddings;
    private readonly ILlmService _llm;

    public async Task<RagAnswer> Handle(
        SearchDocumentsQuery request, CancellationToken cancellationToken)
    {
        // 1. Vetorizar a pergunta
        var queryEmbedding = await _embeddings.EmbedAsync(request.Question, cancellationToken);

        // 2. Buscar os chunks mais similares
        var chunks = await _vectors.SearchSimilarAsync(
            queryEmbedding.Vector, topK: 5, minScore: 0.75f, cancellationToken);

        if (chunks.Count == 0)
            return new RagAnswer("Não encontrei informações relevantes sobre sua pergunta.", []);

        // 3. Montar contexto e gerar resposta
        var context = string.Join("\n\n", chunks.Select((c, i) => $"[{i + 1}] {c.Text}"));
        var response = await _llm.CompleteAsync(new LlmRequest(
            SystemPrompt: DocumentPrompts.RagSystem,
            UserPrompt: DocumentPrompts.BuildRagQuestion(request.Question, context),
            Temperature: 0.1f,
            MaxTokens: 800
        ), cancellationToken);

        return new RagAnswer(response.Text, chunks.Select(c => c.DocumentId).Distinct().ToList());
    }
}

public sealed record RagAnswer(string Text, IReadOnlyList<Guid> SourceDocumentIds);
```

#### Interface do vector repository (no Domain)

```csharp
// Documents.Domain/Repositories/IDocumentVectorRepository.cs
namespace Product.Template.Core.Documents.Domain.Repositories;

public interface IDocumentVectorRepository
{
    Task UpsertAsync(IEnumerable<DocumentVector> vectors, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> SearchSimilarAsync(
        float[] queryVector, int topK, float minScore = 0.7f,
        CancellationToken cancellationToken = default);
    Task DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public sealed record DocumentVector(Guid DocumentId, string Text, float[] Vector);
public sealed record DocumentChunk(Guid DocumentId, string Text, float Score);
```

---

### 4.3 OCR (extração de texto de imagens/PDF)

#### Padrão de uso no handler

```csharp
// Finance.Application/Handlers/Ai/ExtractInvoiceFieldsCommandHandler.cs
public sealed class ExtractInvoiceFieldsCommandHandler
    : ICommandHandler<ExtractInvoiceFieldsCommand, ExtractedInvoiceFields>
{
    private readonly IOcrService _ocr;
    private readonly ILlmService _llm;

    public async Task<ExtractedInvoiceFields> Handle(
        ExtractInvoiceFieldsCommand request, CancellationToken cancellationToken)
    {
        // 1. Extrair texto bruto do PDF/imagem
        var ocrResult = await _ocr.ExtractTextAsync(new OcrRequest(
            FileBytes: request.FileBytes,
            MimeType: request.MimeType,
            ExtractKeyValues: true,
            ExtractTables: true
        ), cancellationToken);

        // 2. Usar LLM para estruturar os campos extraídos
        var llmRequest = InvoicePrompts.BuildExtractionRequest(ocrResult.FullText);
        var llmResponse = await _llm.CompleteAsync(llmRequest, cancellationToken);

        // 3. Deserializar JSON retornado pelo LLM
        return JsonSerializer.Deserialize<ExtractedInvoiceFields>(llmResponse.Text)
            ?? throw new DomainException("Não foi possível extrair campos da nota fiscal.");
    }
}

public sealed record ExtractedInvoiceFields(
    string? InvoiceNumber,
    string? Supplier,
    decimal? Total,
    DateTime? IssuedAt,
    DateTime? DueDate,
    float ConfidenceScore
);
```

#### Prompt de extração estruturada

```csharp
// Finance.Application/Ai/Prompts/InvoicePrompts.cs (continuação)
public static LlmRequest BuildExtractionRequest(string rawText) => new(
    SystemPrompt:
        """
        Você é um extrator de dados de documentos fiscais.
        Retorne APENAS JSON válido, sem markdown, sem explicações.
        Se um campo não for encontrado, use null.
        Confidence score: 0.0 a 1.0 baseado em quão claro o texto estava.
        """,
    UserPrompt:
        $"""
        Extraia os campos do seguinte texto de nota fiscal e retorne no formato:
        {{
          "invoiceNumber": "string ou null",
          "supplier": "string ou null",
          "total": número ou null,
          "issuedAt": "yyyy-MM-dd ou null",
          "dueDate": "yyyy-MM-dd ou null",
          "confidenceScore": número entre 0 e 1
        }}

        Texto:
        {rawText}
        """,
    Temperature: 0.0f,   // determinístico para extração
    MaxTokens: 500
);
```

---

### 4.4 Text-to-Speech e Speech-to-Text

```csharp
// Support.Application/Handlers/Ai/GenerateAudioResponseCommandHandler.cs
public sealed class GenerateAudioResponseCommandHandler
    : ICommandHandler<GenerateAudioResponseCommand, AudioResponse>
{
    private readonly ILlmService _llm;
    private readonly ITextToSpeechService _tts;

    public async Task<AudioResponse> Handle(
        GenerateAudioResponseCommand request, CancellationToken cancellationToken)
    {
        // 1. Gerar resposta textual
        var textResponse = await _llm.CompleteAsync(new LlmRequest(
            SystemPrompt: SupportPrompts.AgentSystem,
            UserPrompt: request.UserQuestion,
            History: request.ConversationHistory
        ), cancellationToken);

        // 2. Sintetizar áudio
        var audioBytes = await _tts.SynthesizeAsync(
            textResponse.Text,
            new TtsOptions(Voice: "nova", Language: "pt-BR", Speed: 1.0f),
            cancellationToken);

        return new AudioResponse(textResponse.Text, audioBytes, "audio/mpeg");
    }
}

// Support.Application/Handlers/Ai/TranscribeUserMessageCommandHandler.cs
public sealed class TranscribeUserMessageCommandHandler
    : ICommandHandler<TranscribeUserMessageCommand, string>
{
    private readonly ISpeechToTextService _stt;

    public async Task<string> Handle(
        TranscribeUserMessageCommand request, CancellationToken cancellationToken)
    {
        var result = await _stt.TranscribeAsync(
            request.AudioBytes,
            new SttOptions(Language: "pt-BR"),
            cancellationToken);

        return result.Text;
    }
}
```

---

## 5. Agente Cross-Módulo (Chat em Linguagem Natural)

### 5.1 Visão geral do padrão

Quando o frontend tem um chat onde o usuário pode perguntar qualquer coisa sobre qualquer módulo — *"Qual foi o faturamento de março?"*, *"Quantos usuários novos hoje?"*, *"Abra um ticket para a Acme"* — o padrão correto é um **AI Agent com tool calling**.

O agente não usa `switch` manual de módulos. Você expõe as operações dos módulos como **tools** (ferramentas com schema JSON), e o modelo LLM decide dinamicamente quais chamar e com quais parâmetros. Isso é conhecido como **function calling** (OpenAI) ou **tool use** (Anthropic Claude).

```
User: "Qual faturamento de março e quantos usuários novos?"
        │
        ▼
  ChatCommandHandler
        │
        ▼
  AgentLoop ──► ILlmService (envia tools disponíveis)
        │            │
        │     LLM decide:
        │       → GetRevenueSummaryTool({ month: 3, year: 2026 })
        │       → GetUsersSummaryTool({ month: 3, year: 2026 })
        │            │
        ▼            ▼
  Tool.ExecuteAsync() → _mediator.Send(Query) → resultado JSON
        │
        ▼
  AgentLoop ──► ILlmService (envia resultados das tools)
        │
        ▼
  LLM formula resposta final em linguagem natural
        │
        ▼
  ChatOutput("Em março seu faturamento foi R$ 48.200 e houve 37 novos usuários.")
```

**Princípio chave**: módulos nunca se enxergam. As Tools do módulo `Ai` chamam os módulos via `IMediator` — a única dependência é nos tipos de query/command (MediatR messages), não nos handlers.

---

### 5.2 Estrutura do módulo Ai

```
src/Core/Ai/
  Ai.Application/
    Handlers/
      ChatCommand.cs                   ← record ChatCommand(string Message, ...)
      ChatOutput.cs                    ← record ChatOutput(string Reply, int IterationsUsed)
      ChatCommandHandler.cs            ← entry point: orquestra AgentLoop + tracker
      ChatCommandValidator.cs          ← valida Message (required, max 4000 chars)
    Agent/
      AgentLoop.cs                     ← loop ReAct: Reason → Act (paralelo) → Observe
      ToolRegistry.cs                  ← catálogo de tools; descoberta via IEnumerable<ITool>
      Tools/
        GetUsersSummaryTool.cs         ← wrapa ListUserQuery (Identity)
        GetTenantInfoTool.cs           ← wrapa ListTenantsQuery (Tenants)
        ─── (adicionar tools de novos módulos aqui) ───
    Prompts/
      AgentSystemPrompt.cs             ← instrução de sistema do agente (pt-BR)
  Ai.Infrastructure/
    Ai.Infrastructure.csproj
    DependencyInjection.cs             ← AddAiModule(): ToolRegistry, AgentLoop, tools
```

---

### 5.3 Contrato ITool

```csharp
// Kernel.Application/Ai/ITool.cs
namespace Product.Template.Kernel.Application.Ai;

/// <summary>
/// Representa uma capacidade que o agente pode invocar.
/// Cada módulo expõe suas operações como implementações desta interface.
/// O LLM lê Definition.Name + Definition.Description + Definition.InputSchema
/// para decidir quando e como usar a tool.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Metadados da tool (nome, descrição, schema) enviados ao LLM.
    /// </summary>
    ToolDefinition Definition { get; }

    /// <summary>
    /// Executa a operação e retorna o resultado serializado como JSON string.
    /// <paramref name="call"/> contém o Id, o Name e os Parameters escolhidos pelo LLM.
    /// </summary>
    Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default);
}
```

`ToolDefinition` e `ToolCall` estão em `Kernel.Application/Ai/Models/`:

```csharp
public sealed record ToolDefinition(
    string Name,         // snake_case, único no sistema — o LLM referencia pelo nome
    string Description,  // descreve QUANDO usar, não apenas o que faz (em inglês)
    JsonObject InputSchema  // JSON Schema dos parâmetros
);

public sealed record ToolCall(
    string Id,            // ID único gerado pelo LLM (enviado de volta com o resultado)
    string Name,          // nome da tool escolhida pelo LLM
    JsonObject Parameters // argumentos preenchidos pelo LLM
);
```

---

### 5.4 ToolRegistry — catálogo de ferramentas

```csharp
// Ai.Application/Agent/ToolRegistry.cs
namespace Product.Template.Core.Ai.Application.Agent;

public sealed class ToolRegistry
{
    private readonly IReadOnlyDictionary<string, ITool> _tools;

    // Descoberta automática via DI — recebe IEnumerable<ITool> injetado
    public ToolRegistry(IEnumerable<ITool> tools) =>
        _tools = tools.ToDictionary(t => t.Definition.Name);

    /// <summary>Schemas enviados ao LLM para que ele conheça as tools disponíveis.</summary>
    public IReadOnlyList<ToolDefinition> GetDefinitions() =>
        _tools.Values.Select(t => t.Definition).ToList();

    /// <summary>Executa a tool escolhida pelo LLM e devolve o resultado como JSON string.</summary>
    public Task<string> ExecuteAsync(ToolCall call, CancellationToken ct) =>
        _tools.TryGetValue(call.Name, out var tool)
            ? tool.ExecuteAsync(call, ct)
            : Task.FromResult($"{{\"error\": \"Tool '{call.Name}' not found.\"}}");
}
```

---

### 5.5 AgentLoop — Reason → Act → Observe

O loop implementa o padrão **ReAct** (Reason + Act): o LLM raciocina, decide quais tools chamar, recebe os resultados e raciocina novamente até ter informação suficiente para responder.

```csharp
// Ai.Application/Agent/AgentLoop.cs
namespace Product.Template.Core.Ai.Application.Agent;

public sealed class AgentLoop
{
    private const int MaxIterations = 5;   // teto de segurança contra loops infinitos

    private readonly ILlmService _llm;
    private readonly ToolRegistry _toolRegistry;
    private readonly ILogger<AgentLoop> _logger;

    public AgentLoop(ILlmService llm, ToolRegistry toolRegistry, ILogger<AgentLoop> logger)
    {
        _llm = llm;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<AgentResult> RunAsync(
        string userMessage,
        string systemPrompt,        // injetado pelo ChatCommandHandler (AgentSystemPrompt.Text)
        IReadOnlyList<LlmMessage>? history,
        CancellationToken cancellationToken)
    {
        var conversationHistory = history?.ToList() ?? [];
        var toolDefinitions = _toolRegistry.GetDefinitions();
        var iterations = 0;

        while (iterations < MaxIterations)
        {
            iterations++;

            // ── REASON ────────────────────────────────────────────────────────
            var request = new LlmRequest(
                UserPrompt: userMessage,
                SystemPrompt: systemPrompt,
                History: conversationHistory.Count > 0 ? conversationHistory : null,
                Tools: toolDefinitions.Count > 0 ? toolDefinitions : null
            );

            var response = await _llm.CompleteAsync(request, cancellationToken);

            // Resposta final: nenhuma tool chamada → o LLM tem informação suficiente
            if (response.ToolCalls is not { Count: > 0 })
                return new AgentResult(response.Text, iterations, response.TotalTokens);

            // Adiciona a decisão do assistente ao histórico
            conversationHistory.Add(new LlmMessage("assistant", response.Text ?? string.Empty));

            // ── ACT (paralelo) + OBSERVE ───────────────────────────────────────
            var toolResults = await Task.WhenAll(
                response.ToolCalls.Select(tc => ExecuteToolAsync(tc, cancellationToken)));

            foreach (var result in toolResults)
                conversationHistory.Add(new LlmMessage("tool", result.Output, result.ToolCallId));

            userMessage = string.Empty;  // nas iterações seguintes o histórico fala por si
        }

        _logger.LogWarning("AgentLoop reached max iterations ({Max}) without a final answer", MaxIterations);

        // Fallback: pede ao LLM que sumarize o que foi encontrado
        var fallback = new LlmRequest(
            UserPrompt: "Resuma o que foi encontrado até agora com base nos dados das ferramentas.",
            SystemPrompt: systemPrompt,
            History: conversationHistory.Count > 0 ? conversationHistory : null
        );
        var fallbackResponse = await _llm.CompleteAsync(fallback, cancellationToken);
        return new AgentResult(fallbackResponse.Text, iterations, fallbackResponse.TotalTokens);
    }

    private async Task<ToolResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken ct)
    {
        _logger.LogInformation("Executing tool {ToolName} with params {Params}",
            toolCall.Name, toolCall.Parameters.ToJsonString());

        var output = await _toolRegistry.ExecuteAsync(toolCall, ct);
        return new ToolResult(toolCall.Id, output);
    }

    private sealed record ToolResult(string ToolCallId, string Output);
}

/// <summary>Resultado final do AgentLoop.</summary>
public sealed record AgentResult(string Reply, int IterationsUsed, int TotalTokens);
```

---

### 5.6 Exemplo de Tool por módulo

Cada módulo cria suas próprias tools em `Ai.Application/Agent/Tools/`. A tool:
1. Expõe um `ToolDefinition Definition { get; }` com `Name`, `Description` e `InputSchema` (o LLM lê isso)
2. Em `ExecuteAsync(ToolCall call, ...)`, extrai parâmetros de `call.Parameters` e despacha via `IMediator`

```csharp
// Ai.Application/Agent/Tools/GetUsersSummaryTool.cs
namespace Product.Template.Core.Ai.Application.Agent.Tools;

public sealed class GetUsersSummaryTool : ITool
{
    private readonly IMediator _mediator;

    public GetUsersSummaryTool(IMediator mediator) => _mediator = mediator;

    public ToolDefinition Definition { get; } = new(
        Name: "get_users_summary",
        Description: "Returns a summary of registered users in the system, including total count " +
                     "and recent registrations. Use when the user asks about: number of users, " +
                     "new registrations, active users, or user growth.",
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

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        var pageSize = call.Parameters["page_size"]?.GetValue<int>() ?? 10;
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = new ListUserQuery { PageSize = pageSize, PageNumber = 1 };
        var result = await _mediator.Send(query, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            total_count = result.TotalCount,
            users = result.Data.Select(u => new
            {
                id = u.Id,
                email = u.Email,
                name = $"{u.FirstName} {u.LastName}".Trim(),
                email_confirmed = u.EmailConfirmed,
                created_at = u.CreatedAt
            })
        });
    }
}
```

```csharp
// Ai.Application/Agent/Tools/GetTenantInfoTool.cs
public sealed class GetTenantInfoTool : ITool
{
    private readonly IMediator _mediator;

    public GetTenantInfoTool(IMediator mediator) => _mediator = mediator;

    public ToolDefinition Definition { get; } = new(
        Name: "get_tenant_info",
        Description: "Returns information about the tenants configured in the system. " +
                     "Use when the user asks about tenants, clients, or multi-tenancy configuration.",
        InputSchema: new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["page_size"] = new JsonObject
                {
                    ["type"] = "integer",
                    ["description"] = "Number of tenants to retrieve (default: 20)"
                }
            },
            ["required"] = new JsonArray()
        }
    );

    public async Task<string> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        var pageSize = call.Parameters["page_size"]?.GetValue<int>() ?? 20;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = new ListTenantsQuery(PageNumber: 1, PageSize: pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            total_count = result.TotalCount,
            tenants = result.Data.Select(t => new
            {
                tenant_id = t.TenantId,
                tenant_key = t.TenantKey,
                display_name = t.DisplayName,
                isolation_mode = t.IsolationMode.ToString(),
                is_active = t.IsActive
            })
        });
    }
}
```

> **Como adicionar uma tool de novo módulo**: crie o arquivo em `Ai.Application/Agent/Tools/`, implemente `ITool`, e registe-a em `Ai.Infrastructure/DependencyInjection.cs` com `services.AddScoped<ITool, SuaTool>()`. O `ToolRegistry` descobre via `IEnumerable<ITool>` no próximo boot.

---

### 5.7 ChatCommandHandler — entry point

```csharp
// Ai.Application/Handlers/ChatCommand.cs
public record ChatCommand(
    string Message,
    IReadOnlyList<LlmMessage>? History = null
) : ICommand<ChatOutput>;

// Ai.Application/Handlers/ChatOutput.cs
public record ChatOutput(
    string Reply,           // resposta em linguagem natural
    int IterationsUsed      // quantas iterações do AgentLoop foram necessárias
);
```

```csharp
// Ai.Application/Handlers/ChatCommandHandler.cs
namespace Product.Template.Core.Ai.Application.Handlers;

internal sealed class ChatCommandHandler : ICommandHandler<ChatCommand, ChatOutput>
{
    private readonly AgentLoop _agentLoop;
    private readonly IAiUsageTracker _tracker;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ChatCommandHandler> _logger;

    public async Task<ChatOutput> Handle(ChatCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AI chat request for tenant {TenantId}", _tenantContext.TenantId);

        var started = DateTime.UtcNow;
        AgentResult? result = null;
        string? errorCode = null;

        try
        {
            result = await _agentLoop.RunAsync(
                command.Message,
                AgentSystemPrompt.Text,    // prompt injetado aqui — handler decide o contexto
                command.History,
                cancellationToken);

            return new ChatOutput(result.Reply, result.IterationsUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentLoop failed for tenant {TenantId}", _tenantContext.TenantId);
            errorCode = ex.GetType().Name;
            throw;
        }
        finally
        {
            var latency = DateTime.UtcNow - started;
            await _tracker.TrackAsync(new AiUsageRecord(
                Service: "llm",
                Provider: "azure-openai",
                Model: "agent",
                Module: "ai",
                Operation: "chat",
                TenantId: _tenantContext.TenantId ?? 0,
                TokensUsed: result?.TotalTokens,
                Latency: latency,
                Success: errorCode is null,
                ErrorCode: errorCode
            ), cancellationToken);
        }
    }
}
```

O endpoint no `Api` é um controller simples:

```csharp
// Api/Controllers/v1/AiController.cs
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Tags("AI")]
public class AiController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("chat")]
    [Authorize(Policy = SecurityConfiguration.AuthenticatedPolicy)]
    [ProducesResponseType(typeof(ChatOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChatOutput>> Chat(
        [FromBody] ChatCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
```

---

### 5.8 Registro de Tools no DI

Tools são registadas explicitamente em `Ai.Infrastructure/DependencyInjection.cs`. O `ToolRegistry` recebe `IEnumerable<ITool>` via DI e descobre automaticamente as tools em runtime:

```csharp
// Ai.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddAiModule(this IServiceCollection services)
    {
        services.AddScoped<ToolRegistry>();
        services.AddScoped<AgentLoop>();

        // Registe uma linha por tool — o ToolRegistry descobre via IEnumerable<ITool>
        services.AddScoped<ITool, GetUsersSummaryTool>();
        services.AddScoped<ITool, GetTenantInfoTool>();
        // services.AddScoped<ITool, GetRevenueSummaryTool>();  // adicionar quando o módulo existir

        return services;
    }
}
```

**Como adicionar a tool de um novo módulo:**

1. Crie `{NewModule}Tool.cs` em `Ai.Application/Agent/Tools/`
2. Implemente `ITool` — defina `Definition` (com `Name`, `Description`, `InputSchema`) e `ExecuteAsync(ToolCall call, ...)`
3. Registe em `DependencyInjection.cs`: `services.AddScoped<ITool, SuaNovaTool>()`
4. Pronto. O `ToolRegistry` constrói o catálogo automaticamente via `IEnumerable<ITool>`.

Não é necessário alterar `AgentLoop`, `ToolRegistry` nem `ChatCommandHandler`.

---

### 5.9 Dashboard sem agente (agregação simples)

Para home pages que exibem dados de múltiplos módulos **sem linguagem natural** — apenas cards com totais — não use o agente. Use um controller dedicado na camada Api que dispara queries em paralelo:

```csharp
// Api/Controllers/v1/DashboardController.cs
[HttpGet]
[Authorize(Policy = SecurityConfiguration.AuthenticatedPolicy)]
public async Task<ActionResult<DashboardOutput>> GetDashboard(CancellationToken ct)
{
    // Queries totalmente independentes — executadas em paralelo
    var (users, revenue, tickets) = await (
        _mediator.Send(new GetUsersSummaryQuery(), ct),
        _mediator.Send(new GetRevenueSummaryQuery(DateTime.UtcNow.Month, DateTime.UtcNow.Year), ct),
        _mediator.Send(new GetOpenTicketsSummaryQuery(), ct)
    );

    return Ok(new DashboardOutput(users, revenue, tickets));
}
```

| Cenário | Solução |
|---------|---------|
| Cards/KPIs na home page | `DashboardController` com queries em paralelo |
| Chat em linguagem natural | `AgentLoop` com tools + `ChatCommandHandler` |
| Workflow multi-step guiado por IA | `AgentLoop` com tools de comando (write) |

---

## 6. Adicionando IA a um módulo

### Passo a passo

**1. Declare a dependência no `.csproj` do Application**

```xml
<!-- {Module}.Application.csproj -->
<ProjectReference Include="..\..\..\Shared\Kernel.Application\Kernel.Application.csproj" />
```
> Não adicione referência direta ao SDK de IA (OpenAI, Azure) no Application. Apenas o Infrastructure pode fazê-lo.

**2. Crie os prompts como constantes tipadas**

```
{Module}.Application/Ai/Prompts/{UseCase}Prompts.cs
```

**3. Crie o handler**

```
{Module}.Application/Handlers/Ai/{UseCase}CommandHandler.cs
```

**4. Crie o validator (obrigatório)**

```csharp
// {Module}.Application/Validators/{UseCase}CommandValidator.cs
public class ExtractInvoiceFieldsCommandValidator
    : AbstractValidator<ExtractInvoiceFieldsCommand>
{
    public ExtractInvoiceFieldsCommandValidator()
    {
        RuleFor(x => x.FileBytes).NotEmpty().WithMessage("File is required.");
        RuleFor(x => x.MimeType)
            .Must(m => m is "application/pdf" or "image/png" or "image/jpeg")
            .WithMessage("Only PDF, PNG and JPEG files are supported.");
    }
}
```

**5. Registre os serviços de IA no DI**

A ativação é feita via feature flag em `Api/Configurations/AiConfiguration.cs`. O `IDistributedCache` é registado automaticamente (Redis se configurado, memória como fallback):

```csharp
// Api/Configurations/AiConfiguration.cs
public static IServiceCollection AddAiConfiguration(
    this IServiceCollection services, IConfiguration configuration)
{
    var aiEnabled = configuration.GetValue<bool>("FeatureFlags:EnableAI");

    if (!aiEnabled)
    {
        services.AddNullAiServices();
        return services;
    }

    var redis = configuration["Redis:ConnectionString"];
    if (!string.IsNullOrEmpty(redis))
        services.AddStackExchangeRedisCache(o => o.Configuration = redis);
    else
        services.AddDistributedMemoryCache();   // fallback — não persistente entre restarts

    services.AddAiServices(configuration);
    return services;
}

// Para ambientes sem IA (testes, dev sem credenciais):
public static IServiceCollection AddNullAiServices(this IServiceCollection services)
{
    services.AddSingleton<ILlmService, NullLlmService>();
    services.AddSingleton<IEmbeddingService, NullEmbeddingService>();
    services.AddSingleton<IOcrService, NullOcrService>();
    services.AddSingleton<ITextToSpeechService, NullTextToSpeechService>();
    services.AddSingleton<ISpeechToTextService, NullSpeechToTextService>();
    services.AddSingleton<IAiUsageTracker, NullAiUsageTracker>();
    return services;
}

// Para produção (Azure):
public static IServiceCollection AddAiServices(
    this IServiceCollection services, IConfiguration configuration)
{
    services.Configure<AiOptions>(configuration.GetSection("AI"));
    var opts = configuration.GetSection("AI").Get<AiOptions>() ?? new AiOptions();

    services.AddSingleton(_ => new AzureOpenAIClient(
        new Uri(opts.AzureOpenAI.Endpoint),
        new AzureKeyCredential(opts.AzureOpenAI.ApiKey)));

    services.AddScoped<ILlmService, AzureOpenAiLlmService>();
    services.AddScoped<AzureOpenAiEmbeddingService>();
    services.AddScoped<IEmbeddingService>(sp =>
        new CachedEmbeddingService(
            sp.GetRequiredService<AzureOpenAiEmbeddingService>(),
            sp.GetRequiredService<IDistributedCache>()));

    services.AddScoped<IOcrService, AzureOcrService>();
    services.AddScoped<ITextToSpeechService, AzureTextToSpeechService>();
    services.AddScoped<ISpeechToTextService, AzureSpeechToTextService>();

    services.AddSingleton<IAiUsageTracker, AiUsageTracker>();

    return services;
}
```

**6. Configure o `appsettings.json`**

```json
{
  "FeatureFlags": {
    "EnableAI": false
  },

  "Redis": {
    "ConnectionString": ""
  },

  "AI": {
    "DefaultModel": "gpt-4o-mini",
    "MaxTokens": 4096,
    "Temperature": 0.2,

    "AzureOpenAI": {
      "Endpoint": "https://<resource>.openai.azure.com/",
      "ApiKey": "",
      "ChatDeploymentName": "gpt-4o-mini",
      "EmbeddingDeploymentName": "text-embedding-3-small"
    },

    "AzureCognitive": {
      "DocumentIntelligenceEndpoint": "https://<resource>.cognitiveservices.azure.com/",
      "DocumentIntelligenceApiKey": "",
      "SpeechServiceKey": "",
      "SpeechServiceRegion": "eastus"
    }
  }
}
```

> **Segredos**: Nunca commite `ApiKey` ou `SpeechServiceKey`. Use `secrets.json` em dev e variáveis de ambiente / Azure Key Vault em produção.

---

### Prompt do sistema do agente

O `AgentSystemPrompt` é o mais crítico do sistema — define o comportamento de toda a interação:

```csharp
// Ai.Application/Ai/Prompts/AgentSystemPrompt.cs
namespace Product.Template.Core.Ai.Application.Ai.Prompts;

internal static class AgentSystemPrompt
{
    public static string Build() =>
        """
        Você é um assistente inteligente integrado a um sistema de gestão empresarial.
        Responda sempre em português do Brasil, de forma objetiva e profissional.

        Regras obrigatórias:
        - Use as ferramentas disponíveis para buscar dados antes de responder.
        - NUNCA invente números, datas ou informações — use apenas dados retornados pelas ferramentas.
        - Se uma ferramenta não retornar dados suficientes, informe que não encontrou as informações.
        - Não revele detalhes técnicos de implementação (nomes de tabelas, IDs internos, etc.).
        - Ao apresentar valores monetários, use o formato R$ X.XXX,XX.
        - Ao apresentar datas, use o formato dd/MM/yyyy.
        - Se a pergunta não puder ser respondida com as ferramentas disponíveis, diga claramente.
        """;
}
```

---

## 7. Prompt Engineering

### Regras fundamentais

| Regra | Por quê |
|-------|---------|
| **Sempre use `SystemPrompt`** | Define o comportamento e restringe o escopo; evita "jailbreak" acidental |
| **`Temperature: 0.0–0.2` para extração/classificação** | Respostas determinísticas e consistentes |
| **`Temperature: 0.5–0.8` para criação/sumarização** | Mais natural, menos mecânico |
| **Nunca interpole dados do usuário diretamente no prompt de sistema** | Previne prompt injection |
| **Peça JSON quando precisar de dados estruturados** | Mais confiável que regex no texto livre |
| **Defina `MaxTokens` sempre** | Evita respostas inesperadamente longas e custos altos |
| **Inclua `"Nunca invente informações"` no SystemPrompt** | Reduz alucinação em RAG e extração |

### Prevenção de prompt injection

```csharp
// ❌ VULNERÁVEL — dados do usuário contaminam o system prompt
var prompt = $"Você é assistente de {tenantName}. Pergunta: {userInput}";

// ✅ SEGURO — dados do usuário ficam isolados no UserPrompt
var request = new LlmRequest(
    SystemPrompt: "Você é um assistente financeiro. Responda apenas sobre finanças.",
    UserPrompt: SanitizeUserInput(userInput)   // sanitize antes de usar
);

private static string SanitizeUserInput(string input) =>
    input.Replace("Ignore previous instructions", "")
         .Trim()
         [..Math.Min(input.Length, 2000)];    // limitar tamanho do input
```

### Estrutura de um prompt de extração (output JSON)

```csharp
// Padrão "Output Schema" — instrua o modelo sobre o formato exato
private const string ExtractionSystem =
    """
    Você extrai dados estruturados de textos.

    Regras obrigatórias:
    - Retorne APENAS JSON válido, sem markdown, sem texto adicional
    - Use null para campos não encontrados; nunca invente valores
    - Datas no formato ISO 8601 (yyyy-MM-dd)
    - Números sem formatação de moeda (ex: 1234.56 não R$ 1.234,56)

    Schema esperado:
    { "field1": "tipo", "field2": "tipo | null", ... }
    """;
```

### Versionamento de prompts

```csharp
// {Module}.Application/Ai/Prompts/{UseCase}Prompts.cs
internal static class InvoicePrompts
{
    // Versione prompts como constantes nomeadas — mudanças são rastreadas no git
    private const string SummarySystemV2 = "...";  // quebra de linha intencional no histórico

    // Exponha a versão ativa
    public static LlmRequest BuildSummaryRequest(Invoice invoice) =>
        BuildSummaryRequestV2(invoice);

    private static LlmRequest BuildSummaryRequestV2(Invoice invoice) => new(
        SystemPrompt: SummarySystemV2,
        UserPrompt: $"...",
        Temperature: 0.1f
    );
}
```

---

## 8. Observabilidade e custos

### Logging estruturado em todo handler de IA

```csharp
_logger.LogInformation(
    "AI call completed. Module: {Module} Operation: {Operation} " +
    "Provider: {Provider} Model: {Model} Tokens: {Tokens} Latency: {LatencyMs}ms TenantId: {TenantId}",
    "finance", nameof(SummarizeInvoiceCommandHandler),
    "openai", response.Model, response.TotalTokens, latency, tenantId);
```

### Métricas recomendadas (OpenTelemetry)

```csharp
// Kernel.Infrastructure/Ai/AiMetrics.cs
public static class AiMetrics
{
    private static readonly Meter Meter = new("Product.Template.Ai");

    public static readonly Counter<long> TotalTokens =
        Meter.CreateCounter<long>("ai.tokens.total", "tokens", "Total tokens consumed");

    public static readonly Histogram<double> Latency =
        Meter.CreateHistogram<double>("ai.request.duration", "ms", "AI request latency");

    public static readonly Counter<long> Errors =
        Meter.CreateCounter<long>("ai.errors.total", "errors", "AI request errors");
}

// Uso na implementação
AiMetrics.TotalTokens.Add(response.TotalTokens,
    new("provider", provider), new("model", model),
    new("module", module), new("tenant_id", tenantId));
```

### Caching de embeddings

Embeddings do mesmo texto produzem sempre o mesmo vetor — são candidatos naturais a cache:

```csharp
// Kernel.Infrastructure/Ai/CachedEmbeddingService.cs
public sealed class CachedEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingService _inner;
    private readonly IDistributedCache _cache;

    public async Task<EmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var key = $"emb:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))}";
        var cached = await _cache.GetAsync(key, cancellationToken);

        if (cached is not null)
            return JsonSerializer.Deserialize<EmbeddingResult>(cached)!;

        var result = await _inner.EmbedAsync(text, cancellationToken);

        await _cache.SetAsync(key,
            JsonSerializer.SerializeToUtf8Bytes(result),
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(7) },
            cancellationToken);

        return result;
    }
}
```

Registre o decorator no DI (já incluído em `AddAiServices`):

```csharp
services.AddScoped<AzureOpenAiEmbeddingService>();
services.AddScoped<IEmbeddingService>(sp =>
    new CachedEmbeddingService(
        sp.GetRequiredService<AzureOpenAiEmbeddingService>(),
        sp.GetRequiredService<IDistributedCache>()));
```

O `CachedEmbeddingService` usa `AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)` e chave de cache SHA-256 do texto (`emb:<hex>`).

---

## 9. Tratamento de erros e resiliência

### Categorias de erro em IA

| Tipo | Causa | Tratamento |
|------|-------|------------|
| `RateLimitException` | Muitas requisições | Retry com exponential backoff |
| `TimeoutException` | Modelo lento / rede | Retry limitado + fallback |
| `InvalidResponseException` | JSON malformado do LLM | Retry com prompt ajustado; máx. 2 tentativas |
| `ContextTooLargeException` | Input maior que a janela | Truncar input e tentar novamente |
| `ServiceUnavailableException` | Provider fora do ar | Circuit breaker + fallback |

### Polly para resiliência

`AzureOpenAiLlmService.CompleteAsync` tem um pipeline Polly v8 estático (retry → timeout → circuit breaker):

| Camada | Configuração |
|--------|-------------|
| Retry | 3x, exponential backoff + jitter, 1s base |
| Timeout | 30s por tentativa |
| Circuit breaker | 50% falhas / 5 req mín / 1 min → break 2 min |

Erros tratados: `429`, `503` (Azure), `TimeoutRejectedException`, `HttpRequestException`.

```csharp
// Kernel.Infrastructure/Ai/AzureOpenAiLlmService.cs
private static readonly ResiliencePipeline<LlmResponse> Pipeline =
    new ResiliencePipelineBuilder<LlmResponse>()
        .AddRetry(new RetryStrategyOptions<LlmResponse>
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(1),
            ShouldHandle = new PredicateBuilder<LlmResponse>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .Handle<Azure.RequestFailedException>(e => e.Status is 429 or 503)
        })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<LlmResponse>
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromMinutes(1),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromMinutes(2),
            ShouldHandle = new PredicateBuilder<LlmResponse>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .Handle<Azure.RequestFailedException>(e => e.Status is 429 or 503)
        })
        .Build();

public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
{
    return await Pipeline.ExecuteAsync(async ct =>
    {
        // ... chamada ao AzureOpenAIClient ...
    }, cancellationToken);
}
```

> `StreamAsync` não usa o pipeline — o caller controla o ciclo de vida via `CancellationToken`.

### Resposta de fallback

Para features não críticas, nunca falhe a operação principal por causa de IA:

```csharp
// No handler — IA é enrichment, não bloqueante
try
{
    var summary = await _llm.CompleteAsync(request, cancellationToken);
    invoice.AttachSummary(summary.Text);
}
catch (Exception ex)
{
    // IA falhou — continua sem summary, não bloqueia o save
    _logger.LogWarning(ex, "AI summarization failed for invoice {InvoiceId}. Continuing without summary.", invoiceId);
}

await _unitOfWork.Commit(cancellationToken);
```

---

## 10. Testes

### Princípio: nunca chame APIs de IA nos testes unitários

```csharp
// ❌ ERRADO — depende de API externa, lento, custoso, não determinístico
public class SummarizeInvoiceTests
{
    [Fact]
    public async Task Handle_ShouldSummarize()
    {
        var service = new AzureOpenAiLlmService(client, options);  // chama Azure OpenAI de verdade
        ...
    }
}

// ✅ CERTO — stub inline, rápido, determinístico, gratuito
public class SummarizeInvoiceTests
{
    [Fact]
    public async Task Handle_ShouldReturnSummary_WhenInvoiceExists()
    {
        var llm = new StubLlmService("Nota fiscal da Acme no valor de R$ 1.000,00.");
        var handler = new SummarizeInvoiceCommandHandler(
            invoices: new StubInvoiceRepository(FakeInvoice()),
            llm: llm,
            tracker: NoopAiUsageTracker.Instance,
            tenantContext: FakeTenantContext(),
            logger: NullLogger<SummarizeInvoiceCommandHandler>.Instance);

        var result = await handler.Handle(new SummarizeInvoiceCommand(Guid.NewGuid()), default);

        Assert.Equal("Nota fiscal da Acme no valor de R$ 1.000,00.", result.Summary);
    }

    // Stub inline — sem frameworks de mock
    private sealed class StubLlmService(string response) : ILlmService
    {
        public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken ct)
            => Task.FromResult(new LlmResponse(
                Text: response, PromptTokens: 10, CompletionTokens: 20,
                TotalTokens: 30, Model: "stub", Latency: TimeSpan.Zero));

        public async IAsyncEnumerable<string> StreamAsync(LlmRequest request, CancellationToken ct)
        {
            yield return response;
        }
    }
}
```

### O que testar em cada camada

| Camada | O que testar | Como |
|--------|-------------|------|
| **Application** | Lógica do handler (orquestração, tratamento de erros, fallback) | Stubs inline |
| **Prompts** | Que o prompt contém as informações esperadas (snapshot test) | Assert sobre `LlmRequest` construído |
| **Tools** | Que a tool constrói parâmetros corretamente e chama o mediator certo | Stub de IMediator |
| **AgentLoop** | Fluxo de iterações, execução paralela, teto de iterações | Stub de ILlmService + ToolRegistry |
| **Infrastructure** | Serialização/desserialização, retry, circuit breaker | Testes de integração com WireMock ou API local |
| **E2E** | Fluxo completo com resposta mockada via WireMock | `WebApplicationFactory` + WireMock |

### Testando o AgentLoop

O `AgentLoop` recebe `systemPrompt` como parâmetro e retorna `AgentResult` (com `Reply`, `IterationsUsed`, `TotalTokens`):

```csharp
// tests/UnitTests/Ai/AgentLoopTests.cs
public class AgentLoopTests
{
    [Fact]
    public async Task RunAsync_ShouldReturnDirectReply_WhenNoToolCallsRequested()
    {
        var llm = new StubLlmService("Hello from the model", toolCalls: null);
        var registry = new ToolRegistry([]);
        var loop = new AgentLoop(llm, registry, NullLogger<AgentLoop>.Instance);

        var result = await loop.RunAsync("Hi", "System", null, default);

        Assert.Equal("Hello from the model", result.Reply);
        Assert.Equal(1, result.IterationsUsed);
    }

    [Fact]
    public async Task RunAsync_ShouldCallToolAndReturnFinalAnswer_WhenToolCallRequested()
    {
        var toolCalls = new List<ToolCall>
        {
            new("call-1", "get_info", new JsonObject())
        };

        var llm = new SequentialLlmService(
            first: new LlmResponse("Thinking...", 10, 5, 15, "gpt-4o-mini", TimeSpan.Zero, toolCalls),
            second: new LlmResponse("Based on the data: 42 users.", 20, 10, 30, "gpt-4o-mini", TimeSpan.Zero)
        );

        var tool = new CountingTool("get_info");
        var registry = new ToolRegistry([tool]);
        var loop = new AgentLoop(llm, registry, NullLogger<AgentLoop>.Instance);

        var result = await loop.RunAsync("How many users?", "System", null, default);

        Assert.Equal("Based on the data: 42 users.", result.Reply);
        Assert.Equal(2, result.IterationsUsed);
        Assert.Equal(1, tool.CallCount);
    }

    [Fact]
    public async Task RunAsync_ShouldStopAtMaxIterations_WhenToolCallsNeverEnd()
    {
        var toolCalls = new List<ToolCall> { new("c1", "loop_tool", new JsonObject()) };
        var llm = new InfiniteToolCallLlm(toolCalls);
        var tool = new CountingTool("loop_tool");
        var registry = new ToolRegistry([tool]);
        var loop = new AgentLoop(llm, registry, NullLogger<AgentLoop>.Instance);

        var result = await loop.RunAsync("Infinite?", "System", null, default);

        Assert.Equal(5, result.IterationsUsed);
    }
}
```

### Testando uma Tool

```csharp
public class GetRevenueSummaryToolTests
{
    [Fact]
    public async Task Execute_ShouldDispatchCorrectQuery()
    {
        var capturedQuery = (GetRevenueSummaryQuery?)null;
        var mediator = new StubMediator(query =>
        {
            capturedQuery = query as GetRevenueSummaryQuery;
            return new RevenueSummaryOutput(Total: 48200m);
        });

        var tool = new GetRevenueSummaryTool(mediator);
        var parameters = JsonNode.Parse("""{"month":3,"year":2026}""")!.AsObject();

        await tool.ExecuteAsync(parameters, default);

        Assert.NotNull(capturedQuery);
        Assert.Equal(3, capturedQuery!.Month);
        Assert.Equal(2026, capturedQuery.Year);
    }

    [Fact]
    public void InputSchema_ShouldRequireMonthAndYear()
    {
        var tool = new GetRevenueSummaryTool(null!);
        var required = tool.InputSchema["required"]!.AsArray()
                           .Select(n => n!.GetValue<string>())
                           .ToList();

        Assert.Contains("month", required);
        Assert.Contains("year", required);
    }
}

### Snapshot test de prompts

```csharp
[Fact]
public void BuildSummaryRequest_ShouldIncludeSupplierAndTotal()
{
    var invoice = FakeInvoice(supplier: "Acme Corp", total: 1500.00m);

    var request = InvoicePrompts.BuildSummaryRequest(invoice);

    Assert.Contains("Acme Corp", request.UserPrompt);
    Assert.Contains("1500", request.UserPrompt);
    Assert.NotNull(request.SystemPrompt);
    Assert.True(request.Temperature <= 0.2f, "Extraction prompts should be low temperature");
}
```

---

## 11. Checklist de implementação

### Kernel — contratos e infraestrutura base

- [x] Criar interfaces em `Kernel.Application/Ai/`:
  - [x] `ILlmService` com `CompleteAsync` e `StreamAsync`
  - [x] `IEmbeddingService` com `EmbedAsync` e `EmbedBatchAsync`
  - [x] `IOcrService` com `ExtractTextAsync`
  - [x] `ITextToSpeechService` com `SynthesizeAsync`
  - [x] `ISpeechToTextService` com `TranscribeAsync`
  - [x] `IAiUsageTracker` com `TrackAsync`
  - [x] `ITool` com `Definition` e `ExecuteAsync`
- [x] Criar models em `Kernel.Application/Ai/Models/`:
  - [x] `LlmRequest` com campo `Tools` (`IReadOnlyList<ToolDefinition>?`)
  - [x] `LlmResponse` com campo `ToolCalls` (`IReadOnlyList<ToolCall>?`)
  - [x] `LlmMessage` com campo `ToolCallId` (para mensagens de resultado de tool)
  - [x] `ToolDefinition` (`Name`, `Description`, `InputSchema`)
  - [x] `ToolCall` (`Id`, `Name`, `Parameters`)
  - [x] `EmbeddingResult`
  - [x] `OcrModels` (`OcrRequest`, `OcrResult`, `OcrPage`, `OcrTable`)
  - [x] `TtsModels` (`TtsOptions`)
  - [x] `SttModels` (`SttOptions`, `SttResult`, `SttSegment`)
  - [x] `AiUsageRecord`
- [x] Criar implementações em `Kernel.Infrastructure/Ai/`:
  - [x] `AzureOpenAiLlmService` (com suporte a function calling)
  - [x] `AzureOpenAiEmbeddingService`
  - [x] `AzureOcrService` (Azure Document Intelligence)
  - [x] `AzureTextToSpeechService`
  - [x] `AzureSpeechToTextService`
  - [x] `CachedEmbeddingService` (decorator com `IDistributedCache`, 24h TTL)
  - [x] `AiUsageTracker` (métricas OpenTelemetry: `ai.tokens.used`, `ai.requests.total`, `ai.request.duration_ms`)
  - [x] Null implementations para todos os serviços (`Null/` folder)
- [x] Registrar serviços via `AddAiServices()` (Azure) e `AddNullAiServices()` (dev/test)
- [x] Ativar via feature flag `FeatureFlags:EnableAI` em `AiConfiguration.cs`
- [x] Registar `IDistributedCache` (Redis se configurado, `AddDistributedMemoryCache` como fallback)
- [x] Adicionar seção `AI` no `appsettings.json` com `AiOptions`
- [x] Configurar resiliência (Polly) no `AzureOpenAiLlmService`: retry (3x, exponential+jitter), timeout (30s), circuit breaker (50%, 2 min break), trata 429/503
- [x] Adicionar `Redis:ConnectionString` (vazio) em `appsettings.json`
- [x] Testes unitários do Kernel (`CachedEmbeddingServiceTests`, `AiUsageTrackerTests`)
- [x] `InternalsVisibleTo("UnitTests")` em `Kernel.Infrastructure/Properties/AssemblyInfo.cs`

### Por módulo — ao adicionar uma feature de IA

- [ ] Criar pasta `{Module}.Application/Ai/Prompts/`
- [ ] Criar `{UseCase}Prompts.cs` com templates de prompt como constantes
- [ ] `SystemPrompt` definido e com instrução `"Nunca invente informações"`
- [ ] `Temperature` definida explicitamente (≤ 0.2 para extração, até 0.8 para criação)
- [ ] `MaxTokens` definido em todo `LlmRequest`
- [ ] Criar `{UseCase}CommandHandler.cs` em `Handlers/Ai/`
- [ ] Criar validator `{UseCase}CommandValidator.cs`
- [ ] Chamar `_tracker.TrackAsync(...)` após cada chamada de IA
- [ ] Log estruturado com: Module, Operation, Provider, Model, Tokens, Latency, TenantId
- [ ] Tratar falha de IA sem bloquear operação principal (quando IA é enrichment)
- [ ] Criar interface `I{Entity}VectorRepository` no Domain (se RAG)
- [ ] Criar implementação do vector store na Infrastructure (se RAG)

### Módulo Ai — agente cross-módulo

- [x] Criar `src/Core/Ai/` com projetos `Ai.Application` e `Ai.Infrastructure`
- [x] Criar `ITool` em `Kernel.Application/Ai/` (contrato compartilhado)
- [x] Implementar `ToolRegistry` com descoberta por injeção de `IEnumerable<ITool>`
- [x] Implementar `AgentLoop` com loop ReAct e teto de iterações (`MaxIterations = 5`)
  - [x] Execução paralela de tool calls dentro de uma iteração (`Task.WhenAll`)
  - [x] Log de cada tool chamada pelo agente
  - [x] Fallback com sumarização se `MaxIterations` for atingido
- [x] Implementar `ChatCommand` + `ChatCommandHandler` + `ChatCommandValidator`
- [x] Criar `AgentSystemPrompt.cs` com instruções do sistema
- [x] Criar `AiController` com `POST /api/v1/ai/chat` (policy: `Authenticated`)
- [x] Adicionar entrada em `RBAC_MATRIX.md`
- [x] Registrar tools via `AddAiModule()` (registo explícito em DependencyInjection.cs)
- [x] Criar `AgentSystemPrompt` com as regras do agente:
  - [x] Idioma de resposta (pt-BR)
  - [x] Nunca inventar dados; usar apenas o retorno das tools
  - [x] Não revelar detalhes internos de implementação

### Tools por módulo (uma tool por operação exposta ao agente)

- [x] `GetUsersSummaryTool` — wrapa `ListUserQuery` (Identity)
- [x] `GetTenantInfoTool` — wrapa `ListTenantsQuery` (Tenants)
- [ ] _(adicionar uma tool por módulo futuro)_

**Checklist ao criar uma nova tool:**
- [ ] Implementa `ITool` (campo `Definition`, método `ExecuteAsync(ToolCall call, ...)`)
- [ ] `Definition.Name` em `snake_case` e único no sistema
- [ ] `Definition.Description` em inglês descrevendo **quando usar**, não apenas o que faz
- [ ] `InputSchema` com JSON Schema válido (types, required, descriptions)
- [ ] `ExecuteAsync` despacha via `IMediator.Send()` — nunca acessa repositório diretamente
- [ ] Resultado serializado como JSON string (o LLM lê e interpreta)
- [ ] Registada em `Ai.Infrastructure/DependencyInjection.cs` com `AddScoped<ITool, NovaTool>()`

### Segurança

- [ ] Dados do usuário nunca interpolados no `SystemPrompt`
- [ ] Input do usuário sanitizado e tamanho limitado antes de enviar ao LLM
- [ ] API keys em `secrets.json` / variáveis de ambiente (nunca no `appsettings.json` commitado)
- [ ] PII (dados pessoais) não enviados ao LLM sem consentimento explícito
- [ ] Outputs do LLM tratados como dados não-confiáveis (validar antes de persistir)

### Testes

**Kernel (já implementados):**
- [x] `CachedEmbeddingServiceTests` — hit/miss/batch (4 cenários, stub inline)
- [x] `AiUsageTrackerTests` — sucesso, falha, serviços sem tokens (3 cenários)

**Módulo Ai (já implementados):**
- [x] `AgentLoopTests` — resposta direta, tool call → resposta, teto de iterações (3 cenários)
- [x] `ToolRegistryTests` — descobre tools, tool encontrada, tool não encontrada (3 cenários)

**Por módulo (ao implementar handlers de IA):**
- [ ] Testes unitários do handler com `StubLlmService` inline (sem chamadas reais)
- [ ] Snapshot tests dos prompts (verificar campos obrigatórios presentes)
- [ ] Testes de fallback (handler funciona quando IA falha com exceção)
- [ ] Testes do validator da command de IA

**Módulo Ai — agente:**
- [ ] `AgentLoop`: resposta direta (sem tools), tool call → resposta, teto de iterações
- [ ] Tools: `InputSchema` tem campos `required` corretos; `ExecuteAsync` despacha query certa
- [ ] `ToolRegistry`: descobre todas as tools registradas no DI
- [ ] Testes de integração das implementações de Infrastructure com WireMock (opcional)

### Operação (antes de produção)

- [x] Caching de embeddings ativado (`IDistributedCache` — Redis se `Redis:ConnectionString` configurado)
- [x] Circuit breaker no pipeline Polly (50% falhas / 1 min → break 2 min)
- [ ] Dashboard com `ai.tokens.total` por tenant/módulo
- [ ] Alerta de custo quando tokens/hora ultrapassar threshold
- [ ] Revisão do `MaxTokens` de cada prompt em ambiente de staging
- [ ] Rate limit por tenant aplicado nos endpoints que expõem IA

---

## Referências

- `docs/guides/module-designer-quickstart.md` — como criar um novo módulo
- `docs/Adrs/ADR-006_extensibilidade_e_governanca.md` — governança de capabilities transversais
- `docs/Adrs/ADR-007_observabilidade_e_operacao.md` — tags obrigatórias de observabilidade
- `docs/security/RBAC_MATRIX.md` — adicionar endpoints de IA à matriz de autorização
