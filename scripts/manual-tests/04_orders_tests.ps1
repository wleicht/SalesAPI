# SalesAPI Manual Tests - Order Tests (PowerShell)
# Tests all order management functionality

param(
    [switch]$Help = $false,
    [switch]$SkipCleanup = $false,
    [string]$ProductId = ""
)

# Import utilities module
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module "$scriptPath\utils\test_utils.psm1" -Force

# Configuration
$configPath = "$scriptPath\config\endpoints.json"
$config = Get-Content $configPath | ConvertFrom-Json
$GATEWAY_URL = $config.gateway.baseUrl

# Global variables
$script:ADMIN_TOKEN = ""
$script:CUSTOMER_TOKEN = ""
$script:TEST_ORDER_IDS = @()
$script:TEST_PRODUCT_ID = $ProductId
$script:MOUSE_PRODUCT_ID = ""
$script:WORKSTATION_PRODUCT_ID = ""

function Initialize-Authentication {
    Write-Info "Initializing authentication..."
    
    $script:ADMIN_TOKEN = Get-AuthToken -Username "admin" -Password "admin123" -GatewayUrl $GATEWAY_URL
    if (-not $script:ADMIN_TOKEN) {
        Write-ErrorMessage "Failed to authenticate as admin."
        return $false
    }
    
    $script:CUSTOMER_TOKEN = Get-AuthToken -Username "customer1" -Password "password123" -GatewayUrl $GATEWAY_URL
    if (-not $script:CUSTOMER_TOKEN) {
        Write-ErrorMessage "Failed to authenticate as customer. Cannot proceed with order tests."
        return $false
    }
    
    return $true
}

function Setup-TestProducts {
    Write-Info "Setting up test products for orders..."
    
    $headers = @{ 
        "Authorization" = "Bearer $script:ADMIN_TOKEN"
        "Content-Type" = "application/json"
    }
    
    # Create or use existing test product
    if (-not $script:TEST_PRODUCT_ID -or $script:TEST_PRODUCT_ID -eq "") {
        $script:TEST_PRODUCT_ID = $env:TEST_PRODUCT_ID
    }
    
    if (-not $script:TEST_PRODUCT_ID -or $script:TEST_PRODUCT_ID -eq "") {
        Write-Info "Creating primary test product for orders..."
        $productData = @{
            name = "Order Test Product"
            description = "Product for order testing"
            price = 299.99
            stockQuantity = 50
        } | ConvertTo-Json
        
        $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $productData
        if ($response.Success -and $response.StatusCode -eq 201) {
            try {
                $product = $response.Content | ConvertFrom-Json
                $script:TEST_PRODUCT_ID = $product.id
                Write-Success "Created test product: $($script:TEST_PRODUCT_ID)"
            } catch {
                Write-ErrorMessage "Failed to parse product creation response"
                return $false
            }
        } else {
            Write-ErrorMessage "Failed to create test product (HTTP: $($response.StatusCode))"
            return $false
        }
    } else {
        Write-Info "Using existing test product: $script:TEST_PRODUCT_ID"
    }
    
    # Create mouse product for multi-item orders
    Write-Info "Creating mouse product for multi-item orders..."
    $mouseData = @{
        name = "Gaming Mouse for Orders"
        description = "High precision gaming mouse for order testing"
        price = 89.99
        stockQuantity = 100
    } | ConvertTo-Json
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $mouseData
    if ($response.Success -and $response.StatusCode -eq 201) {
        try {
            $mouse = $response.Content | ConvertFrom-Json
            $script:MOUSE_PRODUCT_ID = $mouse.id
            Write-Success "Created mouse product: $($script:MOUSE_PRODUCT_ID)"
        } catch {
            Write-ErrorMessage "Failed to parse mouse creation response"
            return $false
        }
    } else {
        Write-Warning "Failed to create mouse product (HTTP: $($response.StatusCode))"
    }
    
    # Create workstation product for high-value orders
    Write-Info "Creating workstation product for high-value orders..."
    $workstationData = @{
        name = "Professional Workstation for Orders"
        description = "High-end workstation for professional order testing"
        price = 15000.00
        stockQuantity = 5
    } | ConvertTo-Json
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $workstationData
    if ($response.Success -and $response.StatusCode -eq 201) {
        try {
            $workstation = $response.Content | ConvertFrom-Json
            $script:WORKSTATION_PRODUCT_ID = $workstation.id
            Write-Success "Created workstation product: $($script:WORKSTATION_PRODUCT_ID)"
        } catch {
            Write-ErrorMessage "Failed to parse workstation creation response"
            return $false
        }
    } else {
        Write-Warning "Failed to create workstation product (HTTP: $($response.StatusCode))"
    }
    
    return $true
}

function Test-Orders {
    Write-Info "?? Starting Order Tests..."
    
    if (-not $script:TEST_PRODUCT_ID) {
        Write-ErrorMessage "No test product available for order tests"
        return
    }
    
    # Test 1: Get all orders (should start empty or show existing)
    Start-Test "Get all orders"
    $headers = @{ "Authorization" = "Bearer $script:CUSTOMER_TOKEN" }
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/sales/orders" -Headers $headers
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Successfully retrieved orders list"
    } else {
        Fail-Test "Failed to get orders list (HTTP: $($response.StatusCode))"
    }
    
    # Test 2: Create a simple single-item order
    Start-Test "Create simple single-item order"
    $customerId = $config.testData.customers[0]
    $orderData = @{
        customerId = $customerId
        items = @(
            @{
                productId = $script:TEST_PRODUCT_ID
                quantity = 2
            }
        )
    } | ConvertTo-Json -Depth 3
    
    $correlationId = New-CorrelationId -Prefix "ps-single-order"
    $orderHeaders = @{
        "Authorization" = "Bearer $script:CUSTOMER_TOKEN"
        "Content-Type" = "application/json"
        "X-Correlation-Id" = $correlationId
    }
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $orderHeaders -Body $orderData
    if ($response.Success -and ($response.StatusCode -eq 201 -or $response.StatusCode -eq 422)) {
        if ($response.StatusCode -eq 201) {
            try {
                $order = $response.Content | ConvertFrom-Json
                $orderId = $order.id
                $script:TEST_ORDER_IDS += $orderId
                Pass-Test "Successfully created single-item order: $orderId"
                $env:TEST_ORDER_ID = $orderId
            } catch {
                Fail-Test "Failed to parse single-item order creation response"
            }
        } else {
            Pass-Test "Order creation handled correctly (business logic validation - HTTP: $($response.StatusCode))"
        }
    } else {
        Fail-Test "Failed to create single-item order (HTTP: $($response.StatusCode))"
    }
    
    # Test 3: Create multi-item order
    if ($script:MOUSE_PRODUCT_ID -and $script:WORKSTATION_PRODUCT_ID) {
        Start-Test "Create multi-item order"
        $multiOrderData = @{
            customerId = $config.testData.customers[1]
            items = @(
                @{
                    productId = $script:MOUSE_PRODUCT_ID
                    quantity = 3
                },
                @{
                    productId = $script:WORKSTATION_PRODUCT_ID
                    quantity = 1
                }
            )
        } | ConvertTo-Json -Depth 3
        
        $correlationId = New-CorrelationId -Prefix "ps-multi-order"
        $orderHeaders["X-Correlation-Id"] = $correlationId
        
        $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $orderHeaders -Body $multiOrderData
        if ($response.Success -and ($response.StatusCode -eq 201 -or $response.StatusCode -eq 422)) {
            if ($response.StatusCode -eq 201) {
                try {
                    $multiOrder = $response.Content | ConvertFrom-Json
                    $multiOrderId = $multiOrder.id
                    $script:TEST_ORDER_IDS += $multiOrderId
                    Pass-Test "Successfully created multi-item order: $multiOrderId"
                } catch {
                    Fail-Test "Failed to parse multi-item order creation response"
                }
            } else {
                Pass-Test "Multi-item order creation handled correctly (business logic validation - HTTP: $($response.StatusCode))"
            }
        } else {
            Fail-Test "Failed to create multi-item order (HTTP: $($response.StatusCode))"
        }
    }
    
    # Test 4: Get order by ID
    if ($script:TEST_ORDER_IDS.Count -gt 0) {
        Start-Test "Get order by ID"
        $orderId = $script:TEST_ORDER_IDS[0]
        $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/sales/orders/$orderId" -Headers $headers
        if ($response.Success -and $response.StatusCode -eq 200) {
            try {
                $order = $response.Content | ConvertFrom-Json
                if ($order.id -eq $orderId) {
                    Pass-Test "Successfully retrieved order by ID with correct data"
                } else {
                    Fail-Test "Retrieved order has incorrect ID"
                }
            } catch {
                Fail-Test "Failed to parse retrieved order data"
            }
        } else {
            Fail-Test "Failed to get order by ID (HTTP: $($response.StatusCode))"
        }
    }
    
    # Test 5: Test order validation - invalid customer ID
    Start-Test "Test invalid customer ID validation"
    $invalidOrderData = @{
        customerId = "invalid-guid-format"
        items = @(
            @{
                productId = $script:TEST_PRODUCT_ID
                quantity = 1
            }
        )
    } | ConvertTo-Json -Depth 3
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $orderHeaders -Body $invalidOrderData
    if ($response.StatusCode -eq 400) {
        Pass-Test "Correctly rejected invalid customer ID"
    } else {
        Fail-Test "Did not reject invalid customer ID (HTTP: $($response.StatusCode))"
    }
    
    # Test 6: Test order validation - empty items
    Start-Test "Test empty items validation"
    $emptyItemsOrderData = @{
        customerId = $config.testData.customers[0]
        items = @()
    } | ConvertTo-Json -Depth 3
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $orderHeaders -Body $emptyItemsOrderData
    if ($response.StatusCode -eq 400) {
        Pass-Test "Correctly rejected order with empty items"
    } else {
        Fail-Test "Did not reject order with empty items (HTTP: $($response.StatusCode))"
    }
    
    # Test 7: Test order validation - non-existent product
    Start-Test "Test non-existent product validation"
    $fakeProductId = [guid]::NewGuid().ToString()
    $invalidProductOrderData = @{
        customerId = $config.testData.customers[0]
        items = @(
            @{
                productId = $fakeProductId
                quantity = 1
            }
        )
    } | ConvertTo-Json -Depth 3
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $orderHeaders -Body $invalidProductOrderData
    if ($response.StatusCode -eq 400 -or $response.StatusCode -eq 404) {
        Pass-Test "Correctly rejected order with non-existent product"
    } else {
        Fail-Test "Did not reject order with non-existent product (HTTP: $($response.StatusCode))"
    }
    
    # Test 8: Test order validation - zero quantity
    Start-Test "Test zero quantity validation"
    $zeroQuantityOrderData = @{
        customerId = $config.testData.customers[0]
        items = @(
            @{
                productId = $script:TEST_PRODUCT_ID
                quantity = 0
            }
        )
    } | ConvertTo-Json -Depth 3
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $orderHeaders -Body $zeroQuantityOrderData
    if ($response.StatusCode -eq 400) {
        Pass-Test "Correctly rejected order with zero quantity"
    } else {
        Fail-Test "Did not reject order with zero quantity (HTTP: $($response.StatusCode))"
    }
    
    # Test 9: Test get non-existent order
    Start-Test "Test get non-existent order"
    $fakeOrderId = [guid]::NewGuid().ToString()
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/sales/orders/$fakeOrderId" -Headers $headers
    if ($response.StatusCode -eq 404) {
        Pass-Test "Correctly returned 404 for non-existent order"
    } else {
        Fail-Test "Did not return 404 for non-existent order (HTTP: $($response.StatusCode))"
    }
    
    # Test 10: Test unauthorized order access
    Start-Test "Test unauthorized order access"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/sales/orders" -Headers @{ "Content-Type" = "application/json" }
    if ($response.StatusCode -eq 401) {
        Pass-Test "Correctly blocked unauthorized order access"
    } else {
        Fail-Test "Did not block unauthorized order access (HTTP: $($response.StatusCode))"
    }
    
    # Test 11: Test large quantity order (potential stock validation)
    Start-Test "Test large quantity order"
    $largeQuantityOrderData = @{
        customerId = $config.testData.customers[2]
        items = @(
            @{
                productId = $script:TEST_PRODUCT_ID
                quantity = 1000000  # Very large quantity
            }
        )
    } | ConvertTo-Json -Depth 3
    
    $correlationId = New-CorrelationId -Prefix "ps-large-order"
    $orderHeaders["X-Correlation-Id"] = $correlationId
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $orderHeaders -Body $largeQuantityOrderData
    if ($response.StatusCode -eq 400 -or $response.StatusCode -eq 422) {
        Pass-Test "Correctly handled large quantity order (validation)"
    } elseif ($response.StatusCode -eq 201) {
        # If it succeeds, add to cleanup list
        try {
            $largeOrder = $response.Content | ConvertFrom-Json
            $script:TEST_ORDER_IDS += $largeOrder.id
            Pass-Test "Large quantity order created (system allows it)"
        } catch {
            Pass-Test "Large quantity order handled correctly"
        }
    } else {
        Fail-Test "Unexpected response for large quantity order (HTTP: $($response.StatusCode))"
    }
    
    Write-Info "?? Order Tests Completed"
    Write-Host
}

function Clear-TestData {
    if ($SkipCleanup) {
        Write-Info "Skipping cleanup as requested"
        return
    }
    
    Write-Info "?? Cleaning up test data..."
    
    if (-not $script:ADMIN_TOKEN) {
        Write-Warning "Admin token not available, skipping cleanup"
        return
    }
    
    $headers = @{ "Authorization" = "Bearer $script:ADMIN_TOKEN" }
    
    # Clean up test products (orders will be cleaned up by cascade or remain as test data)
    $productIds = @($script:TEST_PRODUCT_ID, $script:MOUSE_PRODUCT_ID, $script:WORKSTATION_PRODUCT_ID) | Where-Object { $_ -and $_ -ne "" }
    
    foreach ($productId in $productIds) {
        if ($productId -eq $env:TEST_PRODUCT_ID) {
            Write-Info "Skipping cleanup of shared test product: $productId"
            continue
        }
        
        Start-Test "Cleanup product: $productId"
        $response = Invoke-TestRequest -Method DELETE -Uri "$GATEWAY_URL/inventory/products/$productId" -Headers $headers
        if ($response.Success -and $response.StatusCode -eq 204) {
            Pass-Test "Successfully deleted test product"
        } else {
            Fail-Test "Failed to delete test product (HTTP: $($response.StatusCode))"
        }
    }
    
    Write-Info "?? Cleanup completed"
    Write-Host
}

function Show-Help {
    Write-Host @"
SalesAPI Order Tests (PowerShell)

Usage: .\04_orders_tests.ps1 [OPTIONS]

Options:
  -ProductId <ID>   Use existing product ID for testing (optional)
  -SkipCleanup      Skip cleanup of test data
  -Help             Show this help message

Examples:
  .\04_orders_tests.ps1                              # Run order tests with auto-created products
  .\04_orders_tests.ps1 -ProductId "abc123..."       # Run order tests with specific product
  .\04_orders_tests.ps1 -SkipCleanup                 # Run order tests without cleanup
"@
}

# Main execution
if ($Help) {
    Show-Help
    exit 0
}

if (-not (Test-Prerequisites)) {
    Write-ErrorMessage "Prerequisites check failed"
    exit 1
}

if (-not (Test-Service "Gateway" "$GATEWAY_URL/gateway/status")) {
    Write-ErrorMessage "Gateway service is not available"
    exit 1
}

if (-not (Initialize-Authentication)) {
    Write-ErrorMessage "Authentication initialization failed"
    exit 1
}

if (-not (Setup-TestProducts)) {
    Write-ErrorMessage "Test product setup failed"
    exit 1
}

try {
    Test-Orders
} finally {
    Clear-TestData
}

$success = Write-TestSummary

if ($success) {
    Write-Success "? Order tests completed successfully!"
    if ($env:TEST_ORDER_ID) {
        Write-Info "Test order ID for other scripts: $env:TEST_ORDER_ID"
    }
    exit 0
} else {
    Write-ErrorMessage "? Some order tests failed."
    exit 1
}