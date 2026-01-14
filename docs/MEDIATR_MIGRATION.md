# Migração para MediatR - Documentação

## 📋 Resumo

A aplicação foi migrada com sucesso do sistema customizado de Command/Query Bus para **MediatR**, mantendo a arquitetura CQRS e todos os behaviors (Logging, Performance, Validation).

## 🎯 O que foi feito

### 1. **Pacotes Adicionados**
- `MediatR` (v12.4.1) - Biblioteca principal
- `FluentValidation.DependencyInjectionExtensions` (v12.1.0) - Para integração com DI

### 2. **Arquivos Removidos**
- `CommandBus.cs` e `QueryBus.cs` - Substituídos pelo MediatR
- `ICommandBus.cs` e `IQueryBus.cs` - Interfaces não mais necessárias
- `ICommandBehavior.cs` e `IQueryBehavior.cs` - Substituídos por `IPipelineBehavior<,>`

### 3. **Arquivos Modificados**

#### **Interfaces Base** (`Kernel.Application/Messaging/Interfaces/`)
- `ICommand.cs` - Agora herda de `IRequest` (MediatR)
- `IQuery.cs` - Agora herda de `IRequest<TResponse>` (MediatR)
- `ICommandHandler.cs` - Agora herda de `IRequestHandler<,>` (MediatR)
- `IQueryHandler.cs` - Agora herda de `IRequestHandler<,>` (MediatR)

#### **Behaviors** (`Kernel.Application/Behaviors/`)
Todos os behaviors foram convertidos para `IPipelineBehavior<TRequest, TResponse>`:
- `ValidationBehavior<TRequest, TResponse>` - Validação com FluentValidation
- `LoggingBehavior<TRequest, TResponse>` - Logging de requisições
- `PerformanceBehavior<TRequest, TResponse>` - Detecção de requisições lentas

### 4. **Novo Sistema de Configuração**

#### `DependencyInjection.cs` (Kernel.Application)
```csharp
services.AddKernelApplication(assemblies);
```
Registra automaticamente:
- MediatR com todos os handlers dos assemblies fornecidos
- Validators do FluentValidation
- Pipeline behaviors (Validation, Logging, Performance)

#### `KernelConfigurations.cs` (Api)
```csharp
var assemblies = new[]
{
    Assembly.GetExecutingAssembly(), // Api
    typeof(Kernel.Application.DependencyInjection).Assembly, // Kernel.Application
    typeof(LoginCommand).Assembly, // Identity.Application
};

services.AddKernelApplication(assemblies);
```

## 💡 Como Usar

### 1. **Criar um Command**
```csharp
using Product.Template.Kernel.Application.Messaging.Interfaces;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<AuthTokenDto>;
```

### 2. **Criar um Handler**
```csharp
using Product.Template.Kernel.Application.Messaging.Interfaces;

public class LoginCommandHandler : ICommandHandler<LoginCommand, AuthTokenDto>
{
    public async Task<AuthTokenDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Sua lógica aqui
        return new AuthTokenDto(...);
    }
}
```

### 3. **Criar um Validator (Opcional)**
```csharp
using FluentValidation;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres");
    }
}
```

### 4. **Usar no Controller**
```csharp
using MediatR;

[ApiController]
[Route("api/[controller]")]
public class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthTokenDto>> Login(
        [FromBody] LoginCommand command, 
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
```

## 🔄 Pipeline de Execução

Quando você chama `_mediator.Send(command)`, a execução segue esta ordem:

1. **ValidationBehavior** - Valida o comando usando FluentValidation
2. **LoggingBehavior** - Loga o início e fim da execução
3. **PerformanceBehavior** - Monitora o tempo de execução
4. **Handler** - Executa a lógica de negócio
5. **Behaviors** (reverso) - Finalizam o processamento

## ✅ Vantagens da Migração

### **Antes (Custom Bus)**
- ❌ Código customizado para manutenção
- ❌ Behaviors separados para Command e Query
- ❌ Registro manual de handlers
- ❌ Menor suporte da comunidade

### **Depois (MediatR)**
- ✅ Biblioteca madura e amplamente usada
- ✅ Behaviors unificados com `IPipelineBehavior<,>`
- ✅ Descoberta automática de handlers
- ✅ Grande comunidade e documentação
- ✅ Melhor testabilidade
- ✅ Suporte nativo para notificações (events)

## 📚 Exemplos Implementados

### **Identity API** (`IdentityController.cs`)
- `POST /api/v1/identity/login` - Autenticação de usuário
- `POST /api/v1/identity/register` - Registro de novo usuário

### **Handlers Criados**
- `LoginCommandHandler` - Processa login (mock)
- `RegisterUserCommandHandler` - Processa registro (mock)

### **Validators Criados**
- `LoginCommandValidator` - Valida credenciais de login
- `RegisterUserCommandValidator` - Valida dados de registro com regras de senha forte

## 🧪 Testando

```bash
# Compilar
dotnet build

# Executar
dotnet run --project src/Api

# Testar endpoint (exemplo)
curl -X POST https://localhost:5001/api/v1/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password123"}'
```

## 🎓 Boas Práticas

1. **Commands devem ser imutáveis** - Use `record` quando possível
2. **Handlers devem ser stateless** - Injete dependências via construtor
3. **Validators devem ser específicos** - Um validator por command/query
4. **Use CancellationToken** - Para operações assíncronas canceláveis
5. **Logging estruturado** - Use `ILogger` com structured logging

## 📖 Referências

- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

**Data da Migração:** Janeiro 2025  
**Versão do .NET:** 10.0  
**Versão do MediatR:** 12.4.1
