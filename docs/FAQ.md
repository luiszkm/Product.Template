# ❓ FAQ - Perguntas Frequentes

Respostas rápidas para dúvidas comuns sobre o Product Template.

## 🚀 Instalação e Setup

### Como instalar o template?

```bash
# Opção 1: Clonar e instalar localmente
git clone https://github.com/Neuraptor/Product.Template.git
cd Product.Template
dotnet new install .

# Opção 2: Instalar direto do GitHub
dotnet new install https://github.com/Neuraptor/Product.Template/archive/refs/tags/v1.1.0.zip
```

### Como criar um novo projeto?

```bash
dotnet new product-template -n MeuProjeto
cd MeuProjeto
dotnet restore
dotnet build
dotnet run --project src/Api
```

### Qual versão do .NET preciso?

**.NET 10.0 ou superior** é necessário. Verifique com:

```bash
dotnet --version
```

### Como desinstalar o template?

```bash
dotnet new uninstall Neuraptor.Product.Template
```

---

## 🎯 Recursos Avançados

### O que são os "Recursos Avançados"?

São 5 funcionalidades implementadas na v1.1.0:

1. **Response Compression** - Reduz tamanho das respostas
2. **Output Caching** - Cache de respostas HTTP
3. **Request Deduplication** - Previne requisições duplicadas
4. **Feature Flags** - Controle de features
5. **Audit Trail** - Rastreamento de mudanças

### Como habilitar/desabilitar recursos?

Use Feature Flags no `appsettings.json`:

```json
"FeatureFlags": {
  "EnableCaching": true,        // Cache ativado
  "EnableAuditTrail": false     // Audit desativado
}
```

### Response Compression não está funcionando. Por quê?

**Possíveis causas:**

1. Cliente não envia header `Accept-Encoding`:
   ```bash
   # Correto
   curl -H "Accept-Encoding: gzip, br" https://localhost:5001/api
   ```

2. Middleware não está registrado no `Program.cs`:
   ```csharp
   app.UseResponseCompression(); // Deve estar ANTES de UseRouting()
   ```

3. Resposta é muito pequena (< 1KB) para comprimir

### Como usar cache em um endpoint?

Adicione o atributo `[OutputCache]`:

```csharp
[HttpGet("users")]
[OutputCache(PolicyName = "UserCache")] // Cache de 5 minutos
public async Task<IActionResult> GetUsers()
{
    return Ok(await _userService.GetAllAsync());
}
```

**Políticas disponíveis:**
- `UserCache` (5 min)
- `PublicCache` (15 min)
- `ReferenceDataCache` (30 min)
- `NoCache` (desabilita)

### Como invalidar o cache?

```csharp
// Injetar IOutputCacheStore
public class MyController : ControllerBase
{
    private readonly IOutputCacheStore _cache;

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser()
    {
        // Criar usuário...
        
        // Invalidar cache com tag "users"
        await _cache.EvictByTagAsync("users", CancellationToken.None);
        
        return Ok();
    }
}
```

### Como funciona Request Deduplication?

Previne que a mesma requisição seja processada duas vezes:

```bash
# Cliente envia chave única
curl -X POST /api/v1/identity/register \
  -H "X-Idempotency-Key: unique-key-123" \
  -d '{"email":"test@test.com","password":"Pass123!"}'

# Se repetir com mesma chave em 5 minutos → 409 Conflict
```

**Se não enviar `X-Idempotency-Key`:** O middleware gera um hash automático do corpo da requisição.

---

## 🏗️ Arquitetura

### Por que Clean Architecture?

**Benefícios:**
- ✅ Separação clara de responsabilidades
- ✅ Testabilidade (cada camada isolada)
- ✅ Independência de frameworks
- ✅ Facilidade de manutenção
- ✅ Flexibilidade para trocar banco de dados, UI, etc.

### O que é CQRS?

**Command Query Responsibility Segregation** - separa operações de leitura (Queries) das de escrita (Commands).

**Exemplo:**

```csharp
// Command (modifica estado)
public record CreateUserCommand(string Email, string Password) : ICommand<Guid>;

// Query (apenas leitura)
public record GetUserByIdQuery(Guid Id) : IQuery<UserOutput>;
```

### O que é DDD?

**Domain-Driven Design** - abordagem que coloca o domínio do negócio no centro:

- **Entities:** Objetos com identidade única
- **Value Objects:** Objetos sem identidade (ex: Email, CPF)
- **Aggregates:** Conjunto de entidades tratadas como unidade
- **Domain Events:** Eventos que representam mudanças no domínio
- **Repositories:** Abstraem acesso a dados

### Qual a diferença entre Entity e Aggregate Root?

- **Entity:** Qualquer objeto com identidade única
- **Aggregate Root:** Entity principal que garante consistência de um grupo de entities relacionadas

**Exemplo:**
```csharp
// Order é Aggregate Root
public class Order : AggregateRoot<Guid>
{
    private List<OrderItem> _items; // Entities filhas
    
    public void AddItem(Product product, int quantity)
    {
        // Order controla a adição de items
    }
}
```

---

## 🔧 Configuração

### Como mudar de InMemory para SQL Server?

**1. Instalar pacote:**
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

**2. Atualizar `DatabaseConfiguration.cs`:**
```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection"));
    
    // Manter interceptor de auditoria
    var auditInterceptor = sp.GetService<AuditableEntityInterceptor>();
    if (auditInterceptor != null)
    {
        options.AddInterceptors(auditInterceptor);
    }
});
```

**3. Adicionar connection string no `appsettings.json`:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyDb;Trusted_Connection=true"
}
```

### Como adicionar Redis para cache distribuído?

**1. Adicionar connection string:**
```json
"ConnectionStrings": {
  "Redis": "localhost:6379"
}
```

**2. Já está configurado!** O `CachingConfiguration.cs` detecta automaticamente e usa Redis se disponível.

### Como configurar JWT?

**1. Gerar secret key segura:**
```bash
# Gerar chave aleatória de 32 caracteres
openssl rand -base64 32
```

**2. Configurar no `appsettings.json`:**
```json
"Jwt": {
  "Enabled": true,
  "Secret": "sua-chave-super-secreta-min-32-chars",
  "Issuer": "MeuProjeto.Api",
  "Audience": "MeuProjeto.Api",
  "ExpirationMinutes": 60
}
```

**⚠️ IMPORTANTE:** Em produção, use **User Secrets** ou **Azure Key Vault** para a chave!

### Como desabilitar JWT?

```json
"Jwt": {
  "Enabled": false
}
```

---

## 🧪 Testes

### Como rodar os testes?

```bash
# Todos os testes
dotnet test

# Apenas testes unitários
dotnet test tests/UnitTests

# Com cobertura de código
dotnet test /p:CollectCoverage=true
```

### Como criar um teste de integração?

```csharp
public class UserRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly AppDbContext _context;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
    }

    [Fact]
    public async Task Should_Create_User()
    {
        // Arrange
        var repo = new UserRepository(_context);
        var user = User.Create("test@test.com", "hash", "John", "Doe");

        // Act
        await repo.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await repo.GetByIdAsync(user.Id);
        Assert.NotNull(saved);
    }
}
```

---

## 🐛 Troubleshooting

### Build falha com erro de namespace

**Problema:** `Namespace does not correspond to file location`

**Solução:** Arquivos em `Kernel.Infrastructure` devem usar namespace `Kernel.Infrastructure.*` (sem `Product.Template.`):

```csharp
// ❌ Errado
namespace Product.Template.Kernel.Infrastructure.Security;

// ✅ Correto
namespace Kernel.Infrastructure.Security;
```

### Erro: IHttpContextAccessor não encontrado

**Solução:**
```bash
cd src/Shared/Kernel.Infrastructure
dotnet add package Microsoft.AspNetCore.Http.Abstractions
```

### Audit Trail não preenche CreatedBy

**Causas possíveis:**

1. **Usuário não autenticado:**
   ```csharp
   // Verificar
   var isAuth = _currentUserService.IsAuthenticated;
   ```

2. **Interceptor não registrado:**
   ```csharp
   // No DependencyInjection.cs
   services.AddScoped<AuditableEntityInterceptor>();
   ```

3. **DbContext não usa interceptor:**
   ```csharp
   // No DatabaseConfiguration.cs
   options.AddInterceptors(auditInterceptor);
   ```

### Erro 409 em toda requisição POST

**Causa:** Request Deduplication está bloqueando.

**Solução:** Envie chaves únicas:
```bash
curl -H "X-Idempotency-Key: $(uuidgen)" -X POST ...
```

Ou desabilite temporariamente:
```csharp
// Program.cs - comentar linha
// app.UseMiddleware<RequestDeduplicationMiddleware>();
```

### Swagger não mostra versões

**Verificar:**

1. API Versioning está configurado
2. Controllers têm `[ApiVersion("1.0")]`
3. Swagger está configurado para múltiplas versões

---

## 🚀 Performance

### Como melhorar performance da API?

1. **Habilitar cache:**
   ```json
   "Caching": { "Enabled": true }
   ```

2. **Adicionar `[OutputCache]` em endpoints públicos**

3. **Habilitar compressão** (já ativo por padrão)

4. **Usar paginação:**
   ```csharp
   [HttpGet]
   public async Task<ActionResult<PaginatedList<User>>> GetUsers(
       int pageNumber = 1, 
       int pageSize = 10)
   ```

5. **Usar projeções no EF Core:**
   ```csharp
   var users = await _context.Users
       .Select(u => new UserDto { Id = u.Id, Name = u.Name })
       .ToListAsync();
   ```

### Quanto Response Compression realmente ajuda?

**Benchmarks típicos:**

| Endpoint | Sem compressão | Com Brotli | Redução |
|----------|----------------|------------|---------|
| GET /users (100 items) | 45 KB | 12 KB | -73% |
| GET /products (50 items) | 28 KB | 7 KB | -75% |

**Economia de banda mensal (10k req/dia):**
- Sem compressão: ~13.5 GB
- Com Brotli: ~3.6 GB
- **Economia: ~74%**

---

## 📦 Deployment

### Como fazer deploy no Azure?

```bash
# Publicar
dotnet publish -c Release -o ./publish

# Deploy (Azure CLI)
az webapp up --name meu-app --resource-group meu-rg
```

### Como fazer deploy com Docker?

```bash
# Build
docker build -t product-template:1.0 .

# Run
docker run -p 8080:8080 product-template:1.0

# Push para registry
docker tag product-template:1.0 myregistry.azurecr.io/product-template:1.0
docker push myregistry.azurecr.io/product-template:1.0
```

### Variáveis de ambiente para produção

```bash
# JWT Secret (NUNCA commitar!)
export Jwt__Secret="chave-super-secreta-producao"

# Connection string
export ConnectionStrings__DefaultConnection="Server=..."

# Redis (se usar)
export ConnectionStrings__Redis="redis-prod:6379"

# Serilog
export Serilog__MinimumLevel__Default="Warning"
```

---

## 🆚 Comparação de Versões

### Devo migrar da v1.0 para v1.1?

**Sim, se você precisa de:**
- ✅ Cache de respostas (90% mais rápido)
- ✅ Compressão (70% menos banda)
- ✅ Idempotência (evitar duplicatas)
- ✅ Feature toggles (deploy sem downtime)
- ✅ Auditoria automática

**Não é necessário se:**
- API é interna apenas
- Tráfego é muito baixo
- Não há requisitos de auditoria

### Migração é compatível?

**Sim!** A v1.1 é 100% compatível com v1.0. Você pode:
- Manter código existente funcionando
- Adicionar recursos gradualmente
- Ativar/desativar via Feature Flags

**Guia:** [MIGRATION_GUIDE_v1.0_to_v1.1.md](./MIGRATION_GUIDE_v1.0_to_v1.1.md)

---

## 🤝 Contribuindo

### Como contribuir?

1. Fork o projeto
2. Criar branch: `git checkout -b feature/minha-feature`
3. Commit: `git commit -m 'Add nova feature'`
4. Push: `git push origin feature/minha-feature`
5. Abrir Pull Request

**Leia:** [CONTRIBUTING.md](./CONTRIBUTING.md)

### Encontrei um bug, e agora?

1. Verificar se já existe issue
2. Criar nova issue com:
   - Descrição do problema
   - Passos para reproduzir
   - Versão do template
   - Stack trace (se houver)

---

## 📚 Recursos de Aprendizado

### Onde aprender Clean Architecture?

- 📖 [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- 🎥 [Clean Architecture with .NET - YouTube](https://www.youtube.com/results?search_query=clean+architecture+dotnet)

### Onde aprender DDD?

- 📖 [Domain-Driven Design - Eric Evans](https://www.amazon.com/Domain-Driven-Design-Tackling-Complexity-Software/dp/0321125215)
- 📖 [Implementing DDD - Vaughn Vernon](https://www.amazon.com/Implementing-Domain-Driven-Design-Vaughn-Vernon/dp/0321834577)

### Onde aprender CQRS?

- 📖 [CQRS Pattern - Microsoft](https://learn.microsoft.com/azure/architecture/patterns/cqrs)
- 📖 [MediatR Documentation](https://github.com/jbogard/MediatR/wiki)

---

## 📞 Suporte

### Onde obter ajuda?

1. **Documentação:** [INDEX.md](./INDEX.md)
2. **Issues:** GitHub Issues
3. **Discussions:** GitHub Discussions

### Links Úteis

- 🏠 [Repositório GitHub](https://github.com/Neuraptor/Product.Template)
- 📖 [Documentação Completa](./INDEX.md)
- 🐛 [Reportar Bug](https://github.com/Neuraptor/Product.Template/issues)
- 💡 [Sugerir Feature](https://github.com/Neuraptor/Product.Template/issues)

---

**Última atualização:** 2026-01-17  
**Versão:** 1.1.0

**Não encontrou sua pergunta?** Abra uma [issue](https://github.com/Neuraptor/Product.Template/issues) ou [discussion](https://github.com/Neuraptor/Product.Template/discussions)!

