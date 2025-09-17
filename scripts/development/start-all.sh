#!/bin/bash

# SalesAPI Quick Start Script
# Starts all services in development mode

echo "?? Iniciando SalesAPI - Arquitetura Profissional de Microserviços"
echo "================================================================="

# Function to check if a command exists
check_command() {
    if ! command -v $1 &> /dev/null; then
        echo "? $1 não está instalado. Por favor, instale $1 para continuar."
        exit 1
    fi
}

# Function to check if a port is available
check_port() {
    if lsof -Pi :$1 -sTCP:LISTEN -t >/dev/null 2>&1; then
        echo "??  Porta $1 está em uso. Tentando parar processo existente..."
        lsof -ti:$1 | xargs kill -9 2>/dev/null || true
        sleep 2
    fi
}

# Check prerequisites
echo "?? Verificando pré-requisitos..."
check_command "dotnet"
check_command "docker"

# Check .NET version
DOTNET_VERSION=$(dotnet --version | cut -d '.' -f 1)
if [ "$DOTNET_VERSION" -lt 8 ]; then
    echo "? .NET 8.0+ é necessário. Versão atual: $(dotnet --version)"
    exit 1
fi

echo "? Pré-requisitos verificados"

# Start infrastructure services
echo ""
echo "?? Iniciando serviços de infraestrutura..."
docker-compose up -d sqlserver rabbitmq

# Wait for services to be ready
echo "? Aguardando serviços de infraestrutura..."
sleep 15

# Check infrastructure health
echo "?? Verificando saúde dos serviços de infraestrutura..."

# Check SQL Server
echo "  Verificando SQL Server..."
for i in {1..30}; do
    if docker exec salesapi-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Your_password123' -Q "SELECT 1" >/dev/null 2>&1; then
        echo "  ? SQL Server está rodando"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "  ? SQL Server não respondeu após 30 tentativas"
        exit 1
    fi
    sleep 2
done

# Check RabbitMQ
echo "  Verificando RabbitMQ..."
for i in {1..30}; do
    if curl -f -u admin:admin123 http://localhost:15672/api/overview >/dev/null 2>&1; then
        echo "  ? RabbitMQ está rodando"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "  ? RabbitMQ não respondeu após 30 tentativas"
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
echo "?? Compilando a solução..."
dotnet build SalesAPI.sln --configuration Debug --verbosity quiet
if [ $? -ne 0 ]; then
    echo "? Falha na compilação"
    exit 1
fi
echo "? Compilação bem-sucedida"

# Start microservices
echo ""
echo "?? Iniciando microserviços..."

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
echo "? Aguardando serviços iniciarem..."
sleep 10

# Health check function
health_check() {
    local service=$1
    local url=$2
    local max_attempts=30
    
    for i in $(seq 1 $max_attempts); do
        if curl -f -s "$url" >/dev/null 2>&1; then
            echo "  ? $service está saudável"
            return 0
        fi
        sleep 2
    done
    echo "  ? $service não respondeu health check"
    return 1
}

# Perform health checks
echo "?? Executando health checks..."
health_check "Gateway" "http://localhost:6000/health"
health_check "Sales API" "http://localhost:5001/health"
health_check "Inventory API" "http://localhost:5000/health"

# Test authentication
echo ""
echo "?? Testando autenticação..."
AUTH_RESPONSE=$(curl -s -X POST http://localhost:6000/auth/login \
    -H "Content-Type: application/json" \
    -d '{"username":"admin","password":"admin123"}')

if echo "$AUTH_RESPONSE" | grep -q "token"; then
    echo "? Autenticação funcionando"
    
    # Extract token for further tests
    TOKEN=$(echo "$AUTH_RESPONSE" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    
    # Test authenticated endpoint
    echo "?? Testando endpoint autenticado..."
    if curl -s -H "Authorization: Bearer $TOKEN" http://localhost:6000/inventory/products >/dev/null 2>&1; then
        echo "? Autorização funcionando"
    else
        echo "??  Autorização pode ter problemas"
    fi
else
    echo "??  Problema na autenticação"
fi

# Final status
echo ""
echo "================================="
echo "?? SalesAPI INICIADO COM SUCESSO!"
echo "================================="
echo ""
echo "?? Status dos Serviços:"
echo "  ?? Gateway:      http://localhost:6000 (PID: $GATEWAY_PID)"
echo "  ?? Sales API:    http://localhost:5001 (PID: $SALES_PID)"
echo "  ?? Inventory:    http://localhost:5000 (PID: $INVENTORY_PID)"
echo ""
echo "?? Documentação:"
echo "  ?? Gateway Swagger:    http://localhost:6000/swagger"
echo "  ?? Sales Swagger:      http://localhost:5001/swagger"
echo "  ?? Inventory Swagger:  http://localhost:5000/swagger"
echo ""
echo "?? Credenciais de Teste:"
echo "  ?? Admin:      admin / admin123"
echo "  ?? Customer:   customer1 / password123"
echo ""
echo "???  Comandos Úteis:"
echo "  Health Check:  curl http://localhost:6000/health"
echo "  Login Admin:   curl -X POST http://localhost:6000/auth/login -H \"Content-Type: application/json\" -d '{\"username\":\"admin\",\"password\":\"admin123\"}'"
echo "  Testes:        ./scripts/manual-tests/demo_tests.sh"
echo "  Parar Todos:   kill $GATEWAY_PID $SALES_PID $INVENTORY_PID"
echo ""
echo "?? Sistema pronto para desenvolvimento e testes!"

# Save PIDs for easy cleanup
echo "$GATEWAY_PID $SALES_PID $INVENTORY_PID" > .services.pid