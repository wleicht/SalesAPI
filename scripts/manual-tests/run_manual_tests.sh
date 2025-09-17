#!/bin/bash

# SalesAPI Manual Tests - Main Test Runner
# Executes all test categories and generates comprehensive report

# Load utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

# Configuration
GATEWAY_URL=$(get_config_value '.gateway.baseUrl' || echo "http://localhost:6000")
INVENTORY_URL=$(get_config_value '.inventory.baseUrl' || echo "http://localhost:5000")
SALES_URL=$(get_config_value '.sales.baseUrl' || echo "http://localhost:5001")

# Test execution control
RUN_AUTHENTICATION=true
RUN_HEALTH=true
RUN_PRODUCTS=true
RUN_ORDERS=true
RUN_RESERVATIONS=true
RUN_VALIDATION=true
RUN_CONCURRENCY=true
SKIP_PREREQUISITES=false

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --skip-auth)
                RUN_AUTHENTICATION=false
                shift
                ;;
            --skip-health)
                RUN_HEALTH=false
                shift
                ;;
            --skip-products)
                RUN_PRODUCTS=false
                shift
                ;;
            --skip-orders)
                RUN_ORDERS=false
                shift
                ;;
            --skip-reservations)
                RUN_RESERVATIONS=false
                shift
                ;;
            --skip-validation)
                RUN_VALIDATION=false
                shift
                ;;
            --skip-concurrency)
                RUN_CONCURRENCY=false
                shift
                ;;
            --skip-prerequisites)
                SKIP_PREREQUISITES=true
                shift
                ;;
            --only-auth)
                RUN_AUTHENTICATION=true
                RUN_HEALTH=false
                RUN_PRODUCTS=false
                RUN_ORDERS=false
                RUN_RESERVATIONS=false
                RUN_VALIDATION=false
                RUN_CONCURRENCY=false
                shift
                ;;
            --only-basic)
                RUN_AUTHENTICATION=true
                RUN_HEALTH=true
                RUN_PRODUCTS=true
                RUN_ORDERS=true
                RUN_RESERVATIONS=false
                RUN_VALIDATION=false
                RUN_CONCURRENCY=false
                shift
                ;;
            --help|-h)
                show_help
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

show_help() {
    echo "SalesAPI Manual Tests Runner"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --skip-auth         Skip authentication tests"
    echo "  --skip-health       Skip health check tests"
    echo "  --skip-products     Skip product management tests"
    echo "  --skip-orders       Skip order management tests"
    echo "  --skip-reservations Skip stock reservation tests"
    echo "  --skip-validation   Skip validation and edge case tests"
    echo "  --skip-concurrency  Skip concurrency tests"
    echo "  --skip-prerequisites Skip prerequisite checks"
    echo "  --only-auth         Run only authentication tests"
    echo "  --only-basic        Run only basic tests (auth, health, products, orders)"
    echo "  --help, -h          Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                      # Run all tests"
    echo "  $0 --only-basic         # Run only basic functionality tests"
    echo "  $0 --skip-concurrency   # Run all tests except concurrency tests"
}

check_services() {
    log_info "?? Checking service availability..."
    
    local all_services_up=true
    
    if ! check_service "Gateway" "$GATEWAY_URL/gateway/status"; then
        all_services_up=false
    fi
    
    if ! check_service "Inventory API" "$INVENTORY_URL/health"; then
        all_services_up=false
    fi
    
    if ! check_service "Sales API" "$SALES_URL/health"; then
        all_services_up=false
    fi
    
    if [ "$all_services_up" = false ]; then
        log_error "Some services are not running. Please start all services before running tests."
        log_info "Required services:"
        log_info "  - Gateway: $GATEWAY_URL"
        log_info "  - Inventory API: $INVENTORY_URL"
        log_info "  - Sales API: $SALES_URL"
        return 1
    fi
    
    log_success "All services are running"
    return 0
}

run_all_tests() {
    log_info "?? Starting SalesAPI Manual Tests Suite"
    log_info "============================================"
    log_info "Timestamp: $(date)"
    log_info "Test configuration:"
    log_info "  - Gateway URL: $GATEWAY_URL"
    log_info "  - Inventory URL: $INVENTORY_URL"
    log_info "  - Sales URL: $SALES_URL"
    log_info "============================================"
    echo
    
    local start_time=$(date +%s)
    
    # Global variables for sharing data between tests
    export ADMIN_TOKEN=""
    export CUSTOMER_TOKEN=""
    export PRODUCT_ID=""
    export MOUSE_ID=""
    export WORKSTATION_ID=""
    export ORDER_ID=""
    export MULTI_ORDER_ID=""
    export RESERVATION_ORDER_ID=""
    export RESERVATION_ID=""
    
    # Run test categories in logical order
    if [ "$RUN_AUTHENTICATION" = true ]; then
        source "$SCRIPT_DIR/01_authentication_tests.sh"
        run_authentication_tests
    fi
    
    if [ "$RUN_HEALTH" = true ]; then
        source "$SCRIPT_DIR/02_health_tests.sh"
        run_health_tests
    fi
    
    if [ "$RUN_PRODUCTS" = true ]; then
        source "$SCRIPT_DIR/03_products_tests.sh"
        run_product_tests
    fi
    
    if [ "$RUN_ORDERS" = true ]; then
        source "$SCRIPT_DIR/04_orders_tests.sh"
        run_order_tests
    fi
    
    if [ "$RUN_RESERVATIONS" = true ]; then
        source "$SCRIPT_DIR/05_reservations_tests.sh"
        run_reservation_tests
    fi
    
    if [ "$RUN_VALIDATION" = true ]; then
        source "$SCRIPT_DIR/06_validation_tests.sh"
        run_validation_tests
    fi
    
    if [ "$RUN_CONCURRENCY" = true ]; then
        source "$SCRIPT_DIR/07_concurrency_tests.sh"
        run_concurrency_tests
    fi
    
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    echo
    log_info "============================================"
    log_info "SalesAPI Manual Tests Suite Completed"
    log_info "============================================"
    log_info "Total execution time: ${duration} seconds"
    
    # Generate final summary
    if generate_summary; then
        log_success "All tests completed successfully!"
        return 0
    else
        log_error "Some tests failed. Check the detailed logs for more information."
        return 1
    fi
}

main() {
    # Parse command line arguments
    parse_arguments "$@"
    
    # Check prerequisites
    if [ "$SKIP_PREREQUISITES" = false ]; then
        if ! check_prerequisites; then
            log_error "Prerequisites check failed"
            exit 1
        fi
        
        if ! check_services; then
            log_error "Service availability check failed"
            exit 1
        fi
    fi
    
    # Run the test suite
    if run_all_tests; then
        log_success "?? Test suite completed successfully!"
        exit 0
    else
        log_error "? Test suite completed with failures."
        exit 1
    fi
}

# Run main function if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    main "$@"
fi