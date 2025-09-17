#!/bin/bash

set -e

echo "?? Iniciando SalesAPI com Docker..."
echo "====================================="

# Navegar para o diret�rio correto
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$PROJECT_ROOT"

echo "?? Diret�rio do projeto: $PROJECT_ROOT"

# Executar verifica��es de pr�-requisitos
if [ -f "./scripts/docker-startup-check.sh" ]; then
    echo "?? Executando verifica��es de pr�-requisitos..."
    bash ./scripts/docker-startup-check.sh
else
    echo "?? Script de verifica��o n�o encontrado, continuando..."
fi

echo ""
echo "?? Parando containers existentes..."
docker-compose -f docker/compose/docker-compose.yml down --remove-orphans --volumes || true

# Limpar builds anteriores se solicitado
if [ "$1" = "--clean" ] || [ "$1" = "-c" ]; then
    echo "?? Limpando builds anteriores..."
    docker system prune -f
    docker-compose -f docker/compose/docker-compose.yml build --no-cache
else
    echo "?? Fazendo build dos servi�os..."
    docker-compose -f docker/compose/docker-compose.yml build
fi

# Iniciar servi�os
echo ""
echo "?? Iniciando servi�os..."
docker-compose -f docker/compose/docker-compose.yml up -d

# Aguardar inicializa��o
echo ""
echo "? Aguardando inicializa��o dos servi�os (60 segundos)..."
for i in {1..12}; do
    echo -n "."
    sleep 5
done
echo ""

# Verificar status dos containers
echo ""
echo "?? Verificando status dos containers..."
docker-compose -f docker/compose/docker-compose.yml ps

# Aguardar mais um pouco antes dos testes
echo ""
echo "? Aguardando servi�os ficarem prontos (30 segundos adicionais)..."
sleep 30

# Testar conectividade b�sica
echo ""
echo "?? Testando conectividade dos servi�os..."

# Teste do Gateway
echo -n "Gateway (porta 6000): "
if curl -s -f http://localhost:6000/gateway/status >/dev/null 2>&1; then
    echo "? OK"
else
    echo "? Falhou"
    echo "   Verificando logs do Gateway:"
    docker logs salesapi-gateway --tail 10
fi

# Teste do Inventory
echo -n "Inventory API (porta 5000): "
if curl -s -f http://localhost:5000/health >/dev/null 2>&1; then
    echo "? OK"
else
    echo "? Falhou"
    echo "   Verificando logs do Inventory:"
    docker logs salesapi-inventory --tail 10
fi

# Teste do Sales
echo -n "Sales API (porta 5001): "
if curl -s -f http://localhost:5001/health >/dev/null 2>&1; then
    echo "? OK"
else
    echo "? Falhou"
    echo "   Verificando logs do Sales:"
    docker logs salesapi-sales --tail 10
fi

echo ""
echo "?? Status final dos servi�os:"
docker-compose -f docker/compose/docker-compose.yml ps --format "table {{.Name}}\t{{.State}}\t{{.Status}}"

# Executar testes b�sicos se dispon�vel
if [ -f "./scripts/manual-tests/demo_tests.sh" ]; then
    echo ""
    echo "?? Executando testes b�sicos de funcionalidade..."
    if bash ./scripts/manual-tests/demo_tests.sh; then
        echo "? Testes b�sicos passaram!"
    else
        echo "?? Alguns testes b�sicos falharam. Verifique os logs acima."
    fi
fi

echo ""
echo "?? Processo de inicializa��o conclu�do!"
echo ""
echo "?? Pr�ximos passos:"
echo "   1. Verificar logs: docker-compose -f docker/compose/docker-compose.yml logs"
echo "   2. Acessar APIs:"
echo "      - Gateway: http://localhost:6000"
echo "      - Inventory API: http://localhost:5000"
echo "      - Sales API: http://localhost:5001"
echo "   3. Executar testes completos: ./scripts/manual-tests/run_manual_tests.sh"
echo "   4. Monitorar: ./scripts/monitor-services.sh"