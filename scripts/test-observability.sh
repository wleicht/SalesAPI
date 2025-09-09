#!/bin/bash

# Etapa 7 - Observability Testing Script
# Tests the complete system with correlation ID tracking and metrics collection

echo "?? Starting Etapa 7 - Observability Testing"
echo "============================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if services are running
check_services() {
    print_status "Checking if services are running..."
    
    if ! curl -s http://localhost:6000/health > /dev/null; then
        print_error "Gateway not responding at http://localhost:6000"
        return 1
    fi
    print_success "Gateway is healthy"
    
    if ! curl -s http://localhost:5000/health > /dev/null; then
        print_error "Inventory API not responding at http://localhost:5000"
        return 1
    fi
    print_success "Inventory API is healthy"
    
    if ! curl -s http://localhost:5001/health > /dev/null; then
        print_error "Sales API not responding at http://localhost:5001"
        return 1
    fi
    print_success "Sales API is healthy"
    
    return 0
}

# Test correlation ID propagation
test_correlation() {
    print_status "Testing correlation ID propagation..."
    
    # Generate a unique correlation ID for this test
    CORRELATION_ID="test-$(date +%s)-$(shuf -i 1000-9999 -n 1)"
    print_status "Using Correlation ID: $CORRELATION_ID"
    
    # Step 1: Get admin token
    print_status "Getting admin authentication token..."
    ADMIN_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
        -H "Content-Type: application/json" \
        -H "X-Correlation-Id: $CORRELATION_ID" \
        -d '{"username":"admin","password":"admin123"}' | \
        jq -r '.accessToken')
    
    if [ "$ADMIN_TOKEN" == "null" ] || [ -z "$ADMIN_TOKEN" ]; then
        print_error "Failed to get admin token"
        return 1
    fi
    print_success "Admin token obtained"
    
    # Step 2: Create a product with correlation ID
    print_status "Creating test product..."
    PRODUCT_RESPONSE=$(curl -s -X POST "http://localhost:6000/inventory/products" \
        -H "Authorization: Bearer $ADMIN_TOKEN" \
        -H "Content-Type: application/json" \
        -H "X-Correlation-Id: $CORRELATION_ID" \
        -d '{
            "name": "Observability Test Product",
            "description": "Product for testing correlation tracking",
            "price": 99.99,
            "stockQuantity": 50
        }')
    
    PRODUCT_ID=$(echo $PRODUCT_RESPONSE | jq -r '.id')
    if [ "$PRODUCT_ID" == "null" ] || [ -z "$PRODUCT_ID" ]; then
        print_error "Failed to create product"
        return 1
    fi
    print_success "Product created: $PRODUCT_ID"
    
    # Step 3: Get customer token
    print_status "Getting customer authentication token..."
    CUSTOMER_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
        -H "Content-Type: application/json" \
        -H "X-Correlation-Id: $CORRELATION_ID" \
        -d '{"username":"customer1","password":"password123"}' | \
        jq -r '.accessToken')
    
    if [ "$CUSTOMER_TOKEN" == "null" ] || [ -z "$CUSTOMER_TOKEN" ]; then
        print_error "Failed to get customer token"
        return 1
    fi
    print_success "Customer token obtained"
    
    # Step 4: Create order with correlation tracking
    print_status "Creating order to test full correlation flow..."
    ORDER_RESPONSE=$(curl -s -X POST "http://localhost:6000/sales/orders" \
        -H "Authorization: Bearer $CUSTOMER_TOKEN" \
        -H "Content-Type: application/json" \
        -H "X-Correlation-Id: $CORRELATION_ID" \
        -d "{
            \"customerId\": \"$(uuidgen)\",
            \"items\": [
                {
                    \"productId\": \"$PRODUCT_ID\",
                    \"quantity\": 3
                }
            ]
        }")
    
    ORDER_ID=$(echo $ORDER_RESPONSE | jq -r '.id')
    ORDER_STATUS=$(echo $ORDER_RESPONSE | jq -r '.status')
    
    if [ "$ORDER_ID" == "null" ] || [ -z "$ORDER_ID" ]; then
        print_warning "Order creation may have failed - checking response"
        echo "$ORDER_RESPONSE" | jq .
        return 1
    fi
    
    print_success "Order created: $ORDER_ID with status: $ORDER_STATUS"
    
    # Step 5: Check stock reservations with correlation
    print_status "Checking stock reservations..."
    sleep 2  # Give time for processing
    
    RESERVATIONS_RESPONSE=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" \
        -H "X-Correlation-Id: $CORRELATION_ID" \
        "http://localhost:6000/inventory/api/stockreservations/order/$ORDER_ID")
    
    RESERVATION_COUNT=$(echo $RESERVATIONS_RESPONSE | jq '. | length')
    if [ "$RESERVATION_COUNT" -gt 0 ]; then
        print_success "Found $RESERVATION_COUNT stock reservations for order"
    else
        print_warning "No stock reservations found yet (may still be processing)"
    fi
    
    print_success "? Correlation ID test completed: $CORRELATION_ID"
    echo ""
    print_status "?? Check your service logs for the correlation ID: $CORRELATION_ID"
    print_status "   You should see the same correlation ID across Gateway ? Sales ? Inventory"
    echo ""
}

# Test metrics endpoints
test_metrics() {
    print_status "Testing Prometheus metrics endpoints..."
    
    # Test Gateway metrics
    if curl -s http://localhost:6000/metrics | head -n 5 > /dev/null; then
        print_success "Gateway metrics endpoint accessible"
    else
        print_error "Gateway metrics endpoint not accessible"
    fi
    
    # Test Inventory metrics
    if curl -s http://localhost:5000/metrics | head -n 5 > /dev/null; then
        print_success "Inventory API metrics endpoint accessible"
    else
        print_error "Inventory API metrics endpoint not accessible"
    fi
    
    # Test Sales metrics
    if curl -s http://localhost:5001/metrics | head -n 5 > /dev/null; then
        print_success "Sales API metrics endpoint accessible"
    else
        print_error "Sales API metrics endpoint not accessible"
    fi
}

# Test observability with concurrent orders
test_concurrent_observability() {
    print_status "Testing observability with concurrent orders..."
    
    # Generate correlation IDs for concurrent tests
    CORRELATION_BASE="concurrent-$(date +%s)"
    
    # Get tokens
    ADMIN_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
        -H "Content-Type: application/json" \
        -d '{"username":"admin","password":"admin123"}' | \
        jq -r '.accessToken')
    
    CUSTOMER_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
        -H "Content-Type: application/json" \
        -d '{"username":"customer1","password":"password123"}' | \
        jq -r '.accessToken')
    
    # Create product for concurrent testing
    PRODUCT_RESPONSE=$(curl -s -X POST "http://localhost:6000/inventory/products" \
        -H "Authorization: Bearer $ADMIN_TOKEN" \
        -H "Content-Type: application/json" \
        -d '{
            "name": "Concurrent Test Product",
            "description": "Product for testing concurrent observability",
            "price": 49.99,
            "stockQuantity": 10
        }')
    
    PRODUCT_ID=$(echo $PRODUCT_RESPONSE | jq -r '.id')
    
    # Launch concurrent orders with different correlation IDs
    print_status "Launching 3 concurrent orders with different correlation IDs..."
    
    for i in {1..3}; do
        CORRELATION_ID="$CORRELATION_BASE-$i"
        print_status "Launching order $i with correlation: $CORRELATION_ID"
        
        curl -s -X POST "http://localhost:6000/sales/orders" \
            -H "Authorization: Bearer $CUSTOMER_TOKEN" \
            -H "Content-Type: application/json" \
            -H "X-Correlation-Id: $CORRELATION_ID" \
            -d "{
                \"customerId\": \"$(uuidgen)\",
                \"items\": [
                    {
                        \"productId\": \"$PRODUCT_ID\",
                        \"quantity\": 2
                    }
                ]
            }" > /dev/null &
    done
    
    wait  # Wait for all background jobs to complete
    print_success "Concurrent orders launched - check logs for different correlation IDs"
    echo ""
    print_status "?? Look for correlation IDs: $CORRELATION_BASE-1, $CORRELATION_BASE-2, $CORRELATION_BASE-3"
    echo ""
}

# Main execution
main() {
    echo "?? Etapa 7 - Observability Testing Started"
    echo "==========================================="
    echo ""
    
    # Check prerequisites
    if ! command -v curl &> /dev/null; then
        print_error "curl is required but not installed"
        exit 1
    fi
    
    if ! command -v jq &> /dev/null; then
        print_error "jq is required but not installed"
        exit 1
    fi
    
    # Run tests
    if check_services; then
        echo ""
        test_correlation
        echo ""
        test_metrics  
        echo ""
        test_concurrent_observability
        echo ""
        
        print_success "?? Observability testing completed!"
        echo ""
        print_status "?? Next steps:"
        print_status "   1. Check service logs for correlation IDs"
        print_status "   2. Visit http://localhost:6000/metrics for Gateway metrics"
        print_status "   3. Visit http://localhost:5000/metrics for Inventory metrics"
        print_status "   4. Visit http://localhost:5001/metrics for Sales metrics"
        print_status "   5. Run: docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d"
        print_status "   6. Visit http://localhost:9090 for Prometheus metrics collection"
        echo ""
        print_success "? Etapa 7 - Observability implementation is working!"
    else
        print_error "? Services are not running. Please start them first with:"
        print_error "   docker compose up -d"
        exit 1
    fi
}

# Run the main function
main