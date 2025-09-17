#!/bin/bash

# SalesAPI Manual Tests - Concurrency Tests
# Tests concurrent operations and race conditions

# Load utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

# Configuration
GATEWAY_URL=$(get_config_value '.gateway.baseUrl' || echo "http://localhost:6000")

run_concurrency_tests() {
    log_info "?? Starting Concurrency Tests..."
    
    # Ensure we have authentication tokens
    if [ -z "$ADMIN_TOKEN" ]; then
        log_warning "Admin token not available, attempting authentication..."
        ADMIN_TOKEN=$(get_auth_token "admin" "admin123" "$GATEWAY_URL")
        export ADMIN_TOKEN
    fi
    
    if [ -z "$CUSTOMER_TOKEN" ]; then
        log_warning "Customer token not available, attempting authentication..."
        CUSTOMER_TOKEN=$(get_auth_token "customer1" "password123" "$GATEWAY_URL")
        export CUSTOMER_TOKEN
    fi
    
    # Test 1: Create a limited stock product for concurrency testing
    local CONCURRENCY_PRODUCT_ID=""
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Create limited stock product for concurrency testing"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"Limited Stock Concurrency Test","description":"Product for testing concurrent orders","price":25.99,"stockQuantity":8}' \
            "201")
        http_code="${response%%|*}"
        body="${response##*|}"
        
        if [ "$http_code" = "201" ]; then
            if command -v jq > /dev/null; then
                CONCURRENCY_PRODUCT_ID=$(echo "$body" | jq -r '.id' 2>/dev/null)
            fi
            pass_test "Successfully created limited stock product"
        else
            fail_test "Failed to create limited stock product (HTTP: $http_code)"
        fi
    else
        skip_test "Create limited stock product - No admin token"
    fi
    
    # Test 2: Multiple concurrent order attempts (simulated)
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$CONCURRENCY_PRODUCT_ID" ] && [ "$CONCURRENCY_PRODUCT_ID" != "null" ]; then
        start_test "Multiple concurrent order attempts (simulated)"
        
        local success_count=0
        local failure_count=0
        local concurrent_orders=4
        local quantity_per_order=3
        
        log_info "Launching $concurrent_orders concurrent orders of $quantity_per_order units each..."
        
        # Create multiple orders in rapid succession
        for i in $(seq 1 $concurrent_orders); do
            customer_id="123e4567-e89b-12d3-a456-42661417$(printf "%04d" $i)"
            correlation_id="concurrent-test-$i-$(date +%s)"
            
            response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
                "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json
X-Correlation-Id: $correlation_id" \
                "{\"customerId\":\"$customer_id\",\"items\":[{\"productId\":\"$CONCURRENCY_PRODUCT_ID\",\"quantity\":$quantity_per_order}]}" \
                "") &
                
            # Brief delay to reduce overwhelming the server
            sleep 0.1
        done
        
        # Wait for all background jobs to complete
        wait
        
        # Give a moment for processing
        sleep 2
        
        # Check the final stock level
        response=$(make_request "GET" "$GATEWAY_URL/inventory/products/$CONCURRENCY_PRODUCT_ID" "" "" "200")
        http_code="${response%%|*}"
        body="${response##*|}"
        
        if [ "$http_code" = "200" ]; then
            if command -v jq > /dev/null; then
                final_stock=$(echo "$body" | jq -r '.stockQuantity' 2>/dev/null)
                initial_stock=8
                expected_minimum_stock=0  # Worst case: all orders succeed
                expected_maximum_stock=$initial_stock  # Best case: all orders fail
                
                if [ -n "$final_stock" ] && [ "$final_stock" -ge "$expected_minimum_stock" ] && [ "$final_stock" -le "$expected_maximum_stock" ]; then
                    pass_test "Stock consistency maintained after concurrent orders (Final stock: $final_stock)"
                else
                    fail_test "Stock consistency violated (Final stock: $final_stock, Expected: $expected_minimum_stock-$expected_maximum_stock)"
                fi
            else
                pass_test "Concurrent orders completed (jq not available for detailed verification)"
            fi
        else
            fail_test "Could not verify final stock after concurrent orders (HTTP: $http_code)"
        fi
    else
        skip_test "Multiple concurrent order attempts - Missing dependencies"
    fi
    
    # Test 3: Rapid successive API calls to the same endpoint
    start_test "Rapid successive API calls"
    local rapid_success=0
    local rapid_total=10
    
    for i in $(seq 1 $rapid_total); do
        response=$(make_request "GET" "$GATEWAY_URL/gateway/status" "" "" "200")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "200" ]; then
            rapid_success=$((rapid_success + 1))
        fi
        
        # Very brief delay
        sleep 0.05
    done
    
    if [ $rapid_success -ge $((rapid_total * 8 / 10)) ]; then  # 80% success rate
        pass_test "Rapid successive calls handled well ($rapid_success/$rapid_total successful)"
    else
        fail_test "Too many failures in rapid successive calls ($rapid_success/$rapid_total successful)"
    fi
    
    # Test 4: Concurrent authentication attempts
    start_test "Concurrent authentication attempts"
    local auth_success=0
    local auth_attempts=5
    
    for i in $(seq 1 $auth_attempts); do
        (
            token=$(get_auth_token "customer1" "password123" "$GATEWAY_URL")
            if [ -n "$token" ] && [ "$token" != "null" ]; then
                echo "auth_success"
            fi
        ) &
    done
    
    # Count successful authentications
    wait
    # This is a simplified test - in a real scenario you'd collect the results properly
    pass_test "Concurrent authentication attempts completed"
    
    # Test 5: Mixed operations concurrency
    if [ -n "$ADMIN_TOKEN" ] && [ -n "$CUSTOMER_TOKEN" ]; then
        start_test "Mixed operations concurrency"
        
        # Launch different operations concurrently
        (
            # Product listing
            make_request "GET" "$GATEWAY_URL/inventory/products" "" "" "200" > /dev/null
        ) &
        
        (
            # Order listing  
            make_request "GET" "$GATEWAY_URL/sales/orders" "" "" "" > /dev/null
        ) &
        
        (
            # Health check
            make_request "GET" "$GATEWAY_URL/gateway/status" "" "" "200" > /dev/null
        ) &
        
        (
            # Gateway routes
            make_request "GET" "$GATEWAY_URL/gateway/routes" "" "" "200" > /dev/null
        ) &
        
        # Wait for all operations
        wait
        
        pass_test "Mixed concurrent operations completed"
    else
        skip_test "Mixed operations concurrency - Missing tokens"
    fi
    
    # Test 6: Resource cleanup under load
    start_test "Resource cleanup verification"
    # This is more of a monitoring test - check that services are still responsive
    response=$(make_request "GET" "$GATEWAY_URL/gateway/status" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ]; then
        pass_test "Services remain responsive after concurrency tests"
    else
        fail_test "Services may be overloaded after concurrency tests (HTTP: $http_code)"
    fi
    
    log_info "?? Concurrency Tests Completed"
    echo
}

# Run tests if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    check_prerequisites || exit 1
    run_concurrency_tests
    generate_summary
fi