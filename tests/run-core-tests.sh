#!/bin/bash

echo "?? Executando Suite de Testes Profissionais CORE (Sem Dependências Externas)"
echo "========================================================================="
echo ""

# Função para exibir separador
separator() {
    echo "========================================================================="
}

# Função para verificar resultado
check_result() {
    if [ $? -eq 0 ]; then
        echo "? $1 - SUCESSO"
    else
        echo "? $1 - FALHA"
        exit 1
    fi
}

echo "?? Suite Core Consolidada (Independente de Serviços):"
echo "- Domain Tests (33 testes)      - Lógica de negócio pura"
echo "- Infrastructure Tests (17 testes) - Persistência e messaging"  
echo "- Integration Tests (4 testes)   - Fluxos entre componentes"
echo "- Contract Tests (9 testes)      - Compatibilidade de APIs"
echo ""
echo "?? Nota: Testes de Endpoint (E2E) requerem serviços rodando (docker-compose up)"
echo ""
separator

# 1. Domain Tests (mais rápidos - executar primeiro)
echo "?? Executando Domain Tests (Unit Tests)..."
dotnet test "tests/SalesAPI.Tests.Professional/Domain.Tests/" --verbosity minimal --no-build
check_result "Domain Tests"
echo ""

# 2. Infrastructure Tests
echo "??? Executando Infrastructure Tests..."
dotnet test "tests/SalesAPI.Tests.Professional/Infrastructure.Tests/" --verbosity minimal --no-build
check_result "Infrastructure Tests"
echo ""

# 3. Integration Tests (Professional)
echo "?? Executando Integration Tests (Professional)..."
dotnet test "tests/SalesAPI.Tests.Professional/Integration.Tests/" --verbosity minimal --no-build
check_result "Integration Tests"
echo ""

# 4. Contract Tests
echo "?? Executando Contract Tests..."
dotnet test "tests/contracts.tests/" --verbosity minimal --no-build
check_result "Contract Tests"
echo ""

separator
echo "?? Executando Suite Core Consolidada (Somente testes independentes)..."

# Executar apenas os testes core (sem endpoint que requerem serviços)
dotnet test "tests/SalesAPI.Tests.Professional/Domain.Tests/" "tests/SalesAPI.Tests.Professional/Infrastructure.Tests/" "tests/SalesAPI.Tests.Professional/Integration.Tests/" "tests/contracts.tests/" --verbosity minimal --no-build

if [ $? -eq 0 ]; then
    echo ""
    separator
    echo "?? SUITE DE TESTES PROFISSIONAIS CORE - SUCESSO TOTAL!"
    echo ""
    echo "?? Resumo da Execução (Testes Independentes):"
    echo "? Domain Tests: 33 testes (lógica de negócio)"
    echo "? Infrastructure Tests: 17 testes (persistência/messaging)"  
    echo "? Integration Tests: 4 testes (fluxos profissionais)"
    echo "? Contract Tests: 9 testes (compatibilidade APIs)"
    echo ""
    echo "?? Total Core: 63 testes de alta qualidade"
    echo "? Arquitetura profissional consolidada"
    echo "?? Zero duplicação, máxima eficiência"
    echo ""
    echo "?? Para executar testes E2E: inicie os serviços com docker-compose up"
    echo ""
    separator
else
    echo ""
    echo "? FALHA NA SUITE DE TESTES CORE"
    echo "   Verifique os logs acima para identificar problemas"
    exit 1
fi