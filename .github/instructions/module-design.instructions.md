# DDD Module Design Instructions

> Regras de design de módulos DDD para o Product.Template.
> Use com o agente `module-designer` antes de iniciar codificação.

---

## Processo obrigatório

Todo novo módulo DEVE passar por estas etapas:

1. **Design** (module-designer) → documento completo em `docs/modules/{module}-design.md`
2. **Revisão** → tech lead + domain expert aprovam
3. **Implementação** (feature-builder) → código seguindo o design
4. **Testes** → validação de conformidade arquitetural

---

## Templates disponíveis

| Template | Quando usar |
|----------|-------------|
| `docs/templates/module-design-template.md` | Módulo do zero |
| `docs/templates/module-design-example-orders.md` | Referência completa |

---

## Regras de Bounded Context

### 1. Um módulo = um bounded context
Cada pasta em `src/Core/{Module}/` é um bounded context isolado.

### 2. Linguagem ubíqua obrigatória
Todo termo técnico do domínio DEVE estar documentado na seção "Linguagem Ubíqua".

❌ **Errado:**
```csharp
public class Data { } // termo vago, sem significado no domínio
```

✅ **Correto:**
```csharp
public class Order { } // termo do domínio, documentado
```

### 3. Sem vazamento de conceitos
Um bounded context NÃO pode referenciar diretamente entidades de outro.

❌ **Errado:**
```csharp
public class Order
{
    public User Customer { get; set; } // vazamento do Identity context
}
```

✅ **Correto:**
```csharp
public class Order
{
    public Guid CustomerId { get; private set; } // apenas ID
}
```

---

## Regras de Aggregate Design

### 1. Tamanho limitado
Um aggregate NÃO deve ter mais de:
- 1 aggregate root
- 3-5 entidades filhas
- 10 value objects

Se crescer além disso, considere dividir em 2 aggregates.

### 2. Invariantes explícitos
Todo aggregate DEVE documentar suas invariantes (regras que nunca podem ser violadas).

**Exemplo:**
```markdown
**Invariantes:**
1. Order deve ter ao menos 1 OrderLine
2. Total = soma de OrderLines
3. Order cancelada não pode ser enviada
```

### 3. Boundaries claros
Documente claramente:
- O que está DENTRO do aggregate (controle total)
- O que está FORA (referenciado por ID)

### 4. Modificação apenas via métodos
Nunca expor setters públicos.

❌ **Errado:**
```csharp
public OrderStatus Status { get; set; } // permite mudança inválida
order.Status = OrderStatus.Delivered; // sem validação
```

✅ **Correto:**
```csharp
public OrderStatus Status { get; private set; }

public void Ship()
{
    if (!CanBeShipped) throw new DomainException(...);
    Status = OrderStatus.Shipped;
    AddDomainEvent(new OrderShippedEvent(Id));
}
```

---

## Regras de Domain Events

### 1. Naming: passado
`{Substantivo}{AçãoNoPassado}Event`

✅ `OrderPlacedEvent`, `PaymentProcessedEvent`, `UserRegisteredEvent`  
❌ `PlaceOrderEvent`, `ProcessPayment`, `RegisterUser`

### 2. Imutabilidade
Todo domain event DEVE ser `record` com `init`.

```csharp
public sealed record OrderPlacedEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

### 3. Payload mínimo
Não incluir a entidade inteira — apenas dados essenciais.

❌ **Errado:**
```csharp
public record OrderPlacedEvent(Order Order); // vazamento de entidade
```

✅ **Correto:**
```csharp
public record OrderPlacedEvent(Guid OrderId, Money Total);
```

### 4. Um evento por ação
Cada método de negócio dispara NO MÁXIMO 1 evento.

---

## Regras de Value Objects

### 1. Imutabilidade
Sempre `record` ou class com `init-only` properties.

### 2. Igualdade por valor
```csharp
var money1 = Money.Create(100, "USD");
var money2 = Money.Create(100, "USD");
money1 == money2; // true (igualdade por valor)
```

### 3. Validação no factory
```csharp
public static Money Create(decimal amount, string currency)
{
    if (amount < 0) throw new DomainException("Amount cannot be negative");
    return new Money { Amount = amount, Currency = currency };
}
```

### 4. Sem identidade
Value Objects NÃO têm `Id`.

---

## Regras de Commands & Queries

### 1. Commands modificam estado
- Nome: `{Verbo}{Substantivo}Command`
- Retornam DTO (nunca entidade)
- Chamam `IUnitOfWork.Commit()`

### 2. Queries apenas leem
- Nome: `{Get|List}{Substantivo}Query`
- NUNCA chamam `Commit()`
- Retornam DTOs

### 3. Um handler por command/query
Nunca um handler chamar outro handler.

❌ **Errado:**
```csharp
public class PlaceOrderHandler
{
    public async Task Handle(...)
    {
        await _mediator.Send(new ReserveInventoryCommand(...)); // handler chamando handler
    }
}
```

✅ **Correto:**
```csharp
public class PlaceOrderHandler
{
    public async Task Handle(...)
    {
        order.Place(); // dispara OrderPlacedEvent
        // Event handler separado cuida de ReserveInventory
    }
}
```

---

## Regras de Repositories

### 1. Apenas para aggregate roots
NÃO criar repository para entidades filhas.

❌ **Errado:**
```csharp
IOrderLineRepository // OrderLine é entidade filha de Order
```

✅ **Correto:**
```csharp
IOrderRepository // Order é aggregate root
```

### 2. Métodos de domínio
```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
    Task UpdateAsync(Order order, CancellationToken ct);
    // Paginação
    Task<PaginatedListOutput<Order>> ListByCustomerAsync(Guid customerId, ListInput input, CancellationToken ct);
}
```

### 3. Nunca retornar IQueryable
```csharp
❌ IQueryable<Order> GetAll();
✅ Task<PaginatedListOutput<Order>> ListAsync(ListInput input, CancellationToken ct);
```

---

## Checklist de Entrega

Antes de aprovar um design de módulo, validar:

**Documentação:**
- [ ] Bounded Context Canvas preenchido
- [ ] Linguagem ubíqua com ao menos 5 termos
- [ ] Aggregates desenhados com invariantes explícitos
- [ ] Boundaries documentados (dentro vs fora)
- [ ] Domain events mapeados
- [ ] Policies (event handlers) definidas
- [ ] Commands e queries listados
- [ ] Estrutura de pastas definida

**Validações:**
- [ ] Nenhum aggregate com mais de 5 entidades filhas
- [ ] Todo aggregate tem invariantes documentados
- [ ] Nenhuma referência direta entre bounded contexts
- [ ] Todo domain event é imutável (record)
- [ ] Todo value object é imutável
- [ ] Repository apenas para aggregate roots
- [ ] Commands retornam DTOs (não entidades)

**Diagramas:**
- [ ] Diagrama de classes (aggregates)
- [ ] Diagrama de eventos (event flow)

---

## Anti-patterns a evitar

### ❌ Anemic Domain Model
Entidades sem comportamento, apenas getters/setters.

```csharp
// Anêmico - ERRADO
public class Order
{
    public Guid Id { get; set; }
    public OrderStatus Status { get; set; }
}

// Service faz toda a lógica
public class OrderService
{
    public void Ship(Order order)
    {
        order.Status = OrderStatus.Shipped; // lógica fora do domínio
    }
}
```

**Solução:** lógica dentro do aggregate.

```csharp
// Rich domain - CORRETO
public class Order
{
    public OrderStatus Status { get; private set; }
    
    public void Ship()
    {
        if (!CanBeShipped) throw new DomainException(...);
        Status = OrderStatus.Shipped;
        AddDomainEvent(new OrderShippedEvent(Id));
    }
}
```

### ❌ God Aggregate
Aggregate que faz tudo.

**Sintomas:**
- Mais de 10 entidades filhas
- Mais de 30 métodos
- Múltiplas responsabilidades não relacionadas

**Solução:** dividir em 2+ aggregates.

### ❌ Leaky Abstraction
Domain conhecendo detalhes de infraestrutura.

```csharp
❌ public class Order
{
    public void Save() // domínio não deve saber de persistência
    {
        _dbContext.SaveChanges();
    }
}
```

---

## Workflow completo

```
1. Stakeholder solicita feature
   ↓
2. Tech Lead aciona module-designer agent
   ↓
3. Agent conduz Event Storming simplificado
   ↓
4. Agent gera documento de design completo
   ↓
5. Domain Expert + Tech Lead revisam
   ↓
6. Aprovação → salva em docs/modules/{module}-design.md
   ↓
7. Feature-builder agent implementa seguindo o design
   ↓
8. Code-reviewer valida conformidade
   ↓
9. Merge + deploy
```

---

## Referências

- Bounded Context Canvas: https://github.com/ddd-crew/bounded-context-canvas
- Event Storming: Alberto Brandolini
- Aggregate Design: Vaughn Vernon (Red Book)
- Módulo de referência: `src/Core/Identity/`

