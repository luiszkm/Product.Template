# Prompt: Revisar Código e Levantar Brechas

> Prompt reutilizável para revisão técnica completa de uma feature ou módulo — com levantamento de brechas e proposta de implementação para cada achado.

---

## Instruções para o agente

Antes de revisar, leia obrigatoriamente:
1. `.github/copilot-instructions.md` — regras e proibições do projeto
2. `.github/instructions/backend.instructions.md` — padrão de handlers, validators, DTOs
3. `.github/instructions/api.instructions.md` — padrão de controllers, RBAC, status codes
4. `.github/instructions/infrastructure.instructions.md` — persistência, repositories, DI
5. `docs/security/RBAC_MATRIX.md` — matriz de autorização vigente
6. `.ai/checklists/pull-request.md` — checklist completo de PR

**Referência canônica**: módulo Identity em `src/Core/Identity/`. Qualquer divergência em relação a ele é um achado.

---

## Template de uso

```
Revise o código da feature {FEATURE} no módulo {MODULE}.

Escopo da revisão:
- src/Core/{Module}/{Module}.Domain/
- src/Core/{Module}/{Module}.Application/
- src/Core/{Module}/{Module}.Infrastructure/
- src/Api/Controllers/v1/{Module}Controller.cs
- tests/UnitTests/{Feature}/
- tests/IntegrationTests/Authorization/{Module}Tests.cs

(ou especifique arquivos individuais)

Para cada brecha encontrada:
1. Informe o arquivo e linha
2. Classifique a severidade (🔴 Crítico / 🟡 Importante / 🔵 Sugestão)
3. Entregue o código corrigido completo
```

---

## Checklist de brechas a verificar

### Segurança
- [ ] `[Authorize]` sem `Policy` explícita (`SecurityConfiguration.{PolicyName}`)
- [ ] Endpoint protegido ausente do `docs/security/RBAC_MATRIX.md`
- [ ] Policy no controller diferente da declarada na matriz
- [ ] Owner-check ausente em endpoints que expõem dados por ID (`CanAccessUser(id)`)
- [ ] Dados sensíveis em templates de log (senha, token, secret)

### Arquitetura
- [ ] Domain referenciando Application ou Infrastructure
- [ ] Application referenciando Infrastructure ou Api
- [ ] Handler chamando outro handler
- [ ] Lógica de negócio no controller
- [ ] Entidade de domínio retornada de handler ou controller
- [ ] Repository para entidade filha (apenas aggregate roots)
- [ ] `IQueryable<T>` retornado de repository

### Validação e contrato
- [ ] Command sem `AbstractValidator` em `Validators/`
- [ ] Validator validando regra de negócio (deve ser no handler)
- [ ] `NotEmpty` / `MaximumLength` ausentes em campos obrigatórios
- [ ] `[ProducesResponseType]` incompleto
- [ ] POST que cria recurso retornando 200 em vez de 201
- [ ] DELETE retornando 200 em vez de 204

### Persistência
- [ ] `IUnitOfWork.Commit()` em query handler
- [ ] `IUnitOfWork.Commit()` ausente em command handler
- [ ] Paginação em memória (`ToList()` antes de `Skip`/`Take`)
- [ ] N+1 por `Include`/`ThenInclude` ausente
- [ ] Entidade sem `IEntityTypeConfiguration`
- [ ] `DbSet<T>` ausente no `AppDbContext`
- [ ] Repository não registrado em `DependencyInjection.cs`
- [ ] Entidade sem `IMultiTenantEntity`

### Observabilidade
- [ ] String interpolation em template Serilog
- [ ] Log ausente em command handler
- [ ] Exceção silenciada (`catch {}`)

### Testes
- [ ] Handler sem teste unitário
- [ ] Validator sem teste unitário
- [ ] Endpoint protegido sem integration test (401 + 403 + 200)
- [ ] Mocking framework usado (usar fakes/stubs inline)
- [ ] Integration test sem header `X-Tenant: public`
- [ ] Naming fora do padrão `{Method}_{Scenario}_{Expected}`

---

## Formato de resposta esperado

```
## Revisão: {Feature} — {Module}

### 📋 Inventário
| Arquivo | Camada | Linhas |
|---------|--------|--------|

---

### 🔴 Críticos
#### [CRÍTICO-1] {Título}
- **Arquivo**: `{caminho}:L{linha}`
- **Área**: {Segurança / Arquitetura / etc.}
- **Evidência**: (trecho problemático)
- **Proposta**: (código corrigido completo)

---

### 🟡 Importantes
#### [IMP-1] {Título}
- **Arquivo**: `{caminho}:L{linha}`
- **Proposta**: (código corrigido)

---

### 🔵 Sugestões
- `{caminho}` — {descrição}

---

### 📊 Resumo
| Área | 🔴 | 🟡 | 🔵 |
|------|----|----|----|

### 🗺️ Roadmap de correções
1. (crítico mais urgente)
2. ...
```

