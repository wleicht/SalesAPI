# SalesAPI Quick Start Script for Windows
# Starts all services in development mode

Write-Host "?? Iniciando SalesAPI - Arquitetura Profissional de Microserviços" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green

function Test-CommandExists {
    param([string]$Command)
    
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Test-PortInUse {
    param([int]$Port)
    
    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    return $connection -ne $null
}

function Stop-ProcessOnPort {
    param([int]$Port)
    
    $process = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "??  Porta $Port está em uso. Tentando parar processo..." -ForegroundColor Yellow
        $processId = (Get-Process -Id $process.OwningProcess -ErrorAction SilentlyContinue).Id
        if ($processId) {
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
        }
    }
}

function Test-ServiceHealth {
    param(
        [string]$ServiceName,
        [string]$Url,
        [int]$MaxAttempts = 30
    )
    
    for ($i = 1; $i -le $MaxAttempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                Write-Host "  ? $ServiceName está saudável" -ForegroundColor Green
                return $true
            }
        }
        catch {
            # Service not ready yet
        }
        Start-Sleep -Seconds 2
    }
    Write-Host "  ? $ServiceName não respondeu health check" -ForegroundColor Red
    return $false
}

# Check prerequisites
Write-Host "?? Verificando pré-requisitos..." -ForegroundColor Cyan

if (-not (Test-CommandExists "dotnet")) {
    Write-Host "? .NET SDK não está instalado" -ForegroundColor Red
    exit 1
}

if (-not (Test-CommandExists "docker")) {
    Write-Host "? Docker não está instalado" -ForegroundColor Red
    exit 1
}

# Check .NET version
$dotnetVersion = (dotnet --version).Split('.')[0]
if ([int]$dotnetVersion -lt 8) {
    Write-Host "? .NET 8.0+ é necessário. Versão atual: $(dotnet --version)" -ForegroundColor Red
    exit 1
}

Write-Host "? Pré-requisitos verificados" -ForegroundColor Green

# Start infrastructure services
Write-Host "" 
Write-Host "?? Iniciando serviços de infraestrutura..." -ForegroundColor Cyan
docker-compose up -d sqlserver rabbitmq

# Wait for services
Write-Host "? Aguardando serviços de infraestrutura..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Check ports
Write-Host ""
Write-Host "?? Verificando disponibilidade de portas..." -ForegroundColor Cyan
Stop-ProcessOnPort -Port 6000  # Gateway
Stop-ProcessOnPort -Port 5001  # Sales API
Stop-ProcessOnPort -Port 5000  # Inventory API

# Build solution
Write-Host ""
Write-Host "?? Compilando a solução..." -ForegroundColor Cyan
$buildResult = dotnet build SalesAPI.sln --configuration Debug --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Falha na compilação" -ForegroundColor Red
    exit 1
}
Write-Host "? Compilação bem-sucedida" -ForegroundColor Green

# Start microservices
Write-Host ""
Write-Host "?? Iniciando microserviços..." -ForegroundColor Cyan

Write-Host "  Iniciando Gateway (porta 6000)..." -ForegroundColor Yellow
$gatewayProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", "Debug" -WorkingDirectory "src\gateway" -PassThru -WindowStyle Hidden

Start-Sleep -Seconds 3

Write-Host "  Iniciando Sales API (porta 5001)..." -ForegroundColor Yellow
$salesProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", "Debug" -WorkingDirectory "src\sales.api" -PassThru -WindowStyle Hidden

Start-Sleep -Seconds 3

Write-Host "  Iniciando Inventory API (porta 5000)..." -ForegroundColor Yellow
$inventoryProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", "Debug" -WorkingDirectory "src\inventory.api" -PassThru -WindowStyle Hidden

# Wait for services to start
Write-Host ""
Write-Host "? Aguardando serviços iniciarem..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Perform health checks
Write-Host "?? Executando health checks..." -ForegroundColor Cyan
$gatewayHealthy = Test-ServiceHealth -ServiceName "Gateway" -Url "http://localhost:6000/health"
$salesHealthy = Test-ServiceHealth -ServiceName "Sales API" -Url "http://localhost:5001/health"
$inventoryHealthy = Test-ServiceHealth -ServiceName "Inventory API" -Url "http://localhost:5000/health"

# Test authentication
Write-Host ""
Write-Host "?? Testando autenticação..." -ForegroundColor Cyan

try {
    $authBody = @{
        username = "admin"
        password = "admin123"
    } | ConvertTo-Json

    $authResponse = Invoke-WebRequest -Uri "http://localhost:6000/auth/login" -Method POST -Body $authBody -ContentType "application/json" -UseBasicParsing
    
    if ($authResponse.Content -match '"token"') {
        Write-Host "? Autenticação funcionando" -ForegroundColor Green
        
        # Extract token
        $tokenMatch = [regex]::Match($authResponse.Content, '"token":"([^"]*)"')
        if ($tokenMatch.Success) {
            $token = $tokenMatch.Groups[1].Value
            
            # Test authenticated endpoint
            Write-Host "?? Testando endpoint autenticado..." -ForegroundColor Cyan
            try {
                $headers = @{ "Authorization" = "Bearer $token" }
                Invoke-WebRequest -Uri "http://localhost:6000/inventory/products" -Headers $headers -UseBasicParsing -TimeoutSec 5 | Out-Null
                Write-Host "? Autorização funcionando" -ForegroundColor Green
            }
            catch {
                Write-Host "??  Autorização pode ter problemas" -ForegroundColor Yellow
            }
        }
    }
}
catch {
    Write-Host "??  Problema na autenticação" -ForegroundColor Yellow
}

# Final status
Write-Host ""
Write-Host "=================================" -ForegroundColor Green
Write-Host "?? SalesAPI INICIADO COM SUCESSO!" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""
Write-Host "?? Status dos Serviços:" -ForegroundColor White
Write-Host "  ?? Gateway:      http://localhost:6000 (PID: $($gatewayProcess.Id))" -ForegroundColor Gray
Write-Host "  ?? Sales API:    http://localhost:5001 (PID: $($salesProcess.Id))" -ForegroundColor Gray  
Write-Host "  ?? Inventory:    http://localhost:5000 (PID: $($inventoryProcess.Id))" -ForegroundColor Gray
Write-Host ""
Write-Host "?? Documentação:" -ForegroundColor White
Write-Host "  ?? Gateway Swagger:    http://localhost:6000/swagger" -ForegroundColor Gray
Write-Host "  ?? Sales Swagger:      http://localhost:5001/swagger" -ForegroundColor Gray
Write-Host "  ?? Inventory Swagger:  http://localhost:5000/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "?? Credenciais de Teste:" -ForegroundColor White
Write-Host "  ?? Admin:      admin / admin123" -ForegroundColor Gray
Write-Host "  ?? Customer:   customer1 / password123" -ForegroundColor Gray
Write-Host ""
Write-Host "???  Comandos Úteis:" -ForegroundColor White
Write-Host "  Parar Gateway:     Stop-Process -Id $($gatewayProcess.Id)" -ForegroundColor Gray
Write-Host "  Parar Sales:       Stop-Process -Id $($salesProcess.Id)" -ForegroundColor Gray
Write-Host "  Parar Inventory:   Stop-Process -Id $($inventoryProcess.Id)" -ForegroundColor Gray
Write-Host "  Testes Rápidos:    .\scripts\manual-tests\demo_tests.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "?? Sistema pronto para desenvolvimento e testes!" -ForegroundColor Green

# Save PIDs for reference
$pids = @{
    Gateway = $gatewayProcess.Id
    Sales = $salesProcess.Id
    Inventory = $inventoryProcess.Id
}
$pids | ConvertTo-Json | Out-File -FilePath ".services.json" -Encoding UTF8