# Prompt: Revisar Feature Existente

> Prompt reutilizável para revisão arquitetural de uma feature.

---

## Instruções para o agente

Antes de revisar:

1. Leia `.github/copilot-instructions.md` para as regras do projeto.
2. Leia `.github/instructions/backend.instructions.md` para o padrão esperado.
3. Leia `.github/instructions/api.instructions.md` se houver endpoints envolvidos.
4. Leia `.github/instructions/infrastructure.instructions.md` para o padrão de persistência.

## Template de uso

```
Revise a feature {FEATURE} no módulo {MODULE}.

Arquivos a analisar:
- {caminho/do/arquivo1.cs}
- {caminho/do/arquivo2.cs}
- ...

(ou)

Revise todos os arquivos sob:
- src/Core/{Module}/
- src/Api/Controllers/v1/{Module}Controller.cs
```

## Critérios de revisão

### Arquitetura
- [ ] Arquivos no lugar certo? (conforme estrutura em `backend.instructions.md`)
- [ ] Dependências entre camadas respeitadas? (Domain ← App ← Infra ← Api)
- [ ] Nenhum handler chama outro handler?
- [ ] Nenhum controller com lógica de negócio?

### Domain
- [ ] Entidade herda `Entity` ou `AggregateRoot`?
- [ ] Implementa `IMultiTenantEntity`?
- [ ] Construtor privado + factory `Create(...)`?
- [ ] Estado alterado via métodos de comportamento (nunca por setters)?
- [ ] Invariantes validadas no domínio?

### Application
- [ ] Commands são `record : ICommand<T>` ou `record : ICommand`?
- [ ] Queries são `record : IQuery<T>`?
- [ ] Todo command tem validator correspondente?
- [ ] Handler retorna Output DTO, nunca entidade?
- [ ] Command handler chama `_unitOfWork.Commit()`?
- [ ] Query handler NÃO chama `Commit()`?
- [ ] `CancellationToken` em todo async?
- [ ] Logging estruturado com Serilog (sem interpolation)?

### Infrastructure
- [ ] Repository não retorna `IQueryable`?
- [ ] EF Configuration existe para a entidade?
- [ ] Registrado em `DependencyInjection.cs`?

### API
- [ ] `[Authorize(Policy = ...)]` com policy explícita?
- [ ] `[ProducesResponseType]` para todos os status codes?
- [ ] `CancellationToken` como último parâmetro?
- [ ] Controller é thin dispatcher?
- [ ] `RBAC_MATRIX.md` atualizada?

### Testes
- [ ] Handler tem teste unitário (happy path + failure)?
- [ ] Validator tem teste unitário?
- [ ] Naming segue `{Method}_{Scenario}_{Expected}`?
- [ ] Usa fakes/stubs inline (não mocking framework)?

## Formato de resposta esperado

```
## Revisão: {Feature} — {Module}

### ✅ Conformidades
- (o que está correto, por arquivo)

### ⚠️ Problemas encontrados
- **{arquivo.cs}:L{linha}** — {descrição do problema}
  - Regra violada: {qual regra de qual instructions file}
  - Correção: {código ou instrução concreta}

### 🔧 Melhorias sugeridas
- (otimizações, refactorings não-blocking)

### 📊 Resumo
| Critério | Status |
|----------|--------|
| Arquitetura | ✅/⚠️/❌ |
| Domain | ✅/⚠️/❌ |
| Application | ✅/⚠️/❌ |
| Infrastructure | ✅/⚠️/❌ |
| API | ✅/⚠️/❌ |
| Testes | ✅/⚠️/❌ |
| RBAC Matrix | ✅/⚠️/❌ |
```

