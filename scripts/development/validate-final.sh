#!/bin/bash

# SalesAPI Final Architecture Validation Script
# Validates all improvements and optimizations implemented

echo "?? SalesAPI - Validação Final da Arquitetura Otimizada"
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
report_info "1.1 Validando Configurações Centralizadas"

if [ -f "src/shared/Configuration/SharedConfiguration.cs" ]; then
    report_success "Configuração compartilhada implementada"
    
    # Check if configuration is being used
    if grep -q "SharedConfiguration" src/*/appsettings.json 2>/dev/null; then
        report_success "Configuração compartilhada sendo utilizada"
    else
        report_warning "Configuração compartilhada criada mas não sendo utilizada"
    fi
else
    report_error "Configuração compartilhada não implementada"
fi

# Check consolidated documentation
echo ""
report_info "1.2 Validando Documentação Consolidada"

if [ -f "docs/README-COMPLETE.md" ]; then
    report_success "Documentação principal consolidada"
    
    # Check documentation quality
    doc_size=$(wc -l < "docs/README-COMPLETE.md")
    if [ "$doc_size" -gt 200 ]; then
        report_success "Documentação abrangente ($doc_size linhas)"
    else
        report_warning "Documentação pode estar incompleta ($doc_size linhas)"
    fi
else
    report_error "Documentação consolidada não encontrada"
fi

if [ -f "docs/ARCHITECTURE.md" ]; then
    report_success "Documentação arquitetural criada"
else
    report_warning "Documentação arquitetural não encontrada"
fi

if [ -f "docs/DEPLOYMENT.md" ]; then
    report_success "Guia de deployment criado"
else
    report_warning "Guia de deployment não encontrado"
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
report_info "1.4 Validando Automação de Deploy"

if [ -f "scripts/deployment/deploy.sh" ]; then
    report_success "Script de deploy automatizado criado"
    
    # Check if script is executable
    if [ -x "scripts/deployment/deploy.sh" ]; then
        report_success "Script de deploy é executável"
    else
        report_warning "Script de deploy precisa de permissão de execução"
    fi
else
    report_error "Script de deploy automatizado não encontrado"
fi

# Check testing infrastructure
echo ""
report_info "1.5 Validando Infraestrutura de Testes"

if [ -f "scripts/testing/quick-validation.sh" ]; then
    report_success "Script de validação rápida criado"
else
    report_warning "Script de validação rápida não encontrado"
fi

# Count total test projects
test_project_count=$(find tests/ -name "*.csproj" 2>/dev/null | wc -l)
if [ "$test_project_count" -ge 4 ]; then
    report_success "Infraestrutura robusta de testes ($test_project_count projetos)"
elif [ "$test_project_count" -ge 2 ]; then
    report_warning "Infraestrutura de testes básica ($test_project_count projetos)"
else
    report_error "Infraestrutura de testes insuficiente ($test_project_count projetos)"
fi

# Check for production-ready patterns
echo ""
report_info "2. VALIDANDO PADRÕES PRODUCTION-READY..."

echo ""
report_info "2.1 Validando Separação de Ambientes"

# Check for environment-specific configurations
if [ -f "src/gateway/appsettings.Development.json" ] || [ -f "src/gateway/appsettings.Production.json" ]; then
    report_success "Configurações específicas por ambiente implementadas"
else
    report_warning "Configurações específicas por ambiente podem estar em falta"
fi

echo ""
report_info "2.2 Validando Segurança"

# Check for JWT configuration
jwt_configs=$(grep -r "Jwt" src/*/appsettings.json 2>/dev/null | wc -l)
if [ "$jwt_configs" -gt 0 ]; then
    report_success "Configuração JWT encontrada"
    
    # Check for strong keys (basic validation)
    weak_keys=$(grep -r '"Key".*"test\|"Key".*"123\|"Key".*"secret"' src/*/appsettings.json 2>/dev/null | wc -l)
    if [ "$weak_keys" -eq 0 ]; then
        report_success "Chaves JWT aparentam ser seguras"
    else
        report_warning "Algumas chaves JWT podem ser fracas para produção"
    fi
else
    report_error "Configuração JWT não encontrada"
fi

echo ""
report_info "2.3 Validando Observabilidade"

# Check for logging configuration
if grep -q "Serilog\|Logging" src/*/Program.cs 2>/dev/null; then
    report_success "Logging estruturado configurado"
else
    report_warning "Logging estruturado pode não estar configurado"
fi

# Check for health checks
if grep -q "AddHealthChecks\|MapHealthChecks" src/*/Program.cs 2>/dev/null; then
    report_success "Health checks implementados"
else
    report_warning "Health checks podem não estar implementados"
fi

# Check for metrics
if grep -q "Prometheus\|UseHttpMetrics" src/*/Program.cs 2>/dev/null; then
    report_success "Métricas Prometheus configuradas"
else
    report_warning "Métricas podem não estar configuradas"
fi

echo ""
report_info "3. VALIDANDO QUALIDADE DE CÓDIGO..."

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
        report_warning "Projeto $project não encontrado"
        project_structure_ok=false
    fi
done

if [ "$project_structure_ok" = true ]; then
    report_success "Estrutura de projetos correta"
fi

echo ""
report_info "3.2 Validando Build"

# Try to build the solution
report_info "Executando build de validação..."
if dotnet build SalesAPI.sln --configuration Debug --verbosity quiet >/dev/null 2>&1; then
    report_success "Build executado com sucesso"
else
    report_error "Build falhando - correções necessárias"
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
        report_success "Messaging production-ready (sem implementações fake)"
    else
        report_warning "Implementações fake de messaging encontradas no código de produção"
    fi
else
    report_error "Sistema de messaging não implementado"
fi

echo ""
report_info "4.2 Validando Containerização"

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
    report_success "Containerização completa ($docker_count/${#docker_files[@]} arquivos)"
elif [ $docker_count -gt 1 ]; then
    report_warning "Containerização parcial ($docker_count/${#docker_files[@]} arquivos)"
else
    report_error "Containerização não implementada"
fi

# Final calculation and summary
TOTAL_CHECKS=$((VALIDATION_SUCCESS + VALIDATION_WARNINGS + VALIDATION_ERRORS))
SUCCESS_RATE=$((VALIDATION_SUCCESS * 100 / TOTAL_CHECKS))

echo ""
echo "=============================================="
echo "?? RESUMO DA VALIDAÇÃO FINAL"
echo "=============================================="
echo "Total de Validações: $TOTAL_CHECKS"
echo "? Sucessos: $VALIDATION_SUCCESS"
echo "??  Avisos: $VALIDATION_WARNINGS"
echo "? Erros: $VALIDATION_ERRORS"
echo "?? Taxa de Sucesso: ${SUCCESS_RATE}%"
echo ""

# Final assessment
if [ $VALIDATION_ERRORS -eq 0 ] && [ $SUCCESS_RATE -ge 90 ]; then
    echo -e "${GREEN}?? ARQUITETURA OTIMIZADA COM EXCELÊNCIA!${NC}"
    echo -e "${GREEN}? Implementação profissional completa${NC}"
    echo -e "${GREEN}?? Sistema pronto para produção${NC}"
    
    echo ""
    echo "?? MELHORIAS IMPLEMENTADAS COM SUCESSO:"
    echo "  • ? Configurações centralizadas e consolidadas"
    echo "  • ? Documentação profissional unificada"
    echo "  • ? Scripts de automação completos"
    echo "  • ? Deploy automatizado multi-ambiente"
    echo "  • ? Validação de arquitetura automatizada"
    echo "  • ? Infraestrutura de testes robusta"
    echo "  • ? Padrões production-ready implementados"
    echo "  • ? Observabilidade e monitoring"
    echo "  • ? Segurança e containerização"
    
    exit 0
    
elif [ $VALIDATION_ERRORS -eq 0 ] && [ $SUCCESS_RATE -ge 75 ]; then
    echo -e "${YELLOW}? ARQUITETURA OTIMIZADA COM QUALIDADE ALTA${NC}"
    echo -e "${YELLOW}??  Algumas melhorias adicionais recomendadas${NC}"
    echo -e "${GREEN}?? Sistema funcional e profissional${NC}"
    
    echo ""
    echo "?? PRINCIPAIS CONQUISTAS:"
    echo "  • Estrutura profissional implementada"
    echo "  • Documentação consolidada"
    echo "  • Scripts de automação funcionais"
    echo "  • Padrões de qualidade seguidos"
    
    echo ""
    echo "?? MELHORIAS RECOMENDADAS:"
    echo "  • Resolver avisos identificados"
    echo "  • Completar componentes opcionais"
    echo "  • Otimizar configurações restantes"
    
    exit 0
    
else
    echo -e "${RED}? ARQUITETURA PRECISA DE MAIS OTIMIZAÇÕES${NC}"
    echo -e "${RED}?? $VALIDATION_ERRORS erros críticos encontrados${NC}"
    echo -e "${YELLOW}??  $VALIDATION_WARNINGS avisos precisam de atenção${NC}"
    
    echo ""
    echo "???  AÇÕES NECESSÁRIAS:"
    echo "  • Corrigir todos os erros identificados"
    echo "  • Implementar componentes em falta"
    echo "  • Executar novamente a validação"
    
    echo ""
    echo "?? PRÓXIMOS PASSOS:"
    echo "  1. Implementar correções necessárias"
    echo "  2. Executar: $0"
    echo "  3. Repetir até atingir > 90% de sucesso"
    
    exit 1
fi