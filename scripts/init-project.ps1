#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Cria um novo projeto a partir do Product Template com Solution Folders organizados.

.DESCRIPTION
    Este script automatiza a criação de um novo projeto a partir do Product Template,
    aplicando automaticamente a organização de Solution Folders e preparando o ambiente.

.PARAMETER ProjectName
    Nome do novo projeto a ser criado (obrigatório).

.PARAMETER OutputPath
    Caminho onde o projeto será criado. Padrão: diretório atual.

.PARAMETER SkipBuild
    Se especificado, não executa dotnet build após a criação.

.PARAMETER OpenIDE
    Se especificado, abre o projeto no IDE padrão após a criação.

.EXAMPLE
    .\init-project.ps1 -ProjectName "MeuNovoProjeto"

.EXAMPLE
    .\init-project.ps1 -ProjectName "API.Vendas" -OutputPath "C:\Projects" -OpenIDE

.NOTES
    Versão: 1.0.0
    Autor: Product Template Team
    Requer: .NET SDK 8.0+
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectName,

    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".",

    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,

    [Parameter(Mandatory=$false)]
    [switch]$OpenIDE
)

# Configuração
$ErrorActionPreference = "Stop"

# Cores para output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Banner
Clear-Host
Write-ColorOutput "`n╔════════════════════════════════════════════════════════╗" Cyan
Write-ColorOutput "║     Product Template - Project Initialization Tool     ║" Cyan
Write-ColorOutput "╚════════════════════════════════════════════════════════╝`n" Cyan

Write-ColorOutput "🚀 Criando projeto: $ProjectName" Green
Write-ColorOutput "📂 Localização: $OutputPath`n" White

# 1. Verificar se o template está instalado
Write-ColorOutput "🔍 Verificando instalação do template..." Yellow
$templateInstalled = dotnet new list | Select-String "product-template"

if (-not $templateInstalled) {
    Write-ColorOutput "❌ Template 'product-template' não encontrado!" Red
    Write-ColorOutput "`n💡 Para instalar o template, execute:" Yellow
    Write-ColorOutput "   cd <caminho-do-template>" White
    Write-ColorOutput "   dotnet new install .`n" White
    exit 1
}

Write-ColorOutput "✅ Template encontrado`n" Green

# 2. Criar o projeto
Write-ColorOutput "📦 Criando projeto do template..." Cyan
$originalLocation = Get-Location

try {
    Set-Location $OutputPath

    dotnet new product-template -n $ProjectName

    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao criar projeto do template"
    }

    Write-ColorOutput "✅ Projeto criado com sucesso`n" Green

    # 3. Navegar para a pasta do projeto
    Set-Location $ProjectName
    Write-ColorOutput "📂 Diretório de trabalho: $(Get-Location)`n" White

    # 4. Organizar Solution Folders
    Write-ColorOutput "🔧 Organizando Solution Folders..." Cyan

    # Encontrar o arquivo .sln
    $slnFile = Get-ChildItem -Filter "*.sln" -File | Select-Object -First 1

    if ($null -eq $slnFile) {
        Write-ColorOutput "⚠️  Arquivo .sln não encontrado, pulando organização..." Yellow
    }
    else {
        $sln = $slnFile.Name
        Write-ColorOutput "📄 Solution: $sln" White

        # Buscar projetos
        $srcProjects = @()
        $testProjects = @()

        if (Test-Path "src") {
            $srcProjects = Get-ChildItem -Path "src" -Filter "*.csproj" -Recurse -File
        }

        if (Test-Path "tests") {
            $testProjects = Get-ChildItem -Path "tests" -Filter "*.csproj" -Recurse -File
        }

        # Remover todos os projetos
        Write-ColorOutput "   📤 Removendo projetos da solution..." White
        $allProjects = @($srcProjects) + @($testProjects)
        foreach ($project in $allProjects) {
            dotnet sln $sln remove $project.FullName 2>&1 | Out-Null
        }

        # Adicionar com Solution Folders
        if ($srcProjects.Count -gt 0) {
            Write-ColorOutput "   📁 Adicionando projetos ao folder 'src'..." White
            foreach ($project in $srcProjects) {
                $relativePath = $project.FullName.Replace((Get-Location).Path + "\", "")
                dotnet sln $sln add $relativePath --solution-folder src | Out-Null
            }
        }

        if ($testProjects.Count -gt 0) {
            Write-ColorOutput "   📁 Adicionando projetos ao folder 'tests'..." White
            foreach ($project in $testProjects) {
                $relativePath = $project.FullName.Replace((Get-Location).Path + "\", "")
                dotnet sln $sln add $relativePath --solution-folder tests | Out-Null
            }
        }

        Write-ColorOutput "✅ Solution Folders organizados`n" Green
    }

    # 5. Restaurar pacotes
    Write-ColorOutput "📦 Restaurando pacotes NuGet..." Cyan
    dotnet restore

    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "⚠️  Falha ao restaurar pacotes" Yellow
    }
    else {
        Write-ColorOutput "✅ Pacotes restaurados`n" Green
    }

    # 6. Build (opcional)
    if (-not $SkipBuild) {
        Write-ColorOutput "🔨 Compilando solução..." Cyan
        dotnet build --no-restore

        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "⚠️  Falha na compilação" Yellow
        }
        else {
            Write-ColorOutput "✅ Compilação concluída`n" Green
        }
    }

    # 7. Exibir estrutura
    Write-ColorOutput "📋 Estrutura do projeto:" Cyan
    if ($null -ne $slnFile) {
        dotnet sln list
    }

    # 8. Abrir IDE (opcional)
    if ($OpenIDE -and $null -ne $slnFile) {
        Write-ColorOutput "`n🎯 Abrindo no IDE..." Cyan
        Start-Process $slnFile.FullName
    }

    # 9. Mensagem final
    Write-ColorOutput "`n╔════════════════════════════════════════════════════════╗" Green
    Write-ColorOutput "║            ✨ Projeto Criado com Sucesso! ✨           ║" Green
    Write-ColorOutput "╚════════════════════════════════════════════════════════╝" Green

    Write-ColorOutput "`n📂 Projeto: $ProjectName" White
    Write-ColorOutput "📁 Localização: $(Get-Location)`n" White

    Write-ColorOutput "🚀 Próximos passos:" Yellow
    Write-ColorOutput "   1. cd $ProjectName" White
    Write-ColorOutput "   2. Configure o banco de dados em appsettings.Development.json" White
    Write-ColorOutput "   3. dotnet ef database update (se usar Entity Framework)" White
    Write-ColorOutput "   4. dotnet run --project src/Api (para iniciar a API)`n" White

    Write-ColorOutput "📖 Documentação: docs/INDEX.md" Cyan
    Write-ColorOutput "❓ Perguntas: docs/FAQ.md`n" Cyan
}
catch {
    Write-ColorOutput "`n❌ Erro durante a criação do projeto:" Red
    Write-ColorOutput $_.Exception.Message Red
    exit 1
}
finally {
    Set-Location $originalLocation
}

