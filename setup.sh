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

# set -e  # Desabilitado para permitir tratamento de erros manual

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
    # Variável global para retornar o valor
    name=""
    
    while true; do
        echo ""
        echo -e "${CYAN}📝 Digite o nome do novo projeto:${NC}"
        print_info "   Exemplos: MyCompany.MyProduct, Contoso.Ecommerce, AcmeCorp.Api"
        printf "   ${YELLOW}→ ${NC}"
        
        # Ler do stdin padrão (funciona melhor no Git Bash)
        read -r name
        
        if validate_project_name "$name"; then
            # name será usado como variável global
            return 0
        fi
    done
}

get_valid_output_path() {
    local default_path=$1
    # Variável global para retornar o valor
    path=""
    
    while true; do
        echo ""
        echo -e "${CYAN}📁 Digite o caminho de destino (Enter para usar o padrão):${NC}"
        print_info "   Padrão: $default_path"
        printf "   ${YELLOW}→ ${NC}"
        
        # Ler do stdin padrão (funciona melhor no Git Bash)
        read -r path
        
        if [[ -z "$path" ]]; then
            # Usar padrão e retornar via variável global
            path="$default_path"
            return 0
        fi
        
        # Validar se é um caminho válido
        if [[ "$path" =~ ^[a-zA-Z0-9/._-]+$ ]] || [[ "$path" =~ ^~.*$ ]]; then
            # Expandir ~ se necessário
            path="${path/#\~/$HOME}"
            # path será usado como variável global
            return 0
        fi
        
        print_error "Caminho inválido. Tente novamente."
    done
}

# ============================================================================
# FUNÇÕES PRINCIPAIS
# ============================================================================

copy_template() {
    local source_path=$1
    local temp_path=$2
    
    # Redirecionar mensagens para stderr para não interferir com o retorno
    print_step "Criando cópia do template (preservando original)..." >&2
    
    # Criar diretório temporário se não existir
    local temp_parent=$(dirname "$temp_path")
    if ! mkdir -p "$temp_parent" 2>/dev/null; then
        # Fallback: usar TMP do Windows se /tmp não funcionar
        if [[ "$OSTYPE" == "msys"* ]] || [[ "$OSTYPE" == "cygwin"* ]]; then
            temp_parent="${TMP:-/tmp}"
            mkdir -p "$temp_parent" 2>/dev/null || temp_parent="/c/tmp"
            mkdir -p "$temp_parent" 2>/dev/null
        fi
    fi
    
    # Remover cópia temporária anterior se existir
    if [[ -d "$temp_path" ]]; then
        rm -rf "$temp_path" 2>/dev/null
    fi
    
    # Copiar todo o conteúdo (excluindo .git)
    local source_name=$(basename "$source_path")
    local temp_dir="$temp_parent/$source_name"
    
    # Criar diretório de destino primeiro
    mkdir -p "$temp_dir" 2>/dev/null || {
        print_error "Não foi possível criar diretório temporário: $temp_dir" >&2
        exit 1
    }
    
    # Método 1: Tentar cp primeiro (mais rápido)
    if cp -r "$source_path"/* "$temp_dir/" 2>/dev/null; then
        # Remover .git se foi copiado
        rm -rf "$temp_dir/.git" 2>/dev/null
        print_success "Cópia criada com sucesso" >&2
        echo "$temp_dir"
        return 0
    fi
    
    # Método 2: Tentar rsync se disponível
    if command -v rsync >/dev/null 2>&1; then
        if rsync -a --exclude='.git' "$source_path/" "$temp_dir/" 2>/dev/null; then
            print_success "Cópia criada com sucesso (via rsync)" >&2
            echo "$temp_dir"
            return 0
        fi
    fi
    
    # Método 3: Cópia manual arquivo por arquivo (mais lento mas funciona sempre)
    print_info "Usando método de cópia manual..." >&2
    local file_count=0
    
    while IFS= read -r -d '' file; do
        local rel_path="${file#$source_path/}"
        local dest_file="$temp_dir/$rel_path"
        local dest_dir=$(dirname "$dest_file")
        
        # Pular .git
        if [[ "$rel_path" == .git* ]]; then
            continue
        fi
        
        # Criar diretório se necessário
        mkdir -p "$dest_dir" 2>/dev/null
        
        # Copiar arquivo
        if cp "$file" "$dest_file" 2>/dev/null; then
            ((file_count++))
        fi
    done < <(find "$source_path" -type f -print0 2>/dev/null)
    
    if [[ $file_count -gt 0 ]]; then
        print_success "Cópia criada com sucesso ($file_count arquivos)" >&2
        echo "$temp_dir"
        return 0
    fi
    
    # Se chegou aqui, todos os métodos falharam
    print_error "Erro ao copiar template. Verifique permissões e espaço em disco." >&2
    print_error "Source: $source_path" >&2
    print_error "Dest: $temp_dir" >&2
    exit 1
}

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

    print_step "Atualizando conteúdo dos arquivos (namespaces, usings, referências)..."

    # Extensões de arquivos para atualizar
    local extensions=("*.cs" "*.csproj" "*.sln" "*.json" "*.md" "*.yml" "*.yaml" "*.xml" "*.config" "*.txt")

    local file_count=0
    local updated_count=0
    local processed=0

    # Primeiro, contar total de arquivos para progresso (usando método mais simples)
    print_info "Contando arquivos..." >&2
    for ext in "${extensions[@]}"; do
        local count=$(find "$path" -type f -name "$ext" \
            ! -path "*/bin/*" \
            ! -path "*/obj/*" \
            ! -path "*/.git/*" \
            ! -path "*/node_modules/*" \
            2>/dev/null | wc -l)
        file_count=$((file_count + count))
    done

    if [[ $file_count -eq 0 ]]; then
        print_info "Nenhum arquivo encontrado para atualizar"
        return 0
    fi

    print_info "Encontrados $file_count arquivos para processar..." >&2

    # Processar arquivos (usando método mais simples e compatível)
    for ext in "${extensions[@]}"; do
        printf "   Buscando arquivos %s..." "$ext" >&2
        
        # Coletar arquivos primeiro em array (evita problemas com pipe e subshell)
        local files=()
        # Usar método mais simples e compatível - salvar em arquivo temporário primeiro
        local temp_list=$(mktemp 2>/dev/null || echo "/tmp/file_list_$$")
        
        find "$path" -type f -name "$ext" \
            ! -path "*/bin/*" \
            ! -path "*/obj/*" \
            ! -path "*/.git/*" \
            ! -path "*/node_modules/*" \
            2>/dev/null > "$temp_list" || true
        
        # Ler arquivos do arquivo temporário
        while IFS= read -r file || [[ -n "$file" ]]; do
            [[ -z "$file" ]] && continue
            [[ ! -f "$file" ]] && continue
            files+=("$file")
        done < "$temp_list"
        
        # Limpar arquivo temporário
        rm -f "$temp_list" 2>/dev/null || true
        
        printf "\r   Encontrados %d arquivos %s\n" "${#files[@]}" "$ext" >&2
        
        if [[ ${#files[@]} -eq 0 ]]; then
            continue
        fi
        
        # Processar cada arquivo
        local ext_processed=0
        for file in "${files[@]}"; do
            ((processed++))
            ((ext_processed++))
            
            # Mostrar progresso a cada 5 arquivos ou no primeiro
            if [[ $((ext_processed % 5)) -eq 0 ]] || [[ $ext_processed -eq 1 ]]; then
                printf "\r   Processando %s: %d/%d (total: %d/%d)..." "$ext" "$ext_processed" "${#files[@]}" "$processed" "$file_count" >&2
            fi

            # Verificar se arquivo contém o texto antigo
            if grep -q "$old_name" "$file" 2>/dev/null; then
                # Usar sed de forma compatível com Linux, Mac e Git Bash (Windows)
                if [[ "$OSTYPE" == "darwin"* ]]; then
                    # macOS - usa sintaxe BSD
                    sed -i '' "s|$old_name|$new_name|g" "$file" 2>/dev/null && ((updated_count++))
                elif [[ "$OSTYPE" == "msys"* ]] || [[ "$OSTYPE" == "cygwin"* ]]; then
                    # Git Bash no Windows - usar método alternativo com arquivo temporário
                    local temp_file="${file}.tmp"
                    if sed "s|$old_name|$new_name|g" "$file" > "$temp_file" 2>/dev/null; then
                        if mv "$temp_file" "$file" 2>/dev/null; then
                            ((updated_count++))
                        else
                            rm -f "$temp_file" 2>/dev/null
                        fi
                    else
                        rm -f "$temp_file" 2>/dev/null
                    fi
                else
                    # Linux - usa sintaxe GNU
                    sed -i "s|$old_name|$new_name|g" "$file" 2>/dev/null && ((updated_count++))
                fi

                if $VERBOSE; then
                    echo "" >&2
                    print_success "✓ Atualizado: $(basename "$file")" >&2
                fi
            fi
        done
        
        # Mostrar progresso após cada extensão
        printf "\r   Concluído %s: %d arquivos processados\n" "$ext" "$ext_processed" >&2
    done

    # Limpar linha de progresso
    printf "\r" >&2
    echo "" >&2
    print_success "Atualizado conteúdo de $updated_count de $file_count arquivos"
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
        
        # Detectar branch padrão (main ou master)
        local default_branch=$(git symbolic-ref --short HEAD 2>/dev/null)
        if [[ -z "$default_branch" ]]; then
            # Tentar obter da configuração do Git, senão usar 'main' como padrão moderno
            default_branch=$(git config --global init.defaultBranch 2>/dev/null)
            if [[ -z "$default_branch" ]]; then
                default_branch="main"
            fi
        fi
        echo -e "   ${NC}git push -u origin $default_branch${NC}"
    else
        print_error "Erro ao inicializar Git"
    fi
    
    cd - > /dev/null
}

move_project() {
    local source_path=$1
    local destination_path=$2
    local project_name=$3
    
    # Redirecionar mensagens para stderr para não interferir com o retorno
    print_step "Movendo projeto para destino final..." >&2
    
    local final_path="$destination_path/$project_name"
    
    if [[ -d "$final_path" ]]; then
        echo "" >&2
        echo -e "${YELLOW}⚠️  O diretório '$final_path' já existe!${NC}" >&2
        printf "Deseja sobrescrever? (S/N): " >&2
        read -r response
        
        if [[ "$response" != "S" && "$response" != "s" ]]; then
            print_error "Operação cancelada pelo usuário" >&2
            exit 1
        fi
        
        rm -rf "$final_path"
    fi
    
    # Criar diretório de destino se não existir
    mkdir -p "$destination_path"
    
    mv "$source_path" "$final_path"
    
    print_success "Projeto movido para: $final_path" >&2
    
    # Retornar apenas o caminho via stdout
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
    
    # Variável para armazenar caminho da cópia temporária (para limpeza em caso de erro)
    local working_path=""
    
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
        # Não usar command substitution para não redirecionar stdin
        get_valid_project_name
        PROJECT_NAME="$name"
    elif ! validate_project_name "$PROJECT_NAME"; then
        exit 1
    fi
    
    # Obter caminho de destino
    local default_output_path=$(dirname "$current_path")
    if [[ -z "$OUTPUT_PATH" ]]; then
        # Não usar command substitution para não redirecionar stdin
        get_valid_output_path "$default_output_path"
        OUTPUT_PATH="$path"
    fi
    
    # Expandir ~ se necessário
    OUTPUT_PATH="${OUTPUT_PATH/#\~/$HOME}"
    
    # Confirmar configurações
    print_header "📋 Configurações"
    echo -e "${NC}Nome do Projeto : ${GREEN}$PROJECT_NAME${NC}"
    echo -e "${NC}Caminho Destino : ${GREEN}$OUTPUT_PATH${NC}"
    echo -e "${NC}Caminho Final   : ${GREEN}$OUTPUT_PATH/$PROJECT_NAME${NC}"
    
    echo ""
    printf "Continuar? (S/N): "
    read -r confirm
    
    if [[ "$confirm" != "S" && "$confirm" != "s" ]]; then
        print_error "Operação cancelada pelo usuário"
        exit 0
    fi
    
    # Executar setup
    print_header "🔧 Iniciando Setup"

    # Criar caminho temporário para cópia
    local temp_dir="${TMPDIR:-/tmp}/Product.Template.Setup.$$"

    # 0. Criar cópia do template (preserva o original)
    working_path=$(copy_template "$current_path" "$temp_dir")
    
    print_info "Trabalhando na cópia: $working_path"
    print_info "Template original preservado em: $current_path"

    # 1. Remover .git da cópia
    remove_git_folder "$working_path"

    # 2. Atualizar conteúdo PRIMEIRO (antes de renomear arquivos e diretórios)
    print_step "PASSO 1: Atualizando conteúdo interno dos arquivos..."
    update_file_contents "$working_path" "$TEMPLATE_NAMESPACE" "$PROJECT_NAME"

    # 3. Renomear arquivos de projeto e solução
    print_step "PASSO 2: Renomeando arquivos..."
    rename_solution_files "$working_path" "$ORIGINAL_TEMPLATE" "$PROJECT_NAME"
    rename_project_files "$working_path" "$ORIGINAL_TEMPLATE" "$PROJECT_NAME"

    # 4. Renomear diretórios (do mais profundo para o mais raso)
    print_step "PASSO 3: Renomeando diretórios..."
    rename_directories "$working_path" "$ORIGINAL_TEMPLATE" "$PROJECT_NAME"

    # 5. Atualizar README
    update_readme_file "$working_path" "$PROJECT_NAME"

    # 6. Mover para destino final
    local final_path=$(move_project "$working_path" "$OUTPUT_PATH" "$PROJECT_NAME")

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
    
    echo -e "\n${GREEN}✅ Template original preservado em: $current_path${NC}"
    
    echo ""
    
    # Limpar cópia temporária se ainda existir (não deveria, pois foi movida)
    if [[ -n "$working_path" ]] && [[ -d "$working_path" ]]; then
        print_info "Limpando cópia temporária..."
        rm -rf "$working_path" 2>/dev/null || true
    fi
    
    # Remover trap ao sair com sucesso
    trap - ERR EXIT
}

# Executar
main
