# SalesAPI - Production-Ready Microservices E-Commerce Solution

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Docker](https://img.shields.io/badge/Docker-Compose-blue.svg)](https://docker.com)
[![Tests](https://img.shields.io/badge/Tests-149%20(99.3%25%20Pass)-green.svg)](#testing)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A comprehensive microservices-based e-commerce solution built with .NET 8, featuring containerized deployment, advanced stock management, event-driven architecture, comprehensive observability, and production-ready monitoring.

## ?? Quick Start

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for development)

### One-Command Deployment
```bash
# Start the complete system with observability
docker compose -f docker-compose-observability-simple.yml up -d

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
???????????????    ???????????????    ???????????????
?   Client    ??????  Gateway    ?????? Microservices?
? Application ?    ? (Port 6000) ?    ?   Cluster   ?
???????????????    ???????????????    ???????????????
                           ?                   ?
                           ?                   ?
                   ???????????????    ???????????????
                   ?    Auth     ?    ?  Load       ?
                   ?   & CORS    ?    ? Balancing   ?
                   ???????????????    ???????????????
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

## ?? Testing

### Test Coverage
The project includes comprehensive testing with **149 tests** achieving **99.3% pass rate**:

| Test Type | Count | Pass Rate | Coverage |
|-----------|-------|-----------|----------|
| **Unit Tests** | 97 | 100% | Business logic, validations, calculations |
| **Integration Tests** | 43 | 100% | Database operations, service integration |
| **Contract Tests** | 9 | 100% | Cross-service compatibility |
| **E2E Tests** | 52 | 98.1% | Complete workflow validation |

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories
- **Unit Tests**: Isolated business logic testing
- **Integration Tests**: Database and service integration validation
- **Contract Tests**: Cross-service communication verification
- **Performance Tests**: Load testing and performance validation
- **Security Tests**: Authentication and authorization validation

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

## ?? Development

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- Visual Studio 2022 or VS Code

### Local Development Setup
```bash
# Clone repository
git clone https://github.com/wleicht/SalesAPI.git
cd SalesAPI

# Start infrastructure services
docker compose -f docker-compose.infrastructure.yml up -d

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
docker compose up -d

# Production with monitoring
docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d

# Infrastructure only
docker compose -f docker-compose.infrastructure.yml up -d
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