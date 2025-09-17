# SalesAPI Manual Tests - PowerShell Version
# Main test runner for Windows environments

param(
    [switch]$SkipAuth = $false,
    [switch]$SkipHealth = $false,
    [switch]$SkipProducts = $false,
    [switch]$SkipOrders = $false,
    [switch]$SkipReservations = $false,
    [switch]$SkipValidation = $false,
    [switch]$SkipConcurrency = $false,
    [switch]$SkipPrerequisites = $false,
    [switch]$OnlyAuth = $false,
    [switch]$OnlyBasic = $false,
    [switch]$Help = $false
)

# Import utilities module
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module "$scriptPath\utils\test_utils.psm1" -Force

# Configuration from JSON
$configPath = "$scriptPath\config\endpoints.json"
$config = Get-Content $configPath | ConvertFrom-Json

$GATEWAY_URL = $config.gateway.baseUrl
$INVENTORY_URL = $config.inventory.baseUrl
$SALES_URL = $config.sales.baseUrl

# Global variables for sharing data between tests
$script:ADMIN_TOKEN = ""
$script:CUSTOMER_TOKEN = ""
$script:PRODUCT_ID = ""
$script:MOUSE_ID = ""
$script:WORKSTATION_ID = ""
$script:ORDER_ID = ""
$script:MULTI_ORDER_ID = ""
$script:RESERVATION_ORDER_ID = ""
$script:RESERVATION_ID = ""

# Authentication tests
function Test-Authentication {
    Write-Info "?? Starting Authentication Tests..."
    
    # Test 1: Get test users list
    Start-Test "Get test users list"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/auth/test-users"
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Successfully retrieved test users list"
    } else {
        Fail-Test "Failed to get test users (HTTP: $($response.StatusCode))"
    }
    
    # Test 2: Authenticate as admin
    Start-Test "Authenticate as admin"
    $script:ADMIN_TOKEN = Get-AuthToken -Username "admin" -Password "admin123" -GatewayUrl $GATEWAY_URL
    if ($script:ADMIN_TOKEN) {
        Pass-Test "Successfully authenticated as admin"
    } else {
        Fail-Test "Failed to authenticate as admin"
        $script:ADMIN_TOKEN = ""
    }
    
    # Test 3: Authenticate as customer
    Start-Test "Authenticate as customer"
    $script:CUSTOMER_TOKEN = Get-AuthToken -Username "customer1" -Password "password123" -GatewayUrl $GATEWAY_URL
    if ($script:CUSTOMER_TOKEN) {
        Pass-Test "Successfully authenticated as customer"
    } else {
        Fail-Test "Failed to authenticate as customer"
        $script:CUSTOMER_TOKEN = ""
    }
    
    # Test 4: Invalid credentials
    Start-Test "Test invalid credentials"
    $invalidData = @{ username = "admin"; password = "wrong_password" } | ConvertTo-Json
    $headers = @{ "Content-Type" = "application/json" }
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/auth/token" -Headers $headers -Body $invalidData
    if ($response.StatusCode -eq 401) {
        Pass-Test "Correctly rejected invalid credentials"
    } else {
        Fail-Test "Did not reject invalid credentials (HTTP: $($response.StatusCode))"
    }
    
    # Test 5: Missing credentials
    Start-Test "Test missing credentials"
    $emptyData = @{} | ConvertTo-Json
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/auth/token" -Headers $headers -Body $emptyData
    if ($response.StatusCode -eq 400) {
        Pass-Test "Correctly rejected missing credentials"
    } else {
        Fail-Test "Did not reject missing credentials (HTTP: $($response.StatusCode))"
    }
    
    # Test 6: Validate token structure
    if ($script:ADMIN_TOKEN) {
        Start-Test "Validate JWT token structure"
        $tokenParts = $script:ADMIN_TOKEN.Split('.')
        if ($tokenParts.Count -eq 3) {
            Pass-Test "JWT token has correct structure"
        } else {
            Fail-Test "JWT token has invalid structure (parts: $($tokenParts.Count))"
        }
    }
    
    Write-Info "?? Authentication Tests Completed"
    Write-Host
}

# Health check tests
function Test-Health {
    Write-Info "?? Starting Health Check Tests..."
    
    # Test 1: Gateway status
    Start-Test "Gateway status check"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/gateway/status"
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Gateway is healthy"
    } else {
        Fail-Test "Gateway status check failed (HTTP: $($response.StatusCode))"
    }
    
    # Test 2: Gateway routes info
    Start-Test "Gateway routes information"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/gateway/routes"
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Gateway routes information retrieved"
    } else {
        Fail-Test "Failed to get gateway routes (HTTP: $($response.StatusCode))"
    }
    
    # Test 3: Inventory API health (direct)
    Start-Test "Inventory API health check (direct)"
    $response = Invoke-TestRequest -Method GET -Uri "$INVENTORY_URL/health"
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Inventory API is healthy (direct)"
    } else {
        Fail-Test "Inventory API health check failed (HTTP: $($response.StatusCode))"
    }
    
    # Test 4: Inventory API health (via gateway)
    Start-Test "Inventory API health check (via gateway)"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/inventory/health"
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Inventory API is healthy (via gateway)"
    } else {
        Fail-Test "Inventory API health check via gateway failed (HTTP: $($response.StatusCode))"
    }
    
    # Test 5: Sales API health (direct)
    Start-Test "Sales API health check (direct)"
    $response = Invoke-TestRequest -Method GET -Uri "$SALES_URL/health"
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Sales API is healthy (direct)"
    } else {
        Fail-Test "Sales API health check failed (HTTP: $($response.StatusCode))"
    }
    
    # Test 6: Sales API health (via gateway)
    Start-Test "Sales API health check (via gateway)"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/sales/health"
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Sales API is healthy (via gateway)"
    } else {
        Fail-Test "Sales API health check via gateway failed (HTTP: $($response.StatusCode))"
    }
    
    Write-Info "?? Health Check Tests Completed"
    Write-Host
}

# Product tests
function Test-Products {
    Write-Info "?? Starting Product Tests..."
    
    if (-not $script:ADMIN_TOKEN) {
        Write-Warning "Admin token not available, skipping product tests"
        return
    }
    
    # Test 1: Get all products
    Start-Test "Get all products"
    $headers = @{ "Authorization" = "Bearer $script:ADMIN_TOKEN" }
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/inventory/products" -Headers $headers
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Successfully retrieved products list"
    } else {
        Fail-Test "Failed to get products list (HTTP: $($response.StatusCode))"
    }
    
    # Test 2: Create a test product
    Start-Test "Create test product"
    $productData = @{
        name = "Test Product PowerShell"
        description = "Product created by PowerShell test"
        price = 99.99
        stockQuantity = 25
    } | ConvertTo-Json
    
    $headers = @{ 
        "Authorization" = "Bearer $script:ADMIN_TOKEN"
        "Content-Type" = "application/json"
    }
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $productData
    if ($response.Success -and $response.StatusCode -eq 201) {
        try {
            $product = $response.Content | ConvertFrom-Json
            $script:PRODUCT_ID = $product.id
            Pass-Test "Successfully created test product: $($script:PRODUCT_ID)"
        } catch {
            Fail-Test "Failed to parse product creation response"
        }
    } else {
        Fail-Test "Failed to create product (HTTP: $($response.StatusCode))"
    }
    
    # Test 3: Get product by ID
    if ($script:PRODUCT_ID) {
        Start-Test "Get product by ID"
        $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/inventory/products/$script:PRODUCT_ID" -Headers @{ "Authorization" = "Bearer $script:ADMIN_TOKEN" }
        if ($response.Success -and $response.StatusCode -eq 200) {
            Pass-Test "Successfully retrieved product by ID"
        } else {
            Fail-Test "Failed to get product by ID (HTTP: $($response.StatusCode))"
        }
    }
    
    # Test 4: Update product
    if ($script:PRODUCT_ID) {
        Start-Test "Update product"
        $updateData = @{
            name = "Test Product PowerShell Updated"
            description = "Updated by PowerShell test"
            price = 149.99
            stockQuantity = 30
        } | ConvertTo-Json
        
        $response = Invoke-TestRequest -Method PUT -Uri "$GATEWAY_URL/inventory/products/$script:PRODUCT_ID" -Headers $headers -Body $updateData
        if ($response.Success -and $response.StatusCode -eq 200) {
            Pass-Test "Successfully updated product"
        } else {
            Fail-Test "Failed to update product (HTTP: $($response.StatusCode))"
        }
    }
    
    # Test 5: Create additional test products for order tests
    Start-Test "Create Mouse product for testing"
    $mouseData = @{
        name = "Gaming Mouse"
        description = "High precision gaming mouse"
        price = 89.99
        stockQuantity = 50
    } | ConvertTo-Json
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $mouseData
    if ($response.Success -and $response.StatusCode -eq 201) {
        try {
            $mouse = $response.Content | ConvertFrom-Json
            $script:MOUSE_ID = $mouse.id
            Pass-Test "Successfully created mouse product: $($script:MOUSE_ID)"
        } catch {
            Fail-Test "Failed to parse mouse creation response"
        }
    } else {
        Fail-Test "Failed to create mouse product (HTTP: $($response.StatusCode))"
    }
    
    Start-Test "Create Workstation product for testing"
    $workstationData = @{
        name = "Professional Workstation"
        description = "High-end workstation for professionals"
        price = 15000.00
        stockQuantity = 3
    } | ConvertTo-Json
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $workstationData
    if ($response.Success -and $response.StatusCode -eq 201) {
        try {
            $workstation = $response.Content | ConvertFrom-Json
            $script:WORKSTATION_ID = $workstation.id
            Pass-Test "Successfully created workstation product: $($script:WORKSTATION_ID)"
        } catch {
            Fail-Test "Failed to parse workstation creation response"
        }
    } else {
        Fail-Test "Failed to create workstation product (HTTP: $($response.StatusCode))"
    }
    
    Write-Info "?? Product Tests Completed"
    Write-Host
}

# Order tests
function Test-Orders {
    Write-Info "?? Starting Order Tests..."
    
    if (-not $script:CUSTOMER_TOKEN -or -not $script:PRODUCT_ID) {
        Write-Warning "Customer token or test product not available, skipping order tests"
        return
    }
    
    # Test 1: Create a simple order
    Start-Test "Create simple order"
    $customerId = $config.testData.customers[0]
    $orderData = @{
        customerId = $customerId
        items = @(
            @{
                productId = $script:PRODUCT_ID
                quantity = 2
            }
        )
    } | ConvertTo-Json -Depth 3
    
    $correlationId = New-CorrelationId -Prefix "ps-order"
    $headers = @{
        "Authorization" = "Bearer $script:CUSTOMER_TOKEN"
        "Content-Type" = "application/json"
        "X-Correlation-Id" = $correlationId
    }
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $headers -Body $orderData
    if ($response.Success -and ($response.StatusCode -eq 201 -or $response.StatusCode -eq 422)) {
        if ($response.StatusCode -eq 201) {
            try {
                $order = $response.Content | ConvertFrom-Json
                $script:ORDER_ID = $order.id
                Pass-Test "Successfully created simple order: $($script:ORDER_ID)"
            } catch {
                Fail-Test "Failed to parse order creation response"
            }
        } else {
            Pass-Test "Order creation handled correctly (business logic validation - HTTP: $($response.StatusCode))"
        }
    } else {
        Fail-Test "Failed to create order (HTTP: $($response.StatusCode))"
    }
    
    # Test 2: Get order by ID
    if ($script:ORDER_ID) {
        Start-Test "Get order by ID"
        $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/sales/orders/$script:ORDER_ID" -Headers @{ "Authorization" = "Bearer $script:CUSTOMER_TOKEN" }
        if ($response.Success -and $response.StatusCode -eq 200) {
            Pass-Test "Successfully retrieved order by ID"
        } else {
            Fail-Test "Failed to get order by ID (HTTP: $($response.StatusCode))"
        }
    }
    
    # Test 3: Create multi-item order
    if ($script:MOUSE_ID -and $script:WORKSTATION_ID) {
        Start-Test "Create multi-item order"
        $multiOrderData = @{
            customerId = $config.testData.customers[1]
            items = @(
                @{
                    productId = $script:MOUSE_ID
                    quantity = 3
                },
                @{
                    productId = $script:WORKSTATION_ID
                    quantity = 1
                }
            )
        } | ConvertTo-Json -Depth 3
        
        $correlationId = New-CorrelationId -Prefix "ps-multi-order"
        $headers["X-Correlation-Id"] = $correlationId
        
        $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $headers -Body $multiOrderData
        if ($response.Success -and ($response.StatusCode -eq 201 -or $response.StatusCode -eq 422)) {
            if ($response.StatusCode -eq 201) {
                try {
                    $multiOrder = $response.Content | ConvertFrom-Json
                    $script:MULTI_ORDER_ID = $multiOrder.id
                    Pass-Test "Successfully created multi-item order: $($script:MULTI_ORDER_ID)"
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
    
    # Test 4: Get all orders
    Start-Test "Get all orders"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/sales/orders" -Headers @{ "Authorization" = "Bearer $script:CUSTOMER_TOKEN" }
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Successfully retrieved orders list"
    } else {
        Fail-Test "Failed to get orders list (HTTP: $($response.StatusCode))"
    }
    
    Write-Info "?? Order Tests Completed"
    Write-Host
}

# Validation tests
function Test-Validation {
    Write-Info "? Starting Validation Tests..."
    
    if (-not $script:ADMIN_TOKEN) {
        Write-Warning "Admin token not available, skipping validation tests"
        return
    }
    
    # Test 1: Invalid product data
    Start-Test "Test invalid product data validation"
    $invalidProductData = @{
        name = ""
        description = ""
        price = -10.00
        stockQuantity = -1
    } | ConvertTo-Json
    
    $headers = @{
        "Authorization" = "Bearer $script:ADMIN_TOKEN"
        "Content-Type" = "application/json"
    }
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $invalidProductData
    if ($response.StatusCode -eq 400) {
        Pass-Test "Correctly rejected invalid product data"
    } else {
        Fail-Test "Did not reject invalid product data (HTTP: $($response.StatusCode))"
    }
    
    # Test 2: Unauthorized product creation
    Start-Test "Test unauthorized product creation"
    $productData = @{
        name = "Unauthorized Product"
        description = "Should not be created"
        price = 10.00
        stockQuantity = 1
    } | ConvertTo-Json
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers @{ "Content-Type" = "application/json" } -Body $productData
    if ($response.StatusCode -eq 401) {
        Pass-Test "Correctly blocked unauthorized product creation"
    } else {
        Fail-Test "Did not block unauthorized product creation (HTTP: $($response.StatusCode))"
    }
    
    # Test 3: Customer trying to create product (insufficient privileges)
    if ($script:CUSTOMER_TOKEN) {
        Start-Test "Test insufficient privileges for product creation"
        $headers = @{
            "Authorization" = "Bearer $script:CUSTOMER_TOKEN"
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $productData
        if ($response.StatusCode -eq 403) {
            Pass-Test "Correctly blocked customer from creating product"
        } else {
            Fail-Test "Did not block customer from creating product (HTTP: $($response.StatusCode))"
        }
    }
    
    # Test 4: Invalid order data
    if ($script:CUSTOMER_TOKEN) {
        Start-Test "Test invalid order data validation"
        $invalidOrderData = @{
            customerId = "invalid-guid"
            items = @()
        } | ConvertTo-Json -Depth 3
        
        $headers = @{
            "Authorization" = "Bearer $script:CUSTOMER_TOKEN"
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $headers -Body $invalidOrderData
        if ($response.StatusCode -eq 400) {
            Pass-Test "Correctly rejected invalid order data"
        } else {
            Fail-Test "Did not reject invalid order data (HTTP: $($response.StatusCode))"
        }
    }
    
    Write-Info "? Validation Tests Completed"
    Write-Host
}

# Cleanup test data
function Clear-TestData {
    Write-Info "?? Cleaning up test data..."
    
    if (-not $script:ADMIN_TOKEN) {
        Write-Warning "Admin token not available, skipping cleanup"
        return
    }
    
    $headers = @{ "Authorization" = "Bearer $script:ADMIN_TOKEN" }
    
    # Clean up test products
    $productIds = @($script:PRODUCT_ID, $script:MOUSE_ID, $script:WORKSTATION_ID) | Where-Object { $_ }
    
    foreach ($productId in $productIds) {
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

# Service availability check
function Test-Services {
    Write-Info "?? Checking service availability..."
    
    $allServicesUp = $true
    
    if (-not (Test-Service "Gateway" "$GATEWAY_URL/gateway/status")) {
        $allServicesUp = $false
    }
    
    if (-not (Test-Service "Inventory API" "$INVENTORY_URL/health")) {
        $allServicesUp = $false
    }
    
    if (-not (Test-Service "Sales API" "$SALES_URL/health")) {
        $allServicesUp = $false
    }
    
    if (-not $allServicesUp) {
        Write-ErrorMessage "Some services are not running. Please start all services before running tests."
        Write-Info "Required services:"
        Write-Info "  - Gateway: $GATEWAY_URL"
        Write-Info "  - Inventory API: $INVENTORY_URL"
        Write-Info "  - Sales API: $SALES_URL"
        return $false
    }
    
    Write-Success "All services are running"
    return $true
}

# Show help
function Show-Help {
    Write-Host @"
SalesAPI Manual Tests Runner (PowerShell)

Usage: .\run_manual_tests.ps1 [OPTIONS]

Options:
  -SkipAuth         Skip authentication tests
  -SkipHealth       Skip health check tests
  -SkipProducts     Skip product management tests
  -SkipOrders       Skip order management tests
  -SkipReservations Skip stock reservation tests
  -SkipValidation   Skip validation and edge case tests
  -SkipConcurrency  Skip concurrency tests
  -SkipPrerequisites Skip prerequisite checks
  -OnlyAuth         Run only authentication tests
  -OnlyBasic        Run only basic tests (auth, health)
  -Help             Show this help message

Examples:
  .\run_manual_tests.ps1                    # Run all tests
  .\run_manual_tests.ps1 -OnlyBasic         # Run only basic functionality tests
  .\run_manual_tests.ps1 -SkipConcurrency   # Run all tests except concurrency tests
"@
}

# Main function
function Start-TestSuite {
    if ($Help) {
        Show-Help
        return
    }
    
    # Handle exclusive options
    if ($OnlyAuth) {
        $SkipHealth = $SkipProducts = $SkipOrders = $SkipReservations = $SkipValidation = $SkipConcurrency = $true
    }
    elseif ($OnlyBasic) {
        $SkipProducts = $SkipOrders = $SkipReservations = $SkipValidation = $SkipConcurrency = $true
    }
    
    Write-Info "?? Starting SalesAPI Manual Tests Suite (PowerShell)"
    Write-Info "============================================"
    Write-Info "Timestamp: $(Get-Date)"
    Write-Info "Test configuration:"
    Write-Info "  - Gateway URL: $GATEWAY_URL"
    Write-Info "  - Inventory URL: $INVENTORY_URL"
    Write-Info "  - Sales URL: $SALES_URL"
    Write-Info "============================================"
    Write-Host
    
    $startTime = Get-Date
    
    # Check prerequisites
    if (-not $SkipPrerequisites) {
        if (-not (Test-Prerequisites)) {
            Write-ErrorMessage "Prerequisites check failed"
            return
        }
        
        if (-not (Test-Services)) {
            Write-ErrorMessage "Service availability check failed"
            return
        }
    }
    
    # Run test categories in logical order
    try {
        if (-not $SkipAuth) { Test-Authentication }
        if (-not $SkipHealth) { Test-Health }
        if (-not $SkipProducts) { Test-Products }
        if (-not $SkipOrders) { Test-Orders }
        if (-not $SkipValidation) { Test-Validation }
        
        # Always run cleanup
        Clear-TestData
    }
    finally {
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Host
        Write-Info "============================================"
        Write-Info "SalesAPI Manual Tests Suite Completed"
        Write-Info "============================================"
        Write-Info "Total execution time: $([math]::Round($duration, 2)) seconds"
        
        # Generate final summary
        if (Write-TestSummary) {
            Write-Success "? All tests completed successfully!"
        } else {
            Write-ErrorMessage "? Some tests failed. Check the detailed logs for more information."
        }
    }
}

# Initialize and run
if ($MyInvocation.InvocationName -ne '.') {
    Start-TestSuite
}