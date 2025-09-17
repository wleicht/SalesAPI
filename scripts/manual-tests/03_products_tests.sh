#!/bin/bash

# SalesAPI Manual Tests - Product Management Tests
# Tests all product-related functionality

# Load utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

# Configuration
GATEWAY_URL=$(get_config_value '.gateway.baseUrl' || echo "http://localhost:6000")

# Global variables for test data
PRODUCT_ID=""
MOUSE_ID=""
WORKSTATION_ID=""
DELETE_PRODUCT_ID=""

run_product_tests() {
    log_info "?? Starting Product Management Tests..."
    
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
    
    # Test 1: List products (public access)
    start_test "List products (public access)"
    response=$(make_request "GET" "$GATEWAY_URL/inventory/products" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ]; then
        pass_test "Successfully listed products"
    else
        fail_test "Failed to list products (HTTP: $http_code)"
    fi
    
    # Test 2: List products with pagination
    start_test "List products with pagination"
    response=$(make_request "GET" "$GATEWAY_URL/inventory/products?page=1&pageSize=5" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ]; then
        pass_test "Successfully listed products with pagination"
    else
        fail_test "Failed to list products with pagination (HTTP: $http_code)"
    fi
    
    # Test 3: Create product (requires admin)
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Create product (admin required)"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"Laptop Gamer","description":"Notebook para jogos de alta performance","price":2999.99,"stockQuantity":10}' \
            "201")
        http_code="${response%%|*}"
        body="${response##*|}"
        
        if [ "$http_code" = "201" ]; then
            if command -v jq > /dev/null; then
                PRODUCT_ID=$(echo "$body" | jq -r '.id' 2>/dev/null)
            fi
            pass_test "Successfully created product"
            export PRODUCT_ID
        else
            fail_test "Failed to create product (HTTP: $http_code)"
        fi
    else
        skip_test "Create product - No admin token available"
    fi
    
    # Test 4: Attempt to create product without authentication
    start_test "Create product without authentication"
    response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
        "Content-Type: application/json" \
        '{"name":"Produto Teste","description":"Teste sem auth","price":100.00,"stockQuantity":5}' \
        "401")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "401" ]; then
        pass_test "Correctly rejected unauthenticated product creation"
    else
        fail_test "Did not reject unauthenticated product creation (HTTP: $http_code)"
    fi
    
    # Test 5: Attempt to create product with customer token
    if [ -n "$CUSTOMER_TOKEN" ]; then
        start_test "Create product with customer token (should fail)"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $CUSTOMER_TOKEN
Content-Type: application/json" \
            '{"name":"Produto Teste","description":"Teste com customer","price":100.00,"stockQuantity":5}' \
            "403")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "403" ]; then
            pass_test "Correctly rejected customer product creation"
        else
            fail_test "Did not reject customer product creation (HTTP: $http_code)"
        fi
    else
        skip_test "Create product with customer token - No customer token available"
    fi
    
    # Test 6: Get product by ID
    if [ -n "$PRODUCT_ID" ] && [ "$PRODUCT_ID" != "null" ]; then
        start_test "Get product by ID"
        response=$(make_request "GET" "$GATEWAY_URL/inventory/products/$PRODUCT_ID" "" "" "200")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "200" ]; then
            pass_test "Successfully retrieved product by ID"
        else
            fail_test "Failed to retrieve product by ID (HTTP: $http_code)"
        fi
    else
        skip_test "Get product by ID - No product ID available"
    fi
    
    # Test 7: Update product (requires admin)
    if [ -n "$ADMIN_TOKEN" ] && [ -n "$PRODUCT_ID" ] && [ "$PRODUCT_ID" != "null" ]; then
        start_test "Update product (admin required)"
        response=$(make_request "PUT" "$GATEWAY_URL/inventory/products/$PRODUCT_ID" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"Laptop Gamer Atualizado","description":"Versão melhorada","price":3299.99,"stockQuantity":15}' \
            "200")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "200" ]; then
            pass_test "Successfully updated product"
        else
            fail_test "Failed to update product (HTTP: $http_code)"
        fi
    else
        skip_test "Update product - No admin token or product ID available"
    fi
    
    # Test 8: Create additional test products
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Create mouse product"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"Mouse Gamer","description":"Mouse com sensor óptico","price":89.99,"stockQuantity":50}' \
            "201")
        http_code="${response%%|*}"
        body="${response##*|}"
        
        if [ "$http_code" = "201" ]; then
            if command -v jq > /dev/null; then
                MOUSE_ID=$(echo "$body" | jq -r '.id' 2>/dev/null)
            fi
            pass_test "Successfully created mouse product"
            export MOUSE_ID
        else
            fail_test "Failed to create mouse product (HTTP: $http_code)"
        fi
        
        start_test "Create workstation product"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"Workstation Profissional","description":"Computador de alta performance","price":15000.00,"stockQuantity":3}' \
            "201")
        http_code="${response%%|*}"
        body="${response##*|}"
        
        if [ "$http_code" = "201" ]; then
            if command -v jq > /dev/null; then
                WORKSTATION_ID=$(echo "$body" | jq -r '.id' 2>/dev/null)
            fi
            pass_test "Successfully created workstation product"
            export WORKSTATION_ID
        else
            fail_test "Failed to create workstation product (HTTP: $http_code)"
        fi
    fi
    
    # Test 9: Create product with invalid data
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Create product with invalid data"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"","description":"Produto sem nome","price":-10.00,"stockQuantity":-5}' \
            "400")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "400" ]; then
            pass_test "Correctly rejected invalid product data"
        else
            fail_test "Did not reject invalid product data (HTTP: $http_code)"
        fi
    fi
    
    # Test 10: Get non-existent product
    start_test "Get non-existent product"
    response=$(make_request "GET" "$GATEWAY_URL/inventory/products/00000000-0000-0000-0000-000000000000" "" "" "404")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "404" ]; then
        pass_test "Correctly returned 404 for non-existent product"
    else
        fail_test "Did not return 404 for non-existent product (HTTP: $http_code)"
    fi
    
    # Test 11: Delete product
    if [ -n "$ADMIN_TOKEN" ]; then
        start_test "Create product for deletion"
        response=$(make_request "POST" "$GATEWAY_URL/inventory/products" \
            "Authorization: Bearer $ADMIN_TOKEN
Content-Type: application/json" \
            '{"name":"Produto para Deletar","description":"Será removido","price":50.00,"stockQuantity":1}' \
            "201")
        http_code="${response%%|*}"
        body="${response##*|}"
        
        if [ "$http_code" = "201" ]; then
            if command -v jq > /dev/null; then
                DELETE_PRODUCT_ID=$(echo "$body" | jq -r '.id' 2>/dev/null)
            fi
            
            if [ -n "$DELETE_PRODUCT_ID" ] && [ "$DELETE_PRODUCT_ID" != "null" ]; then
                start_test "Delete product (admin required)"
                response=$(make_request "DELETE" "$GATEWAY_URL/inventory/products/$DELETE_PRODUCT_ID" \
                    "Authorization: Bearer $ADMIN_TOKEN" \
                    "" \
                    "204")
                http_code="${response%%|*}"
                
                if [ "$http_code" = "204" ]; then
                    pass_test "Successfully deleted product"
                else
                    fail_test "Failed to delete product (HTTP: $http_code)"
                fi
            else
                skip_test "Delete product - Could not get product ID"
            fi
        else
            skip_test "Delete product - Could not create test product"
        fi
    fi
    
    # Test 12: Attempt to delete product with customer token
    if [ -n "$CUSTOMER_TOKEN" ] && [ -n "$PRODUCT_ID" ] && [ "$PRODUCT_ID" != "null" ]; then
        start_test "Delete product with customer token (should fail)"
        response=$(make_request "DELETE" "$GATEWAY_URL/inventory/products/$PRODUCT_ID" \
            "Authorization: Bearer $CUSTOMER_TOKEN" \
            "" \
            "403")
        http_code="${response%%|*}"
        
        if [ "$http_code" = "403" ]; then
            pass_test "Correctly rejected customer product deletion"
        else
            fail_test "Did not reject customer product deletion (HTTP: $http_code)"
        fi
    fi
    
    log_info "?? Product Management Tests Completed"
    echo
}

# Run tests if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    check_prerequisites || exit 1
    run_product_tests
    generate_summary
fi