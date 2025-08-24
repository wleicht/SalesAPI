# Event-Driven Architecture Implementation Guide

This document provides a comprehensive overview of the event-driven architecture implementation in the SalesAPI microservices solution, covering the technical implementation, design patterns, and operational aspects.

## ??? Architecture Overview

The SalesAPI implements a robust event-driven architecture using RabbitMQ as the message broker and Rebus as the .NET messaging framework. This architecture enables loose coupling between services while maintaining data consistency and reliability.

### Core Components

```
???????????????????    ???????????????????    ???????????????????
?   Sales API     ??????   RabbitMQ      ?????? Inventory API   ?
?  (Publisher)    ?    ? Message Broker  ?    ?  (Consumer)     ?
?                 ?    ?                 ?    ?                 ?
? OrderConfirmed  ??????   Exchanges     ?????? StockDeduction  ?
?    Event        ?    ?   Queues        ?    ?    Handler      ?
?                 ?    ?   Routing       ?    ?                 ?
???????????????????    ???????????????????    ???????????????????
         ?                       ?                       ?
         ?                       ?                       ?
         ?               ???????????????????    ???????????????????
         ?               ?   Dead Letter   ?    ? StockDebited    ?
         ?????????????????     Queues      ?    ?     Event       ?
                         ?                 ?    ?   (Response)    ?
                         ???????????????????    ???????????????????
```

## ?? Technical Implementation

### Event Framework: Rebus

The implementation uses **Rebus** instead of MassTransit for several advantages:
- **Simpler Configuration**: Less boilerplate code
- **Lightweight**: Smaller memory footprint
- **Flexible Routing**: Easier message routing configuration
- **Better Error Handling**: More intuitive retry policies

### Message Broker: RabbitMQ

RabbitMQ provides:
- **Persistence**: Durable queues and messages
- **Reliability**: Guaranteed message delivery
- **Scalability**: High-throughput message processing
- **Management**: Web-based administration interface

## ?? Domain Events

### Base Event Structure

All domain events inherit from the base `DomainEvent` class:

```csharp
/// <summary>
/// Base class for all domain events in the system.
/// Provides common properties for event identification, timing, and correlation.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// Used for deduplication and idempotency checks.
    /// </summary>
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the event occurred.
    /// Set automatically to UTC time when event is created.
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation identifier for tracking events across service boundaries.
    /// Enables end-to-end tracing of related operations.
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
```

### OrderConfirmedEvent

Published when an order is successfully created and confirmed:

```csharp
/// <summary>
/// Domain event published when an order has been confirmed and requires inventory processing.
/// Triggers automatic stock deduction in the Inventory service.
/// </summary>
public class OrderConfirmedEvent : DomainEvent
{
    /// <summary>
    /// Unique identifier of the confirmed order.
    /// References the Order entity in the Sales database.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Collection of items in the order that require stock deduction.
    /// Each item contains product information and requested quantity.
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();

    /// <summary>
    /// Correlation identifier inherited from the originating request.
    /// Enables tracing from initial order creation through stock processing.
    /// </summary>
    public new string CorrelationId { get; set; } = string.Empty;
}
```

### StockDebitedEvent

Published in response to stock deduction operations:

```csharp
/// <summary>
/// Domain event published when stock deduction has been processed for an order.
/// Contains the results of inventory operations and any error information.
/// </summary>
public class StockDebitedEvent : DomainEvent
{
    /// <summary>
    /// Identifier of the order for which stock was processed.
    /// Links back to the original OrderConfirmedEvent.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Detailed information about each stock deduction performed.
    /// Includes previous stock, debited quantity, and new stock levels.
    /// </summary>
    public List<StockDeduction> StockDeductions { get; set; } = new();

    /// <summary>
    /// Indicates whether all requested stock deductions were successful.
    /// False if any product had insufficient stock or processing errors.
    /// </summary>
    public bool AllDeductionsSuccessful { get; set; }

    /// <summary>
    /// Human-readable error message if deductions failed.
    /// Null or empty if all deductions were successful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Correlation identifier from the original order request.
    /// Maintains traceability through the complete order fulfillment process.
    /// </summary>
    public new string CorrelationId { get; set; } = string.Empty;
}
```

## ?? Event Publishing

### Event Publisher Interface

The `IEventPublisher` interface provides a contract for event publishing:

```csharp
/// <summary>
/// Defines the contract for publishing domain events to the message bus.
/// Supports both single event and batch event publishing operations.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single domain event to the message bus.
    /// </summary>
    /// <typeparam name="TEvent">Type of domain event to publish</typeparam>
    /// <param name="domainEvent">The event instance to publish</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the asynchronous publish operation</returns>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
        where TEvent : DomainEvent;

    /// <summary>
    /// Publishes multiple domain events to the message bus in sequence.
    /// </summary>
    /// <typeparam name="TEvent">Type of domain events to publish</typeparam>
    /// <param name="domainEvents">Collection of event instances to publish</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task representing the asynchronous publish operations</returns>
    Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
        where TEvent : DomainEvent;
}
```

### Event Publisher Implementation (Sales API)

```csharp
/// <summary>
/// Event publisher implementation using Rebus for RabbitMQ integration.
/// Publishes domain events to the message bus for consumption by other services.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IBus _bus;
    private readonly ILogger<EventPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the EventPublisher.
    /// </summary>
    /// <param name="bus">Rebus bus instance for message publishing</param>
    /// <param name="logger">Logger for tracking event publishing operations</param>
    public EventPublisher(IBus bus, ILogger<EventPublisher> logger)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes a domain event to the message bus using Rebus.
    /// Uses directed routing for OrderConfirmedEvent to ensure reliable delivery.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
        where TEvent : DomainEvent
    {
        try
        {
            _logger.LogInformation(
                "Publishing event {EventType} with ID {EventId} and correlation ID {CorrelationId}",
                typeof(TEvent).Name,
                domainEvent.EventId,
                domainEvent.CorrelationId);

            // Use Send instead of Publish for directed messaging to specific queue
            if (domainEvent is OrderConfirmedEvent)
            {
                await _bus.Advanced.Routing.Send("inventory.api", domainEvent);
            }
            else
            {
                await _bus.Publish(domainEvent);
            }

            _logger.LogInformation(
                "Successfully published event {EventType} with ID {EventId}",
                typeof(TEvent).Name,
                domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event {EventType} with ID {EventId}",
                typeof(TEvent).Name,
                domainEvent.EventId);
            throw;
        }
    }
}
```

## ?? Event Consumption

### Event Handler Implementation (Inventory API)

```csharp
/// <summary>
/// Handles OrderConfirmedEvent to debit stock quantities from inventory.
/// Implements idempotency to prevent duplicate processing and ensures data consistency.
/// </summary>
public class OrderConfirmedEventHandler : IHandleMessages<OrderConfirmedEvent>
{
    private readonly InventoryDbContext _dbContext;
    private readonly IBus _bus;
    private readonly ILogger<OrderConfirmedEventHandler> _logger;

    public OrderConfirmedEventHandler(
        InventoryDbContext dbContext,
        IBus bus,
        ILogger<OrderConfirmedEventHandler> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes OrderConfirmedEvent with full transaction support and idempotency.
    /// Debits stock quantities and publishes response events.
    /// </summary>
    public async Task Handle(OrderConfirmedEvent orderEvent)
    {
        _logger.LogInformation(
            "=== HANDLER CALLED === Processing OrderConfirmedEvent for Order {OrderId}",
            orderEvent.OrderId);

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Check for idempotency - prevent duplicate processing
            var existingProcessedEvent = await _dbContext.ProcessedEvents
                .FirstOrDefaultAsync(pe => pe.EventId == orderEvent.EventId);

            if (existingProcessedEvent != null)
            {
                _logger.LogWarning(
                    "Event {EventId} for Order {OrderId} has already been processed. Skipping to ensure idempotency.",
                    orderEvent.EventId,
                    orderEvent.OrderId);
                return;
            }

            var stockDeductions = new List<StockDeduction>();
            bool allDeductionsSuccessful = true;
            string? errorMessage = null;

            // Process each item in the order
            foreach (var item in orderEvent.Items)
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null)
                {
                    allDeductionsSuccessful = false;
                    errorMessage = $"Product {item.ProductId} not found";
                    continue;
                }

                // Validate sufficient stock
                if (product.StockQuantity < item.Quantity)
                {
                    allDeductionsSuccessful = false;
                    errorMessage = $"Insufficient stock for product {item.ProductName}";
                    continue;
                }

                // Record stock deduction for audit trail
                var stockDeduction = new StockDeduction
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    QuantityDebited = item.Quantity,
                    PreviousStock = product.StockQuantity,
                    NewStock = product.StockQuantity - item.Quantity
                };

                // Apply stock deduction
                product.StockQuantity -= item.Quantity;
                stockDeductions.Add(stockDeduction);
            }

            // Mark event as processed for idempotency
            var processedEvent = new ProcessedEvent
            {
                EventId = orderEvent.EventId,
                EventType = nameof(OrderConfirmedEvent),
                OrderId = orderEvent.OrderId,
                ProcessedAt = DateTime.UtcNow,
                CorrelationId = orderEvent.CorrelationId
            };

            _dbContext.ProcessedEvents.Add(processedEvent);
            await _dbContext.SaveChangesAsync();

            // Publish response event
            var stockDebitedEvent = new StockDebitedEvent
            {
                OrderId = orderEvent.OrderId,
                StockDeductions = stockDeductions,
                AllDeductionsSuccessful = allDeductionsSuccessful,
                ErrorMessage = errorMessage,
                CorrelationId = orderEvent.CorrelationId
            };

            await _bus.Advanced.Routing.Send("sales.api", stockDebitedEvent);

            // Commit transaction
            await transaction.CommitAsync();

            _logger.LogInformation(
                "=== HANDLER COMPLETED === Successfully processed OrderConfirmedEvent for Order {OrderId}",
                orderEvent.OrderId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "=== HANDLER FAILED === Failed to process OrderConfirmedEvent for Order {OrderId}",
                orderEvent.OrderId);
            throw; // Trigger retry mechanism
        }
    }
}
```

## ?? Idempotency Implementation

### ProcessedEvent Entity

Ensures events are processed exactly once:

```csharp
/// <summary>
/// Entity representing a processed event for idempotency control.
/// Prevents duplicate processing of the same event across system restarts.
/// </summary>
public class ProcessedEvent
{
    /// <summary>
    /// Unique identifier of the processed event.
    /// Maps to the EventId from the original domain event.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Type name of the processed event for categorization.
    /// Examples: "OrderConfirmedEvent", "StockDebitedEvent"
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Order identifier associated with the event.
    /// Enables querying processed events by business entity.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Timestamp when the event was successfully processed.
    /// Used for audit trails and troubleshooting.
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Correlation identifier from the original event.
    /// Maintains traceability across the entire request flow.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
}
```

### Idempotency Pattern

```csharp
// Check if event was already processed
var existingProcessedEvent = await _dbContext.ProcessedEvents
    .FirstOrDefaultAsync(pe => pe.EventId == orderEvent.EventId);

if (existingProcessedEvent != null)
{
    _logger.LogWarning("Event {EventId} already processed. Skipping.", orderEvent.EventId);
    return; // Exit without processing
}

// Process event and mark as completed
var processedEvent = new ProcessedEvent
{
    EventId = orderEvent.EventId,
    EventType = nameof(OrderConfirmedEvent),
    OrderId = orderEvent.OrderId,
    ProcessedAt = DateTime.UtcNow,
    CorrelationId = orderEvent.CorrelationId
};

_dbContext.ProcessedEvents.Add(processedEvent);
await _dbContext.SaveChangesAsync();
```

## ?? Configuration

### Rebus Configuration (Sales API)

```csharp
/// <summary>
/// Configures Rebus message bus for event publishing in Sales API.
/// Uses RabbitMQ transport with optimized settings for reliability.
/// </summary>
builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "sales.api"))
    .Options(o => 
    {
        o.SetNumberOfWorkers(1);     // Single worker for ordered processing
        o.SetMaxParallelism(1);      // Sequential message processing
    }));
```

### Rebus Configuration (Inventory API)

```csharp
/// <summary>
/// Configures Rebus message bus for event consumption in Inventory API.
/// Registers event handlers and sets up message processing pipeline.
/// </summary>
builder.Services.AddRebus(configure => configure
    .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "inventory.api"))
    .Options(o => 
    {
        o.SetNumberOfWorkers(1);     // Single worker for consistency
        o.SetMaxParallelism(1);      // Avoid race conditions
    }));

// Register event handlers automatically
builder.Services.AutoRegisterHandlersFromAssemblyOf<OrderConfirmedEventHandler>();
```

### RabbitMQ Connection Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InventoryDb;...",
    "RabbitMQ": "amqp://admin:admin123@localhost:5672/"
  }
}
```

## ?? Message Flow Patterns

### Request-Response Pattern

1. **Request**: Sales API publishes `OrderConfirmedEvent`
2. **Processing**: Inventory API processes stock deduction
3. **Response**: Inventory API publishes `StockDebitedEvent`
4. **Completion**: Sales API receives confirmation

### Event Sourcing Pattern

All events are stored for audit and replay capabilities:

```csharp
/// <summary>
/// Audit trail entity for tracking stock changes.
/// Provides complete history of inventory operations.
/// </summary>
public class StockDeduction
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityDebited { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
}
```

## ??? Error Handling & Resilience

### Retry Policies

Rebus provides automatic retry mechanisms:

```csharp
// Retry configuration in message endpoint setup
e.UseMessageRetry(r => r.Immediate(3)); // Retry 3 times immediately
```

### Dead Letter Handling

Failed messages are moved to dead letter queues for manual intervention:

```csharp
// Messages that fail after all retries are sent to dead letter queue
// Can be reprocessed or analyzed for systematic issues
```

### Circuit Breaker Pattern

Prevents cascade failures when downstream services are unavailable:

```csharp
// Graceful degradation when event publishing fails
try
{
    await _bus.Advanced.Routing.Send("inventory.api", domainEvent);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Event publishing failed, order saved but stock not debited");
    // Order is still saved, can be processed later
}
```

## ?? Monitoring & Observability

### Correlation ID Tracking

Every event includes a correlation ID for end-to-end tracing:

```csharp
// Generate correlation ID at API boundary
var correlationId = HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString();

// Propagate through events
var orderEvent = new OrderConfirmedEvent
{
    OrderId = order.Id,
    Items = orderItems,
    CorrelationId = correlationId
};
```

### Structured Logging

All event operations use structured logging:

```csharp
_logger.LogInformation(
    "Processing OrderConfirmedEvent for Order {OrderId} with {ItemCount} items. CorrelationId: {CorrelationId}",
    orderEvent.OrderId,
    orderEvent.Items.Count,
    orderEvent.CorrelationId);
```

### RabbitMQ Management

Monitor message flow through RabbitMQ Management UI:
- **URL**: http://localhost:15672
- **Credentials**: admin / admin123
- **Metrics**: Queue depths, message rates, error rates

## ?? Testing Strategies

### Integration Testing

Event-driven tests validate end-to-end flows:

```csharp
[Fact]
public async Task CreateOrder_ShouldPublishEventAndDebitStock()
{
    // Arrange - Create product with stock
    var product = await CreateTestProduct(stockQuantity: 100);
    
    // Act - Create order (triggers event)
    var order = await CreateTestOrder(product.Id, quantity: 5);
    
    // Wait for asynchronous event processing
    await Task.Delay(3000);
    
    // Assert - Stock was automatically debited
    var updatedProduct = await GetProduct(product.Id);
    Assert.Equal(95, updatedProduct.StockQuantity);
}
```

### Event Isolation

Each test creates isolated test data:

```csharp
protected async Task<ProductDto> CreateTestProduct(
    string name = null,
    decimal price = 99.99m,
    int stockQuantity = 100)
{
    name ??= $"Test Product {Guid.NewGuid()}";
    // Creates unique test data to avoid interference
}
```

### Correlation Verification

Tests verify correlation ID propagation:

```csharp
// Verify correlation ID is maintained across service boundaries
Assert.Equal(originalCorrelationId, processedEvent.CorrelationId);
```

## ?? Deployment Considerations

### Infrastructure Requirements

```yaml
# docker-compose.infrastructure.yml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    ports:
      - "5672:5672"      # AMQP port
      - "15672:15672"    # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin123
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 30s
      timeout: 30s
      retries: 3
```

### Scaling Considerations

1. **Horizontal Scaling**: Multiple service instances can consume from same queue
2. **Queue Partitioning**: Separate queues for different event types
3. **Message Persistence**: Durable queues survive broker restarts
4. **Load Balancing**: RabbitMQ distributes messages across consumers

### Production Checklist

- [ ] **Message Persistence**: Configure durable queues and messages
- [ ] **Monitoring**: Set up RabbitMQ and application monitoring
- [ ] **Backup Strategy**: Regular backup of message broker state
- [ ] **Security**: Use TLS for message transport in production
- [ ] **Resource Limits**: Configure memory and disk usage limits
- [ ] **Dead Letter Queues**: Set up DLQ for failed message handling
- [ ] **Alerting**: Configure alerts for queue depth and processing failures

## ?? Best Practices

### Event Design

1. **Immutable Events**: Events should never be modified after creation
2. **Versioning**: Plan for event schema evolution
3. **Size Limits**: Keep events small and focused
4. **Business Events**: Model events around business concepts

### Processing Patterns

1. **Idempotency**: Always check for duplicate processing
2. **Transactions**: Use database transactions for consistency
3. **Compensation**: Implement compensating actions for failures
4. **Timeouts**: Set reasonable processing timeouts

### Monitoring

1. **Correlation IDs**: Track requests across service boundaries
2. **Structured Logging**: Use consistent log formats
3. **Metrics**: Monitor queue depths and processing times
4. **Health Checks**: Include message broker in health checks

### Security

1. **Authentication**: Secure message broker access
2. **Authorization**: Control which services can publish/consume
3. **Encryption**: Encrypt sensitive data in messages
4. **Audit Trail**: Log all event processing operations

This event-driven architecture provides a robust foundation for scalable, maintainable microservices that can evolve independently while maintaining data consistency and system reliability.