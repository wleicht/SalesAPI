# PowerShell script para verifica��o de pr�-requisitos
param(
    [switch]$SkipDependencies
)

Write-Host "?? Verificando pr�-requisitos para Docker..." -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Gray

# Verificar se Docker est� rodando
Write-Host "?? Verificando Docker..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "? Docker est� rodando" -ForegroundColor Green
} catch {
    Write-Host "? Docker n�o est� rodando. Inicie o Docker Desktop." -ForegroundColor Red
    exit 1
}

# Verificar se Test-NetConnection est� dispon�vel
$netTestAvailable = Get-Command Test-NetConnection -ErrorAction SilentlyContinue

if (-not $SkipDependencies) {
    # Verificar se SQL Server est� dispon�vel
    Write-Host "?? Verificando SQL Server..." -ForegroundColor Yellow
    $sqlServerAvailable = $false
    
    if ($netTestAvailable) {
        $sqlTest = Test-NetConnection -ComputerName localhost -Port 1433 -InformationLevel Quiet -WarningAction SilentlyContinue
        $sqlServerAvailable = $sqlTest
    }
    
    if (-not $sqlServerAvailable) {
        Write-Host "?? SQL Server n�o encontrado na porta 1433. Iniciando container..." -ForegroundColor Yellow
        
        # Parar container existente se houver
        docker stop sqlserver 2>$null | Out-Null
        docker rm sqlserver 2>$null | Out-Null
        
        $sqlServerArgs = @(
            "run", "-e", "ACCEPT_EULA=Y",
            "-e", "SA_PASSWORD=Your_password123",
            "-e", "MSSQL_PID=Developer",
            "-p", "1433:1433",
            "--name", "sqlserver",
            "--restart", "unless-stopped",
            "-d", "mcr.microsoft.com/mssql/server:2022-latest"
        )
        
        & docker $sqlServerArgs
        
        Write-Host "? Aguardando SQL Server inicializar (30 segundos)..." -ForegroundColor Yellow
        Start-Sleep -Seconds 30
        
        # Verificar se iniciou corretamente
        $running = docker ps --filter "name=sqlserver" --filter "status=running" --quiet
        if ($running) {
            Write-Host "? SQL Server iniciado com sucesso" -ForegroundColor Green
        } else {
            Write-Host "? Falha ao iniciar SQL Server. Verificando logs:" -ForegroundColor Red
            docker logs sqlserver
            exit 1
        }
    } else {
        Write-Host "? SQL Server j� est� dispon�vel" -ForegroundColor Green
    }

    # Verificar se RabbitMQ est� dispon�vel
    Write-Host "?? Verificando RabbitMQ..." -ForegroundColor Yellow
    $rabbitMQAvailable = $false
    
    if ($netTestAvailable) {
        $rabbitTest = Test-NetConnection -ComputerName localhost -Port 5672 -InformationLevel Quiet -WarningAction SilentlyContinue
        $rabbitMQAvailable = $rabbitTest
    }
    
    if (-not $rabbitMQAvailable) {
        Write-Host "?? RabbitMQ n�o encontrado na porta 5672. Iniciando container..." -ForegroundColor Yellow
        
        # Parar container existente se houver
        docker stop rabbitmq 2>$null | Out-Null
        docker rm rabbitmq 2>$null | Out-Null
        
        $rabbitMQArgs = @(
            "run", "-d",
            "--name", "rabbitmq",
            "--restart", "unless-stopped",
            "-p", "5672:5672",
            "-p", "15672:15672",
            "-e", "RABBITMQ_DEFAULT_USER=admin",
            "-e", "RABBITMQ_DEFAULT_PASS=admin123",
            "rabbitmq:3-management"
        )
        
        & docker $rabbitMQArgs
        
        Write-Host "? Aguardando RabbitMQ inicializar (30 segundos)..." -ForegroundColor Yellow
        Start-Sleep -Seconds 30
        
        # Verificar se iniciou corretamente
        $running = docker ps --filter "name=rabbitmq" --filter "status=running" --quiet
        if ($running) {
            Write-Host "? RabbitMQ iniciado com sucesso" -ForegroundColor Green
            Write-Host "?? Interface de gerenciamento dispon�vel em: http://localhost:15672 (admin/admin123)" -ForegroundColor Cyan
        } else {
            Write-Host "? Falha ao iniciar RabbitMQ. Verificando logs:" -ForegroundColor Red
            docker logs rabbitmq
            exit 1
        }
    } else {
        Write-Host "? RabbitMQ j� est� dispon�vel" -ForegroundColor Green
    }
}

# Verificar espa�o em disco
Write-Host "?? Verificando espa�o em disco..." -ForegroundColor Yellow
$drive = Get-PSDrive C
$availableSpaceGB = [math]::Round($drive.Free / 1GB, 2)

if ($availableSpaceGB -lt 1) {
    Write-Host "?? Pouco espa�o em disco dispon�vel ($availableSpaceGB GB). Limpando containers e imagens n�o utilizadas..." -ForegroundColor Yellow
    docker system prune -f
} else {
    Write-Host "? Espa�o em disco suficiente ($availableSpaceGB GB dispon�veis)" -ForegroundColor Green
}

# Verificar se as portas necess�rias est�o livres
Write-Host "?? Verificando disponibilidade das portas..." -ForegroundColor Yellow
$portsToCheck = @(5000, 5001, 6000)

foreach ($port in $portsToCheck) {
    $portInUse = $false
    
    if ($netTestAvailable) {
        $portTest = Test-NetConnection -ComputerName localhost -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue
        $portInUse = $portTest
    }
    
    if ($portInUse) {
        switch ($port) {
            5000 { $serviceName = "Inventory API" }
            5001 { $serviceName = "Sales API" }
            6000 { $serviceName = "Gateway" }
        }
        Write-Host "?? Porta $port j� est� em uso ($serviceName). Isso pode causar conflitos." -ForegroundColor Yellow
    } else {
        Write-Host "? Porta $port dispon�vel" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "? Verifica��o de pr�-requisitos conclu�da!" -ForegroundColor Green
Write-Host "?? Resumo:" -ForegroundColor Cyan
Write-Host "   - Docker: Rodando" -ForegroundColor White
if (-not $SkipDependencies) {
    Write-Host "   - SQL Server: Configurado na porta 1433" -ForegroundColor White
    Write-Host "   - RabbitMQ: Configurado na porta 5672" -ForegroundColor White
}
Write-Host "   - Portas das APIs: Verificadas" -ForegroundColor White
Write-Host "   - Espa�o em disco: $availableSpaceGB GB dispon�veis" -ForegroundColor White
Write-Host ""
Write-Host "?? Sistema pronto para inicializar os servi�os SalesAPI" -ForegroundColor Green