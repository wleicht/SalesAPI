# Unit Tests Documentation

## Overview

This document provides detailed documentation for the unit test suites in the SalesAPI solution. Unit tests focus on testing individual components in isolation to ensure correct business logic implementation.

The SalesAPI follows a consolidated testing approach with the professional test suite located in `tests/SalesAPI.Tests.Professional/`.

## Test Suite Structure

### Current Test Architecture

The professional test suite is organized into three main categories:

```
SalesAPI.Tests.Professional/
??? Domain.Tests/            # 33 tests - Pure business logic
??? Infrastructure.Tests/    # 17 tests - Data access and messaging
??? Integration.Tests/       # 4 tests - Cross-service workflows
??? TestInfrastructure/      # Shared test components
```

## Domain Tests (33 tests)

### Purpose
Domain tests focus on pure business logic without external dependencies. They are the fastest tests in the suite and form the foundation of the test pyramid.

### Key Characteristics
- ? Fast execution (< 1ms per test)
- ?? No external dependencies
- ? Deterministic and reliable
- ?? Focus on business rules

### Test Categories

#### 1. Entity Validation Tests
**Purpose**: Validate domain model behavior and business rules.

**Example Test Cases**:

```csharp
[Fact]
public void Order_ShouldCalculateCorrectTotal_WithMultipleItems()
{
    // Arrange
    var order = OrderBuilder.Create();
    order.AddItem(ProductId.Create(), quantity: 2, unitPrice: 10.00m);
    order.AddItem(ProductId.Create(), quantity: 3, unitPrice: 15.00m);
    
    // Act
    var total = order.CalculateTotal();
    
    // Assert
    total.Should().Be(65.00m);
}
```

```csharp
[Theory]
[InlineData("Pending", "Confirmed")]
[InlineData("Confirmed", "Cancelled")]
[InlineData("Confirmed", "Delivered")]
public void Order_ShouldAllowValidStatusTransitions(string fromStatus, string toStatus)
{
    // Arrange
    var order = OrderBuilder.WithStatus(fromStatus).Build();
    
    // Act & Assert
    order.Invoking(o => o.UpdateStatus(toStatus))
          .Should().NotThrow();
}
```

#### 2. Value Object Tests
**Purpose**: Test value objects and their equality/validation rules.

**Example Test Cases**:

```csharp
[Fact]
public void ProductId_ShouldBeEqual_WhenSameGuidValue()
{
    // Arrange
    var guid = Guid.NewGuid();
    var productId1 = ProductId.From(guid);
    var productId2 = ProductId.From(guid);
    
    // Act & Assert
    productId1.Should().Be(productId2);
}
```

#### 3. Business Rule Tests
**Purpose**: Validate domain-specific business logic and constraints.

**Example Test Cases**:

```csharp
[Fact]
public void StockReservation_ShouldThrowException_WhenQuantityExceedsAvailable()
{
    // Arrange
    var product = ProductBuilder.WithStock(10).Build();
    
    // Act & Assert
    product.Invoking(p => p.ReserveStock(15))
           .Should().Throw<InsufficientStockException>();
}
```

## Infrastructure Tests (17 tests)

### Purpose
Infrastructure tests validate components that interact with external systems like databases and message brokers, using in-memory or fake implementations.

### Key Characteristics
- ?? Persistence with in-memory databases
- ?? Messaging with fake implementations
- ?? Transaction and concurrency testing
- ? Performance and bulk operations

### Test Categories

#### 1. Repository Tests
**Purpose**: Test data access layer functionality.

**Example Test Cases**:

```csharp
[Fact]
public async Task ProductRepository_ShouldPersistProduct_WhenValidProduct()
{
    // Arrange
    using var context = TestDatabaseFactory.CreateContext();
    var repository = new ProductRepository(context);
    var product = ProductBuilder.Create();
    
    // Act
    await repository.AddAsync(product);
    await context.SaveChangesAsync();
    
    // Assert
    var savedProduct = await repository.GetByIdAsync(product.Id);
    savedProduct.Should().NotBeNull();
    savedProduct.Name.Should().Be(product.Name);
}
```

#### 2. Event Publishing Tests
**Purpose**: Test event publishing and messaging infrastructure.

**Example Test Cases**:

```csharp
[Fact]
public async Task EventPublisher_ShouldPublishEvent_WhenValidEvent()
{
    // Arrange
    var fakeEventBus = new FakeEventBus();
    var publisher = new EventPublisher(fakeEventBus);
    var orderEvent = new OrderConfirmedEvent(OrderId.Create());
    
    // Act
    await publisher.PublishAsync(orderEvent);
    
    // Assert
    fakeEventBus.PublishedEvents.Should().ContainSingle();
    fakeEventBus.PublishedEvents.First().Should().BeOfType<OrderConfirmedEvent>();
}
```

#### 3. Database Integration Tests
**Purpose**: Test Entity Framework context operations and database behavior.

**Example Test Cases**:

```csharp
[Fact]
public async Task DbContext_ShouldHandleConcurrency_WhenMultipleUpdates()
{
    // Arrange
    using var context1 = TestDatabaseFactory.CreateContext();
    using var context2 = TestDatabaseFactory.CreateContext();
    
    var product = ProductBuilder.Create();
    context1.Products.Add(product);
    await context1.SaveChangesAsync();
    
    // Act
    var product1 = await context1.Products.FindAsync(product.Id);
    var product2 = await context2.Products.FindAsync(product.Id);
    
    product1.UpdateStock(100);
    product2.UpdateStock(200);
    
    await context1.SaveChangesAsync();
    
    // Assert
    await context2.Invoking(c => c.SaveChangesAsync())
                  .Should().ThrowAsync<DbUpdateConcurrencyException>();
}
```

## Integration Tests (4 tests)

### Purpose
Integration tests validate complete workflows that span multiple services or components.

### Key Characteristics
- ?? Cross-service integration
- ?? Complete business workflows
- ?? Event processing validation
- ? End-to-end scenario testing

### Test Categories

#### 1. Cross-Service Workflow Tests
**Purpose**: Test complete business processes that involve multiple services.

**Example Test Cases**:

```csharp
[Fact]
public async Task OrderProcessing_ShouldReserveStock_WhenOrderCreated()
{
    // Arrange
    var testFixture = new IntegrationTestFixture();
    var orderService = testFixture.GetService<IOrderService>();
    var inventoryService = testFixture.GetService<IInventoryService>();
    
    var product = await inventoryService.CreateProductAsync(
        ProductBuilder.WithStock(10).Build());
    
    // Act
    var order = await orderService.CreateOrderAsync(new CreateOrderRequest
    {
        CustomerId = CustomerId.Create(),
        Items = new[] { new OrderItemRequest(product.Id, 2) }
    });
    
    // Assert
    order.Should().NotBeNull();
    var updatedProduct = await inventoryService.GetProductAsync(product.Id);
    updatedProduct.AvailableStock.Should().Be(8);
}
```

#### 2. Event-Driven Workflow Tests
**Purpose**: Test event processing across service boundaries.

**Example Test Cases**:

```csharp
[Fact]
public async Task EventProcessing_ShouldUpdateInventory_WhenOrderCancelled()
{
    // Arrange
    var testFixture = new IntegrationTestFixture();
    var eventBus = testFixture.GetService<IEventBus>();
    var inventoryService = testFixture.GetService<IInventoryService>();
    
    // Setup initial state
    var productId = ProductId.Create();
    await CreateProductWithReservation(productId, reservedQuantity: 5);
    
    // Act
    await eventBus.PublishAsync(new OrderCancelledEvent(
        OrderId.Create(), 
        productId, 
        quantity: 5));
    
    await testFixture.WaitForEventProcessing();
    
    // Assert
    var product = await inventoryService.GetProductAsync(productId);
    product.ReservedStock.Should().Be(0);
}
```

## Test Infrastructure

### TestDatabaseFactory
Provides in-memory database contexts for testing:

```csharp
public static class TestDatabaseFactory
{
    public static SalesDbContext CreateSalesContext()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
        return new SalesDbContext(options);
    }
    
    public static InventoryDbContext CreateInventoryContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
            
        return new InventoryDbContext(options);
    }
}
```

### Test Data Builders
Builder pattern for creating test data:

```csharp
public class OrderBuilder
{
    private Order _order = new Order();
    
    public static OrderBuilder Create() => new OrderBuilder();
    
    public OrderBuilder WithCustomer(CustomerId customerId)
    {
        _order.CustomerId = customerId;
        return this;
    }
    
    public OrderBuilder WithItem(ProductId productId, int quantity, decimal unitPrice)
    {
        _order.Items.Add(new OrderItem(productId, quantity, unitPrice));
        return this;
    }
    
    public Order Build() => _order;
}
```

### Fake Implementations
Test doubles for external dependencies:

```csharp
public class FakeEventBus : IEventBus
{
    public List<IEvent> PublishedEvents { get; } = new();
    
    public Task PublishAsync<T>(T eventData) where T : IEvent
    {
        PublishedEvents.Add(eventData);
        return Task.CompletedTask;
    }
    
    public void ClearPublishedEvents() => PublishedEvents.Clear();
}
```

## Test Execution

### Running Tests by Category

```bash
# Domain tests (fastest)
dotnet test tests/SalesAPI.Tests.Professional/Domain.Tests/

# Infrastructure tests
dotnet test tests/SalesAPI.Tests.Professional/Infrastructure.Tests/

# Integration tests
dotnet test tests/SalesAPI.Tests.Professional/Integration.Tests/

# All professional tests
dotnet test tests/SalesAPI.Tests.Professional/
```

### Test Configuration
Tests use configuration from `testsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "InMemory",
    "TestDatabase": "InMemory"
  },
  "Messaging": {
    "Enabled": false,
    "UseFakeImplementation": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning"
    }
  }
}
```

## Naming Conventions

### Test Method Naming
**Pattern**: `[MethodUnderTest]_Should[ExpectedBehavior]_When[Condition]`

**Examples**:
- `CreateOrder_ShouldCalculateTotal_WhenValidItems`
- `UpdateStock_ShouldThrowException_WhenQuantityIsNegative`
- `PublishEvent_ShouldAddToQueue_WhenEventIsValid`

### Test Class Naming
**Pattern**: `[ClassUnderTest]Tests`

**Examples**:
- `OrderTests`
- `ProductRepositoryTests`
- `EventPublisherTests`

## Assertion Guidelines

### Use FluentAssertions
```csharp
// Good
result.Should().NotBeNull("because the order should be created successfully");
result.TotalAmount.Should().Be(100.00m, "because the total should match the item prices");
result.Items.Should().HaveCount(2, "because two items were added to the order");

// Avoid
Assert.NotNull(result);
Assert.Equal(100.00m, result.TotalAmount);
Assert.Equal(2, result.Items.Count);
```

### Test Single Concerns
```csharp
// Good - focused test
[Fact]
public void Order_ShouldCalculateTotal_WhenItemsAdded()
{
    var order = OrderBuilder.Create()
        .WithItem(ProductId.Create(), 2, 10.00m)
        .Build();
        
    var total = order.CalculateTotal();
    
    total.Should().Be(20.00m);
}

// Avoid - testing multiple concerns
[Fact]
public void Order_ShouldValidateAndCalculateTotal()
{
    // Testing both validation and calculation
}
```

## Performance Considerations

### Fast Test Execution
- Use in-memory databases for infrastructure tests
- Minimize test setup and teardown overhead
- Use builders for efficient test data creation
- Run tests in parallel where possible

### Resource Management
```csharp
[Fact]
public async Task TestMethod()
{
    // Use using statements for proper disposal
    using var context = TestDatabaseFactory.CreateContext();
    
    // Test logic here
    
    // Context automatically disposed
}
```

## Quality Metrics

### Current Test Performance
| Category | Tests | Avg. Execution Time | Pass Rate |
|----------|-------|-------------------|-----------|
| Domain Tests | 33 | ~2.9s | 100% |
| Infrastructure Tests | 17 | ~2.6s | 100% |
| Integration Tests | 4 | ~2.8s | 100% |
| **Total** | **54** | **~8.3s** | **100%** |

### Coverage Targets
- **Domain Logic Coverage**: 100% (achieved)
- **Infrastructure Coverage**: >95% (achieved)
- **Integration Scenario Coverage**: 100% (achieved)

---

*Last Updated: December 2024*  
*Version: 2.0.0 - Consolidated Professional Structure*