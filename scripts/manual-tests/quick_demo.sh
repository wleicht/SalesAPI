#!/bin/bash

# SalesAPI Manual Tests - Quick Demo
# Simple demonstration that can be run to show the system working

echo "?? SalesAPI Quick Demo"
echo "====================="
echo
echo "This is a simple demonstration of the SalesAPI testing framework."
echo "It will show you key commands and expected outputs."
echo

# Check if basic tools are available
echo "?? Checking prerequisites..."
if command -v curl > /dev/null; then
    echo "? curl is available"
else
    echo "? curl is required but not installed"
    exit 1
fi

if command -v jq > /dev/null; then
    echo "? jq is available for JSON formatting"
    HAS_JQ=true
else
    echo "?? jq not available - JSON will be raw"
    HAS_JQ=false
fi

echo

# Test 1: Check if services are running
echo "?? Testing service availability..."
echo

GATEWAY_URL="http://localhost:6000"
INVENTORY_URL="http://localhost:5000"
SALES_URL="http://localhost:5001"

services_up=0

echo "Testing Gateway (port 6000)..."
if curl -s --max-time 3 "$GATEWAY_URL/gateway/status" > /dev/null; then
    echo "? Gateway is responding"
    ((services_up++))
else
    echo "? Gateway is not responding"
fi

echo "Testing Inventory API (port 5000)..."
if curl -s --max-time 3 "$INVENTORY_URL/health" > /dev/null; then
    echo "? Inventory API is responding"
    ((services_up++))
else
    echo "? Inventory API is not responding"
fi

echo "Testing Sales API (port 5001)..."
if curl -s --max-time 3 "$SALES_URL/health" > /dev/null; then
    echo "? Sales API is responding"
    ((services_up++))
else
    echo "? Sales API is not responding"
fi

echo

if [ $services_up -eq 3 ]; then
    echo "?? All services are running! Proceeding with live demo..."
    LIVE_DEMO=true
else
    echo "?? Only $services_up/3 services are running."
    echo "   This demo will show you what the commands look like,"
    echo "   but won't execute them against real services."
    LIVE_DEMO=false
fi

echo
echo "Press Enter to continue..."
read

# Test 2: Show key commands
echo "?? Key Testing Commands"
echo "======================="
echo

echo "1. Gateway Status Check:"
echo "   curl -X GET $GATEWAY_URL/gateway/status"
echo

if [ "$LIVE_DEMO" = true ]; then
    echo "   Result:"
    if [ "$HAS_JQ" = true ]; then
        curl -s "$GATEWAY_URL/gateway/status" | jq . 2>/dev/null || curl -s "$GATEWAY_URL/gateway/status"
    else
        curl -s "$GATEWAY_URL/gateway/status"
    fi
    echo
fi

echo "2. Get Test Users:"
echo "   curl -X GET $GATEWAY_URL/auth/test-users"
echo

if [ "$LIVE_DEMO" = true ]; then
    echo "   Result:"
    if [ "$HAS_JQ" = true ]; then
        curl -s "$GATEWAY_URL/auth/test-users" | jq . 2>/dev/null || curl -s "$GATEWAY_URL/auth/test-users"
    else
        curl -s "$GATEWAY_URL/auth/test-users"
    fi
    echo
fi

echo "3. Authenticate as Admin:"
echo '   curl -X POST $GATEWAY_URL/auth/token \'
echo '     -H "Content-Type: application/json" \'
echo '     -d '"'"'{"username":"admin","password":"admin123"}'"'"
echo

if [ "$LIVE_DEMO" = true ]; then
    echo "   Result:"
    response=$(curl -s -X POST "$GATEWAY_URL/auth/token" \
        -H "Content-Type: application/json" \
        -d '{"username":"admin","password":"admin123"}')
    
    if [ "$HAS_JQ" = true ]; then
        echo "$response" | jq . 2>/dev/null || echo "$response"
        TOKEN=$(echo "$response" | jq -r '.accessToken' 2>/dev/null)
    else
        echo "$response"
        TOKEN=$(echo "$response" | grep -o '"accessToken":"[^"]*"' | sed 's/"accessToken":"//;s/"//')
    fi
    echo
fi

echo "4. List Products (Public Access):"
echo "   curl -X GET $GATEWAY_URL/inventory/products"
echo

if [ "$LIVE_DEMO" = true ]; then
    echo "   Result:"
    if [ "$HAS_JQ" = true ]; then
        curl -s "$GATEWAY_URL/inventory/products" | jq . 2>/dev/null || curl -s "$GATEWAY_URL/inventory/products"
    else
        curl -s "$GATEWAY_URL/inventory/products"
    fi
    echo
fi

if [ "$LIVE_DEMO" = true ] && [ -n "$TOKEN" ] && [ "$TOKEN" != "null" ]; then
    echo "5. Create a Test Product (Admin Required):"
    echo '   curl -X POST $GATEWAY_URL/inventory/products \'
    echo '     -H "Authorization: Bearer $TOKEN" \'
    echo '     -H "Content-Type: application/json" \'
    echo '     -d '"'"'{"name":"Demo Product","description":"Created by demo","price":99.99,"stockQuantity":5}'"'"
    echo
    
    echo "   Result:"
    product_response=$(curl -s -X POST "$GATEWAY_URL/inventory/products" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"name":"Demo Product","description":"Created by demo","price":99.99,"stockQuantity":5}')
    
    if [ "$HAS_JQ" = true ]; then
        echo "$product_response" | jq . 2>/dev/null || echo "$product_response"
        PRODUCT_ID=$(echo "$product_response" | jq -r '.id' 2>/dev/null)
    else
        echo "$product_response"
        PRODUCT_ID=$(echo "$product_response" | grep -o '"id":"[^"]*"' | sed 's/"id":"//;s/"//')
    fi
    echo
fi

echo "Press Enter to continue..."
read

echo "?? Available Test Categories"
echo "============================"
echo

echo "The full test framework includes these categories:"
echo
echo "?? Authentication Tests    - JWT login, roles, security"
echo "?? Health Check Tests     - Service availability, performance"  
echo "?? Product Tests          - CRUD operations, validation"
echo "?? Order Tests            - Order processing, payment simulation"
echo "?? Stock Reservation Tests - Inventory management, concurrency"
echo "?? Validation Tests       - Input validation, edge cases"
echo "?? Concurrency Tests      - Race conditions, stress testing"
echo

echo "?? How to Run Full Tests"
echo "========================"
echo

echo "1. Quick Demo (what you just saw):"
echo "   ./scripts/manual-tests/quick_demo.sh"
echo

echo "2. Interactive Test Manager:"
echo "   ./scripts/manual-tests/test_manager.sh"
echo

echo "3. Complete Test Suite:"
echo "   ./scripts/manual-tests/run_manual_tests.sh"
echo

echo "4. Individual Test Categories:"
echo "   ./scripts/manual-tests/01_authentication_tests.sh"
echo "   ./scripts/manual-tests/02_health_tests.sh"
echo "   # ... and so on"
echo

echo "5. Manual cURL Commands:"
echo "   See: scripts/manual-tests/curl_examples.md"
echo

echo "?? Documentation"
echo "================="
echo

echo "?? Main Guide:           scripts/manual-tests/README.md"
echo "?? Troubleshooting:      scripts/manual-tests/troubleshooting.md" 
echo "? Validation Checklist: scripts/manual-tests/validation_checklist.md"
echo "?? cURL Examples:        scripts/manual-tests/curl_examples.md"
echo

if [ "$LIVE_DEMO" = true ]; then
    echo "?? Your SalesAPI system is running and ready for comprehensive testing!"
else
    echo "??  Start your services to see live testing:"
    echo "   cd src/gateway && dotnet run &"
    echo "   cd src/inventory.api && dotnet run &" 
    echo "   cd src/sales.api && dotnet run &"
fi

echo
echo "Happy testing! ??"