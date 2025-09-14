# PowerShell script para executar a Suite de Testes Profissionais Consolidada

Write-Host "?? Executando Suite de Testes Profissionais Consolidada - SalesAPI" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Fun��o para exibir separador
function Show-Separator {
    Write-Host "=================================================================" -ForegroundColor Gray
}

# Fun��o para verificar resultado
function Test-Result {
    param($TestName)
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? $TestName - SUCESSO" -ForegroundColor Green
    } else {
        Write-Host "? $TestName - FALHA" -ForegroundColor Red
        exit 1
    }
}

Write-Host "?? Estrutura Consolidada:" -ForegroundColor Yellow
Write-Host "- Domain Tests (33 testes)      - L�gica de neg�cio pura" -ForegroundColor White
Write-Host "- Infrastructure Tests (17 testes) - Persist�ncia e messaging" -ForegroundColor White
Write-Host "- Integration Tests (4 testes)   - Fluxos entre componentes" -ForegroundColor White
Write-Host "- Contract Tests (9 testes)      - Compatibilidade de APIs" -ForegroundColor White
Write-Host "- Endpoint E2E Tests (52 testes) - Cen�rios completos" -ForegroundColor White
Write-Host ""
Show-Separator

# 1. Domain Tests (mais r�pidos - executar primeiro)
Write-Host "?? Executando Domain Tests (Unit Tests)..." -ForegroundColor Blue
dotnet test tests/SalesAPI.Tests.Professional/Domain.Tests/ --verbosity minimal --no-build
Test-Result "Domain Tests"
Write-Host ""

# 2. Infrastructure Tests
Write-Host "??? Executando Infrastructure Tests..." -ForegroundColor Blue
dotnet test tests/SalesAPI.Tests.Professional/Infrastructure.Tests/ --verbosity minimal --no-build
Test-Result "Infrastructure Tests"
Write-Host ""

# 3. Integration Tests (Professional)
Write-Host "?? Executando Integration Tests (Professional)..." -ForegroundColor Blue
dotnet test tests/SalesAPI.Tests.Professional/Integration.Tests/ --verbosity minimal --no-build
Test-Result "Integration Tests"
Write-Host ""

# 4. Contract Tests
Write-Host "?? Executando Contract Tests..." -ForegroundColor Blue
dotnet test tests/contracts.tests/ --verbosity minimal --no-build
Test-Result "Contract Tests"
Write-Host ""

# 5. Endpoint E2E Tests (requer servi�os rodando)
Write-Host "?? Executando Endpoint E2E Tests..." -ForegroundColor Blue
Write-Host "   (Nota: Requer servi�os rodando - docker-compose up)" -ForegroundColor Yellow
dotnet test tests/endpoint.tests/ --verbosity minimal --no-build
Test-Result "Endpoint E2E Tests"
Write-Host ""

Show-Separator
Write-Host "?? Executando Suite Completa Consolidada..." -ForegroundColor Magenta
dotnet test --configuration Release --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Show-Separator
    Write-Host "?? SUITE DE TESTES PROFISSIONAIS CONSOLIDADA - SUCESSO TOTAL!" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? Resumo da Execu��o:" -ForegroundColor Cyan
    Write-Host "? Domain Tests: 33 testes (l�gica de neg�cio)" -ForegroundColor Green
    Write-Host "? Infrastructure Tests: 17 testes (persist�ncia/messaging)" -ForegroundColor Green
    Write-Host "? Integration Tests: 4 testes (fluxos profissionais)" -ForegroundColor Green
    Write-Host "? Contract Tests: 9 testes (compatibilidade APIs)" -ForegroundColor Green
    Write-Host "? Endpoint E2E Tests: 52 testes (cen�rios completos)" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? Total: 115 testes de alta qualidade" -ForegroundColor Yellow
    Write-Host "? Arquitetura profissional consolidada" -ForegroundColor Yellow
    Write-Host "?? Zero duplica��o, m�xima efici�ncia" -ForegroundColor Yellow
    Write-Host ""
    Show-Separator
} else {
    Write-Host ""
    Write-Host "? FALHA NA SUITE DE TESTES" -ForegroundColor Red
    Write-Host "   Verifique os logs acima para identificar problemas" -ForegroundColor Red
    exit 1
}