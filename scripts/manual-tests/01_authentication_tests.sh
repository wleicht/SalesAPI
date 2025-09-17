#!/bin/bash

# SalesAPI Manual Tests - Authentication Tests
# Tests all authentication-related functionality

# Load utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

# Configuration
GATEWAY_URL=$(get_config_value '.gateway.baseUrl' || echo "http://localhost:6000")

run_authentication_tests() {
    log_info "?? Starting Authentication Tests..."
    
    # Test 1: Get test users list
    start_test "Get test users list"
    response=$(make_request "GET" "$GATEWAY_URL/auth/test-users" "" "" "200")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "200" ]; then
        pass_test "Successfully retrieved test users list"
    else
        fail_test "Failed to get test users (HTTP: $http_code)"
    fi
    
    # Test 2: Authenticate as admin
    start_test "Authenticate as admin"
    ADMIN_TOKEN=$(get_auth_token "admin" "admin123" "$GATEWAY_URL")
    
    if [ -n "$ADMIN_TOKEN" ] && [ "$ADMIN_TOKEN" != "null" ]; then
        pass_test "Successfully authenticated as admin"
        export ADMIN_TOKEN
    else
        fail_test "Failed to authenticate as admin"
        export ADMIN_TOKEN=""
    fi
    
    # Test 3: Authenticate as customer
    start_test "Authenticate as customer"
    CUSTOMER_TOKEN=$(get_auth_token "customer1" "password123" "$GATEWAY_URL")
    
    if [ -n "$CUSTOMER_TOKEN" ] && [ "$CUSTOMER_TOKEN" != "null" ]; then
        pass_test "Successfully authenticated as customer"
        export CUSTOMER_TOKEN
    else
        fail_test "Failed to authenticate as customer"
        export CUSTOMER_TOKEN=""
    fi
    
    # Test 4: Test invalid credentials
    start_test "Test invalid credentials"
    response=$(make_request "POST" "$GATEWAY_URL/auth/token" \
        "Content-Type: application/json" \
        '{"username":"admin","password":"wrong_password"}' \
        "401")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "401" ]; then
        pass_test "Correctly rejected invalid credentials"
    else
        fail_test "Did not reject invalid credentials (HTTP: $http_code)"
    fi
    
    # Test 5: Test missing credentials
    start_test "Test missing credentials"
    response=$(make_request "POST" "$GATEWAY_URL/auth/token" \
        "Content-Type: application/json" \
        '{}' \
        "400")
    http_code="${response%%|*}"
    
    if [ "$http_code" = "400" ]; then
        pass_test "Correctly rejected missing credentials"
    else
        fail_test "Did not reject missing credentials (HTTP: $http_code)"
    fi
    
    # Test 6: Validate token structure (if jq is available)
    if command -v jq > /dev/null && [ -n "$ADMIN_TOKEN" ]; then
        start_test "Validate JWT token structure"
        
        # JWT tokens have 3 parts separated by dots
        token_parts=$(echo "$ADMIN_TOKEN" | tr '.' '\n' | wc -l)
        
        if [ "$token_parts" -eq 3 ]; then
            pass_test "JWT token has correct structure"
        else
            fail_test "JWT token has invalid structure (parts: $token_parts)"
        fi
    fi
    
    log_info "?? Authentication Tests Completed"
    echo
}

# Run tests if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    check_prerequisites || exit 1
    run_authentication_tests
    generate_summary
fi