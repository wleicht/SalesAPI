#!/bin/bash

# SalesAPI Architecture Validation Script
# Validates the professional architecture standards are maintained

echo "?? Validando arquitetura do SalesAPI..."

# Initialize validation results
VALIDATION_ERRORS=0
VALIDATION_WARNINGS=0

# Function to report error
report_error() {
    echo "? ERRO: $1"
    ((VALIDATION_ERRORS++))
}

# Function to report warning  
report_warning() {
    echo "??  AVISO: $1"
    ((VALIDATION_WARNINGS++))
}

# Function to report success
report_success() {
    echo "? $1"
}

echo ""
echo "?? 1. Verificando implementações fake/mock em produção..."

# Check for fake/mock implementations in production code
fake_files=$(find src/ -name "*.cs" -exec grep -l "Fake\|Mock\|Dummy\|Test" {} \; 2>/dev/null | grep -v "Test" | grep -v "Mock")

if [ -z "$fake_files" ]; then
    report_success "Nenhuma implementação fake encontrada no código de produção"
else
    report_error "Implementações fake/mock encontradas no código de produção:"
    echo "$fake_files" | while read -r file; do
        echo "  - $file"
    done
fi

echo ""
echo "?? 2. Verificando estrutura de pastas obrigatória..."

# Required directories
required_dirs=(
    "src/gateway"
    "src/sales.api" 
    "src/inventory.api"
    "tests"
    "docs"
    "scripts"
)

for dir in "${required_dirs[@]}"; do
    if [ -d "$dir" ]; then
        report_success "$dir existe"
    else
        report_error "$dir não encontrado"
    fi
done

echo ""
echo "?? 3. Verificando arquivos de configuração..."

# Check for configuration files
config_files=(
    "src/gateway/appsettings.json"
    "src/sales.api/appsettings.json"
    "src/inventory.api/appsettings.json"
)

for file in "${config_files[@]}"; do
    if [ -f "$file" ]; then
        report_success "$file existe"
        
        # Check for sensitive information exposure
        if grep -q "password.*=.*[^*]" "$file" 2>/dev/null; then
            report_warning "$file pode conter senhas em texto claro"
        fi
    else
        report_error "$file não encontrado"
    fi
done

echo ""
echo "?? 4. Verificando dependências desnecessárias..."

# Check for unused projects
echo "Verificando projetos não referenciados..."
# This is a simplified check - in production, use tools like dotnet-outdated
project_files=$(find . -name "*.csproj" -exec basename {} .csproj \;)
echo "Projetos encontrados:"
echo "$project_files" | while read -r project; do
    echo "  - $project"
done

echo ""
echo "?? 5. Verificando testes..."

# Check test structure
if [ -d "tests/SalesAPI.Tests.Professional" ]; then
    report_success "Estrutura de testes profissionais encontrada"
    
    test_projects=$(find tests/ -name "*.csproj" | wc -l)
    report_success "Encontrados $test_projects projetos de teste"
    
    if [ "$test_projects" -ge 3 ]; then
        report_success "Cobertura de testes adequada (3+ projetos)"
    else
        report_warning "Cobertura de testes pode ser insuficiente (<3 projetos)"
    fi
else
    report_error "Estrutura de testes profissionais não encontrada"
fi

echo ""
echo "?? 6. Verificando documentação..."

# Check for documentation
doc_files=("README.md" "docs/")
for doc in "${doc_files[@]}"; do
    if [ -e "$doc" ]; then
        report_success "Documentação $doc encontrada"
    else
        report_warning "Documentação $doc não encontrada"
    fi
done

echo ""
echo "?? 7. Verificando scripts de automação..."

# Check for automation scripts
script_dirs=("scripts/development" "scripts/testing" "scripts/manual-tests")
for script_dir in "${script_dirs[@]}"; do
    if [ -d "$script_dir" ]; then
        script_count=$(find "$script_dir" -name "*.sh" -o -name "*.ps1" | wc -l)
        if [ "$script_count" -gt 0 ]; then
            report_success "$script_dir contém $script_count scripts"
        else
            report_warning "$script_dir existe mas não contém scripts"
        fi
    else
        report_warning "$script_dir não encontrado"
    fi
done

echo ""
echo "?? 8. Verificando padrões de código..."

# Check for TODO/FIXME comments that should be addressed
todo_count=$(find src/ -name "*.cs" -exec grep -c "TODO\|FIXME\|HACK" {} \; 2>/dev/null | paste -sd+ - | bc 2>/dev/null || echo "0")
if [ "$todo_count" -eq 0 ]; then
    report_success "Nenhum TODO/FIXME/HACK encontrado no código de produção"
elif [ "$todo_count" -le 5 ]; then
    report_warning "$todo_count TODOs/FIXMEs/HACKs encontrados (aceitável)"
else
    report_error "$todo_count TODOs/FIXMEs/HACKs encontrados (muitos para produção)"
fi

# Final validation summary
echo ""
echo "=================================="
echo "?? RESUMO DA VALIDAÇÃO"
echo "=================================="

if [ $VALIDATION_ERRORS -eq 0 ] && [ $VALIDATION_WARNINGS -eq 0 ]; then
    echo "?? ARQUITETURA PERFEITA!"
    echo "? Todos os critérios de qualidade foram atendidos"
    exit 0
elif [ $VALIDATION_ERRORS -eq 0 ]; then
    echo "? ARQUITETURA APROVADA COM RESSALVAS"
    echo "??  $VALIDATION_WARNINGS avisos encontrados"
    echo "?? Considere resolver os avisos para melhorar ainda mais a qualidade"
    exit 0
else
    echo "? ARQUITETURA PRECISA DE CORREÇÕES"
    echo "?? $VALIDATION_ERRORS erros críticos encontrados"
    echo "??  $VALIDATION_WARNINGS avisos encontrados"
    echo "???  Corrija os erros antes de prosseguir para produção"
    exit 1
fi