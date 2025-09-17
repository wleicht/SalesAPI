# SalesAPI Manual Tests - Health Check Tests (PowerShell)
# Tests all health check endpoints

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
$INVENTORY_URL = $config.inventory.baseUrl
$SALES_URL = $config.sales.baseUrl

function Test-Health {
    Write-Info "?? Starting Health Check Tests..."
    
    # Test 1: Gateway status
    Start-Test "Gateway status check"
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/gateway/status"
    if ($response.Success -and $response.StatusCode -eq 200) {
        if ($response.Content -match "Healthy") {
            Pass-Test "Gateway is healthy"
        } else {
            Pass-Test "Gateway responded but may not be fully healthy"
        }
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
    
    # Test 7: Response time validation
    Start-Test "Response time validation"
    $startTime = Get-Date
    $response = Invoke-TestRequest -Method GET -Uri "$GATEWAY_URL/gateway/status"
    $endTime = Get-Date
    $responseTime = ($endTime - $startTime).TotalMilliseconds
    
    if ($response.Success -and $responseTime -lt 5000) {
        Pass-Test "Response time acceptable ($([math]::Round($responseTime, 0))ms)"
    } else {
        Fail-Test "Response time too high ($([math]::Round($responseTime, 0))ms)"
    }
    
    # Test 8: Concurrent health checks
    Start-Test "Concurrent health checks"
    $jobs = @()
    
    # Start multiple health check requests
    $jobs += Start-Job -ScriptBlock {
        param($url)
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
            return $response.StatusCode
        } catch {
            return 0
        }
    } -ArgumentList "$GATEWAY_URL/gateway/status"
    
    $jobs += Start-Job -ScriptBlock {
        param($url)
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
            return $response.StatusCode
        } catch {
            return 0
        }
    } -ArgumentList "$INVENTORY_URL/health"
    
    $jobs += Start-Job -ScriptBlock {
        param($url)
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
            return $response.StatusCode
        } catch {
            return 0
        }
    } -ArgumentList "$SALES_URL/health"
    
    # Wait for all jobs and check results
    $results = $jobs | Wait-Job | Receive-Job
    $jobs | Remove-Job
    
    $successCount = ($results | Where-Object { $_ -eq 200 }).Count
    if ($successCount -eq 3) {
        Pass-Test "All concurrent health checks successful"
    } else {
        Fail-Test "Some concurrent health checks failed ($successCount/3 successful)"
    }
    
    Write-Info "?? Health Check Tests Completed"
    Write-Host
}

function Show-Help {
    Write-Host @"
SalesAPI Health Check Tests (PowerShell)

Usage: .\02_health_tests.ps1 [OPTIONS]

Options:
  -Help             Show this help message

Examples:
  .\02_health_tests.ps1                     # Run health check tests
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

Test-Health
$success = Write-TestSummary

if ($success) {
    Write-Success "? Health check tests completed successfully!"
    exit 0
} else {
    Write-ErrorMessage "? Some health check tests failed."
    exit 1
}