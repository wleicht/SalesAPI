#!/bin/bash

# SalesAPI Manual Tests - Sample Test Execution
# This script shows practical examples of how to run and interpret tests

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

show_sample_execution() {
    echo
    log_info "?? SalesAPI Sample Test Execution"
    log_info "=================================="
    echo
    log_info "This script demonstrates practical test execution examples"
    log_info "and shows you what to expect when running the tests."
    echo
}

demonstrate_basic_workflow() {
    log_info "?? Basic Testing Workflow:"
    echo
    log_info "1. First, check if services are running:"
    echo "   $ ./scripts/manual-tests/02_health_tests.sh"
    echo
    log_info "2. Run authentication tests to get tokens:"
    echo "   $ ./scripts/manual-tests/01_authentication_tests.sh"
    echo
    log_info "3. Test core functionality:"
    echo "   $ ./scripts/manual-tests/03_products_tests.sh"
    echo "   $ ./scripts/manual-tests/04_orders_tests.sh"
    echo
    log_info "4. Run complete suite:"
    echo "   $ ./scripts/manual-tests/run_manual_tests.sh"
    echo
}

demonstrate_individual_commands() {
    log_info "?? Individual cURL Commands:"
    echo
    log_info "Here are some key commands you can run manually:"
    echo
    
    echo -e "${BLUE}# 1. Check Gateway Status${NC}"
    echo "curl -X GET http://localhost:6000/gateway/status | jq"
    echo
    
    echo -e "${BLUE}# 2. Get Authentication Token${NC}"
    echo 'curl -X POST http://localhost:6000/auth/token \'
    echo '  -H "Content-Type: application/json" \'
    echo '  -d '"'"'{"username":"admin","password":"admin123"}'"'"' | jq'
    echo
    
    echo -e "${BLUE}# 3. List Products (No Auth Required)${NC}"
    echo "curl -X GET http://localhost:6000/inventory/products | jq"
    echo
    
    echo -e "${BLUE}# 4. Create Product (Admin Token Required)${NC}"
    echo 'curl -X POST http://localhost:6000/inventory/products \'
    echo '  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \'
    echo '  -H "Content-Type: application/json" \'
    echo '  -d '"'"'{"name":"Test Product","description":"Created via cURL","price":99.99,"stockQuantity":10}'"'"' | jq'
    echo
    
    echo -e "${BLUE}# 5. Create Order (Customer Token Required)${NC}"
    echo 'curl -X POST http://localhost:6000/sales/orders \'
    echo '  -H "Authorization: Bearer YOUR_CUSTOMER_TOKEN" \'
    echo '  -H "Content-Type: application/json" \'
    echo '  -d '"'"'{"customerId":"12345678-1234-1234-1234-123456789012","items":[{"productId":"YOUR_PRODUCT_ID","quantity":1}]}'"'"' | jq'
    echo
}

demonstrate_expected_results() {
    log_info "?? Expected Test Results:"
    echo
    log_info "When everything is working correctly, you should see:"
    echo
    
    echo -e "${GREEN}? Authentication Tests (6/6 passed)${NC}"
    echo "   - Admin login successful"
    echo "   - Customer login successful"
    echo "   - Invalid credentials rejected"
    echo "   - JWT token structure valid"
    echo
    
    echo -e "${GREEN}? Health Check Tests (7/7 passed)${NC}"
    echo "   - Gateway status: Healthy"
    echo "   - Inventory API: Healthy"
    echo "   - Sales API: Healthy"
    echo "   - Response times acceptable"
    echo
    
    echo -e "${GREEN}? Product Tests (12/12 passed)${NC}"
    echo "   - Product creation (admin only)"
    echo "   - Product listing (public)"
    echo "   - Product updates (admin only)"
    echo "   - Authorization enforced"
    echo
    
    echo -e "${GREEN}? Order Tests (13/13 passed)${NC}"
    echo "   - Order creation with stock reservation"
    echo "   - Payment processing simulation"
    echo "   - Order status management"
    echo "   - Stock consistency maintained"
    echo
}

demonstrate_troubleshooting() {
    log_info "?? Common Issues and Solutions:"
    echo
    
    echo -e "${YELLOW}Issue: Services not responding${NC}"
    echo "Solution: Check if all services are running:"
    echo "  netstat -an | grep -E '(5000|5001|6000)'"
    echo "  Or start services with: docker-compose up -d"
    echo
    
    echo -e "${YELLOW}Issue: Authentication fails${NC}"
    echo "Solution: Verify default credentials:"
    echo "  Admin: admin/admin123"
    echo "  Customer: customer1/password123"
    echo
    
    echo -e "${YELLOW}Issue: Tests report database errors${NC}"
    echo "Solution: Ensure SQL Server is running:"
    echo "  docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Your_password123' \\"
    echo "    -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest"
    echo
    
    echo -e "${YELLOW}Issue: Order creation fails with 422${NC}"
    echo "Solution: This may be expected due to payment simulation"
    echo "  - High-price orders (>$1000) often fail payment simulation"
    echo "  - Low-price orders (<$100) usually succeed"
    echo "  - This tests the error handling and compensation logic"
    echo
}

run_sample_test_if_services_available() {
    log_info "?? Live Test Sample (if services are available):"
    echo
    
    # Quick check if services are running
    if curl -s --max-time 2 http://localhost:6000/gateway/status > /dev/null 2>&1; then
        log_success "? Gateway is responding - running live sample!"
        echo
        
        # Test 1: Get gateway status
        start_test "Gateway Status Check (Live)"
        response=$(curl -s --max-time 5 http://localhost:6000/gateway/status)
        if [ $? -eq 0 ]; then
            log_success "Gateway Response: $response"
            pass_test "Gateway is healthy and responding"
        else
            fail_test "Gateway did not respond properly"
        fi
        
        # Test 2: Try to get test users
        start_test "Test Users Endpoint (Live)"
        response=$(curl -s --max-time 5 http://localhost:6000/auth/test-users)
        if [ $? -eq 0 ]; then
            log_success "Test users endpoint is accessible"
            pass_test "Authentication endpoints are working"
        else
            fail_test "Test users endpoint not accessible"
        fi
        
        # Test 3: Try to list products
        start_test "Products Listing (Live)"
        response=$(curl -s --max-time 5 http://localhost:6000/inventory/products)
        if [ $? -eq 0 ]; then
            log_success "Products endpoint is accessible"
            pass_test "Product listing is working"
        else
            fail_test "Products endpoint not accessible"
        fi
        
        echo
        generate_summary
        echo
        log_info "? This was a real test against your running services!"
        
    else
        log_warning "?? Services don't appear to be running - skipping live tests"
        echo
        log_info "To see live tests, start the services and run this script again:"
        echo "  cd src/gateway && dotnet run &"
        echo "  cd src/inventory.api && dotnet run &"
        echo "  cd src/sales.api && dotnet run &"
    fi
}

show_next_steps() {
    log_info "?? Next Steps:"
    echo
    log_info "Ready to start testing? Here are your options:"
    echo
    echo -e "${BLUE}Quick Start:${NC}"
    echo "  ./scripts/manual-tests/demo_tests.sh           # Quick validation"
    echo "  ./scripts/manual-tests/test_manager.sh         # Interactive menu"
    echo
    echo -e "${BLUE}Comprehensive Testing:${NC}"
    echo "  ./scripts/manual-tests/run_manual_tests.sh     # Full test suite"
    echo "  ./scripts/manual-tests/interactive_tests.sh    # Guided testing"
    echo
    echo -e "${BLUE}Individual Categories:${NC}"
    echo "  ./scripts/manual-tests/01_authentication_tests.sh"
    echo "  ./scripts/manual-tests/02_health_tests.sh"
    echo "  ./scripts/manual-tests/03_products_tests.sh"
    echo "  # ... and more"
    echo
    echo -e "${BLUE}Documentation:${NC}"
    echo "  scripts/manual-tests/README.md              # Main documentation"
    echo "  scripts/manual-tests/troubleshooting.md     # Problem solutions"
    echo "  scripts/manual-tests/curl_examples.md       # Manual commands"
    echo
}

main() {
    show_sample_execution
    demonstrate_basic_workflow
    echo
    demonstrate_individual_commands
    echo
    demonstrate_expected_results
    echo
    demonstrate_troubleshooting
    echo
    run_sample_test_if_services_available
    echo
    show_next_steps
}

# Run if executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    check_prerequisites || exit 1
    main
fi