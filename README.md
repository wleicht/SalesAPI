# SalesAPI - Microservices Architecture with Professional Testing Suite

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/wleicht/SalesAPI)
[![Tests](https://img.shields.io/badge/tests-115%20passing-brightgreen.svg)](./tests/)
[![Architecture](https://img.shields.io/badge/architecture-professional-blue.svg)](./docs/ARCHITECTURE.md)

## Overview

SalesAPI is a modern microservices architecture demonstrating enterprise-grade patterns including CQRS, Event-Driven Architecture, and comprehensive testing strategies. The solution implements a consolidated professional testing approach that eliminates mock dependencies in favor of real implementations and in-memory databases.

## Key Features

### Microservices Architecture
- **API Gateway**: YARP-based routing and authentication
- **Sales Service**: Order management and processing  
- **Inventory Service**: Product and stock management
- **Event-Driven Communication**: RabbitMQ messaging between services
- **JWT Authentication**: Role-based security across services

### Professional Testing Suite
- **115 Tests Total** - Optimized for reliability and speed
- **Zero Mock Dependencies** - Using real implementations and in-memory providers
- **Fast Execution** - ~15 seconds for complete test suite
- **100% Pass Rate** - Deterministic and reliable tests
- **Test Pyramid Architecture** - Balanced distribution from unit to integration tests

### Development Excellence
- **Docker Compose**: Complete containerized development environment
- **Clean Code**: SOLID principles and clean architecture
- **In-Memory Testing**: Fast, isolated tests using EF Core in-memory provider

## Test Suite Statistics (Consolidated Structure)

| Test Category | Project | Tests | Focus | Execution |
|---------------|---------|-------|--------|-----------|
| **Domain Tests** | SalesAPI.Tests.Professional | 33 | Business Logic | ~2.9s |
| **Infrastructure Tests** | SalesAPI.Tests.Professional | 17 | Data & Messaging | ~2.6s |  
| **Integration Tests** | SalesAPI.Tests.Professional | 4 | Cross-Service | ~2.8s |
| **Contract Tests** | contracts.tests | 9 | API Compatibility | ~1.5s |
| **End-to-End Tests** | endpoint.tests | 52 | Full System | ~6.2s |
| **TOTAL** | **3 Projects** | **115** | **Complete Coverage** | **~15s** |

## Quick Start

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code

### Running the Application

1. **Start Infrastructure Services**
   ```bash
   docker-compose up -d sqlserver rabbitmq
   ```

2. **Start the APIs**
   ```bash
   # Terminal 1 - Gateway
   dotnet run --project src/gateway --urls "http://localhost:6000"
   
   # Terminal 2 - Sales API
   dotnet run --project src/sales.api --urls "http://localhost:5001"
   
   # Terminal 3 - Inventory API
   dotnet run --project src/inventory.api --urls "http://localhost:5000"
   ```

3. **Verify Installation**
   ```bash
   # Health checks
   curl http://localhost:6000/health     # Gateway
   curl http://localhost:5001/health     # Sales API  
   curl http://localhost:5000/health     # Inventory API
   ```

### Running Tests

```bash
# Complete professional test suite
dotnet test tests/SalesAPI.Tests.Professional/

# Contract validation tests
dotnet test tests/contracts.tests/

# End-to-end integration tests
dotnet test tests/endpoint.tests/

# All tests
dotnet test
```

## API Usage Examples

### Authentication
```bash
# Get authentication token
curl -X POST http://localhost:6000/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

### Product Management
```bash
# Create product (requires admin role)
curl -X POST http://localhost:6000/inventory/products \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Laptop","description":"Gaming laptop","price":1299.99,"stockQuantity":10}'

# Get products (public)
curl http://localhost:6000/inventory/products
```

### Order Processing
```bash
# Create order (requires authentication)
curl -X POST http://localhost:6000/sales/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"123e4567-e89b-12d3-a456-426614174000","items":[{"productId":"$PRODUCT_ID","quantity":1}]}'

# Get orders (public)
curl http://localhost:6000/sales/orders
```

## Service URLs

Once started, services are available at:
- **Gateway**: http://localhost:6000
- **Inventory API**: http://localhost:5000  
- **Sales API**: http://localhost:5001
- **RabbitMQ Management**: http://localhost:15672 (admin/admin123)

## Project Structure

```
SalesAPI/
??? src/                             # Source code
?   ??? gateway/                     # API Gateway service
?   ??? inventory.api/               # Inventory microservice
?   ??? sales.api/                   # Sales microservice
?   ??? buildingblocks.*/            # Shared contracts and events
??? tests/                           # Professional Testing Suite
?   ??? SalesAPI.Tests.Professional/ # Primary test suite (54 tests)
?   ??? contracts.tests/             # Contract tests (9 tests)
?   ??? endpoint.tests/              # End-to-end tests (52 tests)
??? docs/                            # Documentation
??? scripts/                         # Management and automation scripts
??? docker/                          # Docker-related files
```

## Docker Management

### Using Docker Compose
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

## Benefits of Consolidated Testing Structure

### Performance Benefits
- **~15 second execution** for complete test suite (vs previous ~45 seconds)
- **115 high-quality tests** (vs previous 160+ duplicated tests)
- **Zero flaky tests** - deterministic execution
- **Parallel execution** where appropriate

### Maintainability Benefits
- **Single source of truth** for each test scenario
- **Shared test infrastructure** reduces code duplication
- **Professional patterns** - Test Data Builders, Fixtures, Factories
- **No heavy mocks** - easier to understand and maintain

### Quality Benefits
- **100% pass rate** consistently achieved
- **Real integration testing** with in-memory providers
- **Contract validation** ensures API compatibility
- **Comprehensive coverage** from domain to end-to-end

## Documentation

- [Testing Strategy](./docs/test-strategy.md)
- [Architecture Overview](./docs/ARCHITECTURE.md)
- [Deployment Guide](./docs/DEPLOYMENT.md)
- [API Documentation](./docs/README-tests.md)

## Key Achievements

? **Eliminated duplicate test projects** - Removed `inventory.api.tests` and `sales.api.tests`  
? **Zero mock dependencies** - Professional suite uses real implementations  
? **Fast test execution** - 15 seconds for 115 comprehensive tests  
? **100% reliable tests** - No flaky or intermittent failures  
? **Modern test architecture** - Following industry best practices  
? **Enhanced test builders** - Comprehensive scenario coverage  
? **Clean documentation** - Clear guidance for all stakeholders  

---

**This solution represents a professional-grade microservices implementation with enterprise testing standards - ready for production deployment and team scalability.**