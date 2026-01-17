#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Organiza os projetos da solution em Solution Folders.

.DESCRIPTION
    Este script reorganiza uma solution .NET para refletir a estrutura de diretórios,
    criando Solution Folders para 'src' e 'tests' e movendo os projetos correspondentes.

.PARAMETER SolutionPath
    Caminho para o arquivo .sln. Se não especificado, procura automaticamente.

.EXAMPLE
    .\organize-solution.ps1

.EXAMPLE
    .\organize-solution.ps1 -SolutionPath "MeuProjeto.sln"

.NOTES
    Versão: 1.0.0
    Autor: Product Template Team
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$SolutionPath
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
Write-ColorOutput "`n╔════════════════════════════════════════════════╗" Cyan
Write-ColorOutput "║  Solution Folder Organizer - Product Template  ║" Cyan
Write-ColorOutput "╚════════════════════════════════════════════════╝`n" Cyan

# 1. Encontrar o arquivo .sln se não foi especificado
if ([string]::IsNullOrEmpty($SolutionPath)) {
    Write-ColorOutput "🔍 Procurando arquivo .sln..." Yellow

    $slnFiles = Get-ChildItem -Path . -Filter "*.sln" -File

    if ($slnFiles.Count -eq 0) {
        Write-ColorOutput "❌ Nenhum arquivo .sln encontrado no diretório atual!" Red
        exit 1
    }
    elseif ($slnFiles.Count -eq 1) {
        $SolutionPath = $slnFiles[0].Name
        Write-ColorOutput "✅ Encontrado: $SolutionPath" Green
    }
    else {
        Write-ColorOutput "⚠️  Múltiplos arquivos .sln encontrados:" Yellow
        $slnFiles | ForEach-Object { Write-ColorOutput "   - $($_.Name)" White }
        $SolutionPath = $slnFiles[0].Name
        Write-ColorOutput "📌 Usando: $SolutionPath" Cyan
    }
}

# Verificar se o arquivo existe
if (-not (Test-Path $SolutionPath)) {
    Write-ColorOutput "❌ Arquivo não encontrado: $SolutionPath" Red
    exit 1
}

Write-ColorOutput "`n📂 Organizando solution: $SolutionPath`n" Cyan

# 2. Listar projetos atuais
Write-ColorOutput "📋 Projetos atuais na solution:" Yellow
dotnet sln $SolutionPath list | Select-Object -Skip 2

# 3. Encontrar projetos em src/ e tests/
Write-ColorOutput "`n🔎 Buscando projetos em src/ e tests/..." Yellow

$srcProjects = @()
$testProjects = @()

if (Test-Path "src") {
    $srcProjects = Get-ChildItem -Path "src" -Filter "*.csproj" -Recurse -File
    Write-ColorOutput "✅ Encontrados $($srcProjects.Count) projeto(s) em src/" Green
    $srcProjects | ForEach-Object { Write-ColorOutput "   📦 $($_.Name)" White }
}

if (Test-Path "tests") {
    $testProjects = Get-ChildItem -Path "tests" -Filter "*.csproj" -Recurse -File
    Write-ColorOutput "✅ Encontrados $($testProjects.Count) projeto(s) em tests/" Green
    $testProjects | ForEach-Object { Write-ColorOutput "   🧪 $($_.Name)" White }
}

if ($srcProjects.Count -eq 0 -and $testProjects.Count -eq 0) {
    Write-ColorOutput "`n⚠️  Nenhum projeto encontrado em src/ ou tests/" Yellow
    Write-ColorOutput "A solution já pode estar organizada ou não há projetos para organizar." White
    exit 0
}

# 4. Reorganizar a solution
Write-ColorOutput "`n🔧 Reorganizando solution..." Cyan

# Remover todos os projetos da solution (sem deletar arquivos)
Write-ColorOutput "`n📤 Removendo projetos da solution..." Yellow
$allProjects = @($srcProjects) + @($testProjects)
foreach ($project in $allProjects) {
    try {
        dotnet sln $SolutionPath remove $project.FullName 2>&1 | Out-Null
    }
    catch {
        # Ignorar erros (projeto pode já não estar na solution)
    }
}

# Adicionar projetos de volta com Solution Folders
if ($srcProjects.Count -gt 0) {
    Write-ColorOutput "`n📁 Criando Solution Folder 'src' e adicionando projetos..." Cyan
    foreach ($project in $srcProjects) {
        $relativePath = $project.FullName.Replace((Get-Location).Path + "\", "")
        Write-ColorOutput "   ➕ $($project.Name)" White
        dotnet sln $SolutionPath add $relativePath --solution-folder src | Out-Null
    }
}

if ($testProjects.Count -gt 0) {
    Write-ColorOutput "`n📁 Criando Solution Folder 'tests' e adicionando projetos..." Cyan
    foreach ($project in $testProjects) {
        $relativePath = $project.FullName.Replace((Get-Location).Path + "\", "")
        Write-ColorOutput "   ➕ $($project.Name)" White
        dotnet sln $SolutionPath add $relativePath --solution-folder tests | Out-Null
    }
}

# 5. Exibir resultado final
Write-ColorOutput "`n✅ Solution reorganizada com sucesso!" Green
Write-ColorOutput "`n📋 Estrutura final:" Cyan
dotnet sln $SolutionPath list

# 6. Mensagem final
Write-ColorOutput "`n╔════════════════════════════════════════════════╗" Green
Write-ColorOutput "║           ✨ Organização Concluída! ✨         ║" Green
Write-ColorOutput "╚════════════════════════════════════════════════╝" Green

Write-ColorOutput "`n💡 Dica: Abra a solution no Visual Studio ou Rider para ver os Solution Folders." Yellow
Write-ColorOutput ""

