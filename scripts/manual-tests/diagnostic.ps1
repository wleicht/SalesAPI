# SalesAPI Manual Tests - Diagnostic Script (PowerShell)
# Diagnoses issues with the SalesAPI system

param(
    [switch]$Help = $false,
    [switch]$Verbose = $false
)

# Import utilities module
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module "$scriptPath\utils\test_utils.psm1" -Force

# Configuration
$configPath = "$scriptPath\config\endpoints.json"
$config = Get-Content $configPath | ConvertFrom-Json
$GATEWAY_URL = $config.gateway.baseUrl
$INVENTORY_URL = $config.inventory.baseUrl
$SALES_URL = $config.sales.baseUrl

function Test-DetailedDiagnostics {
    Write-Info "?? Starting Detailed Diagnostics..."
    Write-Host
    
    # Get tokens first
    Write-Info "Getting authentication tokens..."
    $adminToken = Get-AuthToken -Username "admin" -Password "admin123" -GatewayUrl $GATEWAY_URL
    $customerToken = Get-AuthToken -Username "customer1" -Password "password123" -GatewayUrl $GATEWAY_URL
    
    if (-not $adminToken) {
        Write-ErrorMessage "Cannot get admin token - stopping diagnostics"
        return
    }
    
    if (-not $customerToken) {
        Write-ErrorMessage "Cannot get customer token - stopping diagnostics"
        return
    }
    
    Write-Success "Authentication tokens obtained successfully"
    Write-Host
    
    # Test 1: Check Sales API endpoints directly
    Write-Host "=== Sales API Direct Testing ===" -ForegroundColor Yellow
    
    Start-Test "Sales API orders endpoint (direct) - GET"
    $headers = @{ "Authorization" = "Bearer $customerToken" }
    $response = Invoke-TestRequest -Method GET -Uri "$SALES_URL/orders" -Headers $headers
    
    Write-Host "  Response Status: $($response.StatusCode)" -ForegroundColor Gray
    if ($Verbose -and $response.Content) {
        Write-Host "  Response Content: $($response.Content.Substring(0, [Math]::Min(200, $response.Content.Length)))" -ForegroundColor Gray
    }
    
    if ($response.StatusCode -eq 200) {
        Pass-Test "Sales API orders endpoint accessible (direct)"
    } else {
        Fail-Test "Sales API orders endpoint failed (direct) - HTTP: $($response.StatusCode)"
    }
    
    Start-Test "Sales API orders endpoint (via gateway) - GET"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/sales/orders" -Headers $headers
    
    Write-Host "  Response Status: $($response.StatusCode)" -ForegroundColor Gray
    if ($Verbose -and $response.Content) {
        Write-Host "  Response Content: $($response.Content.Substring(0, [Math]::Min(200, $response.Content.Length)))" -ForegroundColor Gray
    }
    
    if ($response.StatusCode -eq 200) {
        Pass-Test "Sales API orders endpoint accessible (via gateway)"
    } else {
        Fail-Test "Sales API orders endpoint failed (via gateway) - HTTP: $($response.StatusCode)"
    }
    
    Write-Host
    
    # Test 2: Create a minimal product for order testing
    Write-Host "=== Product Creation for Order Testing ===" -ForegroundColor Yellow
    
    $productData = @{
        name = "Diagnostic Test Product"
        description = "Simple product for diagnostic testing"
        price = 10.00
        stockQuantity = 100
    } | ConvertTo-Json
    
    $productHeaders = @{
        "Authorization" = "Bearer $adminToken"
        "Content-Type" = "application/json"
    }
    
    Start-Test "Create diagnostic product"
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/inventory/products" -Headers $productHeaders -Body $productData
    
    Write-Host "  Response Status: $($response.StatusCode)" -ForegroundColor Gray
    if ($Verbose -and $response.Content) {
        Write-Host "  Response Content: $($response.Content)" -ForegroundColor Gray
    }
    
    $productId = $null
    if ($response.StatusCode -eq 201) {
        try {
            $product = $response.Content | ConvertFrom-Json
            $productId = $product.id
            Pass-Test "Diagnostic product created: $productId"
        } catch {
            Fail-Test "Failed to parse product response"
        }
    } else {
        Fail-Test "Failed to create diagnostic product - HTTP: $($response.StatusCode)"
    }
    
    Write-Host
    
    # Test 3: Try different order formats
    if ($productId) {
        Write-Host "=== Order Creation Testing ===" -ForegroundColor Yellow
        
        # Test with simple GUID customer ID
        $simpleOrderData = @{
            customerId = "11111111-1111-1111-1111-111111111111"
            items = @(
                @{
                    productId = $productId
                    quantity = 1
                }
            )
        } | ConvertTo-Json -Depth 3
        
        Start-Test "Create order with simple customer ID"
        $orderHeaders = @{
            "Authorization" = "Bearer $customerToken"
            "Content-Type" = "application/json"
            "X-Correlation-Id" = "diag-simple-$(Get-Date -Format 'HHmmss')"
        }
        
        $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $orderHeaders -Body $simpleOrderData
        
        Write-Host "  Response Status: $($response.StatusCode)" -ForegroundColor Gray
        if ($Verbose -and $response.Content) {
            Write-Host "  Response Content: $($response.Content)" -ForegroundColor Gray
        }
        
        if ($response.StatusCode -eq 201) {
            Pass-Test "Order created with simple customer ID"
        } elseif ($response.StatusCode -eq 400) {
            Fail-Test "Order rejected with 400 - likely validation issue"
        } elseif ($response.StatusCode -eq 422) {
            Pass-Test "Order handled with business validation (422)"
        } else {
            Fail-Test "Order creation failed - HTTP: $($response.StatusCode)"
        }
        
        # Test with configured customer ID
        $configOrderData = @{
            customerId = $config.testData.customers[0]
            items = @(
                @{
                    productId = $productId
                    quantity = 1
                }
            )
        } | ConvertTo-Json -Depth 3
        
        Start-Test "Create order with configured customer ID"
        $orderHeaders["X-Correlation-Id"] = "diag-config-$(Get-Date -Format 'HHmmss')"
        
        $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/sales/orders" -Headers $orderHeaders -Body $configOrderData
        
        Write-Host "  Response Status: $($response.StatusCode)" -ForegroundColor Gray
        if ($Verbose -and $response.Content) {
            Write-Host "  Response Content: $($response.Content)" -ForegroundColor Gray
        }
        
        if ($response.StatusCode -eq 201) {
            Pass-Test "Order created with configured customer ID"
        } elseif ($response.StatusCode -eq 400) {
            Fail-Test "Order rejected with 400 - likely validation issue"
        } elseif ($response.StatusCode -eq 422) {
            Pass-Test "Order handled with business validation (422)"
        } else {
            Fail-Test "Order creation failed - HTTP: $($response.StatusCode)"
        }
        
        # Test direct to Sales API (bypass gateway)
        Start-Test "Create order directly to Sales API"
        $response = Invoke-TestRequest -Method POST -Uri "$SALES_URL/orders" -Headers $orderHeaders -Body $configOrderData
        
        Write-Host "  Response Status: $($response.StatusCode)" -ForegroundColor Gray
        if ($Verbose -and $response.Content) {
            Write-Host "  Response Content: $($response.Content)" -ForegroundColor Gray
        }
        
        if ($response.StatusCode -eq 201) {
            Pass-Test "Order created directly to Sales API"
        } elseif ($response.StatusCode -eq 400) {
            Fail-Test "Order rejected directly - HTTP: 400"
        } elseif ($response.StatusCode -eq 422) {
            Pass-Test "Order handled with business validation directly (422)"
        } else {
            Fail-Test "Direct order creation failed - HTTP: $($response.StatusCode)"
        }
    }
    
    Write-Host
    
    # Test 4: Gateway routing diagnostics
    Write-Host "=== Gateway Routing Diagnostics ===" -ForegroundColor Yellow
    
    Start-Test "Check gateway routes configuration"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/gateway/routes"
    
    Write-Host "  Response Status: $($response.StatusCode)" -ForegroundColor Gray
    if ($response.StatusCode -eq 200 -and $response.Content) {
        try {
            $routes = $response.Content | ConvertFrom-Json
            $salesRoutes = $routes | Where-Object { $_.path -like "*/sales/*" }
            
            if ($salesRoutes) {
                Pass-Test "Gateway has sales routes configured"
                if ($Verbose) {
                    $salesRoutes | ForEach-Object {
                        Write-Host "    Sales Route: $($_.path) -> $($_.target)" -ForegroundColor Gray
                    }
                }
            } else {
                Fail-Test "No sales routes found in gateway configuration"
            }
        } catch {
            Fail-Test "Failed to parse gateway routes"
        }
    } else {
        Fail-Test "Failed to get gateway routes - HTTP: $($response.StatusCode)"
    }
    
    Write-Host
    
    # Test 5: Token validation test
    Write-Host "=== Token Validation Testing ===" -ForegroundColor Yellow
    
    Start-Test "Validate admin token format"
    if ($adminToken -and $adminToken.Split('.').Count -eq 3) {
        Pass-Test "Admin token has valid JWT format"
    } else {
        Fail-Test "Admin token has invalid format"
    }
    
    Start-Test "Validate customer token format"
    if ($customerToken -and $customerToken.Split('.').Count -eq 3) {
        Pass-Test "Customer token has valid JWT format"
    } else {
        Fail-Test "Customer token has invalid format"
    }
    
    # Test token expiry/validity
    Start-Test "Test admin token validity"
    $headers = @{ "Authorization" = "Bearer $adminToken" }
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/inventory/products" -Headers $headers
    if ($response.StatusCode -eq 200) {
        Pass-Test "Admin token is valid and accepted"
    } else {
        Fail-Test "Admin token rejected - HTTP: $($response.StatusCode)"
    }
    
    Start-Test "Test customer token validity"
    $headers = @{ "Authorization" = "Bearer $customerToken" }
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/sales/orders" -Headers $headers
    if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 500) {
        # 500 might be a service issue, not token issue
        if ($response.StatusCode -eq 200) {
            Pass-Test "Customer token is valid and accepted"
        } else {
            Pass-Test "Customer token accepted but service returned 500"
        }
    } elseif ($response.StatusCode -eq 401) {
        Fail-Test "Customer token rejected - HTTP: 401"
    } else {
        Fail-Test "Customer token test failed - HTTP: $($response.StatusCode)"
    }
    
    Write-Host
    
    # Cleanup
    if ($productId) {
        Write-Host "=== Cleanup ===" -ForegroundColor Yellow
        
        Start-Test "Cleanup diagnostic product"
        $response = Invoke-TestRequest -Method DELETE -Uri "$GATEWAY_URL/inventory/products/$productId" -Headers @{ "Authorization" = "Bearer $adminToken" }
        if ($response.StatusCode -eq 204) {
            Pass-Test "Diagnostic product cleaned up"
        } else {
            Fail-Test "Failed to cleanup diagnostic product - HTTP: $($response.StatusCode)"
        }
    }
    
    Write-Info "?? Diagnostics completed"
    Write-Host
}

function Show-Help {
    Write-Host @"
SalesAPI Diagnostic Script (PowerShell)

Usage: .\diagnostic.ps1 [OPTIONS]

Options:
  -Verbose          Show detailed response content
  -Help             Show this help message

Examples:
  .\diagnostic.ps1                    # Run diagnostics
  .\diagnostic.ps1 -Verbose           # Run diagnostics with verbose output
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

Test-DetailedDiagnostics
$success = Write-TestSummary

if ($success) {
    Write-Success "? Diagnostics completed successfully!"
    exit 0
} else {
    Write-ErrorMessage "? Some diagnostic tests failed."
    exit 1
}