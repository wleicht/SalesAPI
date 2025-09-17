# SalesAPI Manual Tests - Product Tests (PowerShell)
# Tests all product management functionality

param(
    [switch]$Help = $false,
    [switch]$SkipCleanup = $false
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
$script:TEST_PRODUCT_IDS = @()

function Initialize-Authentication {
    Write-Info "Initializing authentication..."
    
    $script:ADMIN_TOKEN = Get-AuthToken -Username "admin" -Password "admin123" -GatewayUrl $GATEWAY_URL
    if (-not $script:ADMIN_TOKEN) {
        Write-ErrorMessage "Failed to authenticate as admin. Cannot proceed with product tests."
        return $false
    }
    
    $script:CUSTOMER_TOKEN = Get-AuthToken -Username "customer1" -Password "password123" -GatewayUrl $GATEWAY_URL
    if (-not $script:CUSTOMER_TOKEN) {
        Write-Warning "Failed to authenticate as customer. Some tests may be skipped."
    }
    
    return $true
}

function Test-Products {
    Write-Info "?? Starting Product Tests..."
    
    # Test 1: Get all products (should work without authentication for reading)
    Start-Test "Get all products (no auth)"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/inventory/products"
    if ($response.Success -and ($response.StatusCode -eq 200 -or $response.StatusCode -eq 401)) {
        if ($response.StatusCode -eq 200) {
            Pass-Test "Successfully retrieved products list without auth"
        } else {
            Pass-Test "Products endpoint correctly requires authentication"
        }
    } else {
        Fail-Test "Failed to test products endpoint (HTTP: $($response.StatusCode))"
    }
    
    # Test 2: Get all products with admin auth
    Start-Test "Get all products (with admin auth)"
    $headers = @{ "Authorization" = "Bearer $script:ADMIN_TOKEN" }
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/inventory/products" -Headers $headers
    if ($response.Success -and $response.StatusCode -eq 200) {
        Pass-Test "Successfully retrieved products list with admin auth"
    } else {
        Fail-Test "Failed to get products list with admin auth (HTTP: $($response.StatusCode))"
    }
    
    # Test 3: Create a basic product
    Start-Test "Create basic product"
    $productData = @{
        name = "Test Product PS Basic"
        description = "Basic product created by PowerShell test"
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
            $productId = $product.id
            $script:TEST_PRODUCT_IDS += $productId
            Pass-Test "Successfully created basic product: $productId"
            $env:TEST_PRODUCT_ID = $productId
        } catch {
            Fail-Test "Failed to parse product creation response"
        }
    } else {
        Fail-Test "Failed to create basic product (HTTP: $($response.StatusCode))"
    }
    
    # Test 4: Create product with all fields
    Start-Test "Create detailed product"
    $detailedProductData = @{
        name = "Test Product PS Detailed"
        description = "Detailed product with all fields - created by PowerShell test suite for comprehensive validation"
        price = 1299.99
        stockQuantity = 15
    } | ConvertTo-Json
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $detailedProductData
    if ($response.Success -and $response.StatusCode -eq 201) {
        try {
            $product = $response.Content | ConvertFrom-Json
            $detailedProductId = $product.id
            $script:TEST_PRODUCT_IDS += $detailedProductId
            Pass-Test "Successfully created detailed product: $detailedProductId"
        } catch {
            Fail-Test "Failed to parse detailed product creation response"
        }
    } else {
        Fail-Test "Failed to create detailed product (HTTP: $($response.StatusCode))"
    }
    
    # Test 5: Get product by ID
    if ($script:TEST_PRODUCT_IDS.Count -gt 0) {
        Start-Test "Get product by ID"
        $productId = $script:TEST_PRODUCT_IDS[0]
        $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/inventory/products/$productId" -Headers @{ "Authorization" = "Bearer $script:ADMIN_TOKEN" }
        if ($response.Success -and $response.StatusCode -eq 200) {
            try {
                $product = $response.Content | ConvertFrom-Json
                if ($product.id -eq $productId) {
                    Pass-Test "Successfully retrieved product by ID with correct data"
                } else {
                    Fail-Test "Retrieved product has incorrect ID"
                }
            } catch {
                Fail-Test "Failed to parse retrieved product data"
            }
        } else {
            Fail-Test "Failed to get product by ID (HTTP: $($response.StatusCode))"
        }
    }
    
    # Test 6: Update product
    if ($script:TEST_PRODUCT_IDS.Count -gt 0) {
        Start-Test "Update product"
        $productId = $script:TEST_PRODUCT_IDS[0]
        $updateData = @{
            name = "Test Product PS Basic Updated"
            description = "Updated basic product - modified by PowerShell test"
            price = 149.99
            stockQuantity = 30
        } | ConvertTo-Json
        
        $response = Invoke-TestRequest -Method PUT -Uri "$GATEWAY_URL/inventory/products/$productId" -Headers $headers -Body $updateData
        if ($response.Success -and $response.StatusCode -eq 200) {
            Pass-Test "Successfully updated product"
        } else {
            Fail-Test "Failed to update product (HTTP: $($response.StatusCode))"
        }
    }
    
    # Test 7: Test customer cannot create product
    if ($script:CUSTOMER_TOKEN) {
        Start-Test "Test customer cannot create product"
        $productData = @{
            name = "Unauthorized Product"
            description = "Should not be created by customer"
            price = 10.00
            stockQuantity = 1
        } | ConvertTo-Json
        
        $customerHeaders = @{
            "Authorization" = "Bearer $script:CUSTOMER_TOKEN"
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $customerHeaders -Body $productData
        if ($response.StatusCode -eq 403) {
            Pass-Test "Correctly blocked customer from creating product"
        } else {
            Fail-Test "Did not block customer from creating product (HTTP: $($response.StatusCode))"
        }
    }
    
    # Test 8: Test invalid product data
    Start-Test "Test invalid product data validation"
    $invalidProductData = @{
        name = ""
        description = ""
        price = -10.00
        stockQuantity = -1
    } | ConvertTo-Json
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $invalidProductData
    if ($response.StatusCode -eq 400) {
        Pass-Test "Correctly rejected invalid product data"
    } else {
        Fail-Test "Did not reject invalid product data (HTTP: $($response.StatusCode))"
    }
    
    # Test 9: Test product with missing fields
    Start-Test "Test product with missing required fields"
    $incompleteProductData = @{
        description = "Product missing name and price"
        stockQuantity = 5
    } | ConvertTo-Json
    
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $headers -Body $incompleteProductData
    if ($response.StatusCode -eq 400) {
        Pass-Test "Correctly rejected incomplete product data"
    } else {
        Fail-Test "Did not reject incomplete product data (HTTP: $($response.StatusCode))"
    }
    
    # Test 10: Test get non-existent product
    Start-Test "Test get non-existent product"
    $fakeId = [guid]::NewGuid().ToString()
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/inventory/products/$fakeId" -Headers @{ "Authorization" = "Bearer $script:ADMIN_TOKEN" }
    if ($response.StatusCode -eq 404) {
        Pass-Test "Correctly returned 404 for non-existent product"
    } else {
        Fail-Test "Did not return 404 for non-existent product (HTTP: $($response.StatusCode))"
    }
    
    # Test 11: Test update non-existent product
    Start-Test "Test update non-existent product"
    $updateData = @{
        name = "Non-existent Product"
        description = "This should fail"
        price = 99.99
        stockQuantity = 10
    } | ConvertTo-Json
    
    $response = Invoke-TestRequest -Method PUT -Uri "$GATEWAY_URL/inventory/products/$fakeId" -Headers $headers -Body $updateData
    if ($response.StatusCode -eq 404) {
        Pass-Test "Correctly returned 404 for updating non-existent product"
    } else {
        Fail-Test "Did not return 404 for updating non-existent product (HTTP: $($response.StatusCode))"
    }
    
    Write-Info "?? Product Tests Completed"
    Write-Host
}

function Clear-TestProducts {
    if ($SkipCleanup) {
        Write-Info "Skipping cleanup as requested"
        return
    }
    
    Write-Info "?? Cleaning up test products..."
    
    if (-not $script:ADMIN_TOKEN) {
        Write-Warning "Admin token not available, skipping cleanup"
        return
    }
    
    $headers = @{ "Authorization" = "Bearer $script:ADMIN_TOKEN" }
    
    foreach ($productId in $script:TEST_PRODUCT_IDS) {
        Start-Test "Cleanup product: $productId"
        $response = Invoke-TestRequest -Method DELETE -Uri "$GATEWAY_URL/inventory/products/$productId" -Headers $headers
        if ($response.Success -and $response.StatusCode -eq 204) {
            Pass-Test "Successfully deleted test product"
        } else {
            Fail-Test "Failed to delete test product (HTTP: $($response.StatusCode))"
        }
    }
    
    Write-Info "?? Product cleanup completed"
    Write-Host
}

function Show-Help {
    Write-Host @"
SalesAPI Product Tests (PowerShell)

Usage: .\03_products_tests.ps1 [OPTIONS]

Options:
  -SkipCleanup      Skip cleanup of test products
  -Help             Show this help message

Examples:
  .\03_products_tests.ps1                   # Run product tests with cleanup
  .\03_products_tests.ps1 -SkipCleanup      # Run product tests without cleanup
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

try {
    Test-Products
} finally {
    Clear-TestProducts
}

$success = Write-TestSummary

if ($success) {
    Write-Success "? Product tests completed successfully!"
    if ($env:TEST_PRODUCT_ID) {
        Write-Info "Test product ID for other scripts: $env:TEST_PRODUCT_ID"
    }
    exit 0
} else {
    Write-ErrorMessage "? Some product tests failed."
    exit 1
}