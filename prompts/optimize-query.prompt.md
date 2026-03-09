# Prompt: Otimizar Query

> Prompt reutilizável para análise e otimização de queries e acesso a dados.

---

## Instruções para o agente

Antes de analisar:

1. Leia `.github/instructions/infrastructure.instructions.md` para o padrão de persistência.
2. Leia `.github/copilot-instructions.md` para as regras gerais.
3. Inspecione o repository e query handler envolvidos.

## Template de uso

```
Analise e otimize a query/acesso a dados para: {DESCRIÇÃO DO CASO DE USO}

Arquivos envolvidos:
- {caminho/do/queryHandler.cs}
- {caminho/do/repository.cs}

(ou)

Analise toda a cadeia de leitura do endpoint:
- Controller: src/Api/Controllers/v1/{Module}Controller.cs → action {ActionName}
- Query: src/Core/{Module}/{Module}.Application/Queries/{Feature}/...
- Repository: src/Core/{Module}/{Module}.Infrastructure/Data/Persistence/...
```

## O que o agente DEVE analisar

### Diagnóstico
- Quantos roundtrips ao banco a operação gera?
- Existe N+1 (falta de `Include`/`ThenInclude`)?
- Existe over-fetching (dados carregados mas não usados)?
- A paginação acontece no banco ou em memória?
- Existem `AsNoTracking()` onde deveria (queries read-only)?
- O `Include` chain causa cartesian explosion?
- Existem índices adequados para os campos filtrados/buscados?

### Gargalos comuns neste projeto
1. `UserRepository.GetByIdAsync` faz 4 níveis de `Include/ThenInclude` — avaliar se todos são necessários para o caso de uso.
2. `ListAllAsync` faz `CountAsync` + `ToListAsync` (2 roundtrips) — considerar `AsSplitQuery()` ou projection.
3. Queries paginadas sem `OrderBy` explícito — resultados podem ser inconsistentes entre pages.

### Soluções possíveis

**Nível 1 — Quick wins (EF Core):**
```csharp
// Adicionar AsNoTracking
.AsNoTracking()

// Projection para carregar apenas colunas necessárias
.Select(u => new UserOutput(u.Id, u.Email, ...))

// Split query para evitar cartesian explosion
.AsSplitQuery()

// OrderBy explícito para paginação consistente
.OrderBy(u => u.CreatedAt)
```

**Nível 2 — Read service com Dapper:**
```csharp
// Interface em Application
public interface I{Feature}ReadService
{
    Task<PaginatedListOutput<{Output}>> ListAsync(ListInput input, CancellationToken ct);
}

// Implementação em Infrastructure
public class {Feature}ReadService : I{Feature}ReadService
{
    private readonly IDbConnection _connection;
    // SQL otimizado com Dapper
}
```

**Nível 3 — Índices:**
```csharp
// Na EF Configuration
builder.HasIndex(e => new { e.TenantId, e.FieldName });
builder.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
```

## Formato de resposta esperado

```
## Otimização: {Caso de uso}

### 📊 Diagnóstico
| Aspecto | Status | Detalhe |
|---------|--------|---------|
| Roundtrips ao banco | ⚠️/✅ | N roundtrips por request |
| N+1 queries | ⚠️/✅ | Include adequado ou não |
| Over-fetching | ⚠️/✅ | Colunas carregadas vs usadas |
| Paginação | ⚠️/✅ | Banco ou memória |
| Tracking | ⚠️/✅ | AsNoTracking onde possível |
| Índices | ⚠️/✅ | Índices para filtros usados |

### 🎯 Gargalo principal
- (descrição do maior problema encontrado)

### 🔧 Solução proposta
- (código concreto com caminho do arquivo)

### 📈 Impacto esperado
- Roundtrips: de N para M
- Dados trafegados: redução estimada
- Latência esperada: melhoria estimada

### ⚠️ Riscos
- (breaking changes possíveis)
- (impacto em testes existentes)
- (edge cases a considerar)

### 🔄 Alternativa
- (se houver trade-off entre as opções A vs B)
```

