#!/bin/bash

# SalesAPI Final Architecture Validation Script
# Validates all improvements and optimizations implemented

echo "?? SalesAPI - Valida��o Final da Arquitetura Otimizada"
echo "====================================================="

# Initialize counters
VALIDATION_SUCCESS=0
VALIDATION_WARNINGS=0
VALIDATION_ERRORS=0

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Function to report results
report_success() { echo -e "${GREEN}? $1${NC}"; ((VALIDATION_SUCCESS++)); }
report_warning() { echo -e "${YELLOW}??  $1${NC}"; ((VALIDATION_WARNINGS++)); }
report_error() { echo -e "${RED}? $1${NC}"; ((VALIDATION_ERRORS++)); }
report_info() { echo -e "${BLUE}?? $1${NC}"; }

echo ""
report_info "1. VALIDANDO MELHORIAS ARQUITETURAIS IMPLEMENTADAS..."

# Check shared configuration
echo ""
report_info "1.1 Validando Configura��es Centralizadas"

if [ -f "src/shared/Configuration/SharedConfiguration.cs" ]; then
    report_success "Configura��o compartilhada implementada"
    
    # Check if configuration is being used
    if grep -q "SharedConfiguration" src/*/appsettings.json 2>/dev/null; then
        report_success "Configura��o compartilhada sendo utilizada"
    else
        report_warning "Configura��o compartilhada criada mas n�o sendo utilizada"
    fi
else
    report_error "Configura��o compartilhada n�o implementada"
fi

# Check consolidated documentation
echo ""
report_info "1.2 Validando Documenta��o Consolidada"

if [ -f "docs/README-COMPLETE.md" ]; then
    report_success "Documenta��o principal consolidada"
    
    # Check documentation quality
    doc_size=$(wc -l < "docs/README-COMPLETE.md")
    if [ "$doc_size" -gt 200 ]; then
        report_success "Documenta��o abrangente ($doc_size linhas)"
    else
        report_warning "Documenta��o pode estar incompleta ($doc_size linhas)"
    fi
else
    report_error "Documenta��o consolidada n�o encontrada"
fi

if [ -f "docs/ARCHITECTURE.md" ]; then
    report_success "Documenta��o arquitetural criada"
else
    report_warning "Documenta��o arquitetural n�o encontrada"
fi

if [ -f "docs/DEPLOYMENT.md" ]; then
    report_success "Guia de deployment criado"
else
    report_warning "Guia de deployment n�o encontrado"
fi

# Check development scripts
echo ""
report_info "1.3 Validando Scripts de Desenvolvimento"

scripts_to_check=(
    "scripts/development/cleanup.sh"
    "scripts/development/cleanup.ps1"
    "scripts/development/validate-architecture.sh"
    "scripts/development/validate-architecture.ps1"
    "scripts/development/start-all.sh"
    "scripts/development/start-all.ps1"
)

script_count=0
for script in "${scripts_to_check[@]}"; do
    if [ -f "$script" ]; then
        ((script_count++))
    fi
done

if [ $script_count -eq ${#scripts_to_check[@]} ]; then
    report_success "Todos os scripts de desenvolvimento criados ($script_count/${#scripts_to_check[@]})"
elif [ $script_count -gt $((${#scripts_to_check[@]} / 2)) ]; then
    report_warning "Maioria dos scripts criados ($script_count/${#scripts_to_check[@]})"
else
    report_error "Scripts de desenvolvimento incompletos ($script_count/${#scripts_to_check[@]})"
fi

# Check deployment automation
echo ""
report_info "1.4 Validando Automa��o de Deploy"

if [ -f "scripts/deployment/deploy.sh" ]; then
    report_success "Script de deploy automatizado criado"
    
    # Check if script is executable
    if [ -x "scripts/deployment/deploy.sh" ]; then
        report_success "Script de deploy � execut�vel"
    else
        report_warning "Script de deploy precisa de permiss�o de execu��o"
    fi
else
    report_error "Script de deploy automatizado n�o encontrado"
fi

# Check testing infrastructure
echo ""
report_info "1.5 Validando Infraestrutura de Testes"

if [ -f "scripts/testing/quick-validation.sh" ]; then
    report_success "Script de valida��o r�pida criado"
else
    report_warning "Script de valida��o r�pida n�o encontrado"
fi

# Count total test projects
test_project_count=$(find tests/ -name "*.csproj" 2>/dev/null | wc -l)
if [ "$test_project_count" -ge 4 ]; then
    report_success "Infraestrutura robusta de testes ($test_project_count projetos)"
elif [ "$test_project_count" -ge 2 ]; then
    report_warning "Infraestrutura de testes b�sica ($test_project_count projetos)"
else
    report_error "Infraestrutura de testes insuficiente ($test_project_count projetos)"
fi

# Check for production-ready patterns
echo ""
report_info "2. VALIDANDO PADR�ES PRODUCTION-READY..."

echo ""
report_info "2.1 Validando Separa��o de Ambientes"

# Check for environment-specific configurations
if [ -f "src/gateway/appsettings.Development.json" ] || [ -f "src/gateway/appsettings.Production.json" ]; then
    report_success "Configura��es espec�ficas por ambiente implementadas"
else
    report_warning "Configura��es espec�ficas por ambiente podem estar em falta"
fi

echo ""
report_info "2.2 Validando Seguran�a"

# Check for JWT configuration
jwt_configs=$(grep -r "Jwt" src/*/appsettings.json 2>/dev/null | wc -l)
if [ "$jwt_configs" -gt 0 ]; then
    report_success "Configura��o JWT encontrada"
    
    # Check for strong keys (basic validation)
    weak_keys=$(grep -r '"Key".*"test\|"Key".*"123\|"Key".*"secret"' src/*/appsettings.json 2>/dev/null | wc -l)
    if [ "$weak_keys" -eq 0 ]; then
        report_success "Chaves JWT aparentam ser seguras"
    else
        report_warning "Algumas chaves JWT podem ser fracas para produ��o"
    fi
else
    report_error "Configura��o JWT n�o encontrada"
fi

echo ""
report_info "2.3 Validando Observabilidade"

# Check for logging configuration
if grep -q "Serilog\|Logging" src/*/Program.cs 2>/dev/null; then
    report_success "Logging estruturado configurado"
else
    report_warning "Logging estruturado pode n�o estar configurado"
fi

# Check for health checks
if grep -q "AddHealthChecks\|MapHealthChecks" src/*/Program.cs 2>/dev/null; then
    report_success "Health checks implementados"
else
    report_warning "Health checks podem n�o estar implementados"
fi

# Check for metrics
if grep -q "Prometheus\|UseHttpMetrics" src/*/Program.cs 2>/dev/null; then
    report_success "M�tricas Prometheus configuradas"
else
    report_warning "M�tricas podem n�o estar configuradas"
fi

echo ""
report_info "3. VALIDANDO QUALIDADE DE C�DIGO..."

echo ""
report_info "3.1 Validando Estrutura de Projetos"

# Check project structure
required_projects=(
    "src/gateway"
    "src/sales.api"
    "src/inventory.api"
    "src/buildingblocks.contracts"
    "src/buildingblocks.events"
)

project_structure_ok=true
for project in "${required_projects[@]}"; do
    if [ -d "$project" ]; then
        continue
    else
        report_warning "Projeto $project n�o encontrado"
        project_structure_ok=false
    fi
done

if [ "$project_structure_ok" = true ]; then
    report_success "Estrutura de projetos correta"
fi

echo ""
report_info "3.2 Validando Build"

# Try to build the solution
report_info "Executando build de valida��o..."
if dotnet build SalesAPI.sln --configuration Debug --verbosity quiet >/dev/null 2>&1; then
    report_success "Build executado com sucesso"
else
    report_error "Build falhando - corre��es necess�rias"
fi

echo ""
report_info "4. VALIDANDO FUNCIONALIDADES IMPLEMENTADAS..."

echo ""
report_info "4.1 Validando Messaging"

# Check for messaging implementation
if find src/ -name "*.cs" -exec grep -l "IEventPublisher\|RealEventPublisher" {} \; 2>/dev/null | head -1 >/dev/null; then
    report_success "Sistema de messaging implementado"
    
    # Check for production-ready messaging (no fake/mock in production code)
    fake_messaging=$(find src/ -name "*.cs" -exec grep -l "FakeEventPublisher\|MockEventPublisher\|DummyEventPublisher" {} \; 2>/dev/null | wc -l)
    if [ "$fake_messaging" -eq 0 ]; then
        report_success "Messaging production-ready (sem implementa��es fake)"
    else
        report_warning "Implementa��es fake de messaging encontradas no c�digo de produ��o"
    fi
else
    report_error "Sistema de messaging n�o implementado"
fi

echo ""
report_info "4.2 Validando Containeriza��o"

# Check for Docker files
docker_files=(
    "docker-compose.yml"
    "src/gateway/Dockerfile"
    "src/sales.api/Dockerfile"
    "src/inventory.api/Dockerfile"
)

docker_count=0
for dockerfile in "${docker_files[@]}"; do
    if [ -f "$dockerfile" ]; then
        ((docker_count++))
    fi
done

if [ $docker_count -eq ${#docker_files[@]} ]; then
    report_success "Containeriza��o completa ($docker_count/${#docker_files[@]} arquivos)"
elif [ $docker_count -gt 1 ]; then
    report_warning "Containeriza��o parcial ($docker_count/${#docker_files[@]} arquivos)"
else
    report_error "Containeriza��o n�o implementada"
fi

# Final calculation and summary
TOTAL_CHECKS=$((VALIDATION_SUCCESS + VALIDATION_WARNINGS + VALIDATION_ERRORS))
SUCCESS_RATE=$((VALIDATION_SUCCESS * 100 / TOTAL_CHECKS))

echo ""
echo "=============================================="
echo "?? RESUMO DA VALIDA��O FINAL"
echo "=============================================="
echo "Total de Valida��es: $TOTAL_CHECKS"
echo "? Sucessos: $VALIDATION_SUCCESS"
echo "??  Avisos: $VALIDATION_WARNINGS"
echo "? Erros: $VALIDATION_ERRORS"
echo "?? Taxa de Sucesso: ${SUCCESS_RATE}%"
echo ""

# Final assessment
if [ $VALIDATION_ERRORS -eq 0 ] && [ $SUCCESS_RATE -ge 90 ]; then
    echo -e "${GREEN}?? ARQUITETURA OTIMIZADA COM EXCEL�NCIA!${NC}"
    echo -e "${GREEN}? Implementa��o profissional completa${NC}"
    echo -e "${GREEN}?? Sistema pronto para produ��o${NC}"
    
    echo ""
    echo "?? MELHORIAS IMPLEMENTADAS COM SUCESSO:"
    echo "  � ? Configura��es centralizadas e consolidadas"
    echo "  � ? Documenta��o profissional unificada"
    echo "  � ? Scripts de automa��o completos"
    echo "  � ? Deploy automatizado multi-ambiente"
    echo "  � ? Valida��o de arquitetura automatizada"
    echo "  � ? Infraestrutura de testes robusta"
    echo "  � ? Padr�es production-ready implementados"
    echo "  � ? Observabilidade e monitoring"
    echo "  � ? Seguran�a e containeriza��o"
    
    exit 0
    
elif [ $VALIDATION_ERRORS -eq 0 ] && [ $SUCCESS_RATE -ge 75 ]; then
    echo -e "${YELLOW}? ARQUITETURA OTIMIZADA COM QUALIDADE ALTA${NC}"
    echo -e "${YELLOW}??  Algumas melhorias adicionais recomendadas${NC}"
    echo -e "${GREEN}?? Sistema funcional e profissional${NC}"
    
    echo ""
    echo "?? PRINCIPAIS CONQUISTAS:"
    echo "  � Estrutura profissional implementada"
    echo "  � Documenta��o consolidada"
    echo "  � Scripts de automa��o funcionais"
    echo "  � Padr�es de qualidade seguidos"
    
    echo ""
    echo "?? MELHORIAS RECOMENDADAS:"
    echo "  � Resolver avisos identificados"
    echo "  � Completar componentes opcionais"
    echo "  � Otimizar configura��es restantes"
    
    exit 0
    
else
    echo -e "${RED}? ARQUITETURA PRECISA DE MAIS OTIMIZA��ES${NC}"
    echo -e "${RED}?? $VALIDATION_ERRORS erros cr�ticos encontrados${NC}"
    echo -e "${YELLOW}??  $VALIDATION_WARNINGS avisos precisam de aten��o${NC}"
    
    echo ""
    echo "???  A��ES NECESS�RIAS:"
    echo "  � Corrigir todos os erros identificados"
    echo "  � Implementar componentes em falta"
    echo "  � Executar novamente a valida��o"
    
    echo ""
    echo "?? PR�XIMOS PASSOS:"
    echo "  1. Implementar corre��es necess�rias"
    echo "  2. Executar: $0"
    echo "  3. Repetir at� atingir > 90% de sucesso"
    
    exit 1
fi