#!/bin/bash

# SalesAPI Manual Tests - Interactive Test Runner
# Provides a menu-driven interface for running specific tests

# Load utilities
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/utils/test_utils.sh"

show_main_menu() {
    clear
    echo "============================================"
    echo "   SalesAPI Manual Tests - Interactive"
    echo "============================================"
    echo
    echo "Available Test Categories:"
    echo
    echo "1) Authentication Tests"
    echo "2) Health Check Tests" 
    echo "3) Product Management Tests"
    echo "4) Order Management Tests"
    echo "5) Stock Reservation Tests"
    echo "6) Validation & Edge Cases Tests"
    echo "7) Concurrency Tests"
    echo
    echo "8) Run All Tests"
    echo "9) Run Basic Tests Only (1-4)"
    echo
    echo "0) Exit"
    echo
    echo "============================================"
}

show_results_menu() {
    echo
    echo "Test Results Options:"
    echo
    echo "1) View Summary"
    echo "2) View Failed Tests"
    echo "3) View Latest Results File"
    echo "4) Return to Main Menu"
    echo
}

run_single_test_category() {
    local category="$1"
    local script_name="$2"
    
    echo
    log_info "Running $category..."
    echo
    
    if [ -f "$SCRIPT_DIR/$script_name" ]; then
        source "$SCRIPT_DIR/$script_name"
        
        case $category in
            "Authentication Tests")
                run_authentication_tests
                ;;
            "Health Check Tests")
                run_health_tests
                ;;
            "Product Management Tests")
                run_product_tests
                ;;
            "Order Management Tests")
                run_order_tests
                ;;
            "Stock Reservation Tests")
                run_reservation_tests
                ;;
            "Validation & Edge Cases Tests")
                run_validation_tests
                ;;
            "Concurrency Tests")
                run_concurrency_tests
                ;;
        esac
        
        echo
        log_info "Test category completed. Press Enter to continue..."
        read -r
    else
        log_error "Test script not found: $script_name"
        echo "Press Enter to continue..."
        read -r
    fi
}

run_all_tests_interactive() {
    echo
    log_info "Running All Tests..."
    echo
    
    if [ -f "$SCRIPT_DIR/run_manual_tests.sh" ]; then
        bash "$SCRIPT_DIR/run_manual_tests.sh"
    else
        log_error "Main test runner script not found"
    fi
    
    echo
    log_info "All tests completed. Press Enter to continue..."
    read -r
}

run_basic_tests() {
    echo
    log_info "Running Basic Tests (Authentication, Health, Products, Orders)..."
    echo
    
    if [ -f "$SCRIPT_DIR/run_manual_tests.sh" ]; then
        bash "$SCRIPT_DIR/run_manual_tests.sh" --only-basic
    else
        log_error "Main test runner script not found"
    fi
    
    echo
    log_info "Basic tests completed. Press Enter to continue..."
    read -r
}

view_summary() {
    local latest_summary=$(ls -t "$SCRIPT_DIR/results/test_summary_"*.txt 2>/dev/null | head -1)
    
    if [ -f "$latest_summary" ]; then
        echo
        log_info "Latest Test Summary:"
        echo "============================================"
        cat "$latest_summary"
        echo "============================================"
    else
        log_warning "No test summary found. Run some tests first."
    fi
    
    echo
    echo "Press Enter to continue..."
    read -r
}

view_failed_tests() {
    local failed_log="$SCRIPT_DIR/results/failed_tests.log"
    
    if [ -f "$failed_log" ] && [ -s "$failed_log" ]; then
        echo
        log_info "Failed Tests Log:"
        echo "============================================"
        cat "$failed_log"
        echo "============================================"
    else
        log_info "No failed tests found. Great job!"
    fi
    
    echo
    echo "Press Enter to continue..."
    read -r
}

view_latest_results() {
    local latest_results=$(ls -t "$SCRIPT_DIR/results/test_results_"*.json 2>/dev/null | head -1)
    
    if [ -f "$latest_results" ]; then
        echo
        log_info "Latest Test Results File: $latest_results"
        
        if command -v jq > /dev/null; then
            echo
            log_info "Test Results Summary:"
            echo "============================================"
            jq -r '.testRun | "Timestamp: " + .timestamp, "Total Tests: " + (.tests | length | tostring), "Status Summary:", (.tests | group_by(.status) | .[] | .[0].status + ": " + (length | tostring))' "$latest_results" 2>/dev/null || cat "$latest_results"
            echo "============================================"
        else
            echo
            log_info "Raw JSON content (install jq for better formatting):"
            echo "============================================"
            cat "$latest_results"
            echo "============================================"
        fi
    else
        log_warning "No test results found. Run some tests first."
    fi
    
    echo
    echo "Press Enter to continue..."
    read -r
}

handle_results_menu() {
    while true; do
        show_results_menu
        read -p "Select an option [1-4]: " results_choice
        
        case $results_choice in
            1)
                view_summary
                ;;
            2)
                view_failed_tests
                ;;
            3)
                view_latest_results
                ;;
            4)
                break
                ;;
            *)
                log_warning "Invalid option. Please try again."
                sleep 1
                ;;
        esac
    done
}

main_interactive() {
    # Check prerequisites
    if ! check_prerequisites; then
        log_error "Prerequisites check failed"
        exit 1
    fi
    
    # Main menu loop
    while true; do
        show_main_menu
        read -p "Select an option [0-9]: " choice
        
        case $choice in
            1)
                run_single_test_category "Authentication Tests" "01_authentication_tests.sh"
                ;;
            2)
                run_single_test_category "Health Check Tests" "02_health_tests.sh"
                ;;
            3)
                run_single_test_category "Product Management Tests" "03_products_tests.sh"
                ;;
            4)
                run_single_test_category "Order Management Tests" "04_orders_tests.sh"
                ;;
            5)
                run_single_test_category "Stock Reservation Tests" "05_reservations_tests.sh"
                ;;
            6)
                run_single_test_category "Validation & Edge Cases Tests" "06_validation_tests.sh"
                ;;
            7)
                run_single_test_category "Concurrency Tests" "07_concurrency_tests.sh"
                ;;
            8)
                run_all_tests_interactive
                handle_results_menu
                ;;
            9)
                run_basic_tests
                handle_results_menu
                ;;
            0)
                echo
                log_info "Exiting interactive test runner..."
                echo
                exit 0
                ;;
            *)
                log_warning "Invalid option. Please try again."
                sleep 1
                ;;
        esac
    done
}

# Run interactive mode if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    main_interactive
fi