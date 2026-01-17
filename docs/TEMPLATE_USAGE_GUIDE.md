# 📦 Guia Completo de Uso do Template

**Versão:** 1.1.0  
**Última Atualização:** 2026-01-17

---

## 📑 Índice

1. [Instalação do Template](#instalação-do-template)
2. [Criando Novo Projeto](#criando-novo-projeto)
3. [Organização de Solution Folders](#organização-de-solution-folders)
4. [Estrutura do Projeto Gerado](#estrutura-do-projeto-gerado)
5. [Configuração Inicial](#configuração-inicial)
6. [Dúvidas Frequentes](#dúvidas-frequentes)

---

## 🎯 Instalação do Template

### Pré-requisitos

- ✅ .NET SDK 8.0 ou superior
- ✅ PowerShell 7+ (para scripts de organização)
- ✅ Git (opcional, para clonar do repositório)

Verifique sua instalação:

```bash
dotnet --version
pwsh --version
```

### Opção 1: Instalar do Repositório Git (Recomendado)

```bash
# 1. Clonar o repositório
git clone https://github.com/SeuUsuario/Product.Template.git
cd Product.Template

# 2. Instalar o template
dotnet new install .

# 3. Verificar instalação
dotnet new list | findstr product-template
```

### Opção 2: Instalar de Arquivo Local

Se você tem o template em um diretório local:

```bash
# Na pasta raiz do template
dotnet new install .
```

### Opção 3: Instalar de Arquivo .zip

```bash
dotnet new install C:\caminho\para\Product.Template.zip
```

### Verificar Templates Instalados

```bash
# Listar todos os templates
dotnet new list

# Buscar especificamente o Product Template
dotnet new list | findstr product
```

Você deve ver algo como:

```
product-template    Luis .NET Product Template    [C#]    Web/API/Clean Architecture
```

---

## 🚀 Criando Novo Projeto

### Método 1: Script Automatizado (Recomendado) 🌟

Este método **automaticamente organiza os Solution Folders**:

```powershell
# Uso básico
pwsh scripts/init-project.ps1 -ProjectName "MeuProjeto"

# Com todas as opções
pwsh scripts/init-project.ps1 `
    -ProjectName "API.Vendas" `
    -OutputPath "C:\Projects" `
    -OpenIDE
```

**Parâmetros:**

| Parâmetro | Obrigatório | Descrição | Exemplo |
|-----------|-------------|-----------|---------|
| `-ProjectName` | ✅ Sim | Nome do projeto | `"MeuProjeto"` |
| `-OutputPath` | ❌ Não | Diretório de destino | `"C:\Projects"` |
| `-SkipBuild` | ❌ Não | Não compila após criar | `-SkipBuild` |
| `-OpenIDE` | ❌ Não | Abre no IDE após criar | `-OpenIDE` |

**O que o script faz:**

1. ✅ Verifica se o template está instalado
2. ✅ Cria o projeto do template
3. ✅ **Organiza automaticamente os Solution Folders**
4. ✅ Restaura pacotes NuGet
5. ✅ Compila o projeto (opcional)
6. ✅ Abre no IDE (opcional)

### Método 2: Criação Manual

```bash
# 1. Criar do template
dotnet new product-template -n MeuProjeto

# 2. Navegar para a pasta
cd MeuProjeto

# 3. ⚠️ IMPORTANTE: Organizar Solution Folders
pwsh ../scripts/organize-solution.ps1

# 4. Restaurar dependências
dotnet restore

# 5. Compilar
dotnet build

# 6. Executar a API
cd src/Api
dotnet run
```

### Método 3: Usando Diretório Específico

```bash
# Criar em diretório específico
dotnet new product-template -n MeuProjeto -o C:\Projects\MeuProjeto

cd C:\Projects\MeuProjeto

# Organizar Solution Folders
pwsh <caminho-do-template>\scripts\organize-solution.ps1
```

---

## 📂 Organização de Solution Folders

### Por Que Organizar?

Por padrão, o comando `dotnet new` cria a solution com todos os projetos **planos** (sem hierarquia):

```
❌ Estrutura PLANA (padrão do .NET):
Solution 'MeuProjeto'
├── Api.csproj
├── Kernel.Domain.csproj
├── Kernel.Application.csproj
├── Kernel.Infrastructure.csproj
├── UnitTests.csproj
├── IntegrationTests.csproj
└── ...
```

Mas a estrutura de **diretórios** é organizada:

```
📁 MeuProjeto/
├── 📁 src/
│   ├── 📁 Api/
│   ├── 📁 Shared/
│   │   ├── Kernel.Domain/
│   │   ├── Kernel.Application/
│   │   └── Kernel.Infrastructure/
└── 📁 tests/
    ├── UnitTests/
    ├── IntegrationTests/
    └── ...
```

### Solution Organizada

Após executar `organize-solution.ps1`, a solution ficará assim:

```
✅ Estrutura ORGANIZADA:
Solution 'MeuProjeto'
├── 📁 src/
│   ├── Api.csproj
│   ├── Kernel.Domain.csproj
│   ├── Kernel.Application.csproj
│   └── Kernel.Infrastructure.csproj
└── 📁 tests/
    ├── UnitTests.csproj
    ├── IntegrationTests.csproj
    ├── E2ETests.csproj
    └── CommonTests.csproj
```

### Como Organizar

#### Se você usou `init-project.ps1`:

✅ **Já está organizado automaticamente!** Não precisa fazer nada.

#### Se você criou manualmente com `dotnet new`:

```powershell
# Na pasta raiz do projeto
pwsh scripts/organize-solution.ps1

# Ou especificando a solution
pwsh scripts/organize-solution.ps1 -SolutionPath "MeuProjeto.sln"
```

#### O que o script faz:

1. 🔍 Procura o arquivo `.sln` automaticamente
2. 📋 Lista todos os projetos atuais
3. 🔎 Busca projetos em `src/` e `tests/`
4. 📤 Remove projetos da solution (sem deletar arquivos)
5. 📁 Cria Solution Folders `src` e `tests`
6. ➕ Adiciona projetos nos folders corretos
7. ✅ Exibe a estrutura final

### Verificar Organização

```bash
# Listar projetos na solution
dotnet sln list
```

Saída esperada:

```
Project(s)
----------
src\Api\Api.csproj
src\Shared\Kernel.Domain\Kernel.Domain.csproj
src\Shared\Kernel.Application\Kernel.Application.csproj
src\Shared\Kernel.Infrastructure\Kernel.Infrastructure.csproj
tests\UnitTests\UnitTests.csproj
tests\IntegrationTests\IntegrationTests.csproj
...
```

---

## 📁 Estrutura do Projeto Gerado

### Visão Geral

```
MeuProjeto/
├── 📄 MeuProjeto.sln                    # Solution principal
├── 📄 README.md                         # Documentação do projeto
├── 📄 .gitignore                        # Configuração Git
├── 📄 global.json                       # Versão do .NET SDK
├── 📄 Directory.Build.props             # Propriedades compartilhadas
│
├── 📁 src/                              # Código-fonte
│   ├── 📁 Api/                          # Camada de API
│   │   ├── Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── 📁 Controllers/
│   │   ├── 📁 Configurations/
│   │   ├── 📁 Middleware/
│   │   └── 📁 HealthChecks/
│   │
│   ├── 📁 Core/                         # Módulos de domínio
│   │   └── 📁 Identity/
│   │       ├── 📁 Domain/
│   │       ├── 📁 Application/
│   │       └── 📁 Infrastructure/
│   │
│   └── 📁 Shared/                       # Kernel compartilhado
│       ├── 📁 Kernel.Domain/
│       ├── 📁 Kernel.Application/
│       └── 📁 Kernel.Infrastructure/
│
├── 📁 tests/                            # Testes
│   ├── 📁 UnitTests/
│   ├── 📁 IntegrationTests/
│   ├── 📁 E2ETests/
│   └── 📁 CommonTests/
│
├── 📁 docs/                             # Documentação
│   ├── INDEX.md
│   ├── FAQ.md
│   ├── ARCHITECTURE.md
│   └── ...
│
└── 📁 scripts/                          # Scripts utilitários
    ├── organize-solution.ps1
    └── init-project.ps1
```

### Camadas e Responsabilidades

#### 1️⃣ **API Layer** (`src/Api/`)

- Controllers (REST endpoints)
- Middleware
- Configurações (DI, Cache, Logging, etc.)
- Health Checks
- Swagger/OpenAPI

#### 2️⃣ **Kernel Shared** (`src/Shared/`)

**Kernel.Domain:**
- SeedWorks (Entity, AggregateRoot, ValueObject)
- Domain Events
- Interfaces de repositório

**Kernel.Application:**
- CQRS (ICommand, IQuery)
- Behaviors (Logging, Validation, Performance)
- Interfaces de serviços
- DTOs compartilhados

**Kernel.Infrastructure:**
- DbContext base
- Repositórios genéricos
- Implementações de serviços
- Migrations

#### 3️⃣ **Core Modules** (`src/Core/`)

Módulos de domínio seguindo Clean Architecture:

```
Identity/
├── Domain/           # Entidades, Value Objects, Interfaces
├── Application/      # Commands, Queries, Handlers, DTOs
└── Infrastructure/   # Repositórios, Configurações EF
```

#### 4️⃣ **Tests** (`tests/`)

- **UnitTests:** Testes de unidade (domínio, handlers)
- **IntegrationTests:** Testes de integração (repositórios, DB)
- **E2ETests:** Testes end-to-end (API completa)
- **CommonTests:** Helpers e fixtures compartilhados

---

## ⚙️ Configuração Inicial

### 1. Banco de Dados

Edite `src/Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MeuProjeto;User Id=sa;Password=SuaSenha123!;TrustServerCertificate=True"
  }
}
```

**Bancos suportados:**
- SQL Server (padrão)
- PostgreSQL
- MySQL
- SQLite

### 2. Aplicar Migrations

```bash
# Navegar para a pasta da API
cd src/Api

# Criar migration inicial
dotnet ef migrations add InitialCreate -c AppDbContext

# Aplicar ao banco de dados
dotnet ef database update

# Verificar
dotnet ef migrations list
```

### 3. Configurar JWT

Edite `appsettings.Development.json`:

```json
{
  "Jwt": {
    "SecretKey": "SuaChaveSecretaSuperSegura123!@#",
    "Issuer": "MeuProjeto.Api",
    "Audience": "MeuProjeto.Client",
    "ExpirationMinutes": 60
  }
}
```

### 4. Configurar Features (Opcional)

```json
{
  "FeatureManagement": {
    "ResponseCompression": true,
    "OutputCaching": true,
    "RequestDeduplication": true,
    "AuditTrail": true
  }
}
```

### 5. Executar a Aplicação

```bash
# Método 1: dotnet run
cd src/Api
dotnet run

# Método 2: dotnet watch (hot reload)
dotnet watch run

# Método 3: Visual Studio/Rider
# Abra a solution e pressione F5
```

Acesse:
- **Swagger UI:** https://localhost:5001/swagger
- **Health Check:** https://localhost:5001/health
- **API:** https://localhost:5001/api/v1

---

## ❓ Dúvidas Frequentes

### A solution reflete automaticamente a estrutura de diretórios?

❌ **Não.** Por padrão, o .NET cria solutions com projetos planos. Você precisa executar o script `organize-solution.ps1` para organizar.

### Como sei se a solution está organizada?

Execute:

```bash
dotnet sln list
```

Se ver `src\` e `tests\` nos caminhos, está organizado. ✅

### Posso organizar manualmente no Visual Studio?

✅ **Sim**, mas é trabalhoso:

1. Clique direito na solution → Add → New Solution Folder
2. Crie folders `src` e `tests`
3. Arraste cada projeto para o folder correto

O script automatiza isso! 🚀

### O script deleta meus projetos?

❌ **Não!** O script apenas reorganiza a **solution**. Os arquivos físicos permanecem intactos.

### Posso executar o script múltiplas vezes?

✅ **Sim**, é seguro. O script é idempotente.

### E se eu adicionar novos projetos depois?

Execute o script novamente:

```powershell
pwsh scripts/organize-solution.ps1
```

Ele reorganizará todos os projetos, incluindo os novos.

### O template funciona no Linux/Mac?

✅ **Sim!** Use PowerShell Core:

```bash
# Linux/Mac
pwsh scripts/organize-solution.ps1
```

### Como atualizar o template?

```bash
# Desinstalar versão antiga
dotnet new uninstall Product.Template

# Instalar nova versão
cd <caminho-do-template-atualizado>
dotnet new install .
```

### O script funciona no VS Code?

✅ **Sim!** Abra o terminal integrado e execute:

```powershell
pwsh scripts/organize-solution.ps1
```

### Posso customizar a estrutura de folders?

✅ **Sim!** Edite o script `organize-solution.ps1` e adicione seus próprios Solution Folders.

---

## 🎯 Fluxo Completo Recomendado

### Para Novos Projetos

```powershell
# 1. Criar com script automatizado
pwsh scripts/init-project.ps1 -ProjectName "MeuProjeto" -OpenIDE

# 2. Configurar banco de dados
# Editar: src/Api/appsettings.Development.json

# 3. Aplicar migrations
cd src/Api
dotnet ef database update

# 4. Executar
dotnet run
```

### Para Projetos Existentes (Migração)

```bash
# 1. Criar projeto do template
dotnet new product-template -n MeuProjetoNovo

# 2. Organizar solution
cd MeuProjetoNovo
pwsh ../scripts/organize-solution.ps1

# 3. Migrar código do projeto antigo
# Copiar entidades, controllers, etc.

# 4. Testar
dotnet build
dotnet test
```

---

## 📚 Próximos Passos

Após criar e organizar seu projeto:

1. 📖 Leia a [Documentação Completa](./INDEX.md)
2. 🏗️ Entenda a [Arquitetura](./ARCHITECTURE.md)
3. ✨ Explore os [Recursos Avançados](./ADVANCED_FEATURES.md)
4. ❓ Consulte o [FAQ](./FAQ.md)
5. 🤝 Veja o [Guia de Contribuição](./CONTRIBUTING.md)

---

## 🆘 Suporte

**Problemas?**

1. Consulte o [FAQ](./FAQ.md)
2. Verifique as [Issues no GitHub](https://github.com/SeuUsuario/Product.Template/issues)
3. Abra uma nova issue com detalhes

**Sugestões?**

Abra uma [GitHub Discussion](https://github.com/SeuUsuario/Product.Template/discussions)

---

**Versão do Guia:** 1.1.0  
**Última Atualização:** 2026-01-17  
**Template:** Product Template v1.1.0

