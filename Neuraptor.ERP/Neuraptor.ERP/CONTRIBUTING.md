# Contribuindo para o Product Template

Obrigado por considerar contribuir com o Product Template! 🎉

Este documento fornece diretrizes para contribuir com o projeto.

## 📋 Índice

- [Código de Conduta](#código-de-conduta)
- [Como Posso Contribuir?](#como-posso-contribuir)
- [Processo de Desenvolvimento](#processo-de-desenvolvimento)
- [Padrões de Código](#padrões-de-código)
- [Commits e Pull Requests](#commits-e-pull-requests)
- [Reportando Bugs](#reportando-bugs)
- [Sugerindo Melhorias](#sugerindo-melhorias)

## 📜 Código de Conduta

Este projeto adere a um código de conduta. Ao participar, espera-se que você mantenha este código:

- Seja respeitoso e inclusivo
- Aceite críticas construtivas
- Foque no que é melhor para a comunidade
- Mostre empatia com outros membros da comunidade

## 🤝 Como Posso Contribuir?

### Reportando Bugs

Encontrou um bug? Por favor, abra uma issue com:

1. **Título descritivo** - Descreva o problema claramente
2. **Passos para reproduzir** - Como reproduzir o erro
3. **Comportamento esperado** - O que deveria acontecer
4. **Comportamento atual** - O que está acontecendo
5. **Ambiente** - SO, versão do .NET, etc
6. **Screenshots** (se aplicável)

Use o template de [Bug Report](.github/ISSUE_TEMPLATE/bug_report.md).

### Sugerindo Melhorias

Tem uma ideia para melhorar o template?

1. Verifique se já não existe uma issue similar
2. Abra uma nova issue usando o template de [Feature Request](.github/ISSUE_TEMPLATE/feature_request.md)
3. Descreva claramente a funcionalidade proposta
4. Explique por que seria útil para a comunidade

### Contribuindo com Código

1. **Fork** o repositório
2. **Clone** seu fork localmente
3. **Crie um branch** para sua feature (`git checkout -b feature/minha-feature`)
4. **Desenvolva** sua contribuição
5. **Teste** suas mudanças
6. **Commit** suas mudanças
7. **Push** para seu fork
8. Abra um **Pull Request**

## 🛠️ Processo de Desenvolvimento

### Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) ou superior
- Git
- Editor de código (Visual Studio, VS Code, Rider, etc)

### Setup Local

```bash
# Clonar o repositório
git clone https://github.com/Neuraptor/Neuraptor.ERP.git
cd Neuraptor.ERP

# Restaurar dependências
dotnet restore

# Compilar o projeto
dotnet build

# Rodar testes
dotnet test

# Instalar o template localmente para testes
dotnet new install .
```

### Testando o Template

Após fazer alterações, teste o template:

```bash
# Criar um projeto de teste
dotnet new product-template -n TestProject

# Navegar para o projeto
cd TestProject

# Compilar
dotnet build

# Rodar testes
dotnet test

# Rodar a API
dotnet run --project src/Api/Api/Api.csproj
```

### Desinstalar Template de Teste

```bash
dotnet new uninstall Neuraptor.Neuraptor.ERP
```

## 📐 Padrões de Código

### C# Coding Style

Seguimos as convenções do .editorconfig incluído no projeto:

- **Indentação**: 4 espaços
- **New Line**: LF (Linux/Mac) ou CRLF (Windows - configurado automaticamente)
- **Encoding**: UTF-8
- **Trim Trailing Whitespace**: Sim
- **Insert Final Newline**: Sim

### Naming Conventions

```csharp
// Classes, Methods, Properties - PascalCase
public class ProductService
{
    public string ProductName { get; set; }

    public void GetProduct() { }
}

// Private fields - _camelCase com prefixo _
private readonly ILogger<ProductService> _logger;

// Parameters, local variables - camelCase
public void DoSomething(int productId)
{
    var productName = "Test";
}

// Constants - UPPER_SNAKE_CASE
private const int MAX_RETRY_COUNT = 3;
```

### Arquitetura e Padrões

- **Clean Architecture** - Respeite a separação de camadas
- **DDD** - Mantenha a lógica de negócio no Domain
- **CQRS** - Separe Commands de Queries
- **Single Responsibility** - Uma classe, uma responsabilidade
- **Dependency Inversion** - Dependa de abstrações, não de implementações

### Testes

- Escreva testes para novas funcionalidades
- Mantenha cobertura de testes alta
- Use nomes descritivos para testes

```csharp
[Fact]
public void UpdatePrice_WithNegativeValue_ShouldThrowDomainException()
{
    // Arrange
    var product = new Product(Guid.NewGuid(), "Test", 100);

    // Act & Assert
    Assert.Throws<DomainException>(() => product.UpdatePrice(-10));
}
```

## 📝 Commits e Pull Requests

### Mensagens de Commit

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Tipos**:
- `feat`: Nova funcionalidade
- `fix`: Correção de bug
- `docs`: Documentação
- `style`: Formatação de código
- `refactor`: Refatoração
- `test`: Testes
- `chore`: Tarefas de build, CI, etc

**Exemplos**:
```
feat(api): add API versioning support

Implementa suporte completo para versionamento de API via URL,
Header e Query String.

Closes #123
```

```
fix(health-checks): correct memory leak in database check

O health check de database estava mantendo conexões abertas.

Fixes #456
```

### Pull Requests

1. **Título claro** - Descreva o que o PR faz
2. **Descrição detalhada** - Explique as mudanças
3. **Referências** - Link para issues relacionadas
4. **Screenshots** - Se aplicável
5. **Checklist**:
   - [ ] Código compila sem erros
   - [ ] Testes passam
   - [ ] Documentação atualizada
   - [ ] Changelog atualizado (para features)

**Template de PR**:

```markdown
## Descrição
Breve descrição do que este PR faz.

## Motivação e Contexto
Por que essa mudança é necessária? Que problema resolve?

## Como foi testado?
Descreva os testes que você executou.

## Screenshots (se aplicável)

## Tipos de mudanças
- [ ] Bug fix (non-breaking change que corrige uma issue)
- [ ] New feature (non-breaking change que adiciona funcionalidade)
- [ ] Breaking change (fix ou feature que causa quebra de compatibilidade)
- [ ] Documentação

## Checklist
- [ ] Meu código segue o style guide do projeto
- [ ] Revisei meu próprio código
- [ ] Comentei código complexo
- [ ] Atualizei a documentação
- [ ] Não gerei novos warnings
- [ ] Adicionei testes
- [ ] Todos os testes passam
- [ ] Atualizei o CHANGELOG.md
```

## 🐛 Reportando Bugs

Use o template de [Bug Report](.github/ISSUE_TEMPLATE/bug_report.md).

Inclua:
- Versão do template
- Versão do .NET SDK
- Sistema operacional
- Passos para reproduzir
- Comportamento esperado vs atual
- Logs ou screenshots

## 💡 Sugerindo Melhorias

Use o template de [Feature Request](.github/ISSUE_TEMPLATE/feature_request.md).

Descreva:
- O problema que a feature resolveria
- A solução proposta
- Alternativas consideradas
- Benefícios para a comunidade

## 🎓 Recursos Úteis

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://www.domainlanguage.com/ddd/)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)

## 📞 Dúvidas?

Se tiver dúvidas sobre como contribuir:

1. Abra uma [Discussion](https://github.com/Neuraptor/Neuraptor.ERP/discussions)
2. Pergunte na issue relacionada
3. Entre em contato via issues

## 🙏 Agradecimentos

Obrigado por contribuir para tornar este template melhor! Toda contribuição, por menor que seja, é muito apreciada!

---

**Feito com ❤️ pela comunidade**
