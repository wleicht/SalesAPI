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
Write-Host "?? 1. Verificando implementações fake/mock em produção..." -ForegroundColor Cyan

# Check for fake/mock implementations in production code
$fakeFiles = Get-ChildItem -Path "src" -Recurse -Include "*.cs" | Where-Object {
    $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
    $content -match "Fake|Mock|Dummy|Test" -and $_.Name -notmatch "Test|Mock"
}

if ($fakeFiles.Count -eq 0) {
    Report-Success "Nenhuma implementação fake encontrada no código de produção"
} else {
    Report-Error "Implementações fake/mock encontradas no código de produção:"
    $fakeFiles | ForEach-Object { Write-Host "  - $($_.FullName)" -ForegroundColor Red }
}

Write-Host ""
Write-Host "?? 2. Verificando estrutura de pastas obrigatória..." -ForegroundColor Cyan

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
        Report-Error "$dir não encontrado"
    }
}

Write-Host ""
Write-Host "?? 3. Verificando arquivos de configuração..." -ForegroundColor Cyan

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
        Report-Error "$file não encontrado"
    }
}

Write-Host ""
Write-Host "?? 4. Verificando dependências desnecessárias..." -ForegroundColor Cyan

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
    Report-Error "Estrutura de testes profissionais não encontrada"
}

Write-Host ""
Write-Host "?? 6. Verificando documentação..." -ForegroundColor Cyan

$docItems = @("README.md", "docs")
foreach ($doc in $docItems) {
    if (Test-Path $doc) {
        Report-Success "Documentação $doc encontrada"
    } else {
        Report-Warning "Documentação $doc não encontrada"
    }
}

Write-Host ""
Write-Host "?? 7. Verificando scripts de automação..." -ForegroundColor Cyan

$scriptDirs = @("scripts\development", "scripts\testing", "scripts\manual-tests")
foreach ($scriptDir in $scriptDirs) {
    if (Test-Path $scriptDir) {
        $scriptCount = (Get-ChildItem -Path $scriptDir -Include "*.sh", "*.ps1" -Recurse).Count
        if ($scriptCount -gt 0) {
            Report-Success "$scriptDir contém $scriptCount scripts"
        } else {
            Report-Warning "$scriptDir existe mas não contém scripts"
        }
    } else {
        Report-Warning "$scriptDir não encontrado"
    }
}

Write-Host ""
Write-Host "?? 8. Verificando padrões de código..." -ForegroundColor Cyan

# Check for TODO/FIXME comments
$todoFiles = Get-ChildItem -Path "src" -Recurse -Include "*.cs" | Where-Object {
    $content = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
    $content -match "TODO|FIXME|HACK"
}

$todoCount = $todoFiles.Count

if ($todoCount -eq 0) {
    Report-Success "Nenhum TODO/FIXME/HACK encontrado no código de produção"
} elseif ($todoCount -le 5) {
    Report-Warning "$todoCount TODOs/FIXMEs/HACKs encontrados (aceitável)"
} else {
    Report-Error "$todoCount TODOs/FIXMEs/HACKs encontrados (muitos para produção)"
}

# Final validation summary
Write-Host ""
Write-Host "==================================" -ForegroundColor White
Write-Host "?? RESUMO DA VALIDAÇÃO" -ForegroundColor White
Write-Host "==================================" -ForegroundColor White

if ($VALIDATION_ERRORS -eq 0 -and $VALIDATION_WARNINGS -eq 0) {
    Write-Host "?? ARQUITETURA PERFEITA!" -ForegroundColor Green
    Write-Host "? Todos os critérios de qualidade foram atendidos" -ForegroundColor Green
    exit 0
} elseif ($VALIDATION_ERRORS -eq 0) {
    Write-Host "? ARQUITETURA APROVADA COM RESSALVAS" -ForegroundColor Yellow
    Write-Host "??  $VALIDATION_WARNINGS avisos encontrados" -ForegroundColor Yellow
    Write-Host "?? Considere resolver os avisos para melhorar ainda mais a qualidade" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "? ARQUITETURA PRECISA DE CORREÇÕES" -ForegroundColor Red
    Write-Host "?? $VALIDATION_ERRORS erros críticos encontrados" -ForegroundColor Red
    Write-Host "??  $VALIDATION_WARNINGS avisos encontrados" -ForegroundColor Yellow
    Write-Host "???  Corrija os erros antes de prosseguir para produção" -ForegroundColor Cyan
    exit 1
}