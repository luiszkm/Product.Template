#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script de setup inicial do Product.Template
.DESCRIPTION
    Automatiza a configuração inicial do template:
    - Remove pasta .git
    - Renomeia solução e projetos
    - Atualiza namespaces
    - Inicializa novo repositório Git
.PARAMETER ProjectName
    Nome do novo projeto (ex: MyCompany.MyProduct)
.PARAMETER OutputPath
    Caminho de destino (opcional, padrão: diretório pai)
.EXAMPLE
    .\setup.ps1
    .\setup.ps1 -ProjectName "MyCompany.Api" -OutputPath "C:\Projects"
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectName,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath,

    [Parameter(Mandatory=$false)]
    [switch]$SkipGitInit,

    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

# ============================================================================
# CONFIGURAÇÕES
# ============================================================================

$ErrorActionPreference = "Stop"
$OriginalTemplate = "Product.Template"
$TemplateNamespace = "Product.Template"

# ============================================================================
# CORES E FORMATAÇÃO
# ============================================================================

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    param([string]$Text)
    Write-Host "`n" -NoNewline
    Write-ColorOutput "════════════════════════════════════════════════════════════════" "Cyan"
    Write-ColorOutput "  $Text" "Cyan"
    Write-ColorOutput "════════════════════════════════════════════════════════════════" "Cyan"
}

function Write-Step {
    param([string]$Text)
    Write-ColorOutput "► $Text" "Yellow"
}

function Write-Success {
    param([string]$Text)
    Write-ColorOutput "✓ $Text" "Green"
}

function Write-Error-Custom {
    param([string]$Text)
    Write-ColorOutput "✗ $Text" "Red"
}

function Write-Info {
    param([string]$Text)
    Write-ColorOutput "ℹ $Text" "Gray"
}

# ============================================================================
# VALIDAÇÕES
# ============================================================================

function Test-ProjectName {
    param([string]$Name)
    
    if ([string]::IsNullOrWhiteSpace($Name)) {
        return $false
    }
    
    # Validar formato (evitar caracteres especiais)
    if ($Name -notmatch '^[a-zA-Z0-9._-]+$') {
        Write-Error-Custom "Nome do projeto contém caracteres inválidos. Use apenas: A-Z, a-z, 0-9, . _ -"
        return $false
    }
    
    return $true
}

function Get-ValidProjectName {
    while ($true) {
        Write-Host "`n"
        Write-ColorOutput "📝 Digite o nome do novo projeto:" "Cyan"
        Write-Info "   Exemplos: MyCompany.MyProduct, Contoso.Ecommerce, AcmeCorp.Api"
        Write-Host "   → " -NoNewline -ForegroundColor Yellow
        
        $name = Read-Host
        
        if (Test-ProjectName -Name $name) {
            return $name
        }
    }
}

function Get-ValidOutputPath {
    param([string]$DefaultPath)
    
    while ($true) {
        Write-Host "`n"
        Write-ColorOutput "📁 Digite o caminho de destino (Enter para usar o padrão):" "Cyan"
        Write-Info "   Padrão: $DefaultPath"
        Write-Host "   → " -NoNewline -ForegroundColor Yellow
        
        $path = Read-Host
        
        if ([string]::IsNullOrWhiteSpace($path)) {
            return $DefaultPath
        }
        
        if (Test-Path -Path $path -IsValid) {
            return $path
        }
        
        Write-Error-Custom "Caminho inválido. Tente novamente."
    }
}

# ============================================================================
# FUNÇÕES PRINCIPAIS
# ============================================================================

function Remove-GitFolder {
    param([string]$Path)
    
    Write-Step "Removendo pasta .git..."
    
    $gitPath = Join-Path $Path ".git"
    
    if (Test-Path $gitPath) {
        Remove-Item -Path $gitPath -Recurse -Force
        Write-Success "Pasta .git removida"
    } else {
        Write-Info "Pasta .git não encontrada (ok se já foi removida)"
    }
}

function Rename-SolutionFiles {
    param(
        [string]$Path,
        [string]$OldName,
        [string]$NewName
    )
    
    Write-Step "Renomeando arquivos da solução..."
    
    # Renomear arquivo .sln
    $slnFiles = Get-ChildItem -Path $Path -Filter "*.sln" -Recurse
    
    foreach ($sln in $slnFiles) {
        $newSlnName = $sln.Name -replace [regex]::Escape($OldName), $NewName
        $newSlnPath = Join-Path $sln.DirectoryName $newSlnName
        
        if ($sln.FullName -ne $newSlnPath) {
            Rename-Item -Path $sln.FullName -NewName $newSlnName -Force
            Write-Success "Renomeado: $($sln.Name) → $newSlnName"
        }
    }
}

function Rename-ProjectFiles {
    param(
        [string]$Path,
        [string]$OldName,
        [string]$NewName
    )
    
    Write-Step "Renomeando arquivos de projeto (.csproj)..."
    
    $csprojFiles = Get-ChildItem -Path $Path -Filter "*.csproj" -Recurse
    
    foreach ($csproj in $csprojFiles) {
        $newCsprojName = $csproj.Name -replace [regex]::Escape($OldName), $NewName
        $newCsprojPath = Join-Path $csproj.DirectoryName $newCsprojName
        
        if ($csproj.FullName -ne $newCsprojPath) {
            Rename-Item -Path $csproj.FullName -NewName $newCsprojName -Force
            Write-Success "Renomeado: $($csproj.Name) → $newCsprojName"
        }
    }
}

function Rename-Directories {
    param(
        [string]$Path,
        [string]$OldName,
        [string]$NewName
    )
    
    Write-Step "Renomeando diretórios..."
    
    # Obter todos os diretórios que contêm o nome antigo (ordenar do mais profundo para o mais raso)
    $directories = Get-ChildItem -Path $Path -Directory -Recurse | 
                   Where-Object { $_.Name -like "*$OldName*" } |
                   Sort-Object { $_.FullName.Split([IO.Path]::DirectorySeparatorChar).Count } -Descending
    
    foreach ($dir in $directories) {
        $newDirName = $dir.Name -replace [regex]::Escape($OldName), $NewName
        $newDirPath = Join-Path $dir.Parent.FullName $newDirName
        
        if ($dir.FullName -ne $newDirPath) {
            try {
                Rename-Item -Path $dir.FullName -NewName $newDirName -Force
                Write-Success "Renomeado diretório: $($dir.Name) → $newDirName"
            }
            catch {
                Write-Error-Custom "Erro ao renomear $($dir.Name): $_"
            }
        }
    }
}

function Update-FileContents {
    param(
        [string]$Path,
        [string]$OldName,
        [string]$NewName
    )

    Write-Step "Atualizando conteúdo dos arquivos (namespaces, usings, referências)..."

    # Extensões de arquivos para atualizar
    $extensions = @("*.cs", "*.csproj", "*.sln", "*.json", "*.md", "*.yml", "*.yaml", "*.xml", "*.config", "*.txt")

    $files = Get-ChildItem -Path $Path -Recurse -Include $extensions | 
             Where-Object { 
                 $_.FullName -notlike "*\bin\*" -and 
                 $_.FullName -notlike "*\obj\*" -and 
                 $_.FullName -notlike "*\.git\*" -and
                 $_.FullName -notlike "*\node_modules\*"
             }

    $totalFiles = $files.Count
    $currentFile = 0
    $updatedCount = 0

    foreach ($file in $files) {
        $currentFile++

        Write-Progress -Activity "Atualizando conteúdo dos arquivos" -Status "Processando: $($file.Name)" -PercentComplete (($currentFile / $totalFiles) * 100)

        try {
            $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8 -ErrorAction Stop

            # Verificar se o arquivo contém o nome antigo
            if ($null -ne $content -and $content -like "*$OldName*") {
                # Fazer replace (usar Replace simples ao invés de -replace regex)
                $newContent = $content.Replace($OldName, $NewName)

                # Salvar o arquivo
                [System.IO.File]::WriteAllText($file.FullName, $newContent, [System.Text.UTF8Encoding]::new($false))

                $updatedCount++

                if ($Verbose) {
                    Write-Success "✓ Atualizado: $($file.Name)"
                }
            }
        }
        catch {
            Write-Error-Custom "Erro ao processar $($file.FullName): $_"
        }
    }

    Write-Progress -Activity "Atualizando conteúdo dos arquivos" -Completed

    Write-Success "Atualizado conteúdo de $updatedCount de $totalFiles arquivos"
}

function Update-ReadmeFile {
    param(
        [string]$Path,
        [string]$ProjectName
    )
    
    Write-Step "Atualizando README.md..."
    
    $readmePath = Join-Path $Path "README.md"
    
    if (Test-Path $readmePath) {
        $newReadme = @"
# $ProjectName

> 🚀 Projeto criado a partir do **Product.Template**

## 📋 Sobre o Projeto

[Descreva aqui o propósito do seu projeto]

## 🛠️ Tecnologias

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- JWT Authentication
- Microsoft Authentication
- Serilog
- OpenTelemetry
- Clean Architecture

## 🚀 Como Executar

``````bash
# Clone o repositório
git clone <seu-repositorio>

# Navegue até a pasta da API
cd src/Api

# Configure user secrets (desenvolvimento)
dotnet user-secrets set "Jwt:Secret" "your-secret-key-min-32-characters-long"
dotnet user-secrets set "MicrosoftAuth:ClientId" "your-client-id"
dotnet user-secrets set "MicrosoftAuth:ClientSecret" "your-client-secret"

# Execute a aplicação
dotnet run
``````

Acesse: https://localhost:7254/scalar/v1

## 📚 Documentação

- [Setup Autenticação Microsoft](docs/MICROSOFT_AUTH_SETUP.md)
- [Extensibilidade de Autenticação](docs/AUTHENTICATION_EXTENSIBILITY.md)

## 📝 Licença

[Defina sua licença aqui]

---

**Criado com ❤️ usando Product.Template**
"@
        
        Set-Content -Path $readmePath -Value $newReadme -Encoding UTF8
        Write-Success "README.md atualizado"
    }
}

function Initialize-GitRepository {
    param([string]$Path)
    
    Write-Step "Inicializando novo repositório Git..."
    
    Push-Location $Path
    
    try {
        git init
        git add .
        git commit -m "chore: initial commit from Product.Template"
        
        Write-Success "Repositório Git inicializado"
        Write-Info "Para conectar a um repositório remoto, execute:"
        Write-ColorOutput "   git remote add origin <url-do-repositorio>" "White"
        Write-ColorOutput "   git push -u origin master" "White"
    }
    catch {
        Write-Error-Custom "Erro ao inicializar Git: $_"
    }
    finally {
        Pop-Location
    }
}

function Move-Project {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [string]$ProjectName
    )
    
    Write-Step "Movendo projeto para destino final..."
    
    $finalPath = Join-Path $DestinationPath $ProjectName
    
    if (Test-Path $finalPath) {
        Write-ColorOutput "`n⚠️  O diretório '$finalPath' já existe!" "Yellow"
        Write-Host "Deseja sobrescrever? (S/N): " -NoNewline -ForegroundColor Yellow
        $response = Read-Host
        
        if ($response -ne "S" -and $response -ne "s") {
            Write-Error-Custom "Operação cancelada pelo usuário"
            exit 1
        }
        
        Remove-Item -Path $finalPath -Recurse -Force
    }
    
    # Criar diretório de destino se não existir
    if (-not (Test-Path $DestinationPath)) {
        New-Item -Path $DestinationPath -ItemType Directory -Force | Out-Null
    }
    
    Move-Item -Path $SourcePath -Destination $finalPath -Force
    
    Write-Success "Projeto movido para: $finalPath"
    
    return $finalPath
}

# ============================================================================
# SCRIPT PRINCIPAL
# ============================================================================

function Start-Setup {
    Write-Header "🚀 Product.Template - Setup Inicial"
    
    # Obter caminho atual
    $currentPath = Get-Location
    
    Write-Info "Diretório atual: $currentPath"
    
    # Validar se está no diretório correto
    if (-not (Test-Path (Join-Path $currentPath "Product.Template.sln"))) {
        Write-Error-Custom "Erro: Product.Template.sln não encontrado!"
        Write-Info "Execute este script na raiz do repositório clonado."
        exit 1
    }
    
    # Obter nome do projeto
    if ([string]::IsNullOrWhiteSpace($ProjectName)) {
        $ProjectName = Get-ValidProjectName
    } elseif (-not (Test-ProjectName -Name $ProjectName)) {
        exit 1
    }
    
    # Obter caminho de destino
    $defaultOutputPath = Split-Path $currentPath -Parent
    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        $OutputPath = Get-ValidOutputPath -DefaultPath $defaultOutputPath
    }
    
    # Confirmar configurações
    Write-Header "📋 Configurações"
    Write-ColorOutput "Nome do Projeto : " "White" -NoNewline
    Write-ColorOutput "$ProjectName" "Green"
    Write-ColorOutput "Caminho Destino : " "White" -NoNewline
    Write-ColorOutput "$OutputPath" "Green"
    Write-ColorOutput "Caminho Final   : " "White" -NoNewline
    Write-ColorOutput (Join-Path $OutputPath $ProjectName) "Green"
    
    Write-Host "`nContinuar? (S/N): " -NoNewline -ForegroundColor Yellow
    $confirm = Read-Host
    
    if ($confirm -ne "S" -and $confirm -ne "s") {
        Write-Error-Custom "Operação cancelada pelo usuário"
        exit 0
    }
    
    # Executar setup
    Write-Header "🔧 Iniciando Setup"

    try {
        # 1. Remover .git
        Remove-GitFolder -Path $currentPath

        # 2. Atualizar conteúdo PRIMEIRO (antes de renomear arquivos e diretórios)
        Write-Step "PASSO 1: Atualizando conteúdo interno dos arquivos..."
        Update-FileContents -Path $currentPath -OldName $TemplateNamespace -NewName $ProjectName

        # 3. Renomear arquivos de projeto e solução
        Write-Step "PASSO 2: Renomeando arquivos..."
        Rename-SolutionFiles -Path $currentPath -OldName $OriginalTemplate -NewName $ProjectName
        Rename-ProjectFiles -Path $currentPath -OldName $OriginalTemplate -NewName $ProjectName

        # 4. Renomear diretórios (do mais profundo para o mais raso)
        Write-Step "PASSO 3: Renomeando diretórios..."
        Rename-Directories -Path $currentPath -OldName $OriginalTemplate -NewName $ProjectName

        # 5. Atualizar README
        Update-ReadmeFile -Path $currentPath -ProjectName $ProjectName

        # 6. Mover para destino final
        $finalPath = Move-Project -SourcePath $currentPath -DestinationPath $OutputPath -ProjectName $ProjectName

        # 7. Inicializar Git
        if (-not $SkipGitInit) {
            Initialize-GitRepository -Path $finalPath
        }
        
        # Sucesso!
        Write-Header "✅ Setup Concluído com Sucesso!"
        
        Write-ColorOutput "`n📂 Localização do Projeto:" "Cyan"
        Write-ColorOutput "   $finalPath" "Green"
        
        Write-ColorOutput "`n🚀 Próximos Passos:" "Cyan"
        Write-ColorOutput "   1. cd `"$finalPath`"" "White"
        Write-ColorOutput "   2. code . (abrir no VS Code)" "White"
        Write-ColorOutput "   3. dotnet build" "White"
        Write-ColorOutput "   4. cd src/Api && dotnet run" "White"
        
        Write-ColorOutput "`n📚 Documentação:" "Cyan"
        Write-ColorOutput "   • README.md - Visão geral" "White"
        Write-ColorOutput "   • docs/MICROSOFT_AUTH_SETUP.md - Configurar autenticação Microsoft" "White"
        Write-ColorOutput "   • docs/AUTHENTICATION_EXTENSIBILITY.md - Adicionar novos providers" "White"
        
        Write-Host "`n"
    }
    catch {
        Write-Header "❌ Erro Durante o Setup"
        Write-Error-Custom $_.Exception.Message
        Write-Info "Stack Trace: $($_.ScriptStackTrace)"
        exit 1
    }
}

# Executar
Start-Setup
