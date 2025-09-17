#!/bin/bash

echo "?? Monitorando servi�os SalesAPI..."
echo "Pressione Ctrl+C para parar o monitoramento"
echo "=========================================="

# Fun��o para verificar se jq est� dispon�vel
check_jq() {
    if command -v jq >/dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Fun��o para formatar JSON sem jq
format_json_simple() {
    sed 's/,/,\n  /g' | sed 's/{/{\n  /' | sed 's/}/\n}/'
}

# Contador de itera��es
ITERATION=0

while true; do
    ITERATION=$((ITERATION + 1))
    clear
    
    echo "?? Monitoramento SalesAPI - Itera��o #$ITERATION"
    echo "==================== $(date) ===================="
    echo ""
    
    # Status dos containers
    echo "?? Status dos Containers:"
    docker-compose -f docker/compose/docker-compose.yml ps --format "table {{.Name}}\t{{.State}}\t{{.Status}}" 2>/dev/null || {
        echo "? Erro ao obter status dos containers"
        echo "   Verifique se o docker-compose.yml est� no local correto"
    }
    echo ""
    
    # Health checks
    echo "?? Health Checks:"
    
    # Gateway
    echo -n "Gateway (6000): "
    GATEWAY_RESPONSE=$(curl -s -m 5 http://localhost:6000/gateway/status 2>/dev/null)
    if [ $? -eq 0 ]; then
        echo "? OK"
        if check_jq; then
            echo "$GATEWAY_RESPONSE" | jq '.' 2>/dev/null | head -5
        else
            echo "$GATEWAY_RESPONSE" | format_json_simple | head -5
        fi
    else
        echo "? Indispon�vel"
    fi
    echo ""
    
    # Inventory
    echo -n "Inventory (5000): "
    INVENTORY_RESPONSE=$(curl -s -m 5 http://localhost:5000/health 2>/dev/null)
    if [ $? -eq 0 ]; then
        echo "? OK"
        if check_jq; then
            echo "$INVENTORY_RESPONSE" | jq '.' 2>/dev/null | head -3
        else
            echo "$INVENTORY_RESPONSE" | format_json_simple | head -3
        fi
    else
        echo "? Indispon�vel"
    fi
    echo ""
    
    # Sales
    echo -n "Sales (5001): "
    SALES_RESPONSE=$(curl -s -m 5 http://localhost:5001/health 2>/dev/null)
    if [ $? -eq 0 ]; then
        echo "? OK"
        if check_jq; then
            echo "$SALES_RESPONSE" | jq '.' 2>/dev/null | head -3
        else
            echo "$SALES_RESPONSE" | format_json_simple | head -3
        fi
    else
        echo "? Indispon�vel"
    fi
    echo ""
    
    # Recursos do sistema
    echo "?? Recursos do Sistema:"
    echo "Docker: $(docker system df --format "table {{.Type}}\t{{.TotalCount}}\t{{.Size}}" 2>/dev/null | tail -n +2 | head -1)"
    
    # Mem�ria dos containers
    echo ""
    echo "?? Uso de Mem�ria dos Containers:"
    docker stats --no-stream --format "table {{.Name}}\t{{.MemUsage}}\t{{.CPUPerc}}" $(docker-compose -f docker/compose/docker-compose.yml ps -q 2>/dev/null) 2>/dev/null || {
        echo "? N�o foi poss�vel obter estat�sticas dos containers"
    }
    
    echo ""
    echo "? Pr�xima atualiza��o em 30 segundos... (Ctrl+C para parar)"
    echo "?? Comandos �teis:"
    echo "   - Ver logs: docker-compose -f docker/compose/docker-compose.yml logs [servi�o]"
    echo "   - Reiniciar: docker-compose -f docker/compose/docker-compose.yml restart [servi�o]"
    echo "   - Parar: docker-compose -f docker/compose/docker-compose.yml down"
    
    sleep 30
done