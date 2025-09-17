# SalesAPI Cleanup Script for Windows
# Removes temporary files, build artifacts, and ensures clean workspace

Write-Host "?? Iniciando limpeza do projeto SalesAPI..." -ForegroundColor Green

function Safe-Remove-Directories {
    param(
        [string]$Pattern,
        [string]$Description
    )
    
    Write-Host "  Removendo $Description..." -ForegroundColor Yellow
    
    Get-ChildItem -Recurse -Directory -Name $Pattern -ErrorAction SilentlyContinue | ForEach-Object {
        $fullPath = Join-Path $PWD $_
        try {
            Write-Host "    Removendo: $_" -ForegroundColor Gray
            Remove-Item $fullPath -Recurse -Force -ErrorAction Stop
        }
        catch {
            Write-Host "    ??  Não foi possível remover: $_" -ForegroundColor Red
        }
    }
}

function Safe-Remove-Files {
    param(
        [string]$Pattern,
        [string]$Description
    )
    
    Write-Host "  Removendo $Description..." -ForegroundColor Yellow
    
    Get-ChildItem -Recurse -File -Name $Pattern -ErrorAction SilentlyContinue | ForEach-Object {
        $fullPath = Join-Path $PWD $_
        try {
            Write-Host "    Removendo: $_" -ForegroundColor Gray
            Remove-Item $fullPath -Force -ErrorAction Stop
        }
        catch {
            Write-Host "    ??  Não foi possível remover: $_" -ForegroundColor Red
        }
    }
}

# Clean build artifacts
Write-Host "?? Limpando artefatos de build..." -ForegroundColor Cyan
Safe-Remove-Directories "bin" "diretórios bin"
Safe-Remove-Directories "obj" "diretórios obj"

# Clean temporary files
Write-Host "???  Limpando arquivos temporários..." -ForegroundColor Cyan
Safe-Remove-Files "*.tmp" "arquivos temporários"
Safe-Remove-Files "*.log" "arquivos de log"

# Clean IDE specific files
Write-Host "?? Limpando arquivos específicos da IDE..." -ForegroundColor Cyan
Safe-Remove-Directories ".vs" "diretórios Visual Studio"
Safe-Remove-Directories ".vscode" "diretórios VS Code"
Safe-Remove-Files "*.user" "arquivos de usuário"

# Clean test results
Write-Host "?? Limpando resultados de testes..." -ForegroundColor Cyan
Safe-Remove-Directories "TestResults" "resultados de testes"
Safe-Remove-Directories "test-results" "resultados de testes"

if (Test-Path "scripts/manual-tests/results") {
    Write-Host "  Limpando resultados de testes manuais..." -ForegroundColor Yellow
    try {
        Remove-Item "scripts/manual-tests/results\*" -Recurse -Force -ErrorAction Stop
        Write-Host "    ? Resultados de testes manuais limpos" -ForegroundColor Green
    }
    catch {
        Write-Host "    ??  Erro ao limpar testes manuais" -ForegroundColor Red
    }
}

# Clean package caches
Write-Host "?? Limpando caches de packages..." -ForegroundColor Cyan
Safe-Remove-Directories "packages" "diretórios packages"

# Clean coverage reports
Write-Host "?? Limpando relatórios de cobertura..." -ForegroundColor Cyan
Safe-Remove-Directories "coverage" "relatórios de cobertura"
Safe-Remove-Files "*.coverage" "arquivos de cobertura"

Write-Host ""
Write-Host "? Limpeza concluída com sucesso!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Resumo da limpeza:" -ForegroundColor White
Write-Host "  • Artefatos de build removidos" -ForegroundColor Gray
Write-Host "  • Arquivos temporários removidos" -ForegroundColor Gray
Write-Host "  • Caches da IDE limpos" -ForegroundColor Gray
Write-Host "  • Resultados de testes limpos" -ForegroundColor Gray
Write-Host ""
Write-Host "?? O workspace está agora limpo e pronto para desenvolvimento!" -ForegroundColor Green