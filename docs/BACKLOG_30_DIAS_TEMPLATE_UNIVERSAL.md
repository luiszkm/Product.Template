# Backlog de 30 Dias — Template Universal (Multi-tenant + RBAC)

Este backlog organiza as próximas 4 semanas para elevar o template para um patamar de produção, com foco em **governança RBAC**, **isolamento multi-tenant**, **qualidade operacional** e **experiência do time**.

> Objetivo do ciclo: sair de uma base sólida para um template realmente repetível em novos produtos com risco baixo de segurança e regressão.

---

## Semana 1 (Dias 1–7) — Fechar lacunas críticas de RBAC e Identity

### 1) Substituir mocks por implementação real no fluxo de usuário
- [ ] Implementar `GetUserByIdQueryHandler` com repositório real (remover retorno mockado).
- [ ] Garantir tratamento de `NotFound` consistente.
- [ ] Adicionar testes unitários para cenários: encontrado, não encontrado, cancelamento.

**Critério de pronto:** nenhum endpoint de Identity retorna dado mockado.

### 2) Harden de autorização no módulo Identity
- [ ] Revisar todos os endpoints para policy explícita (`UsersRead`, `UsersManage`, etc.).
- [ ] Validar regra de escopo self-or-admin em todos endpoints sensíveis.
- [ ] Cobrir cenários 401/403/200 com testes de integração.

**Critério de pronto:** 100% dos endpoints sensíveis do Identity com política documentada e testada.

### 3) Matriz RBAC como gate obrigatório
- [ ] Expandir teste de consistência endpoint x matriz para novos endpoints de Identity.
- [ ] Falhar CI quando houver endpoint protegido sem mapeamento na matriz.

**Critério de pronto:** mudança em autorização sem atualização da matriz não passa no CI.

---

## Semana 2 (Dias 8–14) — Governança Multi-tenant de produção

### 4) Gestão de tenant (provisionamento e ciclo de vida)
- [ ] Criar endpoints/commands para: criar tenant, ativar/desativar tenant, listar tenants.
- [ ] Aplicar autorização administrativa para operações de tenant.
- [ ] Registrar auditoria de operações de provisionamento.

**Critério de pronto:** operações de tenant executáveis via API segura (não apenas via serviço interno).

### 5) Segurança e validação de resolução de tenant
- [ ] Validar formato de `tenantKey` (regex, tamanho, reserved words).
- [ ] Restringir ambiguidades entre header e subdomínio (regra de precedência documentada).
- [ ] Implementar testes para spoofing e conflito de resolução.

**Critério de pronto:** resolução de tenant previsível e protegida contra entradas inválidas.

### 6) Migração por tenant com segurança operacional
- [ ] Definir estratégia de migração por modo (`SharedDb`, `SchemaPerTenant`, `DedicatedDb`).
- [ ] Idempotência para execução de migrator por tenant.
- [ ] Log estruturado por tenant/correlation-id.

**Critério de pronto:** migrar N tenants sem intervenção manual e com rastreabilidade.

---

## Semana 3 (Dias 15–21) — Expandir padrão para novos módulos (template universal)

### 7) Criar módulo de exemplo real (ex.: Catalog ou Orders)
- [ ] Adicionar módulo em `src/Modules/*` com CRUD básico.
- [ ] Aplicar isolamento multi-tenant no módulo.
- [ ] Aplicar RBAC por permissão no módulo.

**Critério de pronto:** template comprova reutilização fora do Identity.

### 8) Governança RBAC cross-módulos
- [ ] Estender `RBAC_MATRIX.md` para novo módulo.
- [ ] Replicar gate automatizado de consistência para todos controllers.
- [ ] Definir padrão de nome de permissões (`resource.action`).

**Critério de pronto:** novo módulo só entra com matriz e testes de autorização completos.

### 9) Pacote de testes de regressão de segurança
- [ ] Testes negativos (acesso cross-tenant, role insuficiente, token sem claim).
- [ ] Testes de boundary por role (Admin/Manager/User).
- [ ] Testes de filtro tenant em leitura/escrita.

**Critério de pronto:** suite de regressão de segurança rodando em CI.

---

## Semana 4 (Dias 22–30) — Operação, DX e release do template

### 10) CI/CD de referência do template
- [ ] Pipeline com stages: restore, build, test, quality gates, package.
- [ ] Publicação automatizada de artefato/template versionado.
- [ ] Badge e documentação do fluxo de release.

**Critério de pronto:** versão do template publicada por pipeline reproduzível.

### 11) Observabilidade mínima mandatória
- [ ] Definir painel base (latência, erros, throughput, health por tenant).
- [ ] Alertas para falhas de autenticação/autorização e erros de banco.
- [ ] Correlação ponta-a-ponta por request id.

**Critério de pronto:** diagnóstico operacional possível sem debugging local.

### 12) DX (Developer Experience) e documentação executável
- [ ] Atualizar docs com “golden path” (do `dotnet new` ao primeiro módulo).
- [ ] Checklist de produção (segredos, CORS, JWT, rate limits, backup).
- [ ] Script de bootstrap de ambiente local + dados seed para demo.

**Critério de pronto:** novo time consegue subir projeto e entregar primeiro módulo em 1 dia.

---

## Priorização objetiva (RICE simplificado)

### Prioridade Alta (fazer neste ciclo)
1. Implementação real do `GetUserById` (sem mock).
2. Governança RBAC com gate de matriz obrigatório.
3. Gestão de tenant via API segura.
4. Módulo de exemplo com RBAC + multi-tenant.

### Prioridade Média
5. Migração por tenant idempotente.
6. Suite de regressão de segurança.
7. Observabilidade mandatória.

### Prioridade Baixa (se sobrar capacidade)
8. Automação de release avançada.
9. Melhorias adicionais de DX e scaffolding.

---

## Entregáveis esperados ao final dos 30 dias

- [ ] Zero mock em fluxos críticos de Identity.
- [ ] RBAC consistente e governado por matriz + CI gate em múltiplos módulos.
- [ ] Multi-tenant com operação segura (provisionamento + migração + auditoria).
- [ ] Pelo menos 1 módulo de negócio demonstrando reutilização real do template.
- [ ] Pipeline e documentação para adoção por novos times.
