#!/bin/bash

set -e

echo "?? Verificando pré-requisitos para Docker..."

# Verificar se Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo "? Docker não está rodando. Inicie o Docker Desktop."
    exit 1
fi

echo "? Docker está rodando"

# Verificar se netcat está disponível para testes de conectividade
if ! command -v nc > /dev/null 2>&1; then
    echo "?? netcat não encontrado. Algumas verificações serão puladas."
    NC_AVAILABLE=false
else
    NC_AVAILABLE=true
fi

# Verificar se SQL Server está disponível
echo "?? Verificando SQL Server..."
if [ "$NC_AVAILABLE" = true ] && ! nc -z localhost 1433 2>/dev/null; then
    echo "?? SQL Server não encontrado na porta 1433. Iniciando container..."
    
    # Parar container existente se houver
    docker stop sqlserver 2>/dev/null || true
    docker rm sqlserver 2>/dev/null || true
    
    docker run -e "ACCEPT_EULA=Y" \
               -e "SA_PASSWORD=Your_password123" \
               -e "MSSQL_PID=Developer" \
               -p 1433:1433 \
               --name sqlserver \
               --restart unless-stopped \
               -d mcr.microsoft.com/mssql/server:2022-latest
    
    echo "? Aguardando SQL Server inicializar (30 segundos)..."
    sleep 30
    
    # Verificar se iniciou corretamente
    if docker ps | grep -q "sqlserver.*Up"; then
        echo "? SQL Server iniciado com sucesso"
    else
        echo "? Falha ao iniciar SQL Server. Verifique os logs:"
        docker logs sqlserver
        exit 1
    fi
else
    echo "? SQL Server já está disponível"
fi

# Verificar se RabbitMQ está disponível
echo "?? Verificando RabbitMQ..."
if [ "$NC_AVAILABLE" = true ] && ! nc -z localhost 5672 2>/dev/null; then
    echo "?? RabbitMQ não encontrado na porta 5672. Iniciando container..."
    
    # Parar container existente se houver
    docker stop rabbitmq 2>/dev/null || true
    docker rm rabbitmq 2>/dev/null || true
    
    docker run -d \
               --name rabbitmq \
               --restart unless-stopped \
               -p 5672:5672 \
               -p 15672:15672 \
               -e RABBITMQ_DEFAULT_USER=admin \
               -e RABBITMQ_DEFAULT_PASS=admin123 \
               rabbitmq:3-management
    
    echo "? Aguardando RabbitMQ inicializar (30 segundos)..."
    sleep 30
    
    # Verificar se iniciou corretamente
    if docker ps | grep -q "rabbitmq.*Up"; then
        echo "? RabbitMQ iniciado com sucesso"
        echo "?? Interface de gerenciamento disponível em: http://localhost:15672 (admin/admin123)"
    else
        echo "? Falha ao iniciar RabbitMQ. Verifique os logs:"
        docker logs rabbitmq
        exit 1
    fi
else
    echo "? RabbitMQ já está disponível"
fi

# Verificar espaço em disco
echo "?? Verificando espaço em disco..."
AVAILABLE_SPACE=$(df / | awk 'NR==2 {print $4}')
REQUIRED_SPACE=1048576  # 1GB em KB

if [ "$AVAILABLE_SPACE" -lt "$REQUIRED_SPACE" ]; then
    echo "?? Pouco espaço em disco disponível. Limpando containers e imagens não utilizadas..."
    docker system prune -f
else
    echo "? Espaço em disco suficiente"
fi

# Verificar se as portas necessárias estão livres
echo "?? Verificando disponibilidade das portas..."
PORTS_TO_CHECK="5000 5001 6000"

for port in $PORTS_TO_CHECK; do
    if [ "$NC_AVAILABLE" = true ] && nc -z localhost $port 2>/dev/null; then
        echo "?? Porta $port já está em uso. Isso pode causar conflitos."
        if [ "$port" = "5000" ]; then
            echo "   Porta 5000 (Inventory API) em uso"
        elif [ "$port" = "5001" ]; then
            echo "   Porta 5001 (Sales API) em uso"
        elif [ "$port" = "6000" ]; then
            echo "   Porta 6000 (Gateway) em uso"
        fi
    else
        echo "? Porta $port disponível"
    fi
done

echo ""
echo "? Verificação de pré-requisitos concluída!"
echo "?? Resumo:"
echo "   - Docker: Rodando"
echo "   - SQL Server: Configurado na porta 1433"
echo "   - RabbitMQ: Configurado na porta 5672"
echo "   - Portas das APIs: Verificadas"
echo ""
echo "?? Sistema pronto para inicializar os serviços SalesAPI"