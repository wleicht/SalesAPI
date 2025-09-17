#!/bin/bash

# SalesAPI Quick Start Script
# Starts all services in development mode

echo "?? Iniciando SalesAPI - Arquitetura Profissional de Microservi�os"
echo "================================================================="

# Function to check if a command exists
check_command() {
    if ! command -v $1 &> /dev/null; then
        echo "? $1 n�o est� instalado. Por favor, instale $1 para continuar."
        exit 1
    fi
}

# Function to check if a port is available
check_port() {
    if lsof -Pi :$1 -sTCP:LISTEN -t >/dev/null 2>&1; then
        echo "??  Porta $1 est� em uso. Tentando parar processo existente..."
        lsof -ti:$1 | xargs kill -9 2>/dev/null || true
        sleep 2
    fi
}

# Check prerequisites
echo "?? Verificando pr�-requisitos..."
check_command "dotnet"
check_command "docker"

# Check .NET version
DOTNET_VERSION=$(dotnet --version | cut -d '.' -f 1)
if [ "$DOTNET_VERSION" -lt 8 ]; then
    echo "? .NET 8.0+ � necess�rio. Vers�o atual: $(dotnet --version)"
    exit 1
fi

echo "? Pr�-requisitos verificados"

# Start infrastructure services
echo ""
echo "?? Iniciando servi�os de infraestrutura..."
docker-compose up -d sqlserver rabbitmq

# Wait for services to be ready
echo "? Aguardando servi�os de infraestrutura..."
sleep 15

# Check infrastructure health
echo "?? Verificando sa�de dos servi�os de infraestrutura..."

# Check SQL Server
echo "  Verificando SQL Server..."
for i in {1..30}; do
    if docker exec salesapi-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q "SELECT 1" >/dev/null 2>&1; then
        echo "  ? SQL Server est� rodando"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "  ? SQL Server n�o respondeu ap�s 30 tentativas"
        exit 1
    fi
    sleep 2
done

# Check RabbitMQ
echo "  Verificando RabbitMQ..."
for i in {1..30}; do
    if curl -f -u admin:admin123 http://localhost:15672/api/overview >/dev/null 2>&1; then
        echo "  ? RabbitMQ est� rodando"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "  ? RabbitMQ n�o respondeu ap�s 30 tentativas"
        exit 1
    fi
    sleep 2
done

# Check and free ports
echo ""
echo "?? Verificando disponibilidade de portas..."
check_port 6000  # Gateway
check_port 5001  # Sales API
check_port 5000  # Inventory API

# Build the solution
echo ""
echo "?? Compilando a solu��o..."
dotnet build SalesAPI.sln --configuration Debug --verbosity quiet
if [ $? -ne 0 ]; then
    echo "? Falha na compila��o"
    exit 1
fi
echo "? Compila��o bem-sucedida"

# Start microservices
echo ""
echo "?? Iniciando microservi�os..."

# Start services in background
echo "  Iniciando Gateway (porta 6000)..."
cd src/gateway && dotnet run --configuration Debug >/dev/null 2>&1 &
GATEWAY_PID=$!
cd ../..

sleep 3

echo "  Iniciando Sales API (porta 5001)..."
cd src/sales.api && dotnet run --configuration Debug >/dev/null 2>&1 &
SALES_PID=$!
cd ../..

sleep 3

echo "  Iniciando Inventory API (porta 5000)..."
cd src/inventory.api && dotnet run --configuration Debug >/dev/null 2>&1 &
INVENTORY_PID=$!
cd ../..

# Wait for services to start
echo ""
echo "? Aguardando servi�os iniciarem..."
sleep 10

# Health check function
health_check() {
    local service=$1
    local url=$2
    local max_attempts=30
    
    for i in $(seq 1 $max_attempts); do
        if curl -f -s "$url" >/dev/null 2>&1; then
            echo "  ? $service est� saud�vel"
            return 0
        fi
        sleep 2
    done
    echo "  ? $service n�o respondeu health check"
    return 1
}

# Perform health checks
echo "?? Executando health checks..."
health_check "Gateway" "http://localhost:6000/health"
health_check "Sales API" "http://localhost:5001/health"
health_check "Inventory API" "http://localhost:5000/health"

# Test authentication
echo ""
echo "?? Testando autentica��o..."
AUTH_RESPONSE=$(curl -s -X POST http://localhost:6000/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"admin","password":"admin123"}')

if echo "$AUTH_RESPONSE" | grep -q "token"; then
    echo "? Autentica��o funcionando"
    
    # Extract token for further tests
    TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    
    # Test authenticated endpoint
    echo "?? Testando endpoint autenticado..."
    if curl -s -H "Authorization: Bearer $TOKEN" http://localhost:6000/inventory/products >/dev/null 2>&1; then
        echo "? Autoriza��o funcionando"
    else
        echo "??  Autoriza��o pode ter problemas"
    fi
else
    echo "??  Problema na autentica��o"
fi

# Final status
echo ""
echo "================================="
echo "?? SalesAPI INICIADO COM SUCESSO!"
echo "================================="
echo ""
echo "?? Status dos Servi�os:"
echo "  ?? Gateway:      http://localhost:6000 (PID: $GATEWAY_PID)"
echo "  ?? Sales API:    http://localhost:5001 (PID: $SALES_PID)"
echo "  ?? Inventory:    http://localhost:5000 (PID: $INVENTORY_PID)"
echo ""
echo "?? Documenta��o:"
echo "  ?? Gateway Swagger:    http://localhost:6000/swagger"
echo "  ?? Sales Swagger:      http://localhost:5001/swagger"
echo "  ?? Inventory Swagger:  http://localhost:5000/swagger"
echo ""
echo "?? Credenciais de Teste:"
echo "  ?? Admin:      admin / admin123"
echo "  ?? Customer:   customer1 / password123"
echo ""
echo "???  Comandos �teis:"
echo "  Health Check:  curl http://localhost:6000/health"
echo "  Login Admin:   curl -X POST http://localhost:6000/auth/login -H \"Content-Type: application/json\" -d '{\"username\":\"admin\",\"password\":\"admin123\"}'"
echo "  Testes:        ./scripts/manual-tests/demo_tests.sh"
echo "  Parar Todos:   kill $GATEWAY_PID $SALES_PID $INVENTORY_PID"
echo ""
echo "?? Sistema pronto para desenvolvimento e testes!"

# Save PIDs for easy cleanup
echo "$GATEWAY_PID $SALES_PID $INVENTORY_PID" > .services.pid