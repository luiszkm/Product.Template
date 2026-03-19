# Guia de Observabilidade — Product Template

> Stack: **Prometheus** (métricas) + **Grafana Tempo** (traces) + **Grafana** (dashboards) + **Seq** (logs)

---

## Arquitetura de Observabilidade

```
┌─────────────────────────────────────────────────────────────────┐
│                  ASP.NET Core API (.NET 10)                     │
│                                                                 │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────┐   │
│  │  Serilog     │   │ OpenTelemetry│   │   /metrics       │   │
│  │  Structured  │   │   SDK        │   │   (Prometheus    │   │
│  │  Logging     │   │  Traces      │   │    endpoint)     │   │
│  └──────┬───────┘   └──────┬───────┘   └────────┬─────────┘   │
└─────────│──────────────────│────────────────────│─────────────┘
          │                  │ OTLP gRPC           │ HTTP scrape
          ▼                  ▼                     ▼
       ┌─────┐          ┌────────┐           ┌──────────┐
       │ Seq │          │ Grafana│           │Prometheus│
       │:5341│          │ Tempo  │           │  :9090   │
       └─────┘          │ :4317  │           └────┬─────┘
          │             └───┬────┘                │
          │                 │                     │
          │          ┌──────▼─────────────────────▼──────┐
          │          │           GRAFANA :3000            │
          │          │                                    │
          │          │  ┌─────────────────────────────┐  │
          │          │  │  Dashboard: API Overview    │  │
          │          │  │  • Taxa de requisições      │  │
          │          │  │  • Latência P50/P95/P99     │  │
          │          │  │  • Erros 4xx/5xx            │  │
          │          │  │  • Memória Heap .NET        │  │
          │          │  │  • GC Collections           │  │
          │          │  │  • Thread Pool              │  │
          │          │  └─────────────────────────────┘  │
          │          └────────────────────────────────────┘
          │
    ┌─────▼───────────────┐
    │    Seq UI :5341     │
    │  • Logs estruturados│
    │  • Filtros avançados│
    │  • CorrelationId    │
    └─────────────────────┘
```

---

## Serviços e Portas

| Serviço | URL | Credenciais | Função |
|---------|-----|-------------|--------|
| **API** | http://localhost:8080 | JWT token | Aplicação .NET |
| **Grafana** | http://localhost:3000 | admin / admin123 | Dashboards (métricas + traces) |
| **Prometheus** | http://localhost:9090 | — | Query de métricas |
| **Grafana Tempo** | http://localhost:3200 | — | Storage de traces |
| **Seq** | http://localhost:5341 | — | Logs estruturados |
| **Endpoint /metrics** | http://localhost:8080/metrics | — | Scraping Prometheus |

---

## Como iniciar

```bash
# 1. Subir toda a infraestrutura
docker compose up

# 2. Aguardar todos os serviços ficarem prontos (~30s)
docker compose ps

# 3. Gerar tráfego para ver dados
curl -s http://localhost:8080/health/live
curl -s http://localhost:8080/health/ready

# 4. Acessar dashboards
open http://localhost:3000   # Grafana
open http://localhost:5341   # Seq
```

---

## Grafana — Dashboard API Overview

### Acessar

1. Abra `http://localhost:3000`
2. Login: `admin` / `admin123`
3. Menu lateral → **Dashboards** → **Product Template Dashboards** → **Product Template — API Overview**

### Painéis disponíveis

| Painel | Métrica | O que mostra |
|--------|---------|--------------|
| **Taxa de Requisições** | `http_server_request_duration_seconds_count` | req/s por rota e status code |
| **Latência P50/P95/P99** | `http_server_request_duration_seconds_bucket` | Percentis de latência por rota |
| **Erros 4xx/5xx** | `http_server_request_duration_seconds_count` | Taxa de erros ao longo do tempo |
| **Memória Heap .NET** | `dotnet_gc_heap_size_bytes` | Heap do GC por geração |
| **GC Collections** | `dotnet_gc_collections_total` | Frequência de coletas do GC |
| **Thread Pool** | `dotnet_thread_pool_thread_count` | Threads ativos + fila de trabalho |
| **Total Req (5min)** | `increase(...)` | Contador de requisições |
| **% Erros 5xx** | `rate(...)` | Porcentagem de erros de servidor |
| **Latência Mediana** | `histogram_quantile(0.5, ...)` | P50 em milissegundos |
| **Memória Total** | `process_working_set_bytes` | Working Set do processo |

---

## Grafana Tempo — Traces Distribuídos

### O que é um Trace?

Um trace mostra o **fluxo completo** de uma requisição, com o tempo gasto em cada etapa:

```
POST /api/v1/identity/register          [243ms] ✅
├─ FluentValidation Pipeline            [3ms]
├─ RegisterUserCommandHandler           [225ms]
│  ├─ IUserRepository.ExistsAsync       [8ms]   → SELECT
│  ├─ User.Create(...)                  [1ms]   → domínio
│  ├─ AppDbContext.SaveChangesAsync     [200ms] → INSERT
│  └─ DomainEventPublisher              [5ms]
└─ Response Serialization               [2ms]
```

### Acessar Traces no Grafana

1. Abra `http://localhost:3000`
2. Menu lateral → **Explore**
3. Selecione datasource: **Tempo**
4. Busque por:
   - **Service name**: `Product.Template.Api`
   - **Span name**: ex. `POST /api/v1/identity/register`
   - **Tag**: ex. `http.response.status_code = 400`
5. Clique em um trace para ver o detalhamento

### Correlacionar Trace com Log (Seq)

Cada requisição gera um `CorrelationId`. Para correlacionar:

1. No **Grafana Tempo**, copie o `trace_id` do span
2. No **Seq** (`http://localhost:5341`), filtre:
   ```
   CorrelationId = "seu-correlation-id"
   ```
3. Veja todos os logs estruturados daquela requisição

---

## Prometheus — Queries Úteis

Acesse `http://localhost:9090` e use as queries abaixo:

### Taxa de requisições por rota

```promql
rate(http_server_request_duration_seconds_count{job="product-template-api"}[2m])
```

### Latência P99 por rota

```promql
histogram_quantile(0.99,
  sum(rate(http_server_request_duration_seconds_bucket{job="product-template-api"}[5m]))
  by (le, http_route)
)
```

### Taxa de erros 5xx

```promql
rate(http_server_request_duration_seconds_count{
  job="product-template-api",
  http_response_status_code=~"5.."
}[2m])
```

### Memória heap do .NET

```promql
dotnet_gc_heap_size_bytes{job="product-template-api"} / 1024 / 1024
```

### Working set total (MB)

```promql
process_working_set_bytes{job="product-template-api"} / 1024 / 1024
```

### Verificar se Prometheus está coletando a API

```promql
up{job="product-template-api"}
# 1 = OK, 0 = Falhou
```

---

## Criar Custom Spans (Traces Manuais)

Para rastrear lógica de negócio específica, use o `ActivitySource` configurado:

```csharp
using Product.Template.Api.Configurations;

public class ProcessOrderCommandHandler : ICommandHandler<ProcessOrderCommand, OrderOutput>
{
    public async Task<OrderOutput> Handle(ProcessOrderCommand command, CancellationToken ct)
    {
        // Inicia um span customizado — aparece no Grafana Tempo
        using var activity = OpenTelemetryConfiguration.ActivitySource
            .StartActivity("ProcessOrder");

        activity?.SetTag("order.id", command.OrderId);
        activity?.SetTag("tenant.id", command.TenantId);

        try
        {
            var result = await _repository.GetAsync(command.OrderId, ct);

            activity?.SetTag("order.status", result.Status.ToString());
            return result.ToOutput();
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

**No Grafana Tempo, você verá:**
```
ProcessOrder [45ms]
├─ Tags: order.id=123, tenant.id=1
└─ Status: OK
```

---

## Seq — Logs Estruturados

### Acessar

```
URL: http://localhost:5341
```

### Filtros úteis

```
# Apenas erros
Level = "Error"

# Requisições de um usuário específico
UserId = "abc-123"

# Trace de uma requisição específica
CorrelationId = "req-xyz"

# Erros de um tenant
TenantId = 1 AND Level = "Error"

# Tempo de resposta alto (via propriedades estruturadas)
Elapsed > 1000
```

### Convenções de logging (Serilog)

```csharp
// ✅ Correto — structured logging com propriedades nomeadas
_logger.LogInformation("User {UserId} registered in tenant {TenantId}", user.Id, tenantId);

// ❌ Errado — string interpolation perde a estrutura
_logger.LogInformation($"User {user.Id} registered");
```

---

## Estrutura de Arquivos de Infraestrutura

```
infra/
├── prometheus/
│   └── prometheus.yml          # Jobs de scraping (API /metrics a cada 15s)
├── tempo/
│   └── tempo.yaml              # Config do Grafana Tempo (OTLP receiver + storage)
└── grafana/
    ├── provisioning/
    │   ├── datasources/
    │   │   └── datasources.yaml   # Prometheus + Tempo como datasources automáticos
    │   └── dashboards/
    │       └── dashboard.yaml     # Provider de dashboards (lê de /dashboards/)
    └── dashboards/
        └── api-overview.json      # Dashboard pré-configurado da API
```

---

## Variáveis de Ambiente (Docker Compose)

| Variável | Valor Docker | Valor Local Dev |
|----------|-------------|-----------------|
| `OpenTelemetry__OtlpTracesEndpoint` | `http://tempo:4317` | `http://localhost:4317` |
| `OpenTelemetry__OtlpEndpoint` | `http://tempo:4317` | `http://localhost:4317` |
| `Serilog__WriteTo__2__Args__serverUrl` | `http://seq:80` | `http://localhost:5341` |

---

## Troubleshooting

### Grafana não mostra dados

```bash
# 1. Verificar se Prometheus está coletando
curl http://localhost:9090/api/v1/targets | python3 -m json.tool | grep -A5 "product-template-api"

# 2. Verificar se API expõe métricas
curl http://localhost:8080/metrics | head -30

# 3. Verificar logs da API
docker compose logs api --tail=50
```

### Tempo não recebe traces

```bash
# 1. Verificar se Tempo está rodando
docker compose ps tempo

# 2. Verificar logs do Tempo
docker compose logs tempo --tail=50

# 3. Verificar se API consegue conectar ao Tempo
docker compose exec api curl -s http://tempo:3200/status/version
```

### Resetar dados de observabilidade

```bash
# Para tudo e remove volumes (APAGA dados históricos)
docker compose down -v

# Reinicia limpo
docker compose up
```

