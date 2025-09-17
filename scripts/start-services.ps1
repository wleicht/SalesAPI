# PowerShell script para inicializar os serviços SalesAPI diretamente (sem Docker)
param(
    [switch]$Clean,
    [switch]$SkipTests,
    [switch]$DirectStart = $false
)

if ($DirectStart) {
    Write-Host "?? Iniciando SalesAPI diretamente (sem Docker)..." -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Gray
} else {
    Write-Host "?? Iniciando SalesAPI com Docker..." -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Gray
}

# Navegar para o diretório correto
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
Set-Location $projectRoot

Write-Host "?? Diretório do projeto: $projectRoot" -ForegroundColor Blue

# Function to check if port is available
function Test-Port {
    param([int]$Port)
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $Port)
        $listener.Start()
        $listener.Stop()
        return $true
    }
    catch {
        return $false
    }
}

# Function to start service directly
function Start-ServiceDirect {
    param(
        [string]$ServiceName,
        [string]$ProjectPath,
        [int]$Port,
        [string]$Environment = "Development"
    )
    
    Write-Host "?? Starting $ServiceName on port $Port..." -ForegroundColor Yellow
    
    if (!(Test-Port -Port $Port)) {
        Write-Host "??  Port $Port is already in use. Attempting to kill existing process..." -ForegroundColor Red
        try {
            $processes = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess
            foreach ($processId in $processes) {
                Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
            }
            Start-Sleep -Seconds 2
        }
        catch {
            Write-Host "Could not kill existing processes on port $Port" -ForegroundColor Red
        }
    }
    
    $env:ASPNETCORE_ENVIRONMENT = $Environment
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$ProjectPath`" --urls `"http://localhost:$Port`"" -WindowStyle Hidden -PassThru
    
    if ($process) {
        Write-Host "? $ServiceName started (PID: $($process.Id))" -ForegroundColor Green
        Start-Sleep -Seconds 5
        
        # Verify service is responding
        $maxRetries = 6
        $retryCount = 0
        $isHealthy = $false
        
        while ($retryCount -lt $maxRetries -and !$isHealthy) {
            try {
                $response = Invoke-WebRequest -Uri "http://localhost:$Port/health" -TimeoutSec 5 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-Host "? $ServiceName health check passed" -ForegroundColor Green
                    $isHealthy = $true
                } else {
                    Write-Host "??  $ServiceName health check failed (Status: $($response.StatusCode)), retrying..." -ForegroundColor Yellow
                    Start-Sleep -Seconds 5
                    $retryCount++
                }
            }
            catch {
                Write-Host "??  $ServiceName health check failed: $($_.Exception.Message), retrying..." -ForegroundColor Yellow
                Start-Sleep -Seconds 5
                $retryCount++
            }
        }
        
        if (!$isHealthy) {
            Write-Host "? $ServiceName health check failed after $maxRetries attempts" -ForegroundColor Red
        }
        
        return $process
    } else {
        Write-Host "? Failed to start $ServiceName" -ForegroundColor Red
        return $null
    }
}

if ($DirectStart) {
    # Direct startup mode - start services without Docker
    Write-Host ""
    Write-Host "?? Service Configuration:" -ForegroundColor Cyan
    Write-Host "  - Sales API: http://localhost:5001" -ForegroundColor Gray
    Write-Host "  - Inventory API: http://localhost:5000" -ForegroundColor Gray  
    Write-Host "  - Gateway: http://localhost:6000" -ForegroundColor Gray
    Write-Host ""

    # Build all projects first
    Write-Host "?? Building all projects..." -ForegroundColor Yellow
    dotnet build --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "? Build completed successfully" -ForegroundColor Green
    Write-Host ""

    # Array to store started processes
    $startedProcesses = @()

    try {
        # Start Sales API
        $salesProcess = Start-ServiceDirect -ServiceName "Sales API" -ProjectPath "src/sales.api/sales.api.csproj" -Port 5001
        if ($salesProcess) { $startedProcesses += $salesProcess }

        # Start Inventory API
        $inventoryProcess = Start-ServiceDirect -ServiceName "Inventory API" -ProjectPath "src/inventory.api/inventory.api.csproj" -Port 5000
        if ($inventoryProcess) { $startedProcesses += $inventoryProcess }

        # Start Gateway
        $gatewayProcess = Start-ServiceDirect -ServiceName "Gateway" -ProjectPath "src/gateway/gateway.csproj" -Port 6000
        if ($gatewayProcess) { $startedProcesses += $gatewayProcess }

        Write-Host ""
        Write-Host "?? All services started successfully!" -ForegroundColor Green
        Write-Host ""

        # Execute diagnostic test
        if (-not $SkipTests) {
            Write-Host "?? Running diagnostic tests..." -ForegroundColor Yellow
            try {
                & "$PSScriptRoot/manual-tests/diagnostic.ps1" -Verbose
            }
            catch {
                Write-Host "??  Diagnostic test failed: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }

        Write-Host ""
        Write-Host "?? Ready for testing! You can now run:" -ForegroundColor Cyan
        Write-Host "  .\scripts\manual-tests\diagnostic.ps1 -Verbose" -ForegroundColor Gray
        Write-Host "  .\scripts\manual-tests\04_orders_tests.ps1" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Press Ctrl+C to stop all services..." -ForegroundColor Yellow
        
        # Keep script running
        try {
            while ($true) {
                Start-Sleep -Seconds 5
                
                # Check if processes are still alive
                $runningProcesses = $startedProcesses | Where-Object { !$_.HasExited }
                if ($runningProcesses.Count -eq 0) {
                    Write-Host "??  All services have stopped." -ForegroundColor Yellow
                    break
                }
            }
        }
        catch {
            Write-Host "?? Stopping services..." -ForegroundColor Yellow
        }
    }
    finally {
        # Clean up processes
        if ($startedProcesses.Count -gt 0) {
            Write-Host "?? Cleaning up processes..." -ForegroundColor Yellow
            foreach ($process in $startedProcesses) {
                try {
                    if (!$process.HasExited) {
                        $process.Kill()
                        Write-Host "  ? Stopped process $($process.Id)" -ForegroundColor Green
                    }
                }
                catch {
                    Write-Host "  ??  Could not stop process $($process.Id): $($_.Exception.Message)" -ForegroundColor Yellow
                }
            }
        }
        
        Write-Host ""
        Write-Host "?? Cleanup completed." -ForegroundColor Green
    }
    
    return
}

# Original Docker mode
# Executar verificações de pré-requisitos
if (Test-Path "./scripts/docker-startup-check.ps1") {
    Write-Host "?? Executando verificações de pré-requisitos..." -ForegroundColor Yellow
    try {
        & "./scripts/docker-startup-check.ps1"
    } catch {
        Write-Host "??  Erro na verificação de pré-requisitos: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "Continuando mesmo assim..." -ForegroundColor Yellow
    }
} else {
    Write-Host "??  Script de verificação não encontrado, continuando..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "?? Parando containers existentes..." -ForegroundColor Yellow
try {
    docker-compose -f docker/compose/docker-compose.yml down --remove-orphans --volumes 2>$null
} catch {
    Write-Host "??  Aviso ao parar containers: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Limpar builds anteriores se solicitado
if ($Clean) {
    Write-Host "?? Limpando builds anteriores..." -ForegroundColor Yellow
    docker system prune -f
    docker-compose -f docker/compose/docker-compose.yml build --no-cache
} else {
    Write-Host "?? Fazendo build dos serviços..." -ForegroundColor Yellow
    docker-compose -f docker/compose/docker-compose.yml build
}

# Iniciar serviços
Write-Host ""
Write-Host "?? Iniciando serviços..." -ForegroundColor Green
docker-compose -f docker/compose/docker-compose.yml up -d

# Aguardar inicialização
Write-Host ""
Write-Host "? Aguardando inicialização dos serviços (60 segundos)..." -ForegroundColor Yellow
for ($i = 1; $i -le 12; $i++) {
    Write-Host -NoNewline "."
    Start-Sleep -Seconds 5
}
Write-Host ""

# Verificar status dos containers
Write-Host ""
Write-Host "?? Verificando status dos containers..." -ForegroundColor Yellow
docker-compose -f docker/compose/docker-compose.yml ps

# Aguardar mais um pouco antes dos testes
Write-Host ""
Write-Host "? Aguardando serviços ficarem prontos (30 segundos adicionais)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Testar conectividade básica
Write-Host ""
Write-Host "?? Testando conectividade dos serviços..." -ForegroundColor Yellow

# Função para testar endpoint
function Test-Endpoint {
    param($Name, $Url, $ContainerName)
    
    Write-Host -NoNewline "$Name (${Url}): "
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "? OK" -ForegroundColor Green
        } else {
            Write-Host "??  Status: $($response.StatusCode)" -ForegroundColor Red
        }
    } catch {
        Write-Host "? Falhou" -ForegroundColor Red
        if ($ContainerName) {
            Write-Host "   Verificando logs do $Name:" -ForegroundColor Yellow
            docker logs $ContainerName --tail 10
        }
    }
}

# Teste do Gateway
Test-Endpoint "Gateway" "http://localhost:6000/gateway/status" "salesapi-gateway"

# Teste do Inventory
Test-Endpoint "Inventory API" "http://localhost:5000/health" "salesapi-inventory"

# Teste do Sales
Test-Endpoint "Sales API" "http://localhost:5001/health" "salesapi-sales"

Write-Host ""
Write-Host "?? Status final dos serviços:" -ForegroundColor Cyan
docker-compose -f docker/compose/docker-compose.yml ps --format "table {{.Name}}\t{{.State}}\t{{.Status}}"

# Executar testes básicos se disponível e não foram pulados
if (-not $SkipTests -and (Test-Path "./scripts/manual-tests/demo_tests.sh")) {
    Write-Host ""
    Write-Host "?? Executando testes básicos de funcionalidade..." -ForegroundColor Yellow
    try {
        if (Get-Command bash -ErrorAction SilentlyContinue) {
            $testResult = bash "./scripts/manual-tests/demo_tests.sh"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "? Testes básicos passaram!" -ForegroundColor Green
            } else {
                Write-Host "??  Alguns testes básicos falharam. Verifique os logs acima." -ForegroundColor Yellow
            }
        } else {
            Write-Host "??  Bash não encontrado. Pule os testes ou instale o WSL/Git Bash." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "??  Erro ao executar testes: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "?? Processo de inicialização concluído!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Próximos passos:" -ForegroundColor Cyan
Write-Host "   1. Verificar logs: docker-compose -f docker/compose/docker-compose.yml logs" -ForegroundColor White
Write-Host "   2. Acessar APIs:" -ForegroundColor White
Write-Host "      - Gateway: http://localhost:6000" -ForegroundColor White
Write-Host "      - Inventory API: http://localhost:5000" -ForegroundColor White
Write-Host "      - Sales API: http://localhost:5001" -ForegroundColor White
Write-Host "   3. Executar testes completos: ./scripts/manual-tests/run_manual_tests.sh" -ForegroundColor White
Write-Host "   4. Monitorar: ./scripts/monitor-services.sh" -ForegroundColor White