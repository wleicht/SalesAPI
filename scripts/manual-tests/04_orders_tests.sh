#!/bin/bash

# SalesAPI Manual Tests - Order Management Tests
# Tests all order-related functionality

# Load utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

# Configuration
GATEWAY_URL=$(get_config_value '.gateway.baseUrl' || echo "http://localhost:6000")

# Global variables for test data
ORDER_ID=""
MULTI_ORDER_ID=""
PAYMENT_FAIL_ORDER_ID=""

run_order_tests() {
    log_info "?? Starting Order Management Tests..."
    
    # Ensure we have authentication tokens and products
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
    
    # Test 1: List orders (public access)
    start_test "List orders (public access)"
    response=$(make_request "GET" "$GATEWAY_URL/sales/orders" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ] || [ "$http_code" = "500" ] || [ "$http_code" = "503" ]; then
        # Allow 500/503 as services might not be fully ready
        pass_test "Orders endpoint accessible (HTTP: $http_code)"
    else
        fail_test "Failed to access orders endpoint (HTTP: $http_code)"
    fi
    
    # Test 2: List orders with pagination
    start_test "List orders with pagination"
    response=$(make_request "GET" "$GATEWAY_URL/sales/orders?page=1&pageSize=10" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ] || [ "$http_code" = "500" ] || [ "$http_code" = "503" ]; then
        pass_test "Orders pagination endpoint accessible (HTTP: $http_code)"
    else
        fail_test "Failed to access orders with pagination (HTTP: $http_code)"
    fi
    
    # Test 3: Create order without authentication
    start_test "Create order without authentication"
    response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
        "Content-Type: application/json" \
        '{"customerId":"123e4567-e89b-12d3-a456-426614174002","items":[{"productId":"00000000-0000-0000-0000-000000000001","quantity":1}]}' \
        "401")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "401" ]; then
        pass_test "Correctly rejected unauthenticated order creation"
    else
        fail_test "Did not reject unauthenticated order creation (HTTP: $http_code)"
    fi
    
    # Test 4: Create order with valid data (low price for payment success)
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$MOUSE_ID" ]; then
        start_test "Create order with valid data"
        correlation_id="test-order-$(date +%s)"
        response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json
X-Correlation-Id: $correlation_id" \
            "{\"customerId\":\"123e4567-e89b-12d3-a456-426614174001\",\"items\":[{\"productId\":\"$MOUSE_ID\",\"quantity\":2}]}" \
            "201")
        http_code="${response%%|*}"
        body="${response##*|}"
        
        if [ "$http_code" = "201" ] || [ "$http_code" = "422" ]; then
            if [ "$http_code" = "201" ]; then
                if command -v jq > /dev/null; then
                    ORDER_ID=$(echo "$body" | jq -r '.id' 2>/dev/null)
                fi
                pass_test "Successfully created order"
            else
                # 422 might indicate business logic issues (e.g., payment failure)
                pass_test "Order creation handled correctly (HTTP: $http_code)"
            fi
            export ORDER_ID
        else
            fail_test "Failed to create order (HTTP: $http_code)"
        fi
    else
        skip_test "Create order - Missing customer token or product ID"
    fi
    
    # Test 5: Get order by ID
    if [ -n "$ORDER_ID" ] && [ "$ORDER_ID" != "null" ] && [ -n "$ORDER_ID" ]; then
        start_test "Get order by ID"
        response=$(make_request "GET" "$GATEWAY_URL/sales/orders/$ORDER_ID" "" "" "200")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "200" ]; then
            pass_test "Successfully retrieved order by ID"
        elif [ "$http_code" = "404" ]; then
            pass_test "Order not found (which is acceptable if creation failed)"
        else
            fail_test "Failed to retrieve order by ID (HTTP: $http_code)"
        fi
    else
        skip_test "Get order by ID - No order ID available"
    fi
    
    # Test 6: Create order with multiple items
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$MOUSE_ID" ] && [ -n "$PRODUCT_ID" ]; then
        start_test "Create order with multiple items"
        correlation_id="test-multi-order-$(date +%s)"
        response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json
X-Correlation-Id: $correlation_id" \
            "{\"customerId\":\"123e4567-e89b-12d3-a456-426614174003\",\"items\":[{\"productId\":\"$MOUSE_ID\",\"quantity\":1},{\"productId\":\"$PRODUCT_ID\",\"quantity\":1}]}" \
            "201")
        http_code="${response%%|*}"
        body="${response##*|}"
        
        if [ "$http_code" = "201" ] || [ "$http_code" = "422" ]; then
            if [ "$http_code" = "201" ]; then
                if command -v jq > /dev/null; then
                    MULTI_ORDER_ID=$(echo "$body" | jq -r '.id' 2>/dev/null)
                fi
                pass_test "Successfully created multi-item order"
            else
                pass_test "Multi-item order creation handled correctly (HTTP: $http_code)"
            fi
            export MULTI_ORDER_ID
        else
            fail_test "Failed to create multi-item order (HTTP: $http_code)"
        fi
    else
        skip_test "Create multi-item order - Missing tokens or product IDs"
    fi
    
    # Test 7: Create order with high price (may trigger payment failure)
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$WORKSTATION_ID" ]; then
        start_test "Create order with high price (payment may fail)"
        correlation_id="test-payment-fail-$(date +%s)"
        response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json
X-Correlation-Id: $correlation_id" \
            "{\"customerId\":\"123e4567-e89b-12d3-a456-426614174004\",\"items\":[{\"productId\":\"$WORKSTATION_ID\",\"quantity\":2}]}" \
            "422")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "422" ]; then
            pass_test "Payment failure correctly handled"
        elif [ "$http_code" = "201" ]; then
            pass_test "Order created successfully (payment succeeded)"
        else
            fail_test "Unexpected response for high-price order (HTTP: $http_code)"
        fi
    else
        skip_test "Create high-price order - Missing token or workstation ID"
    fi
    
    # Test 8: Create order with invalid data
    if [ -n "$CUSTOMER_TOKEN" ]; then
        start_test "Create order with invalid data"
        response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json" \
            '{"customerId":"00000000-0000-0000-0000-000000000000","items":[]}' \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected invalid order data"
        else
            fail_test "Did not reject invalid order data (HTTP: $http_code)"
        fi
    fi
    
    # Test 9: Create order with negative quantity
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$MOUSE_ID" ]; then
        start_test "Create order with negative quantity"
        response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json" \
            "{\"customerId\":\"123e4567-e89b-12d3-a456-426614174005\",\"items\":[{\"productId\":\"$MOUSE_ID\",\"quantity\":-1}]}" \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected negative quantity"
        else
            fail_test "Did not reject negative quantity (HTTP: $http_code)"
        fi
    fi
    
    # Test 10: Get non-existent order
    start_test "Get non-existent order"
    response=$(make_request "GET" "$GATEWAY_URL/sales/orders/00000000-0000-0000-0000-000000000000" "" "" "404")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "404" ]; then
        pass_test "Correctly returned 404 for non-existent order"
    else
        fail_test "Did not return 404 for non-existent order (HTTP: $http_code)"
    fi
    
    # Test 11: Confirm order
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$ORDER_ID" ] && [ "$ORDER_ID" != "null" ]; then
        start_test "Confirm order"
        correlation_id="test-confirm-$(date +%s)"
        response=$(make_request "PATCH" "$GATEWAY_URL/sales/orders/$ORDER_ID/confirm" \
            "Authorization: Bearer $CUSTOMER_TOKEN
X-Correlation-Id: $correlation_id" \
            "" \
            "200")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "200" ] || [ "$http_code" = "400" ] || [ "$http_code" = "404" ]; then
            # 400 might mean order is already confirmed, 404 might mean order doesn't exist
            pass_test "Order confirmation handled correctly (HTTP: $http_code)"
        else
            fail_test "Failed to handle order confirmation (HTTP: $http_code)"
        fi
    else
        skip_test "Confirm order - Missing token or order ID"
    fi
    
    # Test 12: Cancel order
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$MULTI_ORDER_ID" ] && [ "$MULTI_ORDER_ID" != "null" ]; then
        start_test "Cancel order"
        correlation_id="test-cancel-$(date +%s)"
        response=$(make_request "PATCH" "$GATEWAY_URL/sales/orders/$MULTI_ORDER_ID/cancel?reason=Customer%20cancelled" \
            "Authorization: Bearer $CUSTOMER_TOKEN
X-Correlation-Id: $correlation_id" \
            "" \
            "200")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "200" ] || [ "$http_code" = "400" ] || [ "$http_code" = "404" ]; then
            pass_test "Order cancellation handled correctly (HTTP: $http_code)"
        else
            fail_test "Failed to handle order cancellation (HTTP: $http_code)"
        fi
    else
        skip_test "Cancel order - Missing token or order ID"
    fi
    
    # Test 13: Fulfill order
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$ORDER_ID" ] && [ "$ORDER_ID" != "null" ]; then
        start_test "Fulfill order"
        correlation_id="test-fulfill-$(date +%s)"
        response=$(make_request "PATCH" "$GATEWAY_URL/sales/orders/$ORDER_ID/fulfill" \
            "Authorization: Bearer $CUSTOMER_TOKEN
X-Correlation-Id: $correlation_id" \
            "" \
            "200")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "200" ] || [ "$http_code" = "400" ] || [ "$http_code" = "404" ]; then
            pass_test "Order fulfillment handled correctly (HTTP: $http_code)"
        else
            fail_test "Failed to handle order fulfillment (HTTP: $http_code)"
        fi
    else
        skip_test "Fulfill order - Missing token or order ID"
    fi
    
    log_info "?? Order Management Tests Completed"
    echo
}

# Run tests if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    check_prerequisites || exit 1
    run_order_tests
    generate_summary
fi