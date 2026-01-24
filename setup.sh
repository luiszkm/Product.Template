#!/bin/bash

# ============================================================================
# Product.Template - Setup Script (Linux/Mac)
# ============================================================================
# 
# Este script automatiza a configuração inicial do template:
# - Remove pasta .git
# - Renomeia solução e projetos
# - Atualiza namespaces
# - Inicializa novo repositório Git
#
# Uso:
#   ./setup.sh
#   ./setup.sh -n "MyCompany.MyProduct" -o "/home/user/projects"
#
# ============================================================================

set -e  # Exit on error

# ============================================================================
# CONFIGURAÇÕES
# ============================================================================

ORIGINAL_TEMPLATE="Product.Template"
TEMPLATE_NAMESPACE="Product.Template"
PROJECT_NAME=""
OUTPUT_PATH=""
SKIP_GIT_INIT=false
VERBOSE=false

# ============================================================================
# CORES
# ============================================================================

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# ============================================================================
# FUNÇÕES DE OUTPUT
# ============================================================================

print_header() {
    echo -e "\n${CYAN}════════════════════════════════════════════════════════════════${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}════════════════════════════════════════════════════════════════${NC}"
}

print_step() {
    echo -e "${YELLOW}► $1${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${GRAY}ℹ $1${NC}"
}

# ============================================================================
# VALIDAÇÕES
# ============================================================================

validate_project_name() {
    local name=$1
    
    if [[ -z "$name" ]]; then
        return 1
    fi
    
    # Validar formato (evitar caracteres especiais)
    if [[ ! "$name" =~ ^[a-zA-Z0-9._-]+$ ]]; then
        print_error "Nome do projeto contém caracteres inválidos. Use apenas: A-Z, a-z, 0-9, . _ -"
        return 1
    fi
    
    return 0
}

get_valid_project_name() {
    while true; do
        echo -e "\n${CYAN}📝 Digite o nome do novo projeto:${NC}"
        print_info "   Exemplos: MyCompany.MyProduct, Contoso.Ecommerce, AcmeCorp.Api"
        echo -ne "   ${YELLOW}→ ${NC}"
        
        read name
        
        if validate_project_name "$name"; then
            echo "$name"
            return 0
        fi
    done
}

get_valid_output_path() {
    local default_path=$1
    
    while true; do
        echo -e "\n${CYAN}📁 Digite o caminho de destino (Enter para usar o padrão):${NC}"
        print_info "   Padrão: $default_path"
        echo -ne "   ${YELLOW}→ ${NC}"
        
        read path
        
        if [[ -z "$path" ]]; then
            echo "$default_path"
            return 0
        fi
        
        # Validar se é um caminho válido
        if [[ "$path" =~ ^[a-zA-Z0-9/._-]+$ ]] || [[ "$path" =~ ^~.*$ ]]; then
            # Expandir ~ se necessário
            path="${path/#\~/$HOME}"
            echo "$path"
            return 0
        fi
        
        print_error "Caminho inválido. Tente novamente."
    done
}

# ============================================================================
# FUNÇÕES PRINCIPAIS
# ============================================================================

remove_git_folder() {
    local path=$1
    
    print_step "Removendo pasta .git..."
    
    if [[ -d "$path/.git" ]]; then
        rm -rf "$path/.git"
        print_success "Pasta .git removida"
    else
        print_info "Pasta .git não encontrada (ok se já foi removida)"
    fi
}

rename_solution_files() {
    local path=$1
    local old_name=$2
    local new_name=$3
    
    print_step "Renomeando arquivos da solução..."
    
    # Renomear arquivo .sln
    find "$path" -type f -name "*.sln" | while read -r sln; do
        local dir=$(dirname "$sln")
        local filename=$(basename "$sln")
        local new_filename="${filename//$old_name/$new_name}"
        
        if [[ "$filename" != "$new_filename" ]]; then
            mv "$sln" "$dir/$new_filename"
            print_success "Renomeado: $filename → $new_filename"
        fi
    done
}

rename_project_files() {
    local path=$1
    local old_name=$2
    local new_name=$3
    
    print_step "Renomeando arquivos de projeto (.csproj)..."
    
    find "$path" -type f -name "*.csproj" | while read -r csproj; do
        local dir=$(dirname "$csproj")
        local filename=$(basename "$csproj")
        local new_filename="${filename//$old_name/$new_name}"
        
        if [[ "$filename" != "$new_filename" ]]; then
            mv "$csproj" "$dir/$new_filename"
            print_success "Renomeado: $filename → $new_filename"
        fi
    done
}

rename_directories() {
    local path=$1
    local old_name=$2
    local new_name=$3
    
    print_step "Renomeando diretórios..."
    
    # Encontrar e renomear diretórios (do mais profundo para o mais raso)
    find "$path" -depth -type d -name "*$old_name*" | while read -r dir; do
        local parent=$(dirname "$dir")
        local dirname=$(basename "$dir")
        local new_dirname="${dirname//$old_name/$new_name}"
        
        if [[ "$dirname" != "$new_dirname" ]]; then
            mv "$dir" "$parent/$new_dirname"
            print_success "Renomeado diretório: $dirname → $new_dirname"
        fi
    done
}

update_file_contents() {
    local path=$1
    local old_name=$2
    local new_name=$3
    
    print_step "Atualizando conteúdo dos arquivos..."
    
    # Extensões de arquivos para atualizar
    local extensions=("*.cs" "*.csproj" "*.sln" "*.json" "*.md" "*.yml" "*.yaml" "*.xml" "*.config")
    
    local file_count=0
    
    for ext in "${extensions[@]}"; do
        find "$path" -type f -name "$ext" \
            ! -path "*/bin/*" \
            ! -path "*/obj/*" \
            ! -path "*/.git/*" \
            | while read -r file; do
            
            if grep -q "$old_name" "$file" 2>/dev/null; then
                # Usar sed de forma compatível com Linux e Mac
                if [[ "$OSTYPE" == "darwin"* ]]; then
                    # macOS
                    sed -i '' "s/$old_name/$new_name/g" "$file"
                else
                    # Linux
                    sed -i "s/$old_name/$new_name/g" "$file"
                fi
                
                if $VERBOSE; then
                    print_success "Atualizado: $(basename "$file")"
                fi
                
                ((file_count++))
            fi
        done
    done
    
    print_success "Atualizado conteúdo de $file_count arquivos"
}

update_readme_file() {
    local path=$1
    local project_name=$2
    
    print_step "Atualizando README.md..."
    
    local readme="$path/README.md"
    
    if [[ -f "$readme" ]]; then
        cat > "$readme" << EOF
# $project_name

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

\`\`\`bash
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
\`\`\`

Acesse: https://localhost:7254/scalar/v1

## 📚 Documentação

- [Setup Autenticação Microsoft](docs/MICROSOFT_AUTH_SETUP.md)
- [Extensibilidade de Autenticação](docs/AUTHENTICATION_EXTENSIBILITY.md)

## 📝 Licença

[Defina sua licença aqui]

---

**Criado com ❤️ usando Product.Template**
EOF
        
        print_success "README.md atualizado"
    fi
}

initialize_git_repository() {
    local path=$1
    
    print_step "Inicializando novo repositório Git..."
    
    cd "$path"
    
    if git init; then
        git add .
        git commit -m "chore: initial commit from Product.Template"
        
        print_success "Repositório Git inicializado"
        print_info "Para conectar a um repositório remoto, execute:"
        echo -e "   ${NC}git remote add origin <url-do-repositorio>${NC}"
        echo -e "   ${NC}git push -u origin master${NC}"
    else
        print_error "Erro ao inicializar Git"
    fi
    
    cd - > /dev/null
}

move_project() {
    local source_path=$1
    local destination_path=$2
    local project_name=$3
    
    print_step "Movendo projeto para destino final..."
    
    local final_path="$destination_path/$project_name"
    
    if [[ -d "$final_path" ]]; then
        echo -e "\n${YELLOW}⚠️  O diretório '$final_path' já existe!${NC}"
        echo -ne "Deseja sobrescrever? (S/N): "
        read response
        
        if [[ "$response" != "S" && "$response" != "s" ]]; then
            print_error "Operação cancelada pelo usuário"
            exit 1
        fi
        
        rm -rf "$final_path"
    fi
    
    # Criar diretório de destino se não existir
    mkdir -p "$destination_path"
    
    mv "$source_path" "$final_path"
    
    print_success "Projeto movido para: $final_path"
    
    echo "$final_path"
}

# ============================================================================
# PARSE ARGUMENTOS
# ============================================================================

while [[ $# -gt 0 ]]; do
    case $1 in
        -n|--name)
            PROJECT_NAME="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        --skip-git)
            SKIP_GIT_INIT=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            echo "Uso: ./setup.sh [OPÇÕES]"
            echo ""
            echo "Opções:"
            echo "  -n, --name NAME       Nome do novo projeto"
            echo "  -o, --output PATH     Caminho de destino"
            echo "  --skip-git            Não inicializar repositório Git"
            echo "  -v, --verbose         Modo verboso"
            echo "  -h, --help            Exibir esta ajuda"
            exit 0
            ;;
        *)
            print_error "Opção desconhecida: $1"
            echo "Use --help para ver as opções disponíveis"
            exit 1
            ;;
    esac
done

# ============================================================================
# SCRIPT PRINCIPAL
# ============================================================================

main() {
    print_header "🚀 Product.Template - Setup Inicial"
    
    # Obter caminho atual
    local current_path=$(pwd)
    
    print_info "Diretório atual: $current_path"
    
    # Validar se está no diretório correto
    if [[ ! -f "$current_path/Product.Template.sln" ]]; then
        print_error "Erro: Product.Template.sln não encontrado!"
        print_info "Execute este script na raiz do repositório clonado."
        exit 1
    fi
    
    # Obter nome do projeto
    if [[ -z "$PROJECT_NAME" ]]; then
        PROJECT_NAME=$(get_valid_project_name)
    elif ! validate_project_name "$PROJECT_NAME"; then
        exit 1
    fi
    
    # Obter caminho de destino
    local default_output_path=$(dirname "$current_path")
    if [[ -z "$OUTPUT_PATH" ]]; then
        OUTPUT_PATH=$(get_valid_output_path "$default_output_path")
    fi
    
    # Expandir ~ se necessário
    OUTPUT_PATH="${OUTPUT_PATH/#\~/$HOME}"
    
    # Confirmar configurações
    print_header "📋 Configurações"
    echo -e "${NC}Nome do Projeto : ${GREEN}$PROJECT_NAME${NC}"
    echo -e "${NC}Caminho Destino : ${GREEN}$OUTPUT_PATH${NC}"
    echo -e "${NC}Caminho Final   : ${GREEN}$OUTPUT_PATH/$PROJECT_NAME${NC}"
    
    echo -ne "\nContinuar? (S/N): "
    read confirm
    
    if [[ "$confirm" != "S" && "$confirm" != "s" ]]; then
        print_error "Operação cancelada pelo usuário"
        exit 0
    fi
    
    # Executar setup
    print_header "🔧 Iniciando Setup"
    
    # 1. Remover .git
    remove_git_folder "$current_path"
    
    # 2. Renomear arquivos
    rename_solution_files "$current_path" "$ORIGINAL_TEMPLATE" "$PROJECT_NAME"
    rename_project_files "$current_path" "$ORIGINAL_TEMPLATE" "$PROJECT_NAME"
    
    # 3. Renomear diretórios
    rename_directories "$current_path" "$ORIGINAL_TEMPLATE" "$PROJECT_NAME"
    
    # 4. Atualizar conteúdo
    update_file_contents "$current_path" "$TEMPLATE_NAMESPACE" "$PROJECT_NAME"
    
    # 5. Atualizar README
    update_readme_file "$current_path" "$PROJECT_NAME"
    
    # 6. Mover para destino final
    local final_path=$(move_project "$current_path" "$OUTPUT_PATH" "$PROJECT_NAME")
    
    # 7. Inicializar Git
    if ! $SKIP_GIT_INIT; then
        initialize_git_repository "$final_path"
    fi
    
    # Sucesso!
    print_header "✅ Setup Concluído com Sucesso!"
    
    echo -e "\n${CYAN}📂 Localização do Projeto:${NC}"
    echo -e "   ${GREEN}$final_path${NC}"
    
    echo -e "\n${CYAN}🚀 Próximos Passos:${NC}"
    echo -e "   ${NC}1. cd \"$final_path\"${NC}"
    echo -e "   ${NC}2. code . (abrir no VS Code)${NC}"
    echo -e "   ${NC}3. dotnet build${NC}"
    echo -e "   ${NC}4. cd src/Api && dotnet run${NC}"
    
    echo -e "\n${CYAN}📚 Documentação:${NC}"
    echo -e "   ${NC}• README.md - Visão geral${NC}"
    echo -e "   ${NC}• docs/MICROSOFT_AUTH_SETUP.md - Configurar autenticação Microsoft${NC}"
    echo -e "   ${NC}• docs/AUTHENTICATION_EXTENSIBILITY.md - Adicionar novos providers${NC}"
    
    echo ""
}

# Executar
main
