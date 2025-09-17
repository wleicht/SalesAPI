# SalesAPI Manual Tests - PowerShell Setup Script
# Quick setup and launch script for Windows environments

Write-Host "==========================================" -ForegroundColor Blue
Write-Host "   SalesAPI Manual Tests - Quick Start" -ForegroundColor Blue  
Write-Host "==========================================" -ForegroundColor Blue
Write-Host

# Set up results directory
Write-Host "Setting up results directory..." -ForegroundColor Yellow
$resultsDir = "scripts\manual-tests\results"
if (-not (Test-Path $resultsDir)) {
    New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
    Write-Host "? Results directory created" -ForegroundColor Green
} else {
    Write-Host "? Results directory exists" -ForegroundColor Green
}

# Check PowerShell execution policy
Write-Host "Checking PowerShell execution policy..." -ForegroundColor Yellow
$executionPolicy = Get-ExecutionPolicy
if ($executionPolicy -eq "Restricted") {
    Write-Host "? PowerShell execution policy is Restricted" -ForegroundColor Yellow
    Write-Host "To run tests, you may need to run:" -ForegroundColor Yellow
    Write-Host "  Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser" -ForegroundColor Cyan
    Write-Host
} else {
    Write-Host "? PowerShell execution policy: $executionPolicy" -ForegroundColor Green
}

# Check for curl
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    $curlVersion = curl.exe --version 2>$null
    if ($curlVersion) {
        Write-Host "? curl is available" -ForegroundColor Green
    }
} catch {
    Write-Host "? curl not found - using PowerShell Invoke-WebRequest instead" -ForegroundColor Yellow
}

# Check if services are running
Write-Host "Checking if services might be running..." -ForegroundColor Yellow

function Test-Port {
    param([int]$Port, [string]$ServiceName)
    
    try {
        $connection = Test-NetConnection -ComputerName "localhost" -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue
        if ($connection) {
            Write-Host "? Port $Port is in use (likely $ServiceName)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "? Port $Port is not in use ($ServiceName)" -ForegroundColor Red
            return $false
        }
    } catch {
        # Fallback method using netstat
        $netstat = netstat -an | Select-String ":$Port"
        if ($netstat) {
            Write-Host "? Port $Port is in use (likely $ServiceName)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "? Port $Port is not in use ($ServiceName)" -ForegroundColor Red
            return $false
        }
    }
}

$servicesRunning = 0
if (Test-Port 6000 "Gateway") { $servicesRunning++ }
if (Test-Port 5000 "Inventory API") { $servicesRunning++ }
if (Test-Port 5001 "Sales API") { $servicesRunning++ }

Write-Host

if ($servicesRunning -eq 3) {
    Write-Host "? All services appear to be running!" -ForegroundColor Green
    Write-Host "Ready to run tests!" -ForegroundColor Blue
    Write-Host
    Write-Host "Quick start:" -ForegroundColor Yellow
    Write-Host "  .\scripts\manual-tests\run_manual_tests.ps1" -ForegroundColor Cyan
} elseif ($servicesRunning -gt 0) {
    Write-Host "? Some services are running ($servicesRunning/3)" -ForegroundColor Yellow
    Write-Host "Please ensure all services are started before running tests." -ForegroundColor Yellow
} else {
    Write-Host "? No services appear to be running" -ForegroundColor Red
    Write-Host
    Write-Host "To start services:" -ForegroundColor Yellow
    Write-Host "  cd src\gateway; dotnet run" -ForegroundColor Cyan
    Write-Host "  cd src\inventory.api; dotnet run" -ForegroundColor Cyan  
    Write-Host "  cd src\sales.api; dotnet run" -ForegroundColor Cyan
    Write-Host
    Write-Host "Or using Docker:" -ForegroundColor Yellow
    Write-Host "  docker-compose up -d" -ForegroundColor Cyan
}

Write-Host
Write-Host "Available Commands:" -ForegroundColor Blue
Write-Host
Write-Host "PowerShell Commands:" -ForegroundColor Green
Write-Host "  .\scripts\manual-tests\run_manual_tests.ps1              # Run all tests" -ForegroundColor Cyan
Write-Host "  .\scripts\manual-tests\run_manual_tests.ps1 -OnlyBasic   # Run basic tests only" -ForegroundColor Cyan
Write-Host "  .\scripts\manual-tests\run_manual_tests.ps1 -Help        # Show help" -ForegroundColor Cyan
Write-Host
Write-Host "Bash Commands (Git Bash/WSL):" -ForegroundColor Green
Write-Host "  ./scripts/manual-tests/run_manual_tests.sh               # Run all tests" -ForegroundColor Cyan
Write-Host "  ./scripts/manual-tests/interactive_tests.sh              # Interactive menu" -ForegroundColor Cyan
Write-Host

Write-Host "Documentation:" -ForegroundColor Blue
Write-Host "  - Read: scripts\manual-tests\README.md" -ForegroundColor Cyan
Write-Host "  - Troubleshooting: scripts\manual-tests\troubleshooting.md" -ForegroundColor Cyan
Write-Host "  - Validation checklist: scripts\manual-tests\validation_checklist.md" -ForegroundColor Cyan
Write-Host
Write-Host "Happy testing! ??" -ForegroundColor Green

# Prompt to run tests immediately
$response = Read-Host "`nWould you like to run the basic tests now? (y/N)"
if ($response -match "^[Yy]") {
    Write-Host
    Write-Host "Starting basic tests..." -ForegroundColor Yellow
    & ".\scripts\manual-tests\run_manual_tests.ps1" -OnlyBasic
}