#!/bin/bash

# SalesAPI Manual Tests - Quick Start Script
# Sets up the testing environment and runs initial tests

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}"
echo "=========================================="
echo "   SalesAPI Manual Tests - Quick Start"
echo "=========================================="
echo -e "${NC}"

# Make all scripts executable (Linux/macOS)
if [[ "$OSTYPE" != "msys" && "$OSTYPE" != "win32" ]]; then
    echo -e "${YELLOW}Making scripts executable...${NC}"
    chmod +x "$SCRIPT_DIR"/*.sh
    chmod +x "$SCRIPT_DIR"/utils/*.sh
fi

# Create results directory
echo -e "${YELLOW}Setting up results directory...${NC}"
mkdir -p "$SCRIPT_DIR/results"

# Check if running on Windows
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    echo -e "${BLUE}Windows environment detected.${NC}"
    echo -e "${YELLOW}To run tests, use PowerShell:${NC}"
    echo "  .\scripts\manual-tests\run_manual_tests.ps1"
    echo ""
    echo -e "${YELLOW}Or use Git Bash/WSL for bash scripts:${NC}"
    echo "  ./scripts/manual-tests/run_manual_tests.sh"
else
    echo -e "${BLUE}Unix-like environment detected.${NC}"
    
    # Check for required tools
    echo -e "${YELLOW}Checking prerequisites...${NC}"
    
    if command -v curl > /dev/null; then
        echo -e "${GREEN}? curl is available${NC}"
    else
        echo -e "${RED}? curl is required but not installed${NC}"
        echo "Please install curl first."
        exit 1
    fi
    
    if command -v jq > /dev/null; then
        echo -e "${GREEN}? jq is available${NC}"
    else
        echo -e "${YELLOW}? jq is recommended but not required${NC}"
        echo "Install jq for better JSON processing:"
        echo "  - Ubuntu/Debian: sudo apt-get install jq"
        echo "  - macOS: brew install jq"
        echo "  - CentOS/RHEL: sudo yum install jq"
    fi
fi

echo ""
echo -e "${BLUE}Available Commands:${NC}"
echo ""
echo -e "${GREEN}Quick Test Options:${NC}"
echo "  ./scripts/manual-tests/run_manual_tests.sh              # Run all tests"
echo "  ./scripts/manual-tests/run_manual_tests.sh --only-basic # Run basic tests only"
echo "  ./scripts/manual-tests/interactive_tests.sh             # Interactive menu"
echo ""
echo -e "${GREEN}Individual Test Categories:${NC}"
echo "  ./scripts/manual-tests/01_authentication_tests.sh       # Authentication"
echo "  ./scripts/manual-tests/02_health_tests.sh               # Health checks"
echo "  ./scripts/manual-tests/03_products_tests.sh             # Product management"
echo "  ./scripts/manual-tests/04_orders_tests.sh               # Order management"
echo "  ./scripts/manual-tests/05_reservations_tests.sh         # Stock reservations"
echo "  ./scripts/manual-tests/06_validation_tests.sh           # Validation tests"
echo "  ./scripts/manual-tests/07_concurrency_tests.sh          # Concurrency tests"
echo ""

# Check if services are likely running
echo -e "${YELLOW}Checking if services might be running...${NC}"

check_port() {
    local port=$1
    local service=$2
    
    if command -v netstat > /dev/null; then
        if netstat -an 2>/dev/null | grep -q ":$port"; then
            echo -e "${GREEN}? Port $port is in use (likely $service)${NC}"
            return 0
        else
            echo -e "${RED}? Port $port is not in use ($service)${NC}"
            return 1
        fi
    else
        echo -e "${YELLOW}? Cannot check port $port (netstat not available)${NC}"
        return 1
    fi
}

services_running=0

if check_port 6000 "Gateway"; then ((services_running++)); fi
if check_port 5000 "Inventory API"; then ((services_running++)); fi  
if check_port 5001 "Sales API"; then ((services_running++)); fi

echo ""
if [ $services_running -eq 3 ]; then
    echo -e "${GREEN}? All services appear to be running!${NC}"
    echo -e "${BLUE}Ready to run tests!${NC}"
    echo ""
    echo -e "${YELLOW}Quick start:${NC}"
    echo "  ./scripts/manual-tests/interactive_tests.sh"
elif [ $services_running -gt 0 ]; then
    echo -e "${YELLOW}? Some services are running ($services_running/3)${NC}"
    echo -e "${YELLOW}Please ensure all services are started before running tests.${NC}"
else
    echo -e "${RED}? No services appear to be running${NC}"
    echo ""
    echo -e "${YELLOW}To start services:${NC}"
    echo "  cd src/gateway && dotnet run &"
    echo "  cd src/inventory.api && dotnet run &"
    echo "  cd src/sales.api && dotnet run &"
    echo ""
    echo -e "${YELLOW}Or using Docker:${NC}"
    echo "  docker-compose up -d"
fi

echo ""
echo -e "${BLUE}For help and troubleshooting:${NC}"
echo "  - Read: scripts/manual-tests/README.md"
echo "  - Troubleshooting: scripts/manual-tests/troubleshooting.md"
echo "  - Validation checklist: scripts/manual-tests/validation_checklist.md"
echo ""
echo -e "${GREEN}Happy testing! ??${NC}"