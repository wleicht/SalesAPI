# SalesAPI Manual Tests - cURL Examples
# Collection of cURL commands for manual testing

## Prerequisites
# Set these environment variables first:
# export GATEWAY_URL="http://localhost:6000"
# export INVENTORY_URL="http://localhost:5000"
# export SALES_URL="http://localhost:5001"

## 1. AUTHENTICATION

### Get test users list
curl -X GET $GATEWAY_URL/auth/test-users | jq

### Login as admin
curl -X POST $GATEWAY_URL/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }' | jq

# Save the token for subsequent requests
export ADMIN_TOKEN="<paste_token_here>"

### Login as customer
curl -X POST $GATEWAY_URL/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "username": "customer1",
    "password": "password123"
  }' | jq

export CUSTOMER_TOKEN="<paste_token_here>"

## 2. HEALTH CHECKS

### Gateway status
curl -X GET $GATEWAY_URL/gateway/status | jq

### Gateway routes
curl -X GET $GATEWAY_URL/gateway/routes | jq

### Inventory API health
curl -X GET $INVENTORY_URL/health
curl -X GET $GATEWAY_URL/inventory/health

### Sales API health
curl -X GET $SALES_URL/health
curl -X GET $GATEWAY_URL/sales/health

## 3. PRODUCT MANAGEMENT

### List products (public access)
curl -X GET $GATEWAY_URL/inventory/products | jq

### List products with pagination
curl -X GET "$GATEWAY_URL/inventory/products?page=1&pageSize=5" | jq

### Create product (admin required)
curl -X POST $GATEWAY_URL/inventory/products \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop Gamer",
    "description": "Notebook para jogos de alta performance",
    "price": 2999.99,
    "stockQuantity": 10
  }' | jq

# Save the product ID for later use
export PRODUCT_ID="<paste_product_id_here>"

### Get product by ID
curl -X GET $GATEWAY_URL/inventory/products/$PRODUCT_ID | jq

### Update product (admin required)
curl -X PUT $GATEWAY_URL/inventory/products/$PRODUCT_ID \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop Gamer Atualizado",
    "description": "Versão melhorada do notebook gamer",
    "price": 3299.99,
    "stockQuantity": 15
  }' | jq

### Delete product (admin required)
curl -X DELETE $GATEWAY_URL/inventory/products/$PRODUCT_ID \
  -H "Authorization: Bearer $ADMIN_TOKEN"

### Create additional test products

# Low-price product (for successful orders)
curl -X POST $GATEWAY_URL/inventory/products \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Mouse Gamer",
    "description": "Mouse com sensor óptico de alta precisão",
    "price": 89.99,
    "stockQuantity": 50
  }' | jq

export MOUSE_ID="<paste_mouse_id_here>"

# High-price product (for payment failure simulation)
curl -X POST $GATEWAY_URL/inventory/products \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Workstation Profissional",
    "description": "Computador de alta performance",
    "price": 15000.00,
    "stockQuantity": 3
  }' | jq

export WORKSTATION_ID="<paste_workstation_id_here>"

## 4. ORDER MANAGEMENT

### List orders (public access)
curl -X GET $GATEWAY_URL/sales/orders | jq

### List orders with pagination
curl -X GET "$GATEWAY_URL/sales/orders?page=1&pageSize=10" | jq

### Create order (customer required)
curl -X POST $GATEWAY_URL/sales/orders \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: test-order-001" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174001",
    "items": [
      {
        "productId": "'$MOUSE_ID'",
        "quantity": 2
      }
    ]
  }' | jq

export ORDER_ID="<paste_order_id_here>"

### Get order by ID
curl -X GET $GATEWAY_URL/sales/orders/$ORDER_ID | jq

### Create order with multiple items
curl -X POST $GATEWAY_URL/sales/orders \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: test-order-002" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174002",
    "items": [
      {
        "productId": "'$MOUSE_ID'",
        "quantity": 1
      },
      {
        "productId": "'$PRODUCT_ID'",
        "quantity": 1
      }
    ]
  }' | jq

export MULTI_ORDER_ID="<paste_multi_order_id_here>"

### Create order with high price (may trigger payment failure)
curl -X POST $GATEWAY_URL/sales/orders \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: test-order-payment-fail" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174003",
    "items": [
      {
        "productId": "'$WORKSTATION_ID'",
        "quantity": 2
      }
    ]
  }' -i

### Confirm order
curl -X PATCH $GATEWAY_URL/sales/orders/$ORDER_ID/confirm \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "X-Correlation-Id: test-confirm-001" | jq

### Cancel order
curl -X PATCH "$GATEWAY_URL/sales/orders/$MULTI_ORDER_ID/cancel?reason=Customer%20cancelled" \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "X-Correlation-Id: test-cancel-001" | jq

### Mark order as fulfilled
curl -X PATCH $GATEWAY_URL/sales/orders/$ORDER_ID/fulfill \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "X-Correlation-Id: test-fulfill-001" | jq

## 5. STOCK RESERVATIONS

### Create stock reservation directly
curl -X POST $INVENTORY_URL/api/stockreservations \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: test-reservation-001" \
  -d '{
    "orderId": "550e8400-e29b-41d4-a716-446655440001",
    "correlationId": "test-reservation-001",
    "items": [
      {
        "productId": "'$MOUSE_ID'",
        "quantity": 3
      }
    ]
  }' | jq

export RESERVATION_ORDER_ID="550e8400-e29b-41d4-a716-446655440001"

### Query reservations by order
curl -X GET $INVENTORY_URL/api/stockreservations/order/$RESERVATION_ORDER_ID \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

### Query specific reservation
export RESERVATION_ID="<paste_reservation_id_here>"
curl -X GET $INVENTORY_URL/api/stockreservations/$RESERVATION_ID \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

## 6. ERROR TESTING

### Unauthorized access (no token)
curl -X POST $GATEWAY_URL/inventory/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Unauthorized Product",
    "description": "This should fail",
    "price": 100.00,
    "stockQuantity": 5
  }' -i

### Forbidden access (customer trying to create product)
curl -X POST $GATEWAY_URL/inventory/products \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Forbidden Product",
    "description": "This should fail",
    "price": 100.00,
    "stockQuantity": 5
  }' -i

### Invalid data validation
curl -X POST $GATEWAY_URL/inventory/products \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "",
    "description": "Product without name",
    "price": -10.00,
    "stockQuantity": -5
  }' -i

### Non-existent resource
curl -X GET $GATEWAY_URL/inventory/products/00000000-0000-0000-0000-000000000000 -i
curl -X GET $GATEWAY_URL/sales/orders/00000000-0000-0000-0000-000000000000 -i

### Invalid JSON format
curl -X POST $GATEWAY_URL/inventory/products \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Product","price":invalid_json}' -i

## 7. CONCURRENCY TESTING

### Multiple concurrent orders (run these in separate terminals)
# Terminal 1:
curl -X POST $GATEWAY_URL/sales/orders \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: concurrent-test-1" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174010",
    "items": [{"productId": "'$MOUSE_ID'", "quantity": 5}]
  }' &

# Terminal 2:
curl -X POST $GATEWAY_URL/sales/orders \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: concurrent-test-2" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174011",
    "items": [{"productId": "'$MOUSE_ID'", "quantity": 5}]
  }' &

# Terminal 3:
curl -X POST $GATEWAY_URL/sales/orders \
  -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: concurrent-test-3" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174012",
    "items": [{"productId": "'$MOUSE_ID'", "quantity": 5}]
  }' &

### Verify stock integrity after concurrent operations
curl -X GET $GATEWAY_URL/inventory/products/$MOUSE_ID | jq '.stockQuantity'

## 8. MONITORING AND METRICS

### Check Prometheus metrics (if enabled)
curl -X GET $GATEWAY_URL/metrics
curl -X GET $INVENTORY_URL/metrics  
curl -X GET $SALES_URL/metrics

### Performance testing with timing
curl -w "@curl-format.txt" -o /dev/null -s $GATEWAY_URL/gateway/status

# Create curl-format.txt with:
# time_namelookup:    %{time_namelookup}\n
# time_connect:       %{time_connect}\n
# time_appconnect:    %{time_appconnect}\n
# time_pretransfer:   %{time_pretransfer}\n
# time_redirect:      %{time_redirect}\n
# time_starttransfer: %{time_starttransfer}\n
# time_total:         %{time_total}\n

## 9. CLEANUP

### Delete test products (admin required)
curl -X DELETE $GATEWAY_URL/inventory/products/$PRODUCT_ID \
  -H "Authorization: Bearer $ADMIN_TOKEN"

curl -X DELETE $GATEWAY_URL/inventory/products/$MOUSE_ID \
  -H "Authorization: Bearer $ADMIN_TOKEN"

curl -X DELETE $GATEWAY_URL/inventory/products/$WORKSTATION_ID \
  -H "Authorization: Bearer $ADMIN_TOKEN"

## Notes:
# - Replace $VARIABLE_NAME with actual values when running manually
# - Use jq for better JSON formatting (install if not available)
# - Add -i flag to see HTTP headers in responses
# - Add -v flag for verbose output including request headers
# - Some operations may take time due to event processing (stock reservations)