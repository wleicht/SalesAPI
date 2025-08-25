# Test Execution Guide

## Overview

This guide provides comprehensive instructions for running, debugging, and maintaining the test suites in the SalesAPI solution. It covers test execution strategies, troubleshooting procedures, and best practices for different development scenarios.

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code
- Git

### Environment Setup
```bash
# Clone repository
git clone https://github.com/wleicht/SalesAPI.git
cd SalesAPI

# Start containerized environment
docker compose -f docker-compose-observability-simple.yml up -d

# Verify services are running
docker compose -f docker-compose-observability-simple.yml ps
```

### Run All Tests
```bash
# Execute complete test suite
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Execution Strategies

### 1. Development Workflow Testing

#### Fast Feedback Loop (Unit Tests Only)
```bash
# Run unit tests only (fastest feedback)
dotnet test tests/inventory.api.tests/
dotnet test tests/sales.api.tests/
dotnet test tests/contracts.tests/

# Expected execution time: < 15 seconds
```

#### Component Testing
```bash
# Test specific component
dotnet test tests/inventory.api.tests/ --filter "FullyQualifiedName~ProductValidator"
dotnet test tests/sales.api.tests/ --filter "FullyQualifiedName~OrderTests"

# Test specific namespace
dotnet test --filter "Namespace~Models"
dotnet test --filter "Namespace~Validation"
```

### 2. Integration Testing

#### Prerequisites Check
```bash
# Verify services are healthy
curl http://localhost:5000/health  # Inventory API
curl http://localhost:5001/health  # Sales API
curl http://localhost:6000/health  # Gateway

# Expected response: "Healthy"
```

#### Full Integration Test Suite
```bash
# Run all integration tests
dotnet test tests/endpoint.tests/

# Expected execution time: 2-3 minutes
# Prerequisites: All services must be running
```

#### Specific Integration Test Categories
```bash
# Authentication tests
dotnet test tests/endpoint.tests/ --filter "FullyQualifiedName~AuthenticationTests"

# CRUD operation tests
dotnet test tests/endpoint.tests/ --filter "FullyQualifiedName~ProductCrudTests"
dotnet test tests/endpoint.tests/ --filter "FullyQualifiedName~OrderCrudTests"

# Advanced workflow tests
dotnet test tests/endpoint.tests/ --filter "FullyQualifiedName~StockReservationTests"
dotnet test tests/endpoint.tests/ --filter "FullyQualifiedName~EventDrivenTests"
```

### 3. Continuous Integration Testing

#### CI Pipeline Execution
```bash
# Complete CI test suite
dotnet test --configuration Release --logger trx --results-directory TestResults/

# Generate coverage reports
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults/
```

#### Quality Gate Validation
```bash
# Ensure all tests pass
dotnet test --configuration Release --no-build --verbosity minimal

# Expected result: 100% pass rate (158/158 tests)
```

## Test Categories and Filters

### By Test Type
```bash
# Unit tests
dotnet test --filter "Category=Unit"

# Integration tests  
dotnet test --filter "Category=Integration"

# Contract tests
dotnet test --filter "Category=Contract"

# End-to-end tests
dotnet test --filter "Category=E2E"
```

### By Service
```bash
# Inventory service tests
dotnet test tests/inventory.api.tests/

# Sales service tests
dotnet test tests/sales.api.tests/

# Cross-service tests
dotnet test tests/endpoint.tests/
dotnet test tests/contracts.tests/
```

### By Functionality
```bash
# Authentication and authorization
dotnet test --filter "FullyQualifiedName~Authentication"

# Product management
dotnet test --filter "FullyQualifiedName~Product"

# Order processing
dotnet test --filter "FullyQualifiedName~Order"

# Stock management
dotnet test --filter "FullyQualifiedName~Stock"

# Event processing
dotnet test --filter "FullyQualifiedName~Event"
```

### By Performance
```bash
# Fast tests only (< 1 second each)
dotnet test --filter "Category=Fast"

# Slow tests (> 5 seconds each)
dotnet test --filter "Category=Slow"
```

## Development Scenarios

### 1. Feature Development
```bash
# During feature development, run relevant tests frequently
# Example: Working on product validation

# Run related unit tests
dotnet test tests/inventory.api.tests/ --filter "FullyQualifiedName~Product"

# Run related integration tests
dotnet test tests/endpoint.tests/ --filter "FullyQualifiedName~ProductCrud"

# Full validation before commit
dotnet test
```

### 2. Bug Investigation
```bash
# Reproduce bug with specific test
dotnet test --filter "FullyQualifiedName~[TestMethodName]" --verbosity diagnostic

# Debug with detailed logging
dotnet test --filter "FullyQualifiedName~[TestMethodName]" --logger "console;verbosity=detailed"

# Isolate failing test
dotnet test tests/endpoint.tests/ --filter "FullyQualifiedName~ConcurrentOrderCreation"
```

### 3. Refactoring Validation
```bash
# Before refactoring: Ensure all tests pass
dotnet test

# During refactoring: Run affected test suite
dotnet test tests/inventory.api.tests/  # If refactoring inventory

# After refactoring: Full regression test
dotnet test --configuration Release
```

### 4. Performance Testing
```bash
# Measure test execution time
Measure-Command { dotnet test tests/inventory.api.tests/ }

# Run tests multiple times for consistency
for ($i=1; $i -le 5; $i++) { 
    Write-Host "Run $i"
    Measure-Command { dotnet test tests/inventory.api.tests/ --verbosity quiet }
}
```

## Environment-Specific Testing

### 1. Local Development
```bash
# Standard local testing
docker compose -f docker-compose-observability-simple.yml up -d
dotnet test

# Local debugging with IDE
# Use Visual Studio Test Explorer or VS Code Test Runner
```

### 2. Docker Environment Testing
```bash
# Test inside Docker containers
docker compose -f docker-compose-observability-simple.yml exec inventory dotnet test
docker compose -f docker-compose-observability-simple.yml exec sales dotnet test
```

### 3. CI/CD Pipeline Testing
```yaml
# GitHub Actions example
name: Test Suite
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
        
    - name: Start Services
      run: docker compose -f docker-compose-observability-simple.yml up -d
      
    - name: Wait for Services
      run: |
        sleep 30
        curl --retry 10 --retry-delay 5 http://localhost:5000/health
        curl --retry 10 --retry-delay 5 http://localhost:5001/health
        curl --retry 10 --retry-delay 5 http://localhost:6000/health
        
    - name: Run Tests
      run: dotnet test --configuration Release --logger trx --results-directory TestResults/
      
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      with:
        name: test-results
        path: TestResults/
```

## Debugging and Troubleshooting

### 1. Common Issues and Solutions

#### Service Not Ready
```bash
# Issue: Tests fail because services haven't started
# Symptoms: Connection refused, HTTP errors

# Solution: Check service health
curl http://localhost:5000/health
curl http://localhost:5001/health
curl http://localhost:6000/health

# Wait for services to be ready
docker compose -f docker-compose-observability-simple.yml logs inventory
docker compose -f docker-compose-observability-simple.yml logs sales
docker compose -f docker-compose-observability-simple.yml logs gateway
```

#### Authentication Issues
```bash
# Issue: Authentication tests failing
# Symptoms: 401 Unauthorized responses

# Debug: Test authentication manually
curl -X POST http://localhost:6000/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Check user database setup
docker compose -f docker-compose-observability-simple.yml logs gateway
```

#### Database Issues
```bash
# Issue: Database connection or data issues
# Symptoms: Entity Framework errors, data not persisting

# Check database logs
docker compose -f docker-compose-observability-simple.yml logs

# Reset database state
docker compose -f docker-compose-observability-simple.yml down
docker compose -f docker-compose-observability-simple.yml up -d
```

#### Event Processing Issues
```bash
# Issue: Events not being processed
# Symptoms: Stock not updating, event tests failing

# Check RabbitMQ status
curl http://localhost:15672  # RabbitMQ Management UI (guest/guest)

# Check event processing logs
docker compose -f docker-compose-observability-simple.yml logs inventory | grep -i event
docker compose -f docker-compose-observability-simple.yml logs sales | grep -i event
```

### 2. Debugging Specific Test Failures

#### Race Condition Tests
```bash
# Debug concurrent test issues
dotnet test tests/endpoint.tests/ --filter "ConcurrentOrderCreation" --verbosity diagnostic

# Run multiple times to identify intermittent issues
for ($i=1; $i -le 10; $i++) { 
    Write-Host "Attempt $i"
    dotnet test tests/endpoint.tests/ --filter "ConcurrentOrderCreation"
}
```

#### Event-Driven Tests
```bash
# Debug event processing with extended timeout
dotnet test tests/endpoint.tests/ --filter "EventDriven" --verbosity normal

# Check event processing timing
# Look for logs indicating event publish/consume times
```

#### Stock Reservation Tests
```bash
# Debug reservation logic
dotnet test tests/endpoint.tests/ --filter "StockReservation" --verbosity detailed

# Check reservation state in database
# Use database inspection tools if needed
```

### 3. Logging and Diagnostics

#### Enable Detailed Logging
```bash
# Run tests with maximum verbosity
dotnet test --verbosity diagnostic

# Capture detailed test output
dotnet test --logger "console;verbosity=detailed" > test-output.log 2>&1
```

#### Analyze Test Output
```bash
# Search for specific error patterns
grep -i "error\|exception\|fail" test-output.log

# Look for timing issues
grep -i "timeout\|delay\|wait" test-output.log

# Check authentication problems
grep -i "401\|unauthorized\|authentication" test-output.log
```

## Performance Optimization

### 1. Test Execution Optimization

#### Parallel Test Execution
```xml
<!-- In test project files -->
<PropertyGroup>
  <ParallelizeTestCollections>true</ParallelizeTestCollections>
  <ParallelizeTestsWithinAssembly>true</ParallelizeTestsWithinAssembly>
</PropertyGroup>
```

#### Test Data Optimization
```csharp
// Use shared test fixtures for expensive setup
public class DatabaseFixture : IDisposable
{
    public DatabaseFixture()
    {
        // Expensive setup once per test class
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created.
}
```

### 2. Resource Management
```bash
# Monitor resource usage during tests
docker stats

# Optimize Docker resource allocation
# Adjust memory limits in docker-compose files if needed
```

## Maintenance Procedures

### 1. Regular Maintenance Tasks

#### Weekly Test Health Check
```bash
# Run complete test suite
dotnet test --configuration Release

# Check for flaky tests (run multiple times)
for ($i=1; $i -le 5; $i++) { 
    dotnet test --configuration Release --verbosity quiet
}

# Review test execution times
dotnet test --logger "console;verbosity=normal" | Select-String "Test run"
```

#### Test Data Cleanup
```bash
# Clean test artifacts
Remove-Item -Recurse -Force TestResults/ -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force */bin/Debug/ -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force */obj/ -ErrorAction SilentlyContinue

# Rebuild test projects
dotnet clean
dotnet build
```

### 2. Test Suite Updates

#### Adding New Tests
```bash
# After adding new tests, verify they run correctly
dotnet test tests/[project]/ --filter "FullyQualifiedName~[NewTestMethod]"

# Ensure new tests don't break existing suite
dotnet test
```

#### Updating Test Dependencies
```bash
# Update test packages
dotnet add package xunit --version [latest]
dotnet add package FluentAssertions --version [latest]

# Verify compatibility after updates
dotnet test
```

## Reporting and Metrics

### 1. Test Results Reporting
```bash
# Generate test results in various formats
dotnet test --logger "trx;LogFileName=results.trx"
dotnet test --logger "html;LogFileName=results.html"
dotnet test --logger "junit;LogFileName=results.xml"
```

### 2. Code Coverage
```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Install coverage report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML coverage report
reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html
```

### 3. Performance Metrics
```bash
# Measure test suite performance
Measure-Command { dotnet test }

# Track test execution trends over time
# (Store results in CI/CD metrics dashboard)
```

## Best Practices Summary

### Development Workflow
1. **Run unit tests frequently** during development
2. **Run integration tests** before committing changes
3. **Run complete suite** before merging/deploying
4. **Debug failing tests immediately** - don't let them accumulate

### Test Organization
1. **Use descriptive test names** that explain the scenario
2. **Group related tests** in appropriate test classes
3. **Use test categories** for efficient filtering
4. **Maintain test independence** - no shared state

### Environment Management
1. **Keep test environment clean** - restart services when needed
2. **Use unique test data** to avoid interference
3. **Monitor resource usage** to prevent performance issues
4. **Document environment requirements** for new team members

### Quality Assurance
1. **Maintain 100% test pass rate** - fix failures immediately
2. **Review test coverage regularly** - aim for high coverage
3. **Update tests when code changes** - keep them current
4. **Refactor test code** - maintain test quality like production code

---

*Last Updated: January 2025*
*Version: 1.0.0*