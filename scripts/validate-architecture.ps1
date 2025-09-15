# Script para validar que não há códigos fake em produção
Write-Host "?? Validating architecture..." -ForegroundColor Cyan

$errors = 0
$warnings = 0

Write-Host "`n?? Phase 1: Checking for fake implementations in production code..." -ForegroundColor Yellow

# Buscar por arquivos fake/mock/dummy no código de produção (excluindo testes)
$fakeFiles = Get-ChildItem -Recurse -Path "src" -Include "*Dummy*", "*Mock*", "*Fake*" | 
    Where-Object { $_.Name -notlike "*Test*" -and $_.Extension -eq ".cs" }

if ($fakeFiles.Count -gt 0) {
    Write-Host "? Found fake implementations in production code:" -ForegroundColor Red
    $fakeFiles | ForEach-Object { 
        Write-Host "  - $($_.FullName)" -ForegroundColor Red
        $errors++
    }
} else {
    Write-Host "? No fake implementations found in production code" -ForegroundColor Green
}

Write-Host "`n?? Phase 2: Checking for proper test infrastructure..." -ForegroundColor Yellow

# Verificar se MockEventPublisher existe apenas nos testes
$testMockFiles = Get-ChildItem -Recurse -Path "tests" -Include "*Mock*" | 
    Where-Object { $_.Extension -eq ".cs" }

if ($testMockFiles.Count -gt 0) {
    Write-Host "? Found $($testMockFiles.Count) mock implementations in test infrastructure:" -ForegroundColor Green
    $testMockFiles | ForEach-Object { 
        Write-Host "  + $($_.FullName)" -ForegroundColor Gray
    }
} else {
    Write-Host "?? No mock implementations found in tests" -ForegroundColor Yellow
    $warnings++
}

Write-Host "`n?? Phase 3: Validating configuration files..." -ForegroundColor Yellow

# Verificar se arquivos de configuração existem
$configFiles = @(
    "src\sales.api\appsettings.Development.json",
    "src\sales.api\appsettings.Production.json", 
    "src\inventory.api\appsettings.Development.json",
    "src\inventory.api\appsettings.Production.json"
)

$configFiles | ForEach-Object {
    if (Test-Path $_) {
        Write-Host "? Configuration file exists: $_" -ForegroundColor Green
        
        # Verificar se contém seção Messaging
        $content = Get-Content $_ -Raw | ConvertFrom-Json
        if ($content.PSObject.Properties.Name -contains "Messaging") {
            Write-Host "  + Contains Messaging configuration" -ForegroundColor Gray
        } else {
            Write-Host "  ?? Missing Messaging configuration" -ForegroundColor Yellow
            $warnings++
        }
    } else {
        Write-Host "? Configuration file missing: $_" -ForegroundColor Red
        $errors++
    }
}

Write-Host "`n?? Phase 4: Validating architecture implementation..." -ForegroundColor Yellow

# Verificar se RealEventPublisher existe
$realPublisherFiles = @(
    "src\sales.api\Services\EventPublisher\RealEventPublisher.cs"
)

$realPublisherFiles | ForEach-Object {
    if (Test-Path $_) {
        Write-Host "? Real implementation exists: $_" -ForegroundColor Green
    } else {
        Write-Host "? Real implementation missing: $_" -ForegroundColor Red
        $errors++
    }
}

# Verificar se MessagingConfiguration foi atualizada
$messagingConfigFiles = @(
    "src\sales.api\Configuration\MessagingConfiguration.cs",
    "src\inventory.api\Configuration\MessagingConfiguration.cs"
)

$messagingConfigFiles | ForEach-Object {
    if (Test-Path $_) {
        Write-Host "? Messaging configuration exists: $_" -ForegroundColor Green
        
        # Verificar se contém NullEventPublisher
        $content = Get-Content $_
        if ($content -match "NullEventPublisher") {
            Write-Host "  + Contains NullEventPublisher implementation" -ForegroundColor Gray
        } else {
            Write-Host "  ?? Missing NullEventPublisher implementation" -ForegroundColor Yellow
            $warnings++
        }
    } else {
        Write-Host "? Messaging configuration missing: $_" -ForegroundColor Red
        $errors++
    }
}

Write-Host "`n?? Phase 5: Final validation summary..." -ForegroundColor Yellow

Write-Host "`n?? VALIDATION SUMMARY:" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

if ($errors -eq 0 -and $warnings -eq 0) {
    Write-Host "?? ARCHITECTURE VALIDATION PASSED!" -ForegroundColor Green
    Write-Host "? All checks completed successfully" -ForegroundColor Green
    Write-Host "? No fake implementations in production code" -ForegroundColor Green
    Write-Host "? Professional architecture implemented" -ForegroundColor Green
} elseif ($errors -eq 0) {
    Write-Host "?? ARCHITECTURE VALIDATION COMPLETED WITH WARNINGS" -ForegroundColor Yellow
    Write-Host "? No critical errors found" -ForegroundColor Green
    Write-Host "?? $warnings warning(s) found - please review" -ForegroundColor Yellow
} else {
    Write-Host "? ARCHITECTURE VALIDATION FAILED!" -ForegroundColor Red
    Write-Host "? $errors error(s) found - must be fixed" -ForegroundColor Red
    Write-Host "?? $warnings warning(s) found - should be reviewed" -ForegroundColor Yellow
}

Write-Host "`n?? Architecture validation completed" -ForegroundColor Cyan

if ($errors -gt 0) {
    exit 1
} else {
    exit 0
}