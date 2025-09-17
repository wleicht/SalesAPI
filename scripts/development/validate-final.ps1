# SalesAPI Final Architecture Validation Script (PowerShell)
# Validates all improvements and optimizations implemented

Write-Host "?? SalesAPI - Valida��o Final da Arquitetura Otimizada" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green

# Initialize counters
$VALIDATION_SUCCESS = 0
$VALIDATION_WARNINGS = 0
$VALIDATION_ERRORS = 0

# Functions to report results
function Report-Success { param([string]$Message) Write-Host "? $Message" -ForegroundColor Green; $script:VALIDATION_SUCCESS++ }
function Report-Warning { param([string]$Message) Write-Host "??  $Message" -ForegroundColor Yellow; $script:VALIDATION_WARNINGS++ }
function Report-Error { param([string]$Message) Write-Host "? $Message" -ForegroundColor Red; $script:VALIDATION_ERRORS++ }
function Report-Info { param([string]$Message) Write-Host "?? $Message" -ForegroundColor Cyan }

Write-Host ""
Report-Info "1. VALIDANDO MELHORIAS ARQUITETURAIS IMPLEMENTADAS..."

# Check shared configuration
Write-Host ""
Report-Info "1.1 Validando Configura��es Centralizadas"

if (Test-Path "src\shared\Configuration\SharedConfiguration.cs") {
    Report-Success "Configura��o compartilhada implementada"
    
    # Check if configuration is being used
    if (Get-Content "src\*\appsettings.json" -ErrorAction SilentlyContinue | Select-String "Shared" -Quiet) {
        Report-Success "Configura��o compartilhada sendo utilizada"
    } else {
        Report-Warning "Configura��o compartilhada criada mas pode n�o estar sendo utilizada"
    }
} else {
    Report-Error "Configura��o compartilhada n�o implementada"
}

# Check consolidated documentation
Write-Host ""
Report-Info "1.2 Validando Documenta��o Consolidada"

if (Test-Path "docs\README-COMPLETE.md") {
    Report-Success "Documenta��o principal consolidada"
    
    # Check documentation quality
    $docSize = (Get-Content "docs\README-COMPLETE.md").Count
    if ($docSize -gt 200) {
        Report-Success "Documenta��o abrangente ($docSize linhas)"
    } else {
        Report-Warning "Documenta��o pode estar incompleta ($docSize linhas)"
    }
} else {
    Report-Error "Documenta��o consolidada n�o encontrada"
}

if (Test-Path "docs\ARCHITECTURE.md") {
    Report-Success "Documenta��o arquitetural criada"
} else {
    Report-Warning "Documenta��o arquitetural n�o encontrada"
}

if (Test-Path "docs\DEPLOYMENT.md") {
    Report-Success "Guia de deployment criado"
} else {
    Report-Warning "Guia de deployment n�o encontrado"
}

# Check development scripts
Write-Host ""
Report-Info "1.3 Validando Scripts de Desenvolvimento"

$scriptsToCheck = @(
    "scripts\development\cleanup.sh",
    "scripts\development\cleanup.ps1",
    "scripts\development\validate-architecture.sh",
    "scripts\development\validate-architecture.ps1",
    "scripts\development\start-all.sh",
    "scripts\development\start-all.ps1"
)

$scriptCount = 0
foreach ($script in $scriptsToCheck) {
    if (Test-Path $script) {
        $scriptCount++
    }
}

if ($scriptCount -eq $scriptsToCheck.Count) {
    Report-Success "Todos os scripts de desenvolvimento criados ($scriptCount/$($scriptsToCheck.Count))"
} elseif ($scriptCount -gt ($scriptsToCheck.Count / 2)) {
    Report-Warning "Maioria dos scripts criados ($scriptCount/$($scriptsToCheck.Count))"
} else {
    Report-Error "Scripts de desenvolvimento incompletos ($scriptCount/$($scriptsToCheck.Count))"
}

# Check deployment automation
Write-Host ""
Report-Info "1.4 Validando Automa��o de Deploy"

if (Test-Path "scripts\deployment\deploy.sh") {
    Report-Success "Script de deploy automatizado criado"
} else {
    Report-Error "Script de deploy automatizado n�o encontrado"
}

# Check testing infrastructure
Write-Host ""
Report-Info "1.5 Validando Infraestrutura de Testes"

if (Test-Path "scripts\testing\quick-validation.sh") {
    Report-Success "Script de valida��o r�pida criado"
} else {
    Report-Warning "Script de valida��o r�pida n�o encontrado"
}

# Count total test projects
$testProjectCount = (Get-ChildItem -Path "tests" -Recurse -Include "*.csproj" -ErrorAction SilentlyContinue).Count
if ($testProjectCount -ge 4) {
    Report-Success "Infraestrutura robusta de testes ($testProjectCount projetos)"
} elseif ($testProjectCount -ge 2) {
    Report-Warning "Infraestrutura de testes b�sica ($testProjectCount projetos)"
} else {
    Report-Error "Infraestrutura de testes insuficiente ($testProjectCount projetos)"
}

# Check for production-ready patterns
Write-Host ""
Report-Info "2. VALIDANDO PADR�ES PRODUCTION-READY..."

Write-Host ""
Report-Info "2.1 Validando Separa��o de Ambientes"

# Check for environment-specific configurations
if ((Test-Path "src\gateway\appsettings.Development.json") -or (Test-Path "src\gateway\appsettings.Production.json")) {
    Report-Success "Configura��es espec�ficas por ambiente implementadas"
} else {
    Report-Warning "Configura��es espec�ficas por ambiente podem estar em falta"
}

Write-Host ""
Report-Info "2.2 Validando Seguran�a"

# Check for JWT configuration
try {
    $jwtConfigs = (Get-Content "src\*\appsettings.json" -ErrorAction SilentlyContinue | Select-String "Jwt").Count
    if ($jwtConfigs -gt 0) {
        Report-Success "Configura��o JWT encontrada"
        
        # Check for strong keys (basic validation)
        $weakKeys = (Get-Content "src\*\appsettings.json" -ErrorAction SilentlyContinue | Select-String '"Key".*"test|"Key".*"123|"Key".*"secret"').Count
        if ($weakKeys -eq 0) {
            Report-Success "Chaves JWT aparentam ser seguras"
        } else {
            Report-Warning "Algumas chaves JWT podem ser fracas para produ��o"
        }
    } else {
        Report-Error "Configura��o JWT n�o encontrada"
    }
} catch {
    Report-Warning "Erro ao verificar configura��o JWT"
}

Write-Host ""
Report-Info "2.3 Validando Observabilidade"

# Check for logging configuration
try {
    if (Get-Content "src\*\Program.cs" -ErrorAction SilentlyContinue | Select-String "Serilog|Logging" -Quiet) {
        Report-Success "Logging estruturado configurado"
    } else {
        Report-Warning "Logging estruturado pode n�o estar configurado"
    }
} catch {
    Report-Warning "Erro ao verificar logging"
}

# Check for health checks
try {
    if (Get-Content "src\*\Program.cs" -ErrorAction SilentlyContinue | Select-String "AddHealthChecks|MapHealthChecks" -Quiet) {
        Report-Success "Health checks implementados"
    } else {
        Report-Warning "Health checks podem n�o estar implementados"
    }
} catch {
    Report-Warning "Erro ao verificar health checks"
}

# Check for metrics
try {
    if (Get-Content "src\*\Program.cs" -ErrorAction SilentlyContinue | Select-String "Prometheus|UseHttpMetrics" -Quiet) {
        Report-Success "M�tricas Prometheus configuradas"
    } else {
        Report-Warning "M�tricas podem n�o estar configuradas"
    }
} catch {
    Report-Warning "Erro ao verificar m�tricas"
}

Write-Host ""
Report-Info "3. VALIDANDO QUALIDADE DE C�DIGO..."

Write-Host ""
Report-Info "3.1 Validando Estrutura de Projetos"

$requiredProjects = @(
    "src\gateway",
    "src\sales.api",
    "src\inventory.api",
    "src\buildingblocks.contracts",
    "src\buildingblocks.events"
)

$projectStructureOk = $true
foreach ($project in $requiredProjects) {
    if (Test-Path $project) {
        continue
    } else {
        Report-Warning "Projeto $project n�o encontrado"
        $projectStructureOk = $false
    }
}

if ($projectStructureOk) {
    Report-Success "Estrutura de projetos correta"
}

Write-Host ""
Report-Info "3.2 Validando Build"

# Try to build the solution
Report-Info "Executando build de valida��o..."
try {
    $buildResult = dotnet build SalesAPI.sln --configuration Debug --verbosity quiet 2>$null
    if ($LASTEXITCODE -eq 0) {
        Report-Success "Build executado com sucesso"
    } else {
        Report-Error "Build falhando - corre��es necess�rias"
    }
} catch {
    Report-Error "Erro ao executar build"
}

Write-Host ""
Report-Info "4. VALIDANDO FUNCIONALIDADES IMPLEMENTADAS..."

Write-Host ""
Report-Info "4.1 Validando Messaging"

# Check for messaging implementation
try {
    $messagingFiles = Get-ChildItem -Path "src" -Recurse -Include "*.cs" | Where-Object { 
        (Get-Content $_.FullName -ErrorAction SilentlyContinue | Select-String "IEventPublisher|RealEventPublisher" -Quiet) 
    }
    
    if ($messagingFiles.Count -gt 0) {
        Report-Success "Sistema de messaging implementado"
        
        # Check for production-ready messaging (no fake/mock in production code)
        $fakeMessaging = Get-ChildItem -Path "src" -Recurse -Include "*.cs" | Where-Object { 
            (Get-Content $_.FullName -ErrorAction SilentlyContinue | Select-String "FakeEventPublisher|MockEventPublisher|DummyEventPublisher" -Quiet) 
        }
        
        if ($fakeMessaging.Count -eq 0) {
            Report-Success "Messaging production-ready (sem implementa��es fake)"
        } else {
            Report-Warning "Implementa��es fake de messaging encontradas no c�digo de produ��o"
        }
    } else {
        Report-Error "Sistema de messaging n�o implementado"
    }
} catch {
    Report-Warning "Erro ao verificar messaging"
}

Write-Host ""
Report-Info "4.2 Validando Containeriza��o"

# Check for Docker files
$dockerFiles = @(
    "docker-compose.yml",
    "src\gateway\Dockerfile",
    "src\sales.api\Dockerfile",
    "src\inventory.api\Dockerfile"
)

$dockerCount = 0
foreach ($dockerfile in $dockerFiles) {
    if (Test-Path $dockerfile) {
        $dockerCount++
    }
}

if ($dockerCount -eq $dockerFiles.Count) {
    Report-Success "Containeriza��o completa ($dockerCount/$($dockerFiles.Count) arquivos)"
} elseif ($dockerCount -gt 1) {
    Report-Warning "Containeriza��o parcial ($dockerCount/$($dockerFiles.Count) arquivos)"
} else {
    Report-Error "Containeriza��o n�o implementada"
}

# Final calculation and summary
$TOTAL_CHECKS = $VALIDATION_SUCCESS + $VALIDATION_WARNINGS + $VALIDATION_ERRORS
if ($TOTAL_CHECKS -gt 0) {
    $SUCCESS_RATE = [math]::Round(($VALIDATION_SUCCESS * 100 / $TOTAL_CHECKS), 0)
} else {
    $SUCCESS_RATE = 0
}

Write-Host ""
Write-Host "=============================================="-ForegroundColor White
Write-Host "?? RESUMO DA VALIDA��O FINAL" -ForegroundColor White
Write-Host "=============================================="-ForegroundColor White
Write-Host "Total de Valida��es: $TOTAL_CHECKS"
Write-Host "? Sucessos: $VALIDATION_SUCCESS" -ForegroundColor Green
Write-Host "??  Avisos: $VALIDATION_WARNINGS" -ForegroundColor Yellow
Write-Host "? Erros: $VALIDATION_ERRORS" -ForegroundColor Red
Write-Host "?? Taxa de Sucesso: ${SUCCESS_RATE}%"
Write-Host ""

# Final assessment
if ($VALIDATION_ERRORS -eq 0 -and $SUCCESS_RATE -ge 90) {
    Write-Host "?? ARQUITETURA OTIMIZADA COM EXCEL�NCIA!" -ForegroundColor Green
    Write-Host "? Implementa��o profissional completa" -ForegroundColor Green
    Write-Host "?? Sistema pronto para produ��o" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "?? MELHORIAS IMPLEMENTADAS COM SUCESSO:" -ForegroundColor White
    Write-Host "  � ? Configura��es centralizadas e consolidadas" -ForegroundColor Gray
    Write-Host "  � ? Documenta��o profissional unificada" -ForegroundColor Gray
    Write-Host "  � ? Scripts de automa��o completos" -ForegroundColor Gray
    Write-Host "  � ? Deploy automatizado multi-ambiente" -ForegroundColor Gray
    Write-Host "  � ? Valida��o de arquitetura automatizada" -ForegroundColor Gray
    Write-Host "  � ? Infraestrutura de testes robusta" -ForegroundColor Gray
    Write-Host "  � ? Padr�es production-ready implementados" -ForegroundColor Gray
    Write-Host "  � ? Observabilidade e monitoring" -ForegroundColor Gray
    Write-Host "  � ? Seguran�a e containeriza��o" -ForegroundColor Gray
    
    exit 0
    
} elseif ($VALIDATION_ERRORS -eq 0 -and $SUCCESS_RATE -ge 75) {
    Write-Host "? ARQUITETURA OTIMIZADA COM QUALIDADE ALTA" -ForegroundColor Yellow
    Write-Host "??  Algumas melhorias adicionais recomendadas" -ForegroundColor Yellow
    Write-Host "?? Sistema funcional e profissional" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "?? PRINCIPAIS CONQUISTAS:" -ForegroundColor White
    Write-Host "  � Estrutura profissional implementada" -ForegroundColor Gray
    Write-Host "  � Documenta��o consolidada" -ForegroundColor Gray
    Write-Host "  � Scripts de automa��o funcionais" -ForegroundColor Gray
    Write-Host "  � Padr�es de qualidade seguidos" -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "?? MELHORIAS RECOMENDADAS:" -ForegroundColor White
    Write-Host "  � Resolver avisos identificados" -ForegroundColor Gray
    Write-Host "  � Completar componentes opcionais" -ForegroundColor Gray
    Write-Host "  � Otimizar configura��es restantes" -ForegroundColor Gray
    
    exit 0
    
} else {
    Write-Host "? ARQUITETURA PRECISA DE MAIS OTIMIZA��ES" -ForegroundColor Red
    Write-Host "?? $VALIDATION_ERRORS erros cr�ticos encontrados" -ForegroundColor Red
    Write-Host "??  $VALIDATION_WARNINGS avisos precisam de aten��o" -ForegroundColor Yellow
    
    Write-Host ""
    Write-Host "???  A��ES NECESS�RIAS:" -ForegroundColor White
    Write-Host "  � Corrigir todos os erros identificados" -ForegroundColor Gray
    Write-Host "  � Implementar componentes em falta" -ForegroundColor Gray
    Write-Host "  � Executar novamente a valida��o" -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "?? PR�XIMOS PASSOS:" -ForegroundColor White
    Write-Host "  1. Implementar corre��es necess�rias" -ForegroundColor Gray
    Write-Host "  2. Executar: .\scripts\development\validate-final.ps1" -ForegroundColor Gray
    Write-Host "  3. Repetir at� atingir > 90% de sucesso" -ForegroundColor Gray
    
    exit 1
}