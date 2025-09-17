# SalesAPI Manual Tests - Authentication Tests (PowerShell)
# Tests all authentication-related functionality

param(
    [switch]$Help = $false
)

# Import utilities module
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module "$scriptPath\utils\test_utils.psm1" -Force

# Configuration
$configPath = "$scriptPath\config\endpoints.json"
$config = Get-Content $configPath | ConvertFrom-Json
$GATEWAY_URL = $config.gateway.baseUrl

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
    $adminToken = Get-AuthToken -Username "admin" -Password "admin123" -GatewayUrl $GATEWAY_URL
    if ($adminToken) {
        Pass-Test "Successfully authenticated as admin"
        $env:ADMIN_TOKEN = $adminToken
    } else {
        Fail-Test "Failed to authenticate as admin"
        $env:ADMIN_TOKEN = ""
    }
    
    # Test 3: Authenticate as customer
    Start-Test "Authenticate as customer"
    $customerToken = Get-AuthToken -Username "customer1" -Password "password123" -GatewayUrl $GATEWAY_URL
    if ($customerToken) {
        Pass-Test "Successfully authenticated as customer"
        $env:CUSTOMER_TOKEN = $customerToken
    } else {
        Fail-Test "Failed to authenticate as customer"
        $env:CUSTOMER_TOKEN = ""
    }
    
    # Test 4: Test invalid credentials
    Start-Test "Test invalid credentials"
    $invalidData = @{ username = "admin"; password = "wrong_password" } | ConvertTo-Json
    $headers = @{ "Content-Type" = "application/json" }
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/auth/token" -Headers $headers -Body $invalidData
    if ($response.StatusCode -eq 401) {
        Pass-Test "Correctly rejected invalid credentials"
    } else {
        Fail-Test "Did not reject invalid credentials (HTTP: $($response.StatusCode))"
    }
    
    # Test 5: Test missing credentials
    Start-Test "Test missing credentials"
    $emptyData = @{} | ConvertTo-Json
    $response = Invoke-TestRequest -Method POST -Uri "$GATEWAY_URL/auth/token" -Headers $headers -Body $emptyData
    if ($response.StatusCode -eq 400) {
        Pass-Test "Correctly rejected missing credentials"
    } else {
        Fail-Test "Did not reject missing credentials (HTTP: $($response.StatusCode))"
    }
    
    # Test 6: Validate JWT token structure
    if ($adminToken) {
        Start-Test "Validate JWT token structure"
        $tokenParts = $adminToken.Split('.')
        if ($tokenParts.Count -eq 3) {
            Pass-Test "JWT token has correct structure (3 parts)"
        } else {
            Fail-Test "JWT token has invalid structure (parts: $($tokenParts.Count))"
        }
    }
    
    # Test 7: Test token expiration handling
    Start-Test "Test malformed token handling"
    $headers = @{ 
        "Authorization" = "Bearer invalid_token"
        "Content-Type" = "application/json"
    }
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/inventory/products" -Headers $headers
    if ($response.StatusCode -eq 401) {
        Pass-Test "Correctly rejected malformed token"
    } else {
        Fail-Test "Did not reject malformed token (HTTP: $($response.StatusCode))"
    }
    
    Write-Info "?? Authentication Tests Completed"
    Write-Host
}

function Show-Help {
    Write-Host @"
SalesAPI Authentication Tests (PowerShell)

Usage: .\01_authentication_tests.ps1 [OPTIONS]

Options:
  -Help             Show this help message

Examples:
  .\01_authentication_tests.ps1             # Run authentication tests
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

Test-Authentication
$success = Write-TestSummary

if ($success) {
    Write-Success "? Authentication tests completed successfully!"
    exit 0
} else {
    Write-ErrorMessage "? Some authentication tests failed."
    exit 1
}