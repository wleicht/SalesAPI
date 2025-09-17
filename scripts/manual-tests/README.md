# Manual Testing Framework - SalesAPI

## Overview

This is a **complete manual testing framework** for the SalesAPI project, a microservices architecture with:

- **Gateway** (port 6000): Single entry point with JWT authentication
- **Inventory API** (port 5000): Product and stock management  
- **Sales API** (port 5001): Order processing and stock reservations

## Quick Start

### Option 1: Automated Setup (Recommended)
```bash
# Linux/macOS
./scripts/manual-tests/setup.sh

# Windows (PowerShell)
.\scripts\manual-tests\setup.ps1
```

### Option 2: Interactive Menu  
```bash
# Linux/macOS
./scripts/manual-tests/test_manager.sh

# Windows (Git Bash/WSL)
./scripts/manual-tests/interactive_tests.sh
```

### Option 3: Quick Validation Test
```bash
# Demonstrates main functionality in ~2 minutes
./scripts/manual-tests/demo_tests.sh
```

### Option 4: Complete Suite
```bash
# Run all tests (~10-15 minutes)
./scripts/manual-tests/run_manual_tests.sh

# Or basic tests only (~5 minutes)
./scripts/manual-tests/run_manual_tests.sh --only-basic
```

## Complete Framework Structure

```
scripts/manual-tests/
??? README.md                    # This file - main documentation
??? setup.sh/ps1               # Initial configuration scripts
??? demo_tests.sh               # Quick validation test
??? test_manager.sh             # Main interactive menu
??? interactive_tests.sh        # Alternative interactive interface
??? sample_execution.sh         # Practical usage examples
? 
??? run_manual_tests.sh/ps1     # Main test runner
? 
??? Individual Tests:
?   ??? 01_authentication_tests.sh    # JWT authentication
?   ??? 02_health_tests.sh           # Health verification
?   ??? 03_products_tests.sh         # Product management  
?   ??? 04_orders_tests.sh           # Order processing
?   ??? 05_reservations_tests.sh     # Stock reservations
?   ??? 06_validation_tests.sh       # Validation and edge cases
?   ??? 07_concurrency_tests.sh      # Concurrency tests
? 
??? Utilities:
?   ??? utils/test_utils.sh             # Shared bash functions
?   ??? utils/test_utils.psm1           # PowerShell functions
? 
??? Configuration:
?   ??? config/endpoints.json           # URLs and test data
? 
??? Documentation:
?   ??? troubleshooting.md          # Problem solutions
?   ??? validation_checklist.md     # Complete validation checklist
?   ??? curl_examples.md            # Manual cURL commands
? 
??? results/                        # Test results (generated)
    ??? test_results_[timestamp].json
    ??? test_summary_[timestamp].txt
    ??? failed_tests.log
```

## Test Categories Implemented

### 1. Authentication Tests (6 tests)
- ? Admin credentials login
- ? Customer credentials login  
- ? Invalid credentials rejection
- ? JWT token structure validation
- ? Test user listing
- ? Token expiration verification

### 2. Health Check Tests (7 tests)
- ? Gateway status 
- ? Gateway route information
- ? Inventory API health check (direct and via gateway)
- ? Sales API health check (direct and via gateway)
- ? Response time validation
- ? Inter-service connectivity verification

### 3. Product Management Tests (12 tests)
- ? List products (public access)
- ? List products with pagination
- ? Create product (admin required)
- ? Search product by ID
- ? Update product (admin required)
- ? Delete product (admin required)
- ? Role-based access control
- ? Input data validation
- ? Resource not found handling
- ? Edge cases (invalid IDs, malformed data)

### 4. Order Management Tests (13 tests)
- ? List orders (public access)
- ? Create order with authentication
- ? Process order with multiple items
- ? Simulate payment failure (high prices)
- ? Confirm orders manually
- ? Cancel orders with reason
- ? Mark orders as delivered
- ? Required data validation
- ? Invalid quantity handling
- ? Authorization control
- ? Stock system integration
- ? Correlation ID tracking

### 5. Stock Reservation Tests (8 tests)
- ? Create reservation directly via API
- ? Query reservations by order
- ? Query specific reservation by ID
- ? Validate insufficient stock
- ? Reservation for non-existent products
- ? Idempotency testing (duplicates)
- ? Authorization control for reservations
- ? Stock consistency after operations

### 6. Validation Tests (15 tests)
- ? Invalid pagination parameters
- ? Malformed JSON
- ? Missing required fields
- ? Extremely long product names
- ? Negative stock quantities
- ? Zero or negative prices
- ? Empty arrays in orders
- ? Invalid GUID formats
- ? Incorrect Content-Type headers
- ? Strict input validation

### 7. Concurrency Tests (6 tests)
- ? Multiple simultaneous orders
- ? Overselling prevention
- ? Rapid successive calls
- ? Simultaneous authentication
- ? Mixed concurrent operations
- ? Final consistency verification

## Prerequisites
- **Running services**: Gateway (6000), Inventory (5000), Sales (5001)
- **Dependencies**: SQL Server (1433), RabbitMQ (5672)
- **Tools**: curl, bash/PowerShell, jq (recommended)

## Usage Examples

### Step-by-Step Execution

#### 1. Verify Environment
```bash
# Check if services are running
./scripts/manual-tests/02_health_tests.sh

# Or use status checker
./scripts/manual-tests/test_manager.sh  # Option 's'
```

#### 2. Quick Test (2 minutes)
```bash
# Validate basic functionality
./scripts/manual-tests/demo_tests.sh
```

#### 3. Basic Tests (5 minutes)
```bash
# Authentication + Health + Products + Basic Orders
./scripts/manual-tests/run_manual_tests.sh --only-basic
```

#### 4. Complete Suite (10-15 minutes)
```bash
# All tests including concurrency and edge cases
./scripts/manual-tests/run_manual_tests.sh
```

#### 5. Individual Tests
```bash
# Run specific category
./scripts/manual-tests/01_authentication_tests.sh
./scripts/manual-tests/03_products_tests.sh
# ... etc
```

## Result Interpretation

### ? Complete Success
```
============================================
SalesAPI Manual Tests Summary
============================================
Total Tests: 67
Passed: 67
Failed: 0
Success Rate: 100%
============================================
```

### ?? Partial Success
```
============================================
SalesAPI Manual Tests Summary  
============================================
Total Tests: 67
Passed: 62
Failed: 5
Success Rate: 92.5%
============================================
```

### ?? Result Files
- `results/test_results_[timestamp].json` - Detailed results
- `results/test_summary_[timestamp].txt` - Executive summary
- `results/failed_tests.log` - Failure log (if any)

## Troubleshooting

### Problem: Services Not Responding
```bash
# Check ports
netstat -an | grep -E "(5000|5001|6000)"

# Start services
cd src/gateway && dotnet run &
cd src/inventory.api && dotnet run &  
cd src/sales.api && dotnet run &

# Or use Docker
docker-compose up -d
```

### Problem: Authentication Failures
```bash
# Check default credentials
curl http://localhost:6000/auth/test-users

# Default credentials:
# Admin: admin/admin123
# Customer: customer1/password123
```

### Problem: Database Errors
```bash
# Check SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" \
  -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

See **troubleshooting.md** for specific problems.

## Test Coverage

### Covered Functionality ?
- **JWT Authentication** - Login, logout, token validation
- **RBAC Authorization** - Admin vs Customer permissions  
- **Product CRUD** - Create, Read, Update, Delete
- **Order CRUD** - Creation, query, confirmation, cancellation
- **Stock Reservations** - Creation, query, release
- **Gateway Routing** - Routing to microservices
- **Input Validation** - Required fields, formats
- **Error Handling** - Appropriate HTTP codes
- **Concurrency** - Race conditions, overselling prevention
- **Consistency** - Transactions, saga compensation
- **Observability** - Logs, metrics, correlation IDs

### Business Scenarios ??
- **Complete Purchase Flow** - From product to delivered order
- **Payment Failure** - Compensation and stock release
- **Inventory Management** - Stock tracking with reservations
- **Multiple Users** - Different roles and permissions
- **High Concurrency** - Multiple simultaneous users
- **Error Recovery** - Resilience and circuit breakers

## Use Cases

### ????? For Developers
```bash
# Quick test after changes
./scripts/manual-tests/demo_tests.sh

# Test specific category
./scripts/manual-tests/03_products_tests.sh

# CI/CD - all tests
./scripts/manual-tests/run_manual_tests.sh
```

### ?? For QA/Testers
```bash
# Guided interface
./scripts/manual-tests/test_manager.sh

# Complete checklist
./scripts/manual-tests/run_manual_tests.sh
# Consult: validation_checklist.md
```

### ?? For DevOps/Production
```bash
# Production smoke test  
./scripts/manual-tests/02_health_tests.sh

# Stress tests
./scripts/manual-tests/07_concurrency_tests.sh

# Continuous monitoring
./scripts/manual-tests/run_manual_tests.sh --only-basic
```

## Advanced Features

### Correlation IDs
- All tests include correlation IDs for tracking
- Facilitates debugging in distributed environments
- Automatic `X-Correlation-Id` headers

### Retry Logic  
- Automatic retries for operations that may fail
- Exponential backoff in concurrency tests
- Tolerance for temporary failures

### Automatic Cleanup
- Test data cleanup after execution
- Prevention of conflicts between runs
- Isolation between test categories

### Detailed Reports
- Results in structured JSON
- Human-readable executive summaries
- Failure logs with context
- Performance metrics

### Multi-Platform
- Bash scripts for Linux/macOS
- PowerShell scripts for Windows
- Unified configuration
- Consistent behavior

## Validation Checklist

Before considering the system ready:

- [ ] All services respond in < 5 seconds
- [ ] Test success rate > 95%
- [ ] Authentication and authorization working
- [ ] Complete product CRUD functional
- [ ] Order and reservation system operational  
- [ ] Appropriate error handling
- [ ] Structured logs being generated
- [ ] No resource leaks
- [ ] Documentation updated

See **validation_checklist.md** for complete list.

## Support

- **Documentation**: `.md` files in this directory
- **Examples**: `curl_examples.md` and `sample_execution.sh`
- **Problems**: `troubleshooting.md`
- **Validation**: `validation_checklist.md`

## Conclusion

This manual testing framework provides **complete coverage** of SalesAPI with:

- ? **67+ tests** covering all functionality
- ? **7 categories** of specialized tests  
- ? **Multiple interfaces** (CLI, interactive, programmatic)
- ? **Multi-platform** (Linux, macOS, Windows)
- ? **Complete documentation** for all scenarios
- ? **Production-ready** with logging and reports

**The system is ready for complete validation and production use!**