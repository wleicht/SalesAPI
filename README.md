# SalesAPI - Complete Microservices Architecture with Event-Driven Communication & Stock Reservations

A comprehensive microservices-based e-commerce solution built with .NET 8, featuring inventory management, sales processing with **stock reservations**, API Gateway with YARP reverse proxy, JWT authentication with role-based authorization, event-driven architecture with RabbitMQ, Saga pattern implementation, and comprehensive automated testing.

## ??? Architecture Overview

This solution implements a complete microservices architecture with event-driven communication and **advanced stock reservation system**:

- **API Gateway** - Unified entry point using YARP reverse proxy with JWT token issuer (Port 6000)
- **Inventory API** - Product catalog management with **stock reservations** and event consumption (Port 5000)
- **Sales API** - Order processing with **reservation-based workflow** and event publishing (Port 5001)
- **RabbitMQ Message Broker** - Event-driven communication between services (Port 5672)
- **Building Blocks Contracts** - Shared DTOs, events, and contracts between services
- **Building Blocks Events** - Domain events and messaging infrastructure
- **Automated Tests** - Comprehensive endpoint, integration, authentication, routing, event-driven, and **stock reservation testing**

## ?? New Feature: Stock Reservation System (Saga Pattern)

### **? Key Capabilities**

- **??? Overselling Prevention**: Atomic stock reservations prevent race conditions
- **?? Saga Pattern**: Distributed transaction management with compensation logic
- **? Synchronous Reservations**: Immediate stock allocation during order creation
- **?? Asynchronous Confirmation**: Event-driven reservation confirmation/release
- **?? Payment Simulation**: Realistic payment failure scenarios with automatic rollback
- **?? Audit Trail**: Complete reservation lifecycle tracking
- **?? Compensation Logic**: Automatic stock release for failed payments

### **Stock Reservation Workflow**

```
1. Customer creates order ? 2. Synchronous stock reservation
                          ?
5. Order confirmed/cancelled ? 3. Payment processing simulation
                          ?
                     4. Event publishing
                          ?
                  Asynchronous processing
                          ?
          Stock deduction OR reservation release
```

## ?? Project Structure

```
SalesAPI/
??? src/
?   ??? gateway/                    # API Gateway with YARP reverse proxy and JWT auth
?   ??? inventory.api/              # Inventory microservice with stock reservations ??
?   ?   ??? Controllers/
?   ?   ?   ??? StockReservationsController.cs  # ?? Reservation management
?   ?   ??? Models/
?   ?   ?   ??? StockReservation.cs             # ?? Reservation entity
?   ?   ??? EventHandlers/
?   ?       ??? OrderCancelledEventHandler.cs   # ?? Compensation logic
?   ??? sales.api/                  # Sales API with reservation integration ??
?   ?   ??? Controllers/
?   ?   ?   ??? OrdersController.cs             # ?? Enhanced with reservations
?   ?   ??? Services/
?   ?       ??? StockReservationClient.cs       # ?? Reservation HTTP client
?   ??? buildingblocks.contracts/   # Shared contracts with reservation DTOs ??
?   ??? buildingblocks.events/      # Domain events including OrderCancelledEvent ??
??? tests/
?   ??? endpoint.tests/             # Enhanced tests with reservation scenarios ??
?       ??? StockReservationTests.cs            # ?? Comprehensive reservation tests
?       ??? SimpleReservationTests.cs           # ?? Basic connectivity tests
??? deploy/
?   ??? docker-compose.infrastructure.yml       # RabbitMQ and SQL Server containers
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

## ?? Enhanced Event-Driven Architecture Flow with Stock Reservations

```
Client Request ? API Gateway (6000) ? JWT Authentication ? Sales API (5001)
                                    ?                         ?
                              Token Validation         Order Creation
                                    ?                         ?
                             Inventory API (5000)   ? ? StockReservationClient
                                    ?                         ?
                          Stock Reservation API        Stock Validation
                                    ?                         ?
                                Database           ? ? RabbitMQ (5672)
                                    ?                         ?
                             Reservation Created      Event Publishing
                                    ?                         ?
                             OrderConfirmedEvent  ? ? ? Payment Success
                                    ?                         ?
                             Event Consumption         OrderCancelledEvent
                                    ?                         ?
                          Stock Deduction/Release  ? ? Compensation Logic
```

### Enhanced Event Flow Details

1. **Order Creation**: Customer creates order via Sales API
2. **?? Stock Reservation**: Synchronous reservation creation with immediate validation
3. **Payment Simulation**: Realistic payment processing with failure scenarios
4. **Event Publishing**: 
   - `OrderConfirmedEvent` for successful payments
   - **?? `OrderCancelledEvent`** for payment failures (compensation)
5. **Event Consumption**: Inventory API processes events for stock operations
6. **?? Reservation Management**: Status transitions (Reserved ? Debited/Released)
7. **Idempotency**: All events processed idempotently with complete audit trail

## ?? Stock Reservation Components

### Stock Reservation Entity
```csharp
public class StockReservation
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? CorrelationId { get; set; }
    
    // ?? Enhanced audit fields
    public string? ProcessingNotes { get; set; }
    public string? CompensationReason { get; set; }
    public DateTime? CompensatedAt { get; set; }
}
```

### Reservation Status Lifecycle
```csharp
public enum ReservationStatus
{
    Reserved = 1,  // Stock allocated, pending confirmation
    Debited = 2,   // Stock deducted (order confirmed)
    Released = 3   // Stock released (order cancelled/failed)
}
```

### Stock Reservation API Endpoints

#### Inventory API (`/api/stockreservations`) ??
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/api/stockreservations` | Create stock reservations | ? Admin |
| `GET` | `/api/stockreservations/order/{orderId}` | Get reservations by order | ? Admin |
| `GET` | `/api/stockreservations/{reservationId}` | Get specific reservation | ? Admin |

### Stock Reservation Request/Response Models

#### StockReservationRequest ??
```csharp
public class StockReservationRequest
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; }
    public List<StockReservationItem> Items { get; set; }
}

public class StockReservationItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
```

#### StockReservationResponse ??
```csharp
public class StockReservationResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalItemsProcessed { get; set; }
    public List<StockReservationResult> ReservationResults { get; set; }
}

public class StockReservationResult
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableStock { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? ReservationId { get; set; }
}
```

## ?? Enhanced Event-Driven Components with Stock Reservations

### Domain Events

#### OrderConfirmedEvent
```csharp
public class OrderConfirmedEvent : DomainEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new();
    public string Status { get; set; }
    public DateTime OrderCreatedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
```

#### ?? OrderCancelledEvent (New)
```csharp
public class OrderCancelledEvent : DomainEvent
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new();
    public string CancellationReason { get; set; }  // Payment failure, etc.
    public string Status { get; set; }
    public DateTime OrderCreatedAt { get; set; }
    public DateTime CancelledAt { get; set; }
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

### Event Handlers

#### ?? OrderCancelledEventHandler (Compensation Logic)
```csharp
public class OrderCancelledEventHandler : IHandleMessages<OrderCancelledEvent>
{
    public async Task Handle(OrderCancelledEvent orderEvent)
    {
        // Find and release stock reservations for cancelled order
        var reservations = await _context.StockReservations
            .Where(r => r.OrderId == orderEvent.OrderId && r.Status == ReservationStatus.Reserved)
            .ToListAsync();

        foreach (var reservation in reservations)
        {
            reservation.Status = ReservationStatus.Released;
            reservation.ProcessedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Released {Count} stock reservations for cancelled order {OrderId}", 
            reservations.Count, orderEvent.OrderId);
    }
}
```

#### Enhanced OrderConfirmedEventHandler
```csharp
public class OrderConfirmedEventHandler : IHandleMessages<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent orderEvent)
    {
        // Process stock reservations and convert to debited status
        var reservations = await _context.StockReservations
            .Where(r => r.OrderId == orderEvent.OrderId && r.Status == ReservationStatus.Reserved)
            .ToListAsync();

        foreach (var reservation in reservations)
        {
            // Find product and debit stock
            var product = await _context.Products.FindAsync(reservation.ProductId);
            if (product != null)
            {
                product.StockQuantity -= reservation.Quantity;
                reservation.Status = ReservationStatus.Debited;
                reservation.ProcessedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        // Publish StockDebitedEvent for confirmation
    }
}
```

### Event Infrastructure

- **?? Stock Reservation Client**: HTTP client for synchronous reservation operations
- **?? Compensation Pattern**: Automatic rollback via OrderCancelledEvent
- **Event Publisher**: Rebus-based publisher with RabbitMQ transport
- **Event Handlers**: Automatic message consumption with retry policies
- **?? Reservation Tracking**: ProcessedEvent table enhanced for reservation operations
- **Correlation ID**: End-to-end traceability across reservation lifecycle
- **Transactional Outbox**: Ensures reliable event publishing with reservation consistency

## ?? Enhanced Quick Start with Stock Reservations

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

### 3. Apply Database Migrations (Including Stock Reservations)

```bash
# Apply database migrations for Inventory API (includes StockReservations table)
dotnet ef database update --project src/inventory.api

# Apply database migrations for Sales API
dotnet ef database update --project src/sales.api
```

### 4. Start All Microservices

```bash
# Terminal 1: Start Inventory API (with Stock Reservations)
dotnet run --project src/inventory.api --urls "http://localhost:5000"

# Terminal 2: Start Sales API (with Reservation Integration)
dotnet run --project src/sales.api --urls "http://localhost:5001"

# Terminal 3: Start API Gateway
dotnet run --project src/gateway --urls "http://localhost:6000"
```

### 5. Verify Stock Reservation System

```bash
# Check RabbitMQ Management UI
open http://localhost:15672
# Login: admin / admin123

# Check service health
curl http://localhost:6000/health
curl http://localhost:5000/health
curl http://localhost:5001/health

# ?? Test Stock Reservation API (requires admin token)
curl -H "Authorization: Bearer <admin-token>" \
  http://localhost:5000/api/stockreservations/order/00000000-0000-0000-0000-000000000000
```

### **Enhanced Protection Rules with Stock Reservations**
| Service | Operation | Access Level | Required Role |
|---------|-----------|--------------|---------------|
| **Inventory** | `GET /products` | Open Access | None |
| **Inventory** | `POST /products` | Protected | `admin` |
| **Inventory** | `PUT /products/{id}` | Protected | `admin` |
| **Inventory** | `DELETE /products/{id}` | Protected | `admin` |
| **?? Inventory** | `POST /api/stockreservations` | Protected | `admin` |
| **?? Inventory** | `GET /api/stockreservations/order/{id}` | Protected | `admin` |
| **?? Inventory** | `GET /api/stockreservations/{id}` | Protected | `admin` |
| **Sales** | `GET /orders` | Open Access | None |
| **Sales** | `POST /orders` | Protected | `customer` or `admin` |
| **Gateway** | `POST /auth/token` | Open Access | None |
| **All** | `/health` | Open Access | None |

## ?? Enhanced Event-Driven Workflow Example with Stock Reservations

### Complete Order Processing Flow with Reservations

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

# ?? 3. Create order (triggers stock reservation + event-driven processing)
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

# ?? 4. Check stock reservations (admin only)
curl -H "Authorization: Bearer <admin-token>" \
  "http://localhost:6000/inventory/api/stockreservations/order/<order-id>"

# 5. Verify stock was automatically debited after event processing
curl "http://localhost:6000/inventory/products/<product-id>"
# Stock should be reduced from 50 to 48

# ?? 6. Check reservation status (should be "Debited")
curl -H "Authorization: Bearer <admin-token>" \
  "http://localhost:6000/inventory/api/stockreservations/<reservation-id>"
```

### ?? Stock Reservation Workflow Example

```bash
# Test direct stock reservation (admin only)
curl -X POST "http://localhost:6000/inventory/api/stockreservations" \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "123e4567-e89b-12d3-a456-426614174001",
    "correlationId": "test-correlation-123",
    "items": [
      {
        "productId": "<product-id>",
        "quantity": 5
      }
    ]
  }'

# Expected Response:
{
  "success": true,
  "totalItemsProcessed": 1,
  "reservationResults": [
    {
      "productId": "<product-id>",
      "productName": "Gaming Laptop",
      "requestedQuantity": 5,
      "availableStock": 48,
      "success": true,
      "reservationId": "<reservation-id>"
    }
  ]
}
```

### ?? Payment Failure Simulation Example

```bash
# Create expensive order to trigger payment failure
curl -X POST "http://localhost:6000/sales/orders" \
  -H "Authorization: Bearer <customer-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "items": [
      {
        "productId": "<expensive-product-id>",
        "quantity": 1
      }
    ]
  }'

# Expected Response (422 Unprocessable Entity):
{
  "title": "Payment Processing Failed",
  "detail": "Payment could not be processed. Please check your payment information and try again.",
  "status": 422
}

# Check that reservations were released (admin only)
curl -H "Authorization: Bearer <admin-token>" \
  "http://localhost:6000/inventory/api/stockreservations/order/<failed-order-id>"
# Status should be "Released"
```

## ?? Enhanced Event Monitoring & Observability with Stock Reservations

### RabbitMQ Management

Access the RabbitMQ Management UI at: http://localhost:15672
- **Username**: admin
- **Password**: admin123

**?? Monitor Stock Reservation Events:**
- Queue depths for OrderConfirmedEvent and **OrderCancelledEvent**
- **Reservation event** message rates and processing times
- **Compensation event** acknowledgments and failures
- **Saga coordination** exchange and routing statistics
- **Event correlation** tracking across services

### ?? Stock Reservation Logging

All reservation operations are logged with structured information:

```csharp
// Sales API - Reservation Creation
_logger.LogInformation(
    "Creating stock reservations for Order {OrderId} with {ItemCount} items",
    reservationRequest.OrderId,
    reservationRequest.Items.Count);

// Inventory API - Reservation Processing
_logger.LogInformation(
    "Successfully created {Count} stock reservations for Order {OrderId}",
    reservationsToCreate.Count,
    request.OrderId);

// Sales API - Payment Failure (Compensation Trigger)
_logger.LogWarning(
    "Payment processing failed for Order {OrderId}, Amount: {Amount}",
    reservationRequest.OrderId,
    totalAmount);

// Inventory API - Compensation Processing
_logger.LogInformation(
    "Released {Count} stock reservations for cancelled order {OrderId}",
    reservations.Count,
    orderEvent.OrderId);
```

### ?? Stock Reservation Health Monitoring

Enhanced health monitoring includes reservation system status:

```csharp
// Reservation System Health Checks
public class ReservationHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context)
    {
        // Check reservation API availability
        // Validate RabbitMQ connection for events
        // Verify database connectivity for reservations
        // Test basic reservation workflow
    }
}
```

- **?? Reservation API Health**: Stock reservation endpoint availability
- **?? Event Processing Health**: RabbitMQ connection and event flow validation
- **?? Compensation Health**: OrderCancelledEvent processing capability
- **?? Database Health**: StockReservations table accessibility and performance

## ?? Enhanced Event Configuration with Stock Reservations

### ?? Stock Reservation Configuration

Enhanced RabbitMQ configuration for reservation events:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SalesDb;...",
    "RabbitMQ": "amqp://admin:admin123@localhost:5672/"
  },
  "StockReservation": {
    "ReservationTimeoutMinutes": 30,
    "MaxConcurrentReservations": 100,
    "PaymentSimulation": {
      "SmallAmountSuccessRate": 100,
      "MediumAmountSuccessRate": 95,
      "LargeAmountSuccessRate": 90
    }
  }
}
```

### ?? Enhanced Event Bus Configuration for Reservations

```csharp
// Sales API - Enhanced with Reservation Events
builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "sales.api"))
    .Routing(r => r
        .TypeBased()
        .Map<OrderConfirmedEvent>("inventory.api")
        .Map<OrderCancelledEvent>("inventory.api"))  // ?? Compensation routing
    .Options(o => 
    {
        o.SetNumberOfWorkers(2);  // ?? Increased for reservation load
        o.SetMaxParallelism(2);
        o.EnableIdempotence("sales.api.idempotency");  // ?? Reservation safety
    }));

// Inventory API - Enhanced with Compensation Handling
builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "inventory.api"))
    .Routing(r => r
        .TypeBased()
        .Map<StockDebitedEvent>("sales.api"))
    .Options(o => 
    {
        o.SetNumberOfWorkers(2);  // ?? Handle reservation + compensation
        o.SetMaxParallelism(2);
        o.EnableIdempotence("inventory.api.idempotency");  // ?? Event safety
    }));
```

## ?? Enhanced Event-Driven Data Models with Reservations

### ?? Enhanced ProcessedEvent (Reservation-Aware Idempotency)
```csharp
public class ProcessedEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? ProcessingDetails { get; set; }  // ?? Reservation details
    
    // ?? Reservation-specific tracking
    public int? ReservationCount { get; set; }
    public string? ReservationStatus { get; set; }
    public decimal? ReservationAmount { get; set; }
}
```

### ?? StockReservation (Complete Audit Entity)
```csharp
public class StockReservation
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? CorrelationId { get; set; }
    
    // ?? Enhanced audit fields
    public string? ProcessingNotes { get; set; }
    public string? CompensationReason { get; set; }
    public DateTime? CompensatedAt { get; set; }
}
```

### Enhanced StockDeduction (Reservation-Aware Audit)
```csharp
public class StockDeduction
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityDebited { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    
    // ?? Reservation tracking
    public Guid? ReservationId { get; set; }
    public string ReservationCorrelationId { get; set; } = string.Empty;
    public DateTime ReservationCreatedAt { get; set; }
    public TimeSpan ReservationDuration { get; set; }
}
```

## ?? Enhanced Event Security & Reliability with Reservations

### ?? Stock Reservation Idempotency Guarantees
- **?? Reservation Deduplication**: StockReservation table prevents duplicate reservations per order
- **?? Event Correlation**: Each reservation linked to unique correlation ID
- **Enhanced Event Deduplication**: ProcessedEvent table enhanced for reservation events
- **?? Compensation Idempotency**: OrderCancelledEvent processing safely repeatable
- **?? Database Transactions**: All reservation operations within ACID transactions

### ?? Enhanced Error Handling & Resilience for Reservations
- **?? Reservation Timeouts**: Automatic reservation expiry (configurable)
- **?? Payment Retry Logic**: Configurable retry attempts for payment failures
- **Enhanced Retry Policies**: Exponential backoff for reservation and compensation events
- **?? Reservation Dead Letter Queues**: Failed reservations moved to error queues
- **?? Compensation Circuit Breaker**: Prevents cascade failures in Saga workflows
- **?? Graceful Degradation**: System continues with reservation warnings

### ?? Stock Reservation Security Considerations
- **?? Reservation Authorization**: Admin-only access to reservation management APIs
- **?? Event Validation**: All reservation events validated before processing
- **Enhanced Message Encryption**: Reservation data encrypted in transit
- **?? Audit Compliance**: Complete reservation lifecycle logged for compliance
- **?? Data Privacy**: Customer information protected in reservation audit trails

## ?? Enhanced Development Workflow with Stock Reservations

### ?? Local Development Setup with Reservations
```bash
# Start infrastructure with reservation support
docker-compose -f docker-compose.infrastructure.yml up -d

# Apply enhanced migrations (includes StockReservations table)
dotnet ef database update --project src/inventory.api
dotnet ef database update --project src/sales.api

# Start services with reservation capabilities
dotnet run --project src/inventory.api --urls "http://localhost:5000" &
dotnet run --project src/sales.api --urls "http://localhost:5001" &
dotnet run --project src/gateway --urls "http://localhost:6000" &
```

### ?? Stock Reservation Testing Workflow
```bash
# Create test data with reservation support
export ADMIN_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.accessToken')

# Create product for reservation testing
curl -H "Authorization: Bearer $ADMIN_TOKEN" \
  "http://localhost:6000/inventory/products" -X POST \
  -H "Content-Type: application/json" \
  -d '{"name":"Reservation Test Product","description":"Test","price":99.99,"stockQuantity":100}'

# Test reservation-based order processing
export CUSTOMER_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"customer1","password":"password123"}' | jq -r '.accessToken')

# Create order with automatic reservation
curl -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  "http://localhost:6000/sales/orders" -X POST \
  -H "Content-Type: application/json" \
  -d '{"customerId":"123e4567-e89b-12d3-a456-426614174000","items":[{"productId":"<product-id>","quantity":5}]}'