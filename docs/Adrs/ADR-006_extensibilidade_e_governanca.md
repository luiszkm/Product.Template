ADR-006 — Extensibilidade e Governança da Foundation
Status: Proposta
Data: 2026-03-16
Relacionada: ADR-002 — Plataforma Base SaaS Multi-Tenant

## 1. Contexto
- A foundation deve ser tratada como produto técnico, agnóstico de domínio e extensível por contratos claros.
- Precisamos impedir que regras específicas de produto invadam a foundation sem governança.

## 2. Decisão proposta
- Contratos explícitos de extensão: módulos, permissões, features, settings por tenant, jobs, integrações.
- Regra de entrada na foundation: só capacidades transversais ou estruturais (tenancy, auth, observabilidade, infra padrão).
- Processo de governança: toda mudança estrutural requer ADR; revisão arquitetural obrigatória.

## 3. Consequências
- (+) Mantém a foundation limpa de regras de domínio específico.
- (+) Facilita versionamento e reuso entre produtos.
- (-) Exige disciplina e backlog de ADRs para capacidades novas.

## 4. Próximos passos
- Documentar checklist de contribuições: critérios de reuso, impacto em dependências, testes requeridos.
- Definir política de versionamento (semver) e changelog da foundation.
- Criar testes de arquitetura para impedir dependência foundation → produto e para validar contratos de extensão.
