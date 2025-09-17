# SalesAPI Architecture Validation Script for Windows
# Validates the professional architecture standards are maintained

Write-Host "?? Validando arquitetura do SalesAPI..." -ForegroundColor Green

# Initialize validation results
$VALIDATION_ERRORS = 0
$VALIDATION_WARNINGS = 0

function Report-Error {
    param([string]$Message)
    Write-Host "? ERRO: $Message" -ForegroundColor Red
    $script:VALIDATION_ERRORS++
}

function Report-Warning {
    param([string]$Message)
    Write-Host "??  AVISO: $Message" -ForegroundColor Yellow
    $script:VALIDATION_WARNINGS++
}

function Report-Success {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Green
}

Write-Host ""
Write-Host "?? 1. Verificando implementa��es fake/mock em produ��o..." -ForegroundColor Cyan

# Check for fake/mock implementations in production code
$fakeFiles = Get-ChildItem -Path "src" -Recurse -Include "*.cs" | Where-Object {
    $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
    $content -match "Fake|Mock|Dummy|Test" -and $_.Name -notmatch "Test|Mock"
}

if ($fakeFiles.Count -eq 0) {
    Report-Success "Nenhuma implementa��o fake encontrada no c�digo de produ��o"
} else {
    Report-Error "Implementa��es fake/mock encontradas no c�digo de produ��o:"
    $fakeFiles | ForEach-Object { Write-Host "  - $($_.FullName)" -ForegroundColor Red }
}

Write-Host ""
Write-Host "?? 2. Verificando estrutura de pastas obrigat�ria..." -ForegroundColor Cyan

$requiredDirs = @(
    "src\gateway",
    "src\sales.api",
    "src\inventory.api",
    "tests",
    "docs",
    "scripts"
)

foreach ($dir in $requiredDirs) {
    if (Test-Path $dir) {
        Report-Success "$dir existe"
    } else {
        Report-Error "$dir n�o encontrado"
    }
}

Write-Host ""
Write-Host "?? 3. Verificando arquivos de configura��o..." -ForegroundColor Cyan

$configFiles = @(
    "src\gateway\appsettings.json",
    "src\sales.api\appsettings.json",
    "src\inventory.api\appsettings.json"
)

foreach ($file in $configFiles) {
    if (Test-Path $file) {
        Report-Success "$file existe"
        
        # Check for sensitive information exposure
        $content = Get-Content $file -Raw -ErrorAction SilentlyContinue
        if ($content -match 'password.*=.*[^*]') {
            Report-Warning "$file pode conter senhas em texto claro"
        }
    } else {
        Report-Error "$file n�o encontrado"
    }
}

Write-Host ""
Write-Host "?? 4. Verificando depend�ncias desnecess�rias..." -ForegroundColor Cyan

$projectFiles = Get-ChildItem -Recurse -Include "*.csproj" | ForEach-Object { $_.BaseName }
Write-Host "Projetos encontrados:"
$projectFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }

Write-Host ""
Write-Host "?? 5. Verificando testes..." -ForegroundColor Cyan

if (Test-Path "tests\SalesAPI.Tests.Professional") {
    Report-Success "Estrutura de testes profissionais encontrada"
    
    $testProjects = (Get-ChildItem -Path "tests" -Recurse -Include "*.csproj").Count
    Report-Success "Encontrados $testProjects projetos de teste"
    
    if ($testProjects -ge 3) {
        Report-Success "Cobertura de testes adequada (3+ projetos)"
    } else {
        Report-Warning "Cobertura de testes pode ser insuficiente (<3 projetos)"
    }
} else {
    Report-Error "Estrutura de testes profissionais n�o encontrada"
}

Write-Host ""
Write-Host "?? 6. Verificando documenta��o..." -ForegroundColor Cyan

$docItems = @("README.md", "docs")
foreach ($doc in $docItems) {
    if (Test-Path $doc) {
        Report-Success "Documenta��o $doc encontrada"
    } else {
        Report-Warning "Documenta��o $doc n�o encontrada"
    }
}

Write-Host ""
Write-Host "?? 7. Verificando scripts de automa��o..." -ForegroundColor Cyan

$scriptDirs = @("scripts\development", "scripts\testing", "scripts\manual-tests")
foreach ($scriptDir in $scriptDirs) {
    if (Test-Path $scriptDir) {
        $scriptCount = (Get-ChildItem -Path $scriptDir -Include "*.sh", "*.ps1" -Recurse).Count
        if ($scriptCount -gt 0) {
            Report-Success "$scriptDir cont�m $scriptCount scripts"
        } else {
            Report-Warning "$scriptDir existe mas n�o cont�m scripts"
        }
    } else {
        Report-Warning "$scriptDir n�o encontrado"
    }
}

Write-Host ""
Write-Host "?? 8. Verificando padr�es de c�digo..." -ForegroundColor Cyan

# Check for TODO/FIXME comments
$todoFiles = Get-ChildItem -Path "src" -Recurse -Include "*.cs" | Where-Object {
    $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
    $content -match "TODO|FIXME|HACK"
}

$todoCount = $todoFiles.Count

if ($todoCount -eq 0) {
    Report-Success "Nenhum TODO/FIXME/HACK encontrado no c�digo de produ��o"
} elseif ($todoCount -le 5) {
    Report-Warning "$todoCount TODOs/FIXMEs/HACKs encontrados (aceit�vel)"
} else {
    Report-Error "$todoCount TODOs/FIXMEs/HACKs encontrados (muitos para produ��o)"
}

# Final validation summary
Write-Host ""
Write-Host "==================================" -ForegroundColor White
Write-Host "?? RESUMO DA VALIDA��O" -ForegroundColor White
Write-Host "==================================" -ForegroundColor White

if ($VALIDATION_ERRORS -eq 0 -and $VALIDATION_WARNINGS -eq 0) {
    Write-Host "?? ARQUITETURA PERFEITA!" -ForegroundColor Green
    Write-Host "? Todos os crit�rios de qualidade foram atendidos" -ForegroundColor Green
    exit 0
} elseif ($VALIDATION_ERRORS -eq 0) {
    Write-Host "? ARQUITETURA APROVADA COM RESSALVAS" -ForegroundColor Yellow
    Write-Host "??  $VALIDATION_WARNINGS avisos encontrados" -ForegroundColor Yellow
    Write-Host "?? Considere resolver os avisos para melhorar ainda mais a qualidade" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "? ARQUITETURA PRECISA DE CORRE��ES" -ForegroundColor Red
    Write-Host "?? $VALIDATION_ERRORS erros cr�ticos encontrados" -ForegroundColor Red
    Write-Host "??  $VALIDATION_WARNINGS avisos encontrados" -ForegroundColor Yellow
    Write-Host "???  Corrija os erros antes de prosseguir para produ��o" -ForegroundColor Cyan
    exit 1
}