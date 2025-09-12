# SalesAPI - Production-Ready Microservices E-Commerce Solution

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Docker](https://img.shields.io/badge/Docker-Compose-blue.svg)](https://docker.com)
[![Tests](https://img.shields.io/badge/Tests-54%20(100%25%20Pass)-green.svg)](#testing)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A comprehensive microservices-based e-commerce solution built with .NET 8, featuring containerized deployment, advanced stock management, event-driven architecture, comprehensive observability, and production-ready monitoring with **modern test architecture**.

## ?? Quick Start

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for development)

### One-Command Deployment
```bash
# Start the complete system with observability
docker compose -f docker/compose/docker-compose-observability-simple.yml up -d

# Verify all services are running
docker compose ps

# Test the system
curl http://localhost:6000/health
```

### Service Endpoints
| Service | URL | Description |
|---------|-----|-------------|
| API Gateway | http://localhost:6000 | Unified entry point with authentication |
| Inventory API | http://localhost:5000 | Product catalog and stock management |
| Sales API | http://localhost:5001 | Order processing and payments |
| RabbitMQ UI | http://localhost:15672 | Message broker management (admin/admin123) |
| Prometheus | http://localhost:9090 | Metrics and monitoring |

## ??? Architecture Overview

### System Architecture
```
???????????    ???????????    ???????????????
?   Client    ????  Gateway    ???? Microservices?
? Application ?    ? (Port 6000) ?    ?   Cluster   ?
???????????    ???????????    ???????????????
                           ?                   ?
                           ?                   ?
                   ???????????    ???????????
                   ?    Auth     ?    ?  Load       ?
                   ?   & CORS    ?    ? Balancing   ?
                   ???????????    ???????????
```

### Microservices Components

| Component | Technology | Port | Responsibility |
|-----------|------------|------|----------------|
| **API Gateway** | YARP, JWT | 6000 | Authentication, routing, rate limiting |
| **Inventory API** | .NET 8, EF Core | 5000 | Product catalog, stock reservations |
| **Sales API** | .NET 8, EF Core | 5001 | Order processing, payment simulation |
| **Message Broker** | RabbitMQ, Rebus | 5672 | Event-driven communication |
| **Database** | SQL Server | 1433 | Persistent data storage |
| **Monitoring** | Prometheus | 9090 | Metrics collection and monitoring |

## ? Key Features

### ?? Security & Authentication
- **JWT Authentication**: Token-based security with role-based authorization
- **CORS Configuration**: Cross-origin resource sharing for web clients
- **API Gateway Security**: Centralized authentication and authorization

### ?? Advanced Inventory Management
- **Stock Reservations**: Prevent overselling with reservation-based workflow
- **Saga Pattern**: Distributed transaction management across services
- **Concurrency Control**: Race condition prevention with serializable isolation
- **Audit Trails**: Complete tracking of all stock movements

### ?? Order Processing
- **Multi-Step Workflow**: Validation ? Reservation ? Payment ? Fulfillment
- **Payment Simulation**: Configurable payment processing with failure scenarios
- **Order States**: Pending ? Confirmed ? Fulfilled with proper state transitions
- **Customer Management**: Order history and customer-specific processing

### ?? Event-Driven Architecture
- **RabbitMQ Integration**: Production-ready message broker with Rebus framework
- **Domain Events**: OrderConfirmed, OrderCancelled, StockDebited events
- **Idempotency**: Duplicate event processing prevention
- **Dead Letter Queues**: Failed message handling and recovery

### ?? Observability & Monitoring
- **Correlation Tracking**: End-to-end request tracing across all services
- **Structured Logging**: Consistent log format with Serilog
- **Prometheus Metrics**: HTTP metrics, business metrics, health monitoring
- **Health Checks**: Comprehensive service health monitoring
- **Distributed Tracing**: Complete visibility into microservices interactions

### ?? Production Deployment
- **Docker Compose**: Multi-service orchestration with dependency management
- **Health Checks**: Container health validation and restart policies
- **Data Persistence**: Volume management for databases and message queues
- **Network Isolation**: Secure inter-service communication

## ?? Professional Testing Suite

### Modern Test Architecture
The project features a **comprehensive professional testing suite** with **54 tests** achieving **100% pass rate** in under 10 seconds:

| Test Category | Count | Duration | Technology | Coverage |
|---------------|-------|----------|------------|----------|
| **Domain Tests** | 33 tests | ~2.3s | TestDataBuilders | Business logic, domain rules |
| **Infrastructure Tests** | 17 tests | ~2.7s | In-Memory EF Core | Repository operations, persistence |
| **Integration Tests** | 4 tests | ~2.8s | Test Doubles | Service interactions, workflows |
| **TOTAL** | **54 tests** | **~8.3s** | **No Mocking Libraries** | **100% Success** |

### Test Architecture Benefits
- ? **No Legacy Mock Dependencies**: Eliminated Moq in favor of TestDataBuilders and Test Doubles
- ? **Fast Execution**: Complete test suite runs in under 10 seconds
- ? **Reliable**: 100% deterministic tests with no flaky behavior
- ? **Maintainable**: Clean test code using Builder Pattern and shared infrastructure
- ? **Comprehensive**: Tests covering all layers from domain logic to integration flows

### Running the Professional Test Suite
```bash
# Run all professional tests
dotnet test SalesAPI.sln --filter "FullyQualifiedName~SalesAPI.Tests.Professional"

# Run specific test categories
dotnet test tests/SalesAPI.Tests.Professional/Domain.Tests/
dotnet test tests/SalesAPI.Tests.Professional/Infrastructure.Tests/
dotnet test tests/SalesAPI.Tests.Professional/Integration.Tests/

# View detailed test output
dotnet test tests/SalesAPI.Tests.Professional/ --verbosity normal
```

### Test Infrastructure Components
- **TestDataBuilders**: Fluent API for creating test data without duplication
- **TestInfrastructure**: Shared components for database and messaging testing
- **BaseTestFixture**: Unified setup/teardown patterns with logging
- **In-Memory Testing**: Fast, isolated tests using EF Core in-memory provider

## ?? API Documentation

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

## ?? Project Structure

### Clean and Organized Directory Layout
```
SalesAPI/
??? ?? docker/                          # Docker-related files
?   ??? ?? compose/                     # Docker Compose files
?   ?   ??? docker-compose.yml          # Main services configuration
?   ?   ??? docker-compose-observability-simple.yml # With monitoring
?   ??? ?? observability/               # Monitoring configurations
?   ?   ??? prometheus/                 # Prometheus configuration
?   ??? .dockerignore                   # Docker ignore file
??? ?? scripts/                         # Management and automation scripts
?   ??? docker-manage.sh                # Docker management utility
?   ??? Makefile                        # Make commands
?   ??? setup.sh                        # Environment setup
?   ??? start.ps1/start.sh              # Platform-specific start scripts
??? ?? src/                             # Source code
?   ??? ?? gateway/                     # API Gateway service
?   ??? ?? inventory.api/               # Inventory microservice
?   ??? ?? sales.api/                   # Sales microservice
?   ??? ?? buildingblocks.*/            # Shared contracts and events
??? ?? tests/                           # Modern test projects
?   ??? ?? SalesAPI.Tests.Professional/ # Professional testing suite
?   ?   ??? Domain.Tests/               # Domain logic tests
?   ?   ??? Infrastructure.Tests/       # Repository and persistence tests
?   ?   ??? Integration.Tests/          # Service integration tests
?   ?   ??? TestInfrastructure/         # Shared test components
?   ??? ?? endpoint.tests/              # Legacy endpoint tests (kept for compatibility)
??? ?? docs/                            # Documentation
??? ?? deploy/                          # Deployment configurations
??? ?? README.md                        # Project documentation
??? ?? SalesAPI.sln                     # Solution file
```

### ?? Quick Start

#### Environment Setup
```bash
# Clone repository
git clone https://github.com/wleicht/SalesAPI.git
cd SalesAPI

# Run setup script
./scripts/setup.sh

# Start all services
./scripts/docker-manage.sh start
```

#### Alternative with Make
```bash
# Setup environment
./scripts/setup.sh

# Start services
make -C scripts up

# Check status
make -C scripts status
```

### ?? Docker Management

#### Using Docker Management Script
```bash
# Start all services
./scripts/docker-manage.sh start

# Stop all services
./scripts/docker-manage.sh stop

# Check service status
./scripts/docker-manage.sh status

# View service logs
./scripts/docker-manage.sh logs-follow

# Run health checks
./scripts/docker-manage.sh health

# Show service URLs
./scripts/docker-manage.sh urls

# Run integration tests
./scripts/docker-manage.sh test

# Clean up resources
./scripts/docker-manage.sh clean

# Show all commands
./scripts/docker-manage.sh help
```

#### Using Make Commands
```bash
# Show available commands
make -C scripts help

# Start services
make -C scripts up

# Stop services
make -C scripts down

# View status
make -C scripts status

# Run tests
make -C scripts test

# Clean up
make -C scripts clean
```

### ?? Service URLs

Once started, services are available at:
- **Gateway**: http://localhost:6000
- **Inventory API**: http://localhost:5000  
- **Sales API**: http://localhost:5001
- **Prometheus**: http://localhost:9090
- **RabbitMQ Management**: http://localhost:15672 (admin/admin123)

### Alternative Start Options

#### Using PowerShell Script
```powershell
# Start complete system
./scripts/start.ps1

# Start with observability
./scripts/test-observability.ps1
```

#### Using Bash Script  
```bash
# Start complete system
./scripts/start.sh

# Start with observability
./scripts/test-observability.sh
```

## ?? Development

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code

### Local Development Setup
```bash
# Clone and setup
git clone https://github.com/wleicht/SalesAPI.git
cd SalesAPI
./scripts/setup.sh

# Start with observability
./scripts/docker-manage.sh start
# or
make -C scripts observability

# For development mode
make -C scripts dev
```

### Running Services Individually
```bash
# Start infrastructure only
./scripts/docker-manage.sh stop-apps

# Run APIs locally
dotnet run --project src/gateway
dotnet run --project src/inventory.api
dotnet run --project src/sales.api
```

### Project Structure
```
SalesAPI/
??? src/
?   ??? gateway/                    # API Gateway with YARP
?   ??? inventory.api/              # Inventory microservice
?   ??? sales.api/                  # Sales microservice
?   ??? buildingblocks.contracts/   # Shared contracts and DTOs
?   ??? buildingblocks.events/      # Domain events and messaging
??? tests/
?   ??? inventory.api.tests/        # Inventory unit tests
?   ??? sales.api.tests/           # Sales unit tests
?   ??? contracts.tests/           # Contract tests
?   ??? endpoint.tests/            # End-to-end tests
??? docker-compose*.yml            # Docker composition files
??? docs/                          # Additional documentation
```

### Configuration
Environment variables and configuration options:

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Development | Application environment |
| `ConnectionStrings__DefaultConnection` | Local SQL Server | Database connection |
| `ConnectionStrings__RabbitMQ` | Local RabbitMQ | Message broker connection |
| `JWT__Key` | Generated | JWT signing key |
| `JWT__Issuer` | SalesAPI | JWT token issuer |

## ?? Deployment

### Docker Compose Profiles
```bash
# Development environment
docker compose -f docker/compose/docker-compose.yml up -d

# Production with monitoring
docker compose -f docker/compose/docker-compose.yml -f docker/compose/docker-compose-observability-simple.yml up -d

# Using deployment configurations
./scripts/start.sh production
```

### Environment Configuration
- **Development**: SQLite, in-memory caching, verbose logging
- **Staging**: SQL Server, Redis cache, structured logging
- **Production**: SQL Server, Redis cluster, optimized logging, monitoring

## ?? Monitoring & Observability

### Correlation Tracking
Every request is tracked with unique correlation IDs:
```
Request ? Gateway ? Sales API ? Inventory API ? Database
   |         |          |            |           |
[corr-123] [corr-123] [corr-123]  [corr-123]  [corr-123]
```

### Metrics Collection
Key metrics monitored:
- **HTTP Requests**: Rate, duration, status codes
- **Business Metrics**: Orders created, stock movements, payment success rate
- **System Metrics**: Memory usage, CPU utilization, database connections
- **Event Processing**: Message processing rates, queue depths, errors

### Logging
Structured logging with:
- **Correlation IDs**: Request tracing across services
- **Service Context**: Service name, version, instance
- **Business Context**: Order IDs, customer IDs, product IDs
- **Performance Data**: Request duration, database query times

## ?? Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

### Development Guidelines
- Follow C# coding standards and conventions
- Write comprehensive tests for new features
- Update documentation for API changes
- Ensure Docker builds pass
- Add appropriate logging and monitoring

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Related Projects

- [.NET Microservices](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/)
- [YARP Reverse Proxy](https://microsoft.github.io/reverse-proxy/)
- [RabbitMQ with Rebus](https://github.com/rebus-org/Rebus)
- [Prometheus .NET](https://github.com/prometheus-net/prometheus-net)

## ?? Support

For questions and support:
- Create an [Issue](https://github.com/wleicht/SalesAPI/issues)
- Check [Documentation](docs/)
- Review [Examples](examples/)

---

**SalesAPI** - A production-ready microservices e-commerce solution demonstrating modern .NET development practices, event-driven architecture, and comprehensive observability. ??