# SalesAPI Manual Tests - PowerShell Utility Functions
# Common functions used across PowerShell test scripts

# Colors for output
$script:Colors = @{
    Red = "Red"
    Green = "Green"
    Yellow = "Yellow"
    Blue = "Blue"
    Magenta = "Magenta"
    Cyan = "Cyan"
    White = "White"
}

# Test result variables (script-scoped)
$script:TEST_COUNT = 0
$script:PASSED_COUNT = 0
$script:FAILED_COUNT = 0
$script:CURRENT_TEST = ""

# Configuration
$script:RESULTS_DIR = "scripts\manual-tests\results"
$script:TIMESTAMP = Get-Date -Format "yyyyMMdd_HHmmss"
$script:RESULTS_FILE = "$script:RESULTS_DIR\test_results_$script:TIMESTAMP.json"
$script:SUMMARY_FILE = "$script:RESULTS_DIR\test_summary_$script:TIMESTAMP.txt"
$script:FAILED_LOG = "$script:RESULTS_DIR\failed_tests.log"

# Create results directory if it doesn't exist
if (-not (Test-Path $script:RESULTS_DIR)) {
    New-Item -ItemType Directory -Path $script:RESULTS_DIR -Force | Out-Null
}

# Logging functions
function Write-Info { 
    param([string]$Message) 
    Write-Host "[INFO] $Message" -ForegroundColor $script:Colors.Blue
}

function Write-Success { 
    param([string]$Message) 
    Write-Host "[SUCCESS] $Message" -ForegroundColor $script:Colors.Green
}

function Write-Warning { 
    param([string]$Message) 
    Write-Host "[WARNING] $Message" -ForegroundColor $script:Colors.Yellow
}

function Write-ErrorMessage { 
    param([string]$Message) 
    Write-Host "[ERROR] $Message" -ForegroundColor $script:Colors.Red
}

function Write-Test { 
    param([string]$Message) 
    Write-Host "[TEST] $Message" -ForegroundColor $script:Colors.Magenta
}

# Test execution functions
function Start-Test {
    param([string]$TestName)
    $script:CURRENT_TEST = $TestName
    $script:TEST_COUNT++
    Write-Test "Starting: $TestName"
}

function Pass-Test {
    param([string]$Message = "")
    $script:PASSED_COUNT++
    Write-Success "PASSED: $script:CURRENT_TEST"
    if ($Message) { 
        Write-Host "  $Message" -ForegroundColor Gray 
    }
    Record-TestResult "PASSED" $Message
}

function Fail-Test {
    param([string]$Message)
    $script:FAILED_COUNT++
    Write-ErrorMessage "FAILED: $script:CURRENT_TEST - $Message"
    "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - FAILED: $script:CURRENT_TEST - $Message" | Add-Content $script:FAILED_LOG
    Record-TestResult "FAILED" $Message
}

function Skip-Test {
    param([string]$Message)
    Write-Warning "SKIPPED: $script:CURRENT_TEST - $Message"
    Record-TestResult "SKIPPED" $Message
}

# Record test result to file
function Record-TestResult {
    param(
        [string]$Status,
        [string]$Message
    )
    
    # For simplicity, we'll just log to a simple format
    # In a full implementation, you'd use proper JSON handling
    $testResult = @{
        Name = $script:CURRENT_TEST
        Status = $Status
        Message = $Message
        Timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ss"
    }
    
    # Simple JSON-like logging
    "$($testResult | ConvertTo-Json -Compress)" | Add-Content "$script:RESULTS_DIR\test_log_$script:TIMESTAMP.txt"
}

# HTTP helper functions
function Invoke-TestRequest {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [int]$ExpectedStatus = 200,
        [int]$TimeoutSec = 30
    )
    
    try {
        $requestParams = @{
            Uri = $Uri
            Method = $Method
            UseBasicParsing = $true
            TimeoutSec = $TimeoutSec
        }
        
        if ($Headers.Count -gt 0) {
            $requestParams.Headers = $Headers
        }
        
        if ($Body) {
            $requestParams.Body = $Body
            if (-not $Headers.ContainsKey("Content-Type")) {
                $requestParams.ContentType = "application/json"
            }
        }
        
        $response = Invoke-WebRequest @requestParams
        
        return @{
            StatusCode = $response.StatusCode
            Content = $response.Content
            Headers = $response.Headers
            Success = $true
            TimeMs = 0  # PowerShell doesn't provide easy timing
        }
    }
    catch [System.Net.WebException] {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        
        return @{
            StatusCode = $statusCode
            Content = $_.Exception.Message
            Headers = @{}
            Success = $false
            Exception = $_.Exception
        }
    }
    catch {
        return @{
            StatusCode = 0
            Content = $_.Exception.Message
            Headers = @{}
            Success = $false
            Exception = $_.Exception
        }
    }
}

# Authentication helper
function Get-AuthToken {
    param(
        [string]$Username,
        [string]$Password,
        [string]$GatewayUrl
    )
    
    $loginData = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json
    
    $headers = @{ "Content-Type" = "application/json" }
    $response = Invoke-TestRequest -Method POST -Uri "$GatewayUrl/auth/token" -Headers $headers -Body $loginData
    
    if ($response.Success -and $response.StatusCode -eq 200) {
        try {
            $tokenData = $response.Content | ConvertFrom-Json
            return $tokenData.accessToken
        }
        catch {
            return $null
        }
    }
    
    return $null
}

# Service check function
function Test-Service {
    param(
        [string]$ServiceName,
        [string]$Url
    )
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Success "$ServiceName is running"
            return $true
        }
        else {
            Write-ErrorMessage "$ServiceName is not responding (HTTP: $($response.StatusCode))"
            return $false
        }
    }
    catch {
        Write-ErrorMessage "$ServiceName is not responding: $($_.Exception.Message)"
        return $false
    }
}

# Load configuration from JSON file
function Get-ConfigValue {
    param(
        [string]$JsonPath,
        [string]$ConfigFile = "scripts\manual-tests\config\endpoints.json"
    )
    
    if (Test-Path $ConfigFile) {
        try {
            $config = Get-Content $ConfigFile | ConvertFrom-Json
            # Simple path resolution - for more complex paths, you'd need proper JSON path parsing
            $parts = $JsonPath -replace '^\.' -split '\.'
            $current = $config
            
            foreach ($part in $parts) {
                if ($current.$part) {
                    $current = $current.$part
                }
                else {
                    return $null
                }
            }
            
            return $current
        }
        catch {
            Write-Warning "Failed to parse configuration file: $($_.Exception.Message)"
            return $null
        }
    }
    else {
        Write-Warning "Configuration file not found: $ConfigFile"
        return $null
    }
}

# Generate test summary
function Write-TestSummary {
    $total = $script:TEST_COUNT
    $passed = $script:PASSED_COUNT
    $failed = $script:FAILED_COUNT
    $successRate = if ($total -gt 0) { [math]::Round(($passed * 100) / $total, 2) } else { 0 }
    
    # Write to summary file
    $summaryContent = @"
============================================
SalesAPI Manual Tests Summary (PowerShell)
============================================
Timestamp: $(Get-Date)
Total Tests: $total
Passed: $passed
Failed: $failed
Success Rate: $successRate%
============================================
"@
    
    $summaryContent | Set-Content $script:SUMMARY_FILE
    
    if ($failed -gt 0 -and (Test-Path $script:FAILED_LOG)) {
        "" | Add-Content $script:SUMMARY_FILE
        "Failed Tests:" | Add-Content $script:SUMMARY_FILE
        Get-Content $script:FAILED_LOG | Add-Content $script:SUMMARY_FILE
    }
    
    # Display summary
    Write-Host
    Write-Info "============================================"
    Write-Info "SalesAPI Manual Tests Summary"
    Write-Info "============================================"
    Write-Info "Total Tests: $total"
    if ($passed -gt 0) { Write-Success "Passed: $passed" }
    if ($failed -gt 0) { Write-ErrorMessage "Failed: $failed" }
    Write-Info "Success Rate: $successRate%"
    Write-Info "============================================"
    Write-Info "Summary: $script:SUMMARY_FILE"
    
    if ($failed -gt 0) {
        Write-Info "Failed tests log: $script:FAILED_LOG"
        return $false
    }
    
    return $true
}

# Cleanup function
function Clear-TestData {
    Write-Info "Cleaning up test data..."
    # Implementation would depend on specific cleanup requirements
}

# Wait for user input
function Wait-ForUser {
    Write-Host
    Read-Host "Press Enter to continue"
    Write-Host
}

# Check prerequisites
function Test-Prerequisites {
    Write-Info "Checking prerequisites..."
    
    # Check PowerShell version
    $psVersion = $PSVersionTable.PSVersion
    if ($psVersion.Major -lt 5) {
        Write-ErrorMessage "PowerShell 5.0 or later is required (current: $psVersion)"
        return $false
    }
    
    # Check if Invoke-WebRequest is available
    if (Get-Command Invoke-WebRequest -ErrorAction SilentlyContinue) {
        Write-Success "Invoke-WebRequest is available"
    }
    else {
        Write-ErrorMessage "Invoke-WebRequest is not available"
        return $false
    }
    
    # Check if ConvertTo-Json is available
    if (Get-Command ConvertTo-Json -ErrorAction SilentlyContinue) {
        Write-Success "JSON cmdlets are available"
    }
    else {
        Write-ErrorMessage "JSON cmdlets are not available"
        return $false
    }
    
    Write-Success "Prerequisites check completed"
    return $true
}

# Utility function to create correlation ID
function New-CorrelationId {
    param([string]$Prefix = "ps-test")
    return "$Prefix-$([guid]::NewGuid().ToString('N').Substring(0,8))-$(Get-Date -Format 'HHmmss')"
}

# Export functions (PowerShell module style)
Export-ModuleMember -Function @(
    'Write-Info',
    'Write-Success', 
    'Write-Warning',
    'Write-ErrorMessage',
    'Write-Test',
    'Start-Test',
    'Pass-Test',
    'Fail-Test',
    'Skip-Test',
    'Invoke-TestRequest',
    'Get-AuthToken',
    'Test-Service',
    'Get-ConfigValue',
    'Write-TestSummary',
    'Clear-TestData',
    'Wait-ForUser',
    'Test-Prerequisites',
    'New-CorrelationId'
)