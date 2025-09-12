# SalesAPI Professional Test Suite Runner (PowerShell)
# Execute este script para rodar todos os testes profissionais

param(
    [switch]$Verbose = $false
)

# Colors for console output
function Write-ColorText {
    param(
        [string]$Text,
        [ConsoleColor]$Color = [ConsoleColor]::White
    )
    $originalColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $Color
    Write-Host $Text
    $Host.UI.RawUI.ForegroundColor = $originalColor
}

Write-ColorText "?? SalesAPI Professional Test Suite" -Color Cyan
Write-ColorText "====================================" -Color Cyan
Write-Host ""

# Test counters
$script:TotalTests = 0
$script:TotalPassed = 0
$script:TotalFailed = 0
$script:TotalTime = 0

# Function to run tests and capture results
function Invoke-TestProject {
    param(
        [string]$ProjectName,
        [string]$ProjectPath
    )
    
    Write-ColorText "?? Running $ProjectName..." -Color Blue
    
    $startTime = Get-Date
    $verbosity = if ($Verbose) { "normal" } else { "quiet" }
    
    try {
        $testOutput = & dotnet test $ProjectPath --verbosity $verbosity --logger "console;verbosity=minimal" 2>&1
        $endTime = Get-Date
        $duration = [math]::Round(($endTime - $startTime).TotalSeconds, 1)
        
        if ($LASTEXITCODE -eq 0) {
            # Parse test results from output
            $outputStr = $testOutput -join "`n"
            
            # Try to extract test counts
            $passedMatch = [regex]::Match($outputStr, "total: (\d+); falhou: (\d+); bem-sucedido: (\d+)")
            if ($passedMatch.Success) {
                $total = [int]$passedMatch.Groups[1].Value
                $failed = [int]$passedMatch.Groups[2].Value  
                $passed = [int]$passedMatch.Groups[3].Value
            } else {
                # Fallback parsing
                $passed = if ($outputStr -match "(\d+) passed") { [int]$matches[1] } else { 0 }
                $failed = if ($outputStr -match "(\d+) failed") { [int]$matches[1] } else { 0 }
            }
            
            $script:TotalTests += ($passed + $failed)
            $script:TotalPassed += $passed
            $script:TotalFailed += $failed
            $script:TotalTime += $duration
            
            if ($failed -eq 0) {
                Write-ColorText "   ? $passed tests passed ($duration" + "s)" -Color Green
            } else {
                Write-ColorText "   ? $failed tests failed, $passed passed ($duration" + "s)" -Color Red
            }
        } else {
            Write-ColorText "   ? Build/Test execution failed" -Color Red
            if ($Verbose) {
                Write-Host $testOutput
            }
            return $false
        }
    }
    catch {
        Write-ColorText "   ? Exception during test execution: $_" -Color Red
        return $false
    }
    
    Write-Host ""
    return $true
}

# Build TestInfrastructure first
Write-ColorText "?? Building Test Infrastructure..." -Color Yellow
$buildResult = & dotnet build tests/SalesAPI.Tests.Professional/TestInfrastructure/TestInfrastructure.csproj --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-ColorText "? Failed to build TestInfrastructure" -Color Red
    exit 1
}
Write-ColorText "? TestInfrastructure built successfully" -Color Green
Write-Host ""

# Run each test project
$allSuccess = $true

$allSuccess = (Invoke-TestProject "Domain Tests" "tests/SalesAPI.Tests.Professional/Domain.Tests/Domain.Tests.csproj") -and $allSuccess
$allSuccess = (Invoke-TestProject "Infrastructure Tests" "tests/SalesAPI.Tests.Professional/Infrastructure.Tests/Infrastructure.Tests.csproj") -and $allSuccess
$allSuccess = (Invoke-TestProject "Integration Tests" "tests/SalesAPI.Tests.Professional/Integration.Tests/Integration.Tests.csproj") -and $allSuccess

# Summary
Write-ColorText "?? TEST SUMMARY" -Color Cyan
Write-ColorText "===============" -Color Cyan

if ($script:TotalFailed -eq 0 -and $allSuccess) {
    Write-ColorText "?? All tests passed!" -Color Green
    Write-ColorText "   Total: $($script:TotalPassed) tests" -Color Green  
    Write-ColorText "   Time:  $($script:TotalTime)s" -Color Blue
    Write-Host ""
    Write-ColorText "? Professional test suite completed successfully!" -Color Green
    exit 0
} else {
    Write-ColorText "? Some tests failed or build errors occurred" -Color Red
    Write-ColorText "   Passed: $($script:TotalPassed)" -Color Green
    Write-ColorText "   Failed: $($script:TotalFailed)" -Color Red  
    Write-ColorText "   Total:  $($script:TotalTests) tests" -Color White
    Write-ColorText "   Time:   $($script:TotalTime)s" -Color Blue
    Write-Host ""
    Write-ColorText "??  Please check the failed tests above" -Color Yellow
    exit 1
}