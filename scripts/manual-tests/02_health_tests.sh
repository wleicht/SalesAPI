#!/bin/bash

# SalesAPI Manual Tests - Health Check Tests
# Tests all health check endpoints

# Load utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

# Configuration
GATEWAY_URL=$(get_config_value '.gateway.baseUrl' || echo "http://localhost:6000")
INVENTORY_URL=$(get_config_value '.inventory.baseUrl' || echo "http://localhost:5000")
SALES_URL=$(get_config_value '.sales.baseUrl' || echo "http://localhost:5001")

run_health_tests() {
    log_info "?? Starting Health Check Tests..."
    
    # Test 1: Gateway status
    start_test "Gateway status check"
    response=$(make_request "GET" "$GATEWAY_URL/gateway/status" "" "" "200")
    http_code="${response%%|*}"
    body="${response##*|}"
    
    if [ "$http_code" = "200" ]; then
        if echo "$body" | grep -q "Healthy"; then
            pass_test "Gateway is healthy"
        else
            pass_test "Gateway responded but may not be fully healthy"
        fi
    else
        fail_test "Gateway status check failed (HTTP: $http_code)"
    fi
    
    # Test 2: Gateway routes info
    start_test "Gateway routes information"
    response=$(make_request "GET" "$GATEWAY_URL/gateway/routes" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ]; then
        pass_test "Gateway routes information retrieved"
    else
        fail_test "Failed to get gateway routes (HTTP: $http_code)"
    fi
    
    # Test 3: Inventory API health (direct)
    start_test "Inventory API health check (direct)"
    response=$(make_request "GET" "$INVENTORY_URL/health" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ]; then
        pass_test "Inventory API is healthy (direct)"
    else
        fail_test "Inventory API health check failed (HTTP: $http_code)"
    fi
    
    # Test 4: Inventory API health (via gateway)
    start_test "Inventory API health check (via gateway)"
    response=$(make_request "GET" "$GATEWAY_URL/inventory/health" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ]; then
        pass_test "Inventory API is healthy (via gateway)"
    else
        fail_test "Inventory API health check via gateway failed (HTTP: $http_code)"
    fi
    
    # Test 5: Sales API health (direct)
    start_test "Sales API health check (direct)"
    response=$(make_request "GET" "$SALES_URL/health" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ]; then
        pass_test "Sales API is healthy (direct)"
    else
        fail_test "Sales API health check failed (HTTP: $http_code)"
    fi
    
    # Test 6: Sales API health (via gateway)
    start_test "Sales API health check (via gateway)"
    response=$(make_request "GET" "$GATEWAY_URL/sales/health" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ]; then
        pass_test "Sales API is healthy (via gateway)"
    else
        fail_test "Sales API health check via gateway failed (HTTP: $http_code)"
    fi
    
    # Test 7: Response time check
    start_test "Response time validation"
    response=$(make_request "GET" "$GATEWAY_URL/gateway/status" "" "" "200")
    time_total="${response#*|}"
    time_total="${time_total%%|*}"
    
    # Convert to milliseconds for easier comparison (time_total is in seconds)
    time_ms=$(echo "$time_total * 1000" | bc 2>/dev/null || echo "0")
    time_ms_int=${time_ms%.*}
    
    if [ "$time_ms_int" -lt 5000 ]; then
        pass_test "Response time acceptable (${time_ms_int}ms)"
    else
        fail_test "Response time too high (${time_ms_int}ms)"
    fi
    
    log_info "?? Health Check Tests Completed"
    echo
}

# Run tests if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    check_prerequisites || exit 1
    run_health_tests
    generate_summary
fi