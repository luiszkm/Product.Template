ADR-007 — Observabilidade e Operação
Status: Proposta
Data: 2026-03-16
Relacionada: ADR-002 — Plataforma Base SaaS Multi-Tenant

## 1. Contexto
- Observabilidade é capacidade nativa: logs estruturados, tracing, métricas, correlação.
- É necessário padronizar tags obrigatórias (tenant/módulo/produto) e health/operabilidade.

## 2. Decisão proposta
- Tags obrigatórias em logs/traces/métricas: `CorrelationId`, `TenantId`, `UserId`, `Module`, `Product`, `Operation`, `Environment`.
- OTEL + Serilog como base; exportadores configuráveis por ambiente.
- Health checks com visibilidade de tenancy (resolver), DB e catálogos de módulo/feature.
- Métricas mínimas: requisições por tenant/módulo, latência p95/p99, erros por tenant, sucesso/erro de login/authz.

## 3. Consequências
- (+) Facilita troubleshooting multi-tenant e por módulo.
- (+) Instrumentação consistente para produtos futuros.
- (-) Requer disciplina para propagar tags em todos os pontos de log/trace/metric.

## 4. Próximos passos
- Checklist de instrumentação (middleware de correlação, enrichment de TenantId/Module/User).
- Padrões de naming de métricas e spans; guia de sampling.
- Definir painel/dashboards alvo e alertas mínimos (erros por tenant/módulo, saturação, login failure rate).
