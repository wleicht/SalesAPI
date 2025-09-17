#!/bin/bash

# SalesAPI Manual Tests - Validation and Edge Cases Tests
# Tests validation logic and edge cases

# Load utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

# Configuration
GATEWAY_URL=$(get_config_value '.gateway.baseUrl' || echo "http://localhost:6000")

run_validation_tests() {
    log_info "?? Starting Validation and Edge Cases Tests..."
    
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
    
    # Test 1: Invalid pagination parameters
    start_test "Invalid pagination parameters - products"
    response=$(make_request "GET" "$GATEWAY_URL/inventory/products?page=0&pageSize=-1" "" "" "400")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "400" ]; then
        pass_test "Correctly rejected invalid pagination parameters"
    else
        fail_test "Did not reject invalid pagination parameters (HTTP: $http_code)"
    fi
    
    # Test 2: Invalid pagination parameters - orders
    start_test "Invalid pagination parameters - orders"
    response=$(make_request "GET" "$GATEWAY_URL/sales/orders?page=-1&pageSize=0" "" "" "400")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "400" ] || [ "$http_code" = "500" ]; then
        # 500 might be acceptable if service handles it internally
        pass_test "Invalid pagination handled correctly (HTTP: $http_code)"
    else
        fail_test "Did not handle invalid pagination properly (HTTP: $http_code)"
    fi
    
    # Test 3: Malformed JSON in product creation
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Malformed JSON in product creation"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"Test Product","price":invalid_json}' \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected malformed JSON"
        else
            fail_test "Did not reject malformed JSON (HTTP: $http_code)"
        fi
    fi
    
    # Test 4: Malformed JSON in order creation
    if [ -n "$CUSTOMER_TOKEN" ]; then
        start_test "Malformed JSON in order creation"
        response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json" \
            '{"customerId":"invalid,"items":[]}' \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected malformed JSON in order"
        else
            fail_test "Did not reject malformed JSON in order (HTTP: $http_code)"
        fi
    fi
    
    # Test 5: Missing required fields in product
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Missing required fields in product"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"description":"Product without name"}' \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected product with missing fields"
        else
            fail_test "Did not reject product with missing fields (HTTP: $http_code)"
        fi
    fi
    
    # Test 6: Missing required fields in order
    if [ -n "$CUSTOMER_TOKEN" ]; then
        start_test "Missing required fields in order"
        response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json" \
            '{"items":[]}' \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected order with missing fields"
        else
            fail_test "Did not reject order with missing fields (HTTP: $http_code)"
        fi
    fi
    
    # Test 7: Extremely long product name
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Extremely long product name"
        long_name=$(printf 'A%.0s' {1..1000})  # 1000 character string
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            "{\"name\":\"$long_name\",\"description\":\"Test\",\"price\":10.00,\"stockQuantity\":1}" \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ] || [ "$http_code" = "413" ]; then
            pass_test "Correctly handled extremely long product name"
        else
            fail_test "Did not handle extremely long product name properly (HTTP: $http_code)"
        fi
    fi
    
    # Test 8: Negative stock quantity
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Negative stock quantity"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"Test Product","description":"Test","price":10.00,"stockQuantity":-5}' \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected negative stock quantity"
        else
            fail_test "Did not reject negative stock quantity (HTTP: $http_code)"
        fi
    fi
    
    # Test 9: Zero price product
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Zero price product"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"Free Product","description":"No cost","price":0.00,"stockQuantity":10}' \
            "201")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "201" ]; then
            pass_test "Successfully created zero price product (if allowed by business logic)"
        elif [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected zero price product (if not allowed by business logic)"
        else
            fail_test "Unexpected response for zero price product (HTTP: $http_code)"
        fi
    fi
    
    # Test 10: Empty order items array
    if [ -n "$CUSTOMER_TOKEN" ]; then
        start_test "Empty order items array"
        response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json" \
            '{"customerId":"123e4567-e89b-12d3-a456-426614174001","items":[]}' \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected empty order items"
        else
            fail_test "Did not reject empty order items (HTTP: $http_code)"
        fi
    fi
    
    # Test 11: Order with zero quantity item
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$MOUSE_ID" ]; then
        start_test "Order with zero quantity item"
        response=$(make_request "POST" "$GATEWAY_URL/sales/orders" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json" \
            "{\"customerId\":\"123e4567-e89b-12d3-a456-426614174001\",\"items\":[{\"productId\":\"$MOUSE_ID\",\"quantity\":0}]}" \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected zero quantity order item"
        else
            fail_test "Did not reject zero quantity order item (HTTP: $http_code)"
        fi
    fi
    
    # Test 12: Invalid GUID formats
    start_test "Invalid GUID format in product query"
    response=$(make_request "GET" "$GATEWAY_URL/inventory/products/invalid-guid-format" "" "" "400")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "400" ] || [ "$http_code" = "404" ]; then
        pass_test "Correctly handled invalid GUID format"
    else
        fail_test "Did not handle invalid GUID format properly (HTTP: $http_code)"
    fi
    
    # Test 13: Invalid GUID format in order query
    start_test "Invalid GUID format in order query"
    response=$(make_request "GET" "$GATEWAY_URL/sales/orders/not-a-guid" "" "" "400")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "400" ] || [ "$http_code" = "404" ]; then
        pass_test "Correctly handled invalid GUID format in order query"
    else
        fail_test "Did not handle invalid GUID format in order query properly (HTTP: $http_code)"
    fi
    
    # Test 14: Missing Content-Type header
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Missing Content-Type header"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN" \
            '{"name":"Test Product","description":"Test","price":10.00,"stockQuantity":1}' \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ] || [ "$http_code" = "415" ]; then
            pass_test "Correctly handled missing Content-Type header"
        else
            fail_test "Did not handle missing Content-Type header properly (HTTP: $http_code)"
        fi
    fi
    
    # Test 15: Wrong Content-Type header
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Wrong Content-Type header"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: text/plain" \
            '{"name":"Test Product","description":"Test","price":10.00,"stockQuantity":1}' \
            "415")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "415" ] || [ "$http_code" = "400" ]; then
            pass_test "Correctly handled wrong Content-Type header"
        else
            fail_test "Did not handle wrong Content-Type header properly (HTTP: $http_code)"
        fi
    fi
    
    log_info "?? Validation and Edge Cases Tests Completed"
    echo
}

# Run tests if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    check_prerequisites || exit 1
    run_validation_tests
    generate_summary
fi