# SalesAPI - Complete Microservices Architecture with Event-Driven Communication

A comprehensive microservices-based e-commerce solution built with .NET 8, featuring inventory management, sales processing, API Gateway with YARP reverse proxy, JWT authentication with role-based authorization, event-driven architecture with RabbitMQ, and comprehensive automated testing.

## ??? Architecture Overview

This solution implements a complete microservices architecture with event-driven communication:

- **API Gateway** - Unified entry point using YARP reverse proxy with JWT token issuer (Port 6000)
- **Inventory API** - Product catalog management with role-based authorization and event consumption (Port 5000)
- **Sales API** - Order processing with customer authentication and event publishing (Port 5001)
- **RabbitMQ Message Broker** - Event-driven communication between services (Port 5672)
- **Building Blocks Contracts** - Shared DTOs, events, and contracts between services
- **Building Blocks Events** - Domain events and messaging infrastructure
- **Automated Tests** - Comprehensive endpoint, integration, authentication, routing, and event-driven testing

## ?? Project Structure

```
SalesAPI/
??? src/
?   ??? gateway/                    # API Gateway with YARP reverse proxy and JWT auth
?   ??? inventory.api/              # Inventory microservice with admin protection and event consumption
?   ??? sales.api/                  # Sales microservice with customer authentication and event publishing
?   ??? buildingblocks.contracts/   # Shared contracts, DTOs, and auth models
?   ??? buildingblocks.events/      # Domain events and messaging infrastructure
??? tests/
?   ??? endpoint.tests/             # Unified integration tests for all APIs, auth, and events
??? deploy/
?   ??? docker-compose.infrastructure.yml  # RabbitMQ and SQL Server containers
?   ??? Dockerfile.sqlserver              # SQL Server container configuration
??? README.md
```

## ?? Event-Driven Architecture Flow

```
Client Request ? API Gateway (6000) ? JWT Authentication ? Sales API (5001)
                                   ?                         ?
                             Token Validation          Order Creation
                                   ?                         ?
                           Inventory API (5000)    ? ? RabbitMQ (5672)
                                   ?                         ?
                            Event Consumption         Event Publishing
                                   ?                         ?
                           Stock Deduction         OrderConfirmedEvent
                                   ?                         ?
                           StockDebitedEvent    ? RabbitMQ ? Sales API
```

### Event Flow Details

1. **Order Creation**: Customer creates order via Sales API
2. **Event Publishing**: Sales API publishes `OrderConfirmedEvent` to RabbitMQ
3. **Event Consumption**: Inventory API consumes event and debits stock
4. **Response Event**: Inventory API publishes `StockDebitedEvent` back to Sales API
5. **Idempotency**: All events are processed idempotently to prevent duplicate operations

## ?? Event-Driven Components

### Domain Events

#### OrderConfirmedEvent
```csharp
public class OrderConfirmedEvent : DomainEvent
{
    public Guid OrderId { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public string CorrelationId { get; set; } = string.Empty;
}
```

#### StockDebitedEvent
```csharp
public class StockDebitedEvent : DomainEvent
{
    public Guid OrderId { get; set; }
    public List<StockDeduction> StockDeductions { get; set; } = new();
    public bool AllDeductionsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
```

### Event Infrastructure

- **Event Publisher**: Rebus-based publisher with RabbitMQ transport
- **Event Handlers**: Automatic message consumption with retry policies
- **Idempotency**: ProcessedEvent tracking to prevent duplicate processing
- **Correlation ID**: End-to-end traceability across service boundaries
- **Transactional Outbox**: Ensures reliable event publishing

## ?? Quick Start with Event-Driven Architecture

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/) (for RabbitMQ and SQL Server)
- [Git](https://git-scm.com/)

### 1. Clone the Repository

```bash
git clone https://github.com/wleicht/SalesAPI.git
cd SalesAPI
```

### 2. Start Infrastructure Services

```bash
# Start RabbitMQ and SQL Server with Docker Compose
docker-compose -f docker-compose.infrastructure.yml up -d

# Verify services are running
docker ps
```

### 3. Apply Database Migrations

```bash
# Apply database migrations for Inventory API
dotnet ef database update --project src/inventory.api

# Apply database migrations for Sales API
dotnet ef database update --project src/sales.api
```

### 4. Start All Microservices

```bash
# Terminal 1: Start Inventory API
dotnet run --project src/inventory.api --urls "http://localhost:5000"

# Terminal 2: Start Sales API  
dotnet run --project src/sales.api --urls "http://localhost:5001"

# Terminal 3: Start API Gateway
dotnet run --project src/gateway --urls "http://localhost:6000"
```

### 5. Verify Event-Driven System

```bash
# Check RabbitMQ Management UI
open http://localhost:15672
# Login: admin / admin123

# Check service health
curl http://localhost:6000/health
curl http://localhost:5000/health
curl http://localhost:5001/health
```

## ?? Authentication & Authorization

### **JWT Token-Based Security**
- **Token Issuer**: Gateway (`/auth/token`)
- **Token Validation**: All backend services
- **Role-Based Access**: Admin and Customer roles
- **Session Duration**: 1 hour

### **Protection Rules**
| Service | Operation | Access Level | Required Role |
|---------|-----------|--------------|---------------|
| **Inventory** | `GET /products` | Open Access | None |
| **Inventory** | `POST /products` | Protected | `admin` |
| **Inventory** | `PUT /products/{id}` | Protected | `admin` |
| **Inventory** | `DELETE /products/{id}` | Protected | `admin` |
| **Sales** | `GET /orders` | Open Access | None |
| **Sales** | `POST /orders` | Protected | `customer` or `admin` |
| **Gateway** | `POST /auth/token` | Open Access | None |
| **All** | `/health` | Open Access | None |

### **Development Users**
| Username | Password | Role | Permissions |
|----------|----------|------|-------------|
| `admin` | `admin123` | admin | Full access to products and orders |
| `customer1` | `password123` | customer | Can create orders only |
| `customer2` | `password123` | customer | Can create orders only |
| `manager` | `manager123` | manager | Reserved for future use |

## ?? Event-Driven Workflow Example

### Complete Order Processing Flow

```bash
# 1. Authenticate as customer
curl -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "customer1",
    "password": "password123"
  }'

# 2. Create a product (as admin)
curl -X POST "http://localhost:6000/inventory/products" \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop",
    "description": "High-performance gaming laptop",
    "price": 1299.99,
    "stockQuantity": 50
  }'

# 3. Create order (triggers event-driven stock deduction)
curl -X POST "http://localhost:6000/sales/orders" \
  -H "Authorization: Bearer <customer-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "items": [
      {
        "productId": "<product-id-from-step-2>",
        "quantity": 2
      }
    ]
  }'

# 4. Verify stock was automatically debited
curl "http://localhost:6000/inventory/products/<product-id>"
# Stock should be reduced from 50 to 48
```

## ?? Running Event-Driven Tests

The project includes comprehensive automated tests covering all functionality including event-driven communication.

### Run All Tests (47 Tests)
```bash
dotnet test tests/endpoint.tests/endpoint.tests.csproj
```

### Run Event-Driven Specific Tests
```bash
# Event-driven architecture tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "EventDrivenTests"
```

### Current Test Coverage (47 Tests)

#### Event-Driven Tests (3 tests) ? NEW
- ? Order creation with automatic stock deduction via events
- ? Insufficient stock handling without event processing
- ? Multiple concurrent orders with proper event sequencing
- ? Idempotency verification with duplicate events
- ? End-to-end correlation ID tracking

#### Authentication Tests (10 tests)
- ? JWT token generation and validation
- ? Role-based access control
- ? Unauthorized access prevention
- ? Protected endpoint authorization
- ? Open access endpoint validation

#### Gateway Tests (13 tests)
- ? Health check and status endpoints
- ? Routing to backend services
- ? YARP reverse proxy functionality
- ? Error handling for invalid routes

#### Product CRUD Tests (6 tests)
- ? Product creation with admin authentication
- ? Authorization validation
- ? Open access for product reading
- ? Role-based permission enforcement

#### Order CRUD Tests (8 tests)
- ? Order creation with customer authentication
- ? Stock validation integration
- ? Open access for order reading
- ? Business logic validation

#### API Health Tests (7 tests)
- ? Service availability validation
- ? Swagger documentation access
- ? Cross-service communication

## ?? Event Monitoring & Observability

### RabbitMQ Management

Access the RabbitMQ Management UI at: http://localhost:15672
- **Username**: admin
- **Password**: admin123

Monitor:
- Queue depths and message rates
- Message acknowledgments and failures
- Consumer performance metrics
- Exchange and routing statistics

### Event Logging

All event operations are logged with structured information:

```csharp
// Sales API Event Publishing
_logger.LogInformation(
    "Publishing event {EventType} with ID {EventId} and correlation ID {CorrelationId}",
    typeof(TEvent).Name,
    domainEvent.EventId,
    domainEvent.CorrelationId);

// Inventory API Event Consumption
_logger.LogInformation(
    "=== HANDLER CALLED === Processing OrderConfirmedEvent for Order {OrderId}",
    orderEvent.OrderId);
```

### Health Check Integration

Event-driven health monitoring:
- **RabbitMQ Connection**: Verified during service startup
- **Queue Health**: Monitored through management API
- **Event Processing**: Tracked through correlation IDs
- **Service Communication**: Validated in health endpoints

## ?? Event Configuration

### RabbitMQ Configuration

Both Sales and Inventory APIs include RabbitMQ configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SalesDb;...",
    "RabbitMQ": "amqp://admin:admin123@localhost:5672/"
  }
}
```

### Event Bus Configuration

```csharp
// Rebus Configuration for Event Publishing (Sales API)
builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "sales.api"))
    .Options(o => 
    {
        o.SetNumberOfWorkers(1);
        o.SetMaxParallelism(1);
    }));

// Rebus Configuration for Event Consumption (Inventory API)
builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "inventory.api"))
    .Options(o => 
    {
        o.SetNumberOfWorkers(1);
        o.SetMaxParallelism(1);
    }));
```

## ?? Event-Driven Data Models

### ProcessedEvent (Idempotency)
```csharp
public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
```

### StockDeduction (Audit Trail)
```csharp
public class StockDeduction
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityDebited { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
}
```

## ??? Event Security & Reliability

### Idempotency Guarantees
- **Event Deduplication**: ProcessedEvent table prevents duplicate processing
- **Unique Event IDs**: Each event has a unique identifier
- **Database Transactions**: All event processing happens within transactions

### Error Handling & Resilience
- **Retry Policies**: Automatic retry for failed message processing
- **Dead Letter Queues**: Failed messages are moved to error queues
- **Circuit Breaker**: Prevents cascade failures
- **Graceful Degradation**: System continues with reduced functionality

### Security Considerations
- **Message Encryption**: All inter-service communication is secure
- **Access Control**: RabbitMQ uses authentication and authorization
- **Audit Trail**: All events are logged with correlation IDs
- **Data Validation**: Events are validated before processing

## ?? Development Workflow with Events

### 1. Local Development Setup
```bash
# Start infrastructure
docker-compose -f docker-compose.infrastructure.yml up -d

# Start services
dotnet run --project src/inventory.api --urls "http://localhost:5000" &
dotnet run --project src/sales.api --urls "http://localhost:5001" &
dotnet run --project src/gateway --urls "http://localhost:6000" &
```

### 2. Event Testing Workflow
```bash
# Create test data
export ADMIN_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.accessToken')

# Create product with stock
curl -H "Authorization: Bearer $ADMIN_TOKEN" \
  "http://localhost:6000/inventory/products" -X POST \
  -H "Content-Type: application/json" \
  -d '{"name":"Event Test Product","description":"Test","price":10.99,"stockQuantity":100}'

# Test event-driven order processing
export CUSTOMER_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"customer1","password":"password123"}' | jq -r '.accessToken')

curl -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  "http://localhost:6000/sales/orders" -X POST \
  -H "Content-Type: application/json" \
  -d '{"customerId":"123e4567-e89b-12d3-a456-426614174000","items":[{"productId":"<product-id>","quantity":5}]}'