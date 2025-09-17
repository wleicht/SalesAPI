#!/bin/bash

# SalesAPI Manual Tests - Test Manager
# Provides easy management and execution of test suites

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m'

show_banner() {
    echo -e "${BLUE}"
    echo "??????????????????????????????????"
    echo "??????????????????????????????????"
    echo "   ???   ??????  ????????   ???   "
    echo "   ???   ??????  ????????   ???   "
    echo "   ???   ????????????????   ???   "
    echo "   ???   ????????????????   ???   "
    echo
    echo "   SalesAPI Manual Test Manager"
    echo -e "${NC}"
}

show_main_menu() {
    clear
    show_banner
    echo -e "${CYAN}========================================${NC}"
    echo -e "${YELLOW}           Test Categories${NC}"
    echo -e "${CYAN}========================================${NC}"
    echo
    echo -e "${GREEN}Quick Start Options:${NC}"
    echo "  ${PURPLE}d)${NC} Demo Test (Quick validation)"
    echo "  ${PURPLE}q)${NC} Quick Tests (Auth + Health + Basic)"
    echo "  ${PURPLE}a)${NC} All Tests (Complete test suite)"
    echo
    echo -e "${GREEN}Individual Test Categories:${NC}"
    echo "  ${PURPLE}1)${NC} Authentication Tests"
    echo "  ${PURPLE}2)${NC} Health Check Tests"
    echo "  ${PURPLE}3)${NC} Product Management Tests"
    echo "  ${PURPLE}4)${NC} Order Management Tests"
    echo "  ${PURPLE}5)${NC} Stock Reservation Tests"
    echo "  ${PURPLE}6)${NC} Validation & Edge Case Tests"
    echo "  ${PURPLE}7)${NC} Concurrency Tests"
    echo
    echo -e "${GREEN}Utilities:${NC}"
    echo "  ${PURPLE}s)${NC} System Status Check"
    echo "  ${PURPLE}r)${NC} View Recent Test Results"
    echo "  ${PURPLE}c)${NC} Clean Test Data"
    echo "  ${PURPLE}h)${NC} Help & Documentation"
    echo
    echo -e "${GREEN}Interactive Mode:${NC}"
    echo "  ${PURPLE}i)${NC} Interactive Test Runner"
    echo
    echo "  ${PURPLE}0)${NC} Exit"
    echo
    echo -e "${CYAN}========================================${NC}"
}

check_services_status() {
    echo -e "${YELLOW}Checking system status...${NC}"
    echo
    
    local gateway_status="?"
    local inventory_status="?"
    local sales_status="?"
    
    # Check Gateway
    if curl -s --max-time 3 http://localhost:6000/gateway/status > /dev/null; then
        gateway_status="?"
    fi
    
    # Check Inventory API
    if curl -s --max-time 3 http://localhost:5000/health > /dev/null; then
        inventory_status="?"
    fi
    
    # Check Sales API
    if curl -s --max-time 3 http://localhost:5001/health > /dev/null; then
        sales_status="?"
    fi
    
    echo -e "${BLUE}Service Status:${NC}"
    echo "  Gateway (port 6000):      $gateway_status"
    echo "  Inventory API (port 5000): $inventory_status"
    echo "  Sales API (port 5001):     $sales_status"
    echo
    
    if [[ "$gateway_status" == "?" && "$inventory_status" == "?" && "$sales_status" == "?" ]]; then
        echo -e "${GREEN}? All services are running!${NC}"
        return 0
    else
        echo -e "${RED}??  Some services are not running${NC}"
        echo
        echo -e "${YELLOW}To start services:${NC}"
        echo "  cd src/gateway && dotnet run &"
        echo "  cd src/inventory.api && dotnet run &"
        echo "  cd src/sales.api && dotnet run &"
        echo
        echo -e "${YELLOW}Or using Docker:${NC}"
        echo "  docker-compose up -d"
        return 1
    fi
}

run_test_category() {
    local category="$1"
    local script="$2"
    local description="$3"
    
    echo
    echo -e "${BLUE}Running $description...${NC}"
    echo -e "${CYAN}========================================${NC}"
    
    if [ -f "$SCRIPT_DIR/$script" ]; then
        bash "$SCRIPT_DIR/$script"
        local result=$?
        
        echo
        if [ $result -eq 0 ]; then
            echo -e "${GREEN}? $description completed successfully${NC}"
        else
            echo -e "${RED}? $description completed with failures${NC}"
        fi
        
        echo
        echo "Press Enter to continue..."
        read -r
        return $result
    else
        echo -e "${RED}? Test script not found: $script${NC}"
        echo "Press Enter to continue..."
        read -r
        return 1
    fi
}

view_recent_results() {
    echo
    echo -e "${BLUE}Recent Test Results:${NC}"
    echo -e "${CYAN}========================================${NC}"
    
    local results_dir="$SCRIPT_DIR/results"
    
    if [ ! -d "$results_dir" ]; then
        echo -e "${YELLOW}No test results directory found.${NC}"
        echo "Run some tests first to generate results."
        echo
        echo "Press Enter to continue..."
        read -r
        return
    fi
    
    # Show latest summary
    local latest_summary=$(ls -t "$results_dir"/test_summary_*.txt 2>/dev/null | head -1)
    
    if [ -f "$latest_summary" ]; then
        echo -e "${GREEN}Latest Test Summary:${NC}"
        echo
        cat "$latest_summary"
        echo
    else
        echo -e "${YELLOW}No test summary found.${NC}"
    fi
    
    # Show available result files
    echo -e "${BLUE}Available Result Files:${NC}"
    echo
    
    local count=0
    for file in "$results_dir"/test_summary_*.txt; do
        if [ -f "$file" ]; then
            local timestamp=$(basename "$file" .txt | sed 's/test_summary_//')
            local readable_date=$(echo "$timestamp" | sed 's/_/ /' | sed 's/\(.*\) \(.*\)/\1 \2:/' | sed 's/\(..\)\(..\)\(..\)/\1:\2:\3/')
            echo "  $readable_date - $(basename "$file")"
            count=$((count + 1))
        fi
    done
    
    if [ $count -eq 0 ]; then
        echo "  No result files found."
    fi
    
    echo
    echo "Press Enter to continue..."
    read -r
}

clean_test_data() {
    echo
    echo -e "${YELLOW}Cleaning test data...${NC}"
    echo
    
    # This is a placeholder for actual cleanup logic
    echo "Test data cleanup options:"
    echo
    echo "1) Clear result files"
    echo "2) Reset test databases (if applicable)"
    echo "3) Remove test products (requires admin credentials)"
    echo "0) Cancel"
    echo
    
    read -p "Select cleanup option [0-3]: " cleanup_choice
    
    case $cleanup_choice in
        1)
            if [ -d "$SCRIPT_DIR/results" ]; then
                read -p "Are you sure you want to delete all test result files? (y/N): " confirm
                if [[ "$confirm" =~ ^[Yy] ]]; then
                    rm -f "$SCRIPT_DIR/results"/*
                    echo -e "${GREEN}? Test result files cleared${NC}"
                else
                    echo "Cleanup cancelled."
                fi
            else
                echo "No result files to clean."
            fi
            ;;
        2)
            echo -e "${YELLOW}Database cleanup would require specific implementation${NC}"
            echo "This feature is not yet implemented."
            ;;
        3)
            echo -e "${YELLOW}Product cleanup would require admin authentication${NC}"
            echo "This feature could be added to run a cleanup script."
            ;;
        0|*)
            echo "Cleanup cancelled."
            ;;
    esac
    
    echo
    echo "Press Enter to continue..."
    read -r
}

show_help() {
    echo
    echo -e "${BLUE}SalesAPI Manual Tests Help${NC}"
    echo -e "${CYAN}========================================${NC}"
    echo
    echo -e "${GREEN}Documentation Files:${NC}"
    echo "  ?? README.md                 - Main documentation"
    echo "  ?? troubleshooting.md        - Common issues and solutions"
    echo "  ? validation_checklist.md   - Complete validation checklist"
    echo "  ?? curl_examples.md          - Manual cURL commands"
    echo
    echo -e "${GREEN}Test Scripts:${NC}"
    echo "  ?? demo_tests.sh             - Quick validation demo"
    echo "  ?? 01_authentication_tests.sh - Authentication tests"
    echo "  ?? 02_health_tests.sh         - Health check tests"
    echo "  ?? 03_products_tests.sh       - Product management tests"
    echo "  ?? 04_orders_tests.sh         - Order management tests"
    echo "  ?? 05_reservations_tests.sh   - Stock reservation tests"
    echo "  ?? 06_validation_tests.sh     - Validation and edge cases"
    echo "  ?? 07_concurrency_tests.sh    - Concurrency tests"
    echo
    echo -e "${GREEN}Usage Tips:${NC}"
    echo "  • Start with the Demo Test to validate basic functionality"
    echo "  • Ensure all services are running before testing"
    echo "  • Check troubleshooting.md for common issues"
    echo "  • Use Interactive Mode for guided testing"
    echo
    echo -e "${GREEN}Prerequisites:${NC}"
    echo "  • All SalesAPI services running (Gateway, Inventory, Sales)"
    echo "  • curl command available"
    echo "  • jq recommended for JSON processing"
    echo "  • Network access to localhost ports 5000, 5001, 6000"
    echo
    echo "Press Enter to continue..."
    read -r
}

main_loop() {
    while true; do
        show_main_menu
        read -p "Select an option: " choice
        
        case $choice in
            d|D)
                run_test_category "Demo" "demo_tests.sh" "Demo Test"
                ;;
            q|Q)
                echo
                echo -e "${BLUE}Running Quick Tests...${NC}"
                bash "$SCRIPT_DIR/run_manual_tests.sh" --only-basic
                echo "Press Enter to continue..."
                read -r
                ;;
            a|A)
                echo
                echo -e "${BLUE}Running All Tests...${NC}"
                bash "$SCRIPT_DIR/run_manual_tests.sh"
                echo "Press Enter to continue..."
                read -r
                ;;
            1)
                run_test_category "Authentication" "01_authentication_tests.sh" "Authentication Tests"
                ;;
            2)
                run_test_category "Health" "02_health_tests.sh" "Health Check Tests"
                ;;
            3)
                run_test_category "Products" "03_products_tests.sh" "Product Management Tests"
                ;;
            4)
                run_test_category "Orders" "04_orders_tests.sh" "Order Management Tests"
                ;;
            5)
                run_test_category "Reservations" "05_reservations_tests.sh" "Stock Reservation Tests"
                ;;
            6)
                run_test_category "Validation" "06_validation_tests.sh" "Validation Tests"
                ;;
            7)
                run_test_category "Concurrency" "07_concurrency_tests.sh" "Concurrency Tests"
                ;;
            s|S)
                check_services_status
                echo
                echo "Press Enter to continue..."
                read -r
                ;;
            r|R)
                view_recent_results
                ;;
            c|C)
                clean_test_data
                ;;
            h|H)
                show_help
                ;;
            i|I)
                if [ -f "$SCRIPT_DIR/interactive_tests.sh" ]; then
                    bash "$SCRIPT_DIR/interactive_tests.sh"
                else
                    echo "Interactive test runner not found."
                    echo "Press Enter to continue..."
                    read -r
                fi
                ;;
            0)
                echo
                echo -e "${GREEN}Thank you for using SalesAPI Manual Tests!${NC}"
                echo
                exit 0
                ;;
            *)
                echo
                echo -e "${RED}Invalid option. Please try again.${NC}"
                sleep 1
                ;;
        esac
    done
}

# Run main loop if script is executed directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    main_loop
fi