# Quick start script for SalesAPI with Docker Compose (PowerShell)
# Usage: .\start.ps1

Write-Host "?? Starting SalesAPI Complete Environment" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Check if Docker and Docker Compose are available
try {
    docker --version | Out-Null
    docker compose version | Out-Null
    Write-Host "? Docker and Docker Compose are available" -ForegroundColor Green
} catch {
    Write-Host "? Docker or Docker Compose is not available. Please install Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Stop any existing containers
Write-Host "?? Stopping any existing containers..." -ForegroundColor Yellow
docker compose down --remove-orphans 2>$null

# Apply database migrations first (if not already applied)
Write-Host "?? Applying database migrations..." -ForegroundColor Yellow
try {
    # Start only infrastructure to apply migrations
    docker compose -f docker-compose.infrastructure.yml up -d sqlserver rabbitmq
    Start-Sleep 30
    
    # Apply migrations locally
    dotnet ef database update --project src/inventory.api 2>$null
    dotnet ef database update --project src/sales.api 2>$null
    Write-Host "   ? Migrations completed successfully" -ForegroundColor Green
} catch {
    Write-Host "??  Migrations may have failed, but continuing..." -ForegroundColor Yellow
}

# Build and start all services
Write-Host "?? Building and starting all services..." -ForegroundColor Yellow
docker compose up --build -d

# Wait for services to be healthy
Write-Host "? Waiting for services to be healthy..." -ForegroundColor Yellow
Write-Host "   This may take 2-3 minutes for first-time setup..." -ForegroundColor Yellow

# Function to wait for service health
function Wait-ForService {
    param(
        [string]$ServiceName,
        [int]$MaxAttempts = 60
    )
    
    Write-Host "   Waiting for $ServiceName..." -ForegroundColor Cyan
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $healthyServices = docker compose ps --filter "status=running" --filter "health=healthy" 2>$null
        if ($healthyServices -match $ServiceName) {
            Write-Host "   ? $ServiceName is healthy" -ForegroundColor Green
            return $true
        }
        
        if ($attempt % 10 -eq 0) {
            Write-Host "   ? Still waiting for $ServiceName... (${attempt}s)" -ForegroundColor Yellow
        }
        
        Start-Sleep -Seconds 1
    }
    
    Write-Host "   ? $ServiceName failed to become healthy" -ForegroundColor Red
    return $false
}

# Wait for infrastructure services first
if (-not (Wait-ForService "salesapi-sqlserver")) { exit 1 }
if (-not (Wait-ForService "salesapi-rabbitmq")) { exit 1 }

# Wait for application services
if (-not (Wait-ForService "salesapi-inventory")) { exit 1 }
if (-not (Wait-ForService "salesapi-sales")) { exit 1 }
if (-not (Wait-ForService "salesapi-gateway")) { exit 1 }

Write-Host ""
Write-Host "?? SalesAPI is ready!" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host ""
Write-Host "?? Available endpoints:" -ForegroundColor White
Write-Host "   ?? Gateway:     http://localhost:6000" -ForegroundColor Cyan
Write-Host "   ?? Inventory:   http://localhost:5000" -ForegroundColor Cyan
Write-Host "   ?? Sales:       http://localhost:5001" -ForegroundColor Cyan
Write-Host "   ?? RabbitMQ UI: http://localhost:15672 (admin/admin123)" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Management commands:" -ForegroundColor White
Write-Host "   View logs:      docker compose logs -f" -ForegroundColor Gray
Write-Host "   Stop services:  docker compose down" -ForegroundColor Gray
Write-Host "   Restart:        docker compose restart" -ForegroundColor Gray
Write-Host ""
Write-Host "?? Test the system:" -ForegroundColor White
Write-Host "   curl http://localhost:6000/health" -ForegroundColor Gray
Write-Host "   curl http://localhost:6000/inventory/products" -ForegroundColor Gray
Write-Host ""
Write-Host "?? Authentication test:" -ForegroundColor White
Write-Host "   Get token: Invoke-RestMethod -Uri 'http://localhost:6000/auth/token' -Method Post -Headers @{'Content-Type'='application/json'} -Body '{\"username\":\"admin\",\"password\":\"admin123\"}'" -ForegroundColor Gray
Write-Host ""
Write-Host "?? More info in README.md" -ForegroundColor White