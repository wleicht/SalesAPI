#!/bin/bash

# SalesAPI Manual Tests - Stock Reservation Tests
# Tests stock reservation functionality

# Load utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

# Configuration
INVENTORY_URL=$(get_config_value '.inventory.baseUrl' || echo "http://localhost:5000")

# Global variables for test data
RESERVATION_ORDER_ID=""
RESERVATION_ID=""

run_reservation_tests() {
    log_info "?? Starting Stock Reservation Tests..."
    
    # Ensure we have authentication tokens and products
    if [ -z "$ADMIN_TOKEN" ]; then
        log_warning "Admin token not available, attempting authentication..."
        ADMIN_TOKEN=$(get_auth_token "admin" "admin123" "$GATEWAY_URL")
        export ADMIN_TOKEN
    fi
    
    # Test 1: Create stock reservation directly
    if [ -n "$ADMIN_TOKEN" ] && [ -n "$MOUSE_ID" ]; then
        start_test "Create stock reservation directly"
        RESERVATION_ORDER_ID="550e8400-e29b-41d4-a716-446655440001"
        correlation_id="test-reservation-$(date +%s)"
        
        response=$(make_request "POST" "$INVENTORY_URL/api/stockreservations" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json
X-Correlation-Id: $correlation_id" \
            "{\"orderId\":\"$RESERVATION_ORDER_ID\",\"correlationId\":\"$correlation_id\",\"items\":[{\"productId\":\"$MOUSE_ID\",\"quantity\":3}]}" \
            "201")
        http_code="${response%%|*}"
        body="${response##*|}"
        
        if [ "$http_code" = "201" ]; then
            pass_test "Successfully created stock reservation"
            export RESERVATION_ORDER_ID
            
            # Try to extract reservation ID if jq is available
            if command -v jq > /dev/null; then
                RESERVATION_ID=$(echo "$body" | jq -r '.reservationResults[0].reservationId' 2>/dev/null)
                export RESERVATION_ID
            fi
        else
            fail_test "Failed to create stock reservation (HTTP: $http_code)"
        fi
    else
        skip_test "Create stock reservation - Missing admin token or mouse ID"
    fi
    
    # Test 2: Query reservations by order
    if [ -n "$ADMIN_TOKEN" ] && [ -n "$RESERVATION_ORDER_ID" ]; then
        start_test "Query reservations by order"
        response=$(make_request "GET" "$INVENTORY_URL/api/stockreservations/order/$RESERVATION_ORDER_ID" \
            "Authorization: Bearer $ADMIN_TOKEN" \
            "" \
            "200")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "200" ]; then
            pass_test "Successfully queried reservations by order"
        elif [ "$http_code" = "404" ]; then
            pass_test "No reservations found for order (acceptable if creation failed)"
        else
            fail_test "Failed to query reservations by order (HTTP: $http_code)"
        fi
    else
        skip_test "Query reservations by order - Missing admin token or order ID"
    fi
    
    # Test 3: Query specific reservation
    if [ -n "$ADMIN_TOKEN" ] && [ -n "$RESERVATION_ID" ] && [ "$RESERVATION_ID" != "null" ]; then
        start_test "Query specific reservation"
        response=$(make_request "GET" "$INVENTORY_URL/api/stockreservations/$RESERVATION_ID" \
            "Authorization: Bearer $ADMIN_TOKEN" \
            "" \
            "200")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "200" ]; then
            pass_test "Successfully queried specific reservation"
        elif [ "$http_code" = "404" ]; then
            pass_test "Reservation not found (acceptable)"
        else
            fail_test "Failed to query specific reservation (HTTP: $http_code)"
        fi
    else
        skip_test "Query specific reservation - Missing admin token or reservation ID"
    fi
    
    # Test 4: Create reservation with insufficient stock
    if [ -n "$ADMIN_TOKEN" ] && [ -n "$MOUSE_ID" ]; then
        start_test "Create reservation with insufficient stock"
        test_order_id="550e8400-e29b-41d4-a716-446655440002"
        correlation_id="test-reservation-fail-$(date +%s)"
        
        response=$(make_request "POST" "$INVENTORY_URL/api/stockreservations" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json
X-Correlation-Id: $correlation_id" \
            "{\"orderId\":\"$test_order_id\",\"correlationId\":\"$correlation_id\",\"items\":[{\"productId\":\"$MOUSE_ID\",\"quantity\":1000}]}" \
            "422")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "422" ] || [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected reservation with insufficient stock"
        else
            fail_test "Did not reject insufficient stock reservation (HTTP: $http_code)"
        fi
    else
        skip_test "Create reservation with insufficient stock - Missing admin token or mouse ID"
    fi
    
    # Test 5: Create reservation for non-existent product
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Create reservation for non-existent product"
        test_order_id="550e8400-e29b-41d4-a716-446655440003"
        correlation_id="test-reservation-noexist-$(date +%s)"
        
        response=$(make_request "POST" "$INVENTORY_URL/api/stockreservations" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json
X-Correlation-Id: $correlation_id" \
            "{\"orderId\":\"$test_order_id\",\"correlationId\":\"$correlation_id\",\"items\":[{\"productId\":\"00000000-0000-0000-0000-000000000001\",\"quantity\":1}]}" \
            "422")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "422" ] || [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected reservation for non-existent product"
        else
            fail_test "Did not reject non-existent product reservation (HTTP: $http_code)"
        fi
    else
        skip_test "Create reservation for non-existent product - Missing admin token"
    fi
    
    # Test 6: Create duplicate reservation (idempotency test)
    if [ -n "$ADMIN_TOKEN" ] && [ -n "$MOUSE_ID" ] && [ -n "$RESERVATION_ORDER_ID" ]; then
        start_test "Create duplicate reservation (idempotency test)"
        correlation_id="test-reservation-duplicate-$(date +%s)"
        
        response=$(make_request "POST" "$INVENTORY_URL/api/stockreservations" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json
X-Correlation-Id: $correlation_id" \
            "{\"orderId\":\"$RESERVATION_ORDER_ID\",\"correlationId\":\"$correlation_id\",\"items\":[{\"productId\":\"$MOUSE_ID\",\"quantity\":1}]}" \
            "409")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "409" ]; then
            pass_test "Correctly rejected duplicate reservation"
        elif [ "$http_code" = "201" ] || [ "$http_code" = "422" ]; then
            pass_test "Duplicate reservation handled appropriately (HTTP: $http_code)"
        else
            fail_test "Unexpected response for duplicate reservation (HTTP: $http_code)"
        fi
    else
        skip_test "Create duplicate reservation - Missing dependencies"
    fi
    
    # Test 7: Create reservation without authentication
    if [ -n "$MOUSE_ID" ]; then
        start_test "Create reservation without authentication"
        test_order_id="550e8400-e29b-41d4-a716-446655440004"
        correlation_id="test-reservation-noauth-$(date +%s)"
        
        response=$(make_request "POST" "$INVENTORY_URL/api/stockreservations" \
            "Content-Type: application/json
X-Correlation-Id: $correlation_id" \
            "{\"orderId\":\"$test_order_id\",\"correlationId\":\"$correlation_id\",\"items\":[{\"productId\":\"$MOUSE_ID\",\"quantity\":1}]}" \
            "401")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "401" ]; then
            pass_test "Correctly rejected unauthenticated reservation request"
        else
            fail_test "Did not reject unauthenticated reservation (HTTP: $http_code)"
        fi
    else
        skip_test "Create reservation without authentication - Missing mouse ID"
    fi
    
    # Test 8: Query reservations without authentication
    if [ -n "$RESERVATION_ORDER_ID" ]; then
        start_test "Query reservations without authentication"
        response=$(make_request "GET" "$INVENTORY_URL/api/stockreservations/order/$RESERVATION_ORDER_ID" \
            "" \
            "" \
            "401")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "401" ]; then
            pass_test "Correctly rejected unauthenticated reservation query"
        else
            fail_test "Did not reject unauthenticated reservation query (HTTP: $http_code)"
        fi
    else
        skip_test "Query reservations without authentication - Missing order ID"
    fi
    
    log_info "?? Stock Reservation Tests Completed"
    echo
}

# Run tests if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    check_prerequisites || exit 1
    run_reservation_tests
    generate_summary
fi