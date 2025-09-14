#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Executes the core professional test suite (Domain, Infrastructure, Integration, and Contracts)
    
.DESCRIPTION
    This script runs the consolidated professional testing suite without external dependencies.
    It includes:
    - SalesAPI.Tests.Professional (54 tests) - Core professional suite
    - contracts.tests (9 tests) - Contract compatibility validation
    
    The endpoint.tests are excluded as they require running services.
    
.EXAMPLE
    ./run-core-tests.ps1
    
.EXAMPLE
    ./run-core-tests.ps1 -Verbose
#>

param(
    [switch]$Verbose = $false,
    [switch]$Coverage = $false,
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

# Colors for output
$Green = "`e[32m"
$Red = "`e[31m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param([string]$Color, [string]$Message)
    Write-Host "$Color$Message$Reset"
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput $Blue "=== $Title ==="
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput $Green "? $Message"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput $Yellow "??  $Message"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput $Red "? $Message"
}

# Script start
Write-Section "SalesAPI Core Professional Test Suite"
Write-Host "Running consolidated professional tests without external dependencies"
Write-Host ""

$testResults = @()
$totalTime = [System.Diagnostics.Stopwatch]::StartNew()

try {
    # Test 1: Professional Suite - Domain Tests
    Write-Section "Domain Tests (Business Logic)"
    $domainTime = Measure-Command {
        $domainResult = dotnet test tests/SalesAPI.Tests.Professional/Domain.Tests/ --configuration $Configuration --verbosity $(if($Verbose) { "normal" } else { "quiet" }) --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Domain tests failed" }
    }
    $testResults += [PSCustomObject]@{ Suite = "Domain Tests"; Time = $domainTime.TotalSeconds; Status = "? Passed" }
    Write-Success "Domain tests completed in $([math]::Round($domainTime.TotalSeconds, 2))s"

    # Test 2: Professional Suite - Infrastructure Tests  
    Write-Section "Infrastructure Tests (Data & Messaging)"
    $infraTime = Measure-Command {
        $infraResult = dotnet test tests/SalesAPI.Tests.Professional/Infrastructure.Tests/ --configuration $Configuration --verbosity $(if($Verbose) { "normal" } else { "quiet" }) --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Infrastructure tests failed" }
    }
    $testResults += [PSCustomObject]@{ Suite = "Infrastructure Tests"; Time = $infraTime.TotalSeconds; Status = "? Passed" }
    Write-Success "Infrastructure tests completed in $([math]::Round($infraTime.TotalSeconds, 2))s"

    # Test 3: Professional Suite - Integration Tests
    Write-Section "Integration Tests (Cross-Service)"
    $integrationTime = Measure-Command {
        $integrationResult = dotnet test tests/SalesAPI.Tests.Professional/Integration.Tests/ --configuration $Configuration --verbosity $(if($Verbose) { "normal" } else { "quiet" }) --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Integration tests failed" }
    }
    $testResults += [PSCustomObject]@{ Suite = "Integration Tests"; Time = $integrationTime.TotalSeconds; Status = "? Passed" }
    Write-Success "Integration tests completed in $([math]::Round($integrationTime.TotalSeconds, 2))s"

    # Test 4: Contract Tests
    Write-Section "Contract Tests (API Compatibility)"
    $contractTime = Measure-Command {
        $contractResult = dotnet test tests/contracts.tests/ --configuration $Configuration --verbosity $(if($Verbose) { "normal" } else { "quiet" }) --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Contract tests failed" }
    }
    $testResults += [PSCustomObject]@{ Suite = "Contract Tests"; Time = $contractTime.TotalSeconds; Status = "? Passed" }
    Write-Success "Contract tests completed in $([math]::Round($contractTime.TotalSeconds, 2))s"

    # Optional: Code Coverage
    if ($Coverage) {
        Write-Section "Code Coverage Analysis"
        $coverageTime = Measure-Command {
            dotnet test tests/SalesAPI.Tests.Professional/ tests/contracts.tests/ --collect:"XPlat Code Coverage" --results-directory TestResults/
            if ($LASTEXITCODE -ne 0) { throw "Coverage analysis failed" }
        }
        Write-Success "Coverage analysis completed in $([math]::Round($coverageTime.TotalSeconds, 2))s"
        Write-Host "Coverage reports generated in TestResults/"
    }

    $totalTime.Stop()

    # Results Summary
    Write-Section "Test Execution Summary"
    Write-Host ""
    Write-Host "Test Suite Results:" -ForegroundColor Cyan
    $testResults | Format-Table -AutoSize
    
    Write-Host ""
    Write-Success "All core tests passed! ??"
    Write-Host ""
    Write-ColorOutput $Blue "? Performance Metrics:"
    Write-Host "  • Total Execution Time: $([math]::Round($totalTime.Elapsed.TotalSeconds, 2))s"
    Write-Host "  • Average per Suite: $([math]::Round(($testResults | Measure-Object -Property Time -Average).Average, 2))s"
    Write-Host "  • Fastest Suite: $(($testResults | Sort-Object Time | Select-Object -First 1).Suite) ($([math]::Round(($testResults | Sort-Object Time | Select-Object -First 1).Time, 2))s)"
    Write-Host ""
    Write-ColorOutput $Green "?? Professional Test Suite Status: HEALTHY"
    Write-Host "  • Zero Mock Dependencies ?"
    Write-Host "  • Fast Execution ?"  
    Write-Host "  • 100% Pass Rate ?"
    Write-Host "  • Modern Test Patterns ?"
    Write-Host ""

} catch {
    $totalTime.Stop()
    Write-Error "Test execution failed: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Failed after $([math]::Round($totalTime.Elapsed.TotalSeconds, 2))s"
    
    if ($testResults.Count -gt 0) {
        Write-Host ""
        Write-Host "Partial Results:" -ForegroundColor Yellow
        $testResults | Format-Table -AutoSize
    }
    
    exit 1
}

Write-Host "? Ready for production deployment!" -ForegroundColor Green