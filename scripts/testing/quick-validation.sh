#!/bin/bash

# SalesAPI Quick Validation Script
# Validates that all services are running and functional

echo "?? SalesAPI - Valida��o R�pida do Sistema"
echo "========================================"

# Initialize counters
PASSED_TESTS=0
FAILED_TESTS=0
TOTAL_TESTS=8

# Function to test a service
test_service() {
    local test_name="$1"
    local url="$2"
    local expected_status="${3:-200}"
    
    echo "?? Testando: $test_name"
    
    # Use curl with timeout
    if response=$(curl -s -o /dev/null -w "%{http_code}" -m 10 "$url" 2>/dev/null); then
        if [ "$response" = "$expected_status" ]; then
            echo "   ? PASSOU - Status: $response"
            ((PASSED_TESTS++))
            return 0
        else
            echo "   ? FALHOU - Status: $response (esperado: $expected_status)"
            ((FAILED_TESTS++))
            return 1
        fi
    else
        echo "   ? FALHOU - Servi�o n�o respondeu"
        ((FAILED_TESTS++))
        return 1
    fi
}

# Function to test authenticated endpoint
test_authenticated() {
    local test_name="$1"
    local url="$2"
    local token="$3"
    
    echo "?? Testando: $test_name"
    
    if response=$(curl -s -o /dev/null -w "%{http_code}" -m 10 -H "Authorization: Bearer $token" "$url" 2>/dev/null); then
        if [ "$response" = "200" ]; then
            echo "   ? PASSOU - Status: $response"
            ((PASSED_TESTS++))
            return 0
        else
            echo "   ? FALHOU - Status: $response"
            ((FAILED_TESTS++))
            return 1
        fi
    else
        echo "   ? FALHOU - Servi�o n�o respondeu"
        ((FAILED_TESTS++))
        return 1
    fi
}

echo ""
echo "?? 1. Testando Health Checks..."

# Test health endpoints
test_service "Gateway Health" "http://localhost:6000/health"
test_service "Sales API Health" "http://localhost:5001/health"
test_service "Inventory API Health" "http://localhost:5000/health"

echo ""
echo "?? 2. Testando Autentica��o..."

# Test authentication
echo "?? Testando: Login de Admin"
AUTH_RESPONSE=$(curl -s -X POST http://localhost:6000/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"admin","password":"admin123"}' 2>/dev/null)

if echo "$AUTH_RESPONSE" | grep -q '"token":'; then
    echo "   ? PASSOU - Token recebido"
    ((PASSED_TESTS++))
    
    # Extract token
    TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    
    echo ""
    echo "?? 3. Testando Autoriza��o..."
    
    # Test authorized endpoints
    test_authenticated "Produtos (P�blico)" "http://localhost:6000/inventory/products" "$TOKEN"
    test_authenticated "Pedidos (Autenticado)" "http://localhost:6000/sales/orders" "$TOKEN"
    
else
    echo "   ? FALHOU - N�o foi poss�vel obter token"
    ((FAILED_TESTS++))
    
    # Skip authorization tests
    echo ""
    echo "?? 3. Testando Autoriza��o... PULADO (sem token)"
    FAILED_TESTS=$((FAILED_TESTS + 2))
fi

echo ""
echo "?? 4. Testando Conectividade Entre Servi�os..."

# Test direct service endpoints (internal communication)
echo "?? Testando: Sales API direto"
if response=$(curl -s -o /dev/null -w "%{http_code}" -m 10 "http://localhost:5001/orders" 2>/dev/null); then
    if [ "$response" = "200" ] || [ "$response" = "401" ]; then
        echo "   ? PASSOU - Sales API respondendo (Status: $response)"
        ((PASSED_TESTS++))
    else
        echo "   ? FALHOU - Status inesperado: $response"
        ((FAILED_TESTS++))
    fi
else
    echo "   ? FALHOU - Sales API n�o respondeu"
    ((FAILED_TESTS++))
fi

echo "?? Testando: Inventory API direto"
if response=$(curl -s -o /dev/null -w "%{http_code}" -m 10 "http://localhost:5000/products" 2>/dev/null); then
    if [ "$response" = "200" ] || [ "$response" = "401" ]; then
        echo "   ? PASSOU - Inventory API respondendo (Status: $response)"
        ((PASSED_TESTS++))
    else
        echo "   ? FALHOU - Status inesperado: $response"
        ((FAILED_TESTS++))
    fi
else
    echo "   ? FALHOU - Inventory API n�o respondeu"
    ((FAILED_TESTS++))
fi

# Calculate success rate
SUCCESS_RATE=$(( PASSED_TESTS * 100 / TOTAL_TESTS ))

# Final results
echo ""
echo "=========================================="
echo "?? RESULTADOS DA VALIDA��O"
echo "=========================================="
echo "Total de Testes: $TOTAL_TESTS"
echo "Testes Passaram: $PASSED_TESTS"
echo "Testes Falharam: $FAILED_TESTS"
echo "Taxa de Sucesso: ${SUCCESS_RATE}%"
echo ""

if [ $FAILED_TESTS -eq 0 ]; then
    echo "?? SISTEMA TOTALMENTE FUNCIONAL!"
    echo "? Todos os testes passaram"
    echo "?? Sistema pronto para uso"
    exit 0
elif [ $SUCCESS_RATE -ge 75 ]; then
    echo "? SISTEMA MAJORITARIAMENTE FUNCIONAL"
    echo "??  Alguns componentes podem precisar de aten��o"
    echo "?? Verifique os logs dos servi�os que falharam"
    exit 0
else
    echo "? SISTEMA COM PROBLEMAS SIGNIFICATIVOS"
    echo "?? Muitos testes falharam - verifica��o necess�ria"
    echo "???  A��es recomendadas:"
    echo "   � Verificar se todos os servi�os est�o rodando"
    echo "   � Verificar logs dos servi�os"
    echo "   � Executar: ./scripts/development/start-all.sh"
    exit 1
fi