# Contract Tests Documentation

## Overview

This document provides detailed documentation for the contract test suite in the SalesAPI solution. Contract tests ensure API compatibility and data consistency across service boundaries, preventing breaking changes in distributed systems.

## Purpose and Benefits

### Contract Testing Principles
- **API Compatibility**: Ensures service interfaces remain compatible across versions
- **Data Structure Validation**: Validates DTO consistency between services
- **Event Schema Compliance**: Ensures event payloads maintain expected structure
- **Breaking Change Prevention**: Detects incompatible changes before deployment

### Benefits
- **Early Detection**: Identify breaking changes during development
- **Service Independence**: Services can evolve while maintaining compatibility
- **Documentation**: Living documentation of service contracts
- **Confidence**: Deploy with confidence knowing contracts are maintained

## Test Suite (`contracts.tests`)

### ContractCompatibilityTests.cs

**Purpose**: Validates the consistency and compatibility of contracts between Sales and Inventory APIs.

#### Test Categories
- **DTO Structure Validation**: Tests data transfer object schemas
- **Event Contract Validation**: Validates domain event structures
- **Cross-Service Compatibility**: Tests service interface compatibility
- **Data Flow Validation**: Ensures data consistency across service boundaries

### 1. Product Contract Tests

#### ProductDto Structure Validation

```csharp
[Fact]
public void ProductDto_ShouldHaveConsistentStructure()
```
- **Purpose**: Validates ProductDto maintains expected structure
- **Contract Elements**:
  - Required properties (Id, Name, Description, Price, StockQuantity, CreatedAt)
  - Property types and constraints
  - Default values and initialization
- **Test Data**: Complete ProductDto instance with all properties
- **Assertions**:
  - All required properties exist and have correct types
  - Property validation attributes work correctly
  - Timestamp properties use UTC format
  - Numeric properties accept valid ranges

**Validation Rules Tested**:
```csharp
// Validates essential product properties
product.Id.Should().NotBeEmpty();
product.Name.Should().NotBeNullOrEmpty();
product.Description.Should().NotBeNullOrEmpty();
product.Price.Should().BeGreaterOrEqualTo(0);
product.StockQuantity.Should().BeGreaterOrEqualTo(0);
product.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
```

#### CreateProductDto Validation

```csharp
[Fact]
public void CreateProductDto_ShouldValidateRequiredFields()
```
- **Purpose**: Ensures product creation contract enforces required fields
- **Contract Elements**:
  - Required field validation
  - Data format requirements
  - Business rule constraints
- **Test Data**: CreateProductDto with valid data
- **Assertions**:
  - Name field is required and not empty
  - Description field is required
  - Price must be non-negative
  - Stock quantity must be non-negative

**Field Validation Tests**:
```csharp
// Tests various field validation scenarios
createProduct.Name.Should().NotBeNullOrEmpty();
createProduct.Description.Should().NotBeNullOrEmpty();
createProduct.Price.Should().BeGreaterOrEqualTo(0);
createProduct.StockQuantity.Should().BeGreaterOrEqualTo(0);
```

### 2. Order Contract Tests

#### OrderDto Structure Validation

```csharp
[Fact]
public void OrderDto_ShouldHaveCompatibleStructure()
```
- **Purpose**: Validates OrderDto structure for Sales API compatibility
- **Contract Elements**:
  - Order header properties (Id, CustomerId, Status, TotalAmount, CreatedAt)
  - Order items collection structure
  - Calculated properties (TotalAmount)
  - Status enumeration values
- **Test Data**: Complete order with multiple items
- **Assertions**:
  - All required order properties exist
  - Order items collection is properly structured
  - Total amount calculation is accurate
  - Status values are valid
  - Timestamps are properly formatted

**Order Structure Validation**:
```csharp
// Validates complete order structure
order.Id.Should().NotBeEmpty();
order.CustomerId.Should().NotBeEmpty();
order.Status.Should().NotBeNullOrEmpty();
order.TotalAmount.Should().BeGreaterOrEqualTo(0);
order.Items.Should().NotBeNull();
order.Items.Should().HaveCount(1);
```

#### OrderItemDto Validation

```csharp
[Fact]
public void OrderItemDto_ShouldMaintainStructuralIntegrity()
```
- **Purpose**: Validates order item structure and calculations
- **Contract Elements**:
  - Item identification (OrderId, ProductId)
  - Product information (ProductName)
  - Quantity and pricing (Quantity, UnitPrice, TotalPrice)
  - Calculated fields validation
- **Test Data**: OrderItemDto with complete data
- **Assertions**:
  - All required item properties exist
  - Price calculations are accurate
  - Product references are valid
  - Quantity constraints are enforced

### 3. Event Contract Tests

#### OrderConfirmedEvent Structure

```csharp
[Fact]
public void OrderConfirmedEvent_ShouldMatchExpectedStructure()
```
- **Purpose**: Validates OrderConfirmedEvent schema for cross-service communication
- **Contract Elements**:
  - Base event properties (EventId, OccurredAt, CorrelationId)
  - Order-specific properties (OrderId, CustomerId, TotalAmount)
  - Order items collection
  - Status and timestamp information
- **Test Data**: Complete OrderConfirmedEvent instance
- **Assertions**:
  - Event inherits from DomainEvent properly
  - All required order information is present
  - Order items are structured correctly
  - Correlation ID is included for tracing

**Event Structure Validation**:
```csharp
// Validates event contract compliance
orderEvent.OrderId.Should().NotBeEmpty();
orderEvent.CustomerId.Should().NotBeEmpty();
orderEvent.Items.Should().NotBeNull();
orderEvent.Items.Should().HaveCount(1);
orderEvent.CorrelationId.Should().NotBeNullOrEmpty();

// Validates base event properties
orderEvent.EventId.Should().NotBeEmpty();
orderEvent.OccurredAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
```

#### OrderCancelledEvent Contract

```csharp
[Fact]
public void OrderCancelledEvent_ShouldHaveCompensationFields()
```
- **Purpose**: Validates OrderCancelledEvent for compensation transactions
- **Contract Elements**:
  - Standard order information
  - Cancellation-specific fields (CancellationReason, CancelledAt)
  - Compensation workflow support
  - Audit trail information
- **Test Data**: Complete OrderCancelledEvent for failed order
- **Assertions**:
  - Cancellation reason is provided
  - Cancellation timestamp is recorded
  - Order status is set to "Cancelled"
  - All compensation data is present

**Cancellation Contract Validation**:
```csharp
// Validates cancellation-specific fields
cancelEvent.OrderId.Should().NotBeEmpty();
cancelEvent.CancellationReason.Should().NotBeNullOrEmpty();
cancelEvent.CancelledAt.Should().BeAfter(DateTime.MinValue);
cancelEvent.Status.Should().Be("Cancelled");
cancelEvent.CorrelationId.Should().NotBeNullOrEmpty();
```

#### StockDebitedEvent Feedback

```csharp
[Fact]
public void StockDebitedEvent_ShouldProvideInventoryFeedback()
```
- **Purpose**: Validates StockDebitedEvent for inventory feedback
- **Contract Elements**:
  - Order identification (OrderId)
  - Stock deduction details (StockDeductions collection)
  - Operation status (AllDeductionsSuccessful)
  - Error information when applicable
  - Correlation tracking
- **Test Data**: StockDebitedEvent with successful deductions
- **Assertions**:
  - Order ID is present for correlation
  - Stock deductions are detailed
  - Success status is indicated
  - No error messages for successful operations

**Inventory Feedback Validation**:
```csharp
// Validates inventory feedback structure
stockEvent.OrderId.Should().NotBeEmpty();
stockEvent.StockDeductions.Should().NotBeNull();
stockEvent.StockDeductions.Should().HaveCount(1);
stockEvent.AllDeductionsSuccessful.Should().BeTrue();
stockEvent.ErrorMessage.Should().BeNull();
stockEvent.CorrelationId.Should().NotBeNullOrEmpty();
```

### 4. Cross-Service Data Flow Tests

#### Data Consistency Validation

```csharp
[Fact]
public void CrossServiceDataFlow_ShouldMaintainConsistency()
```
- **Purpose**: Tests data consistency across service boundaries
- **Test Scenario**: Simulates complete data flow from product to order to event
- **Contract Elements**:
  - Product information propagation
  - Order item consistency
  - Event payload accuracy
  - Cross-reference integrity
- **Test Flow**:
  1. Create product in Inventory context
  2. Create order item referencing product
  3. Generate OrderConfirmedEvent from order
  4. Validate data consistency throughout flow

**Data Flow Validation**:
```csharp
// Validates data consistency across services
orderItem.ProductId.Should().Be(product.Id);
orderItem.ProductName.Should().Be(product.Name);
orderItem.UnitPrice.Should().Be(product.Price);

orderEvent.Items.First().ProductId.Should().Be(product.Id);
orderEvent.Items.First().ProductName.Should().Be(product.Name);
orderEvent.Items.First().Quantity.Should().Be(orderItem.Quantity);
orderEvent.CorrelationId.Should().Be(correlationId);
```

### 5. Event Sequence Validation

#### Event Ordering and Timing

```csharp
[Fact]
public void EventSequence_ShouldMaintainOrderIntegrity()
```
- **Purpose**: Validates event sequencing and temporal consistency
- **Contract Elements**:
  - Event temporal ordering
  - Correlation ID propagation
  - Event causality relationships
  - Timestamp consistency
- **Test Scenario**: Multiple related events in sequence
- **Assertions**:
  - Events maintain proper temporal order
  - Correlation IDs match across related events
  - Event causality is preserved

**Sequence Validation**:
```csharp
// Validates event sequence integrity
orderConfirmed.OrderId.Should().Be(stockDebited.OrderId);
orderConfirmed.CorrelationId.Should().Be(stockDebited.CorrelationId);
orderConfirmed.OccurredAt.Should().BeBefore(stockDebited.OccurredAt);
```

### 6. Domain Event Base Properties

#### Base Event Contract

```csharp
[Fact]
public void DomainEvent_BaseProperties_ShouldBeConsistent()
```
- **Purpose**: Validates base domain event contract compliance
- **Contract Elements**:
  - EventId generation and uniqueness
  - OccurredAt timestamp in UTC
  - CorrelationId for request tracking
  - Inheritance hierarchy compliance
- **Test Data**: Concrete domain event implementation
- **Assertions**:
  - Base properties are automatically set
  - Timestamps are in UTC format
  - Correlation IDs are preserved
  - Event IDs are unique

**Base Contract Validation**:
```csharp
// Validates base event properties
domainEvent.EventId.Should().NotBeEmpty();
domainEvent.OccurredAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
domainEvent.CorrelationId.Should().Be("base-test-123");
```

## Contract Versioning Strategy

### Version Compatibility
- **Backward Compatibility**: New versions must support existing clients
- **Additive Changes**: New optional properties are allowed
- **Deprecation Process**: Gradual deprecation of obsolete properties
- **Breaking Changes**: Require major version increment

### Schema Evolution
```csharp
// Example of backward-compatible change
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // New optional property (backward compatible)
    public string? Category { get; set; } // Added in v2.0
}
```

## Test Data Management

### Test Data Factories
Contract tests use factories to create consistent test data:

```csharp
public static class ContractTestDataFactory
{
    public static ProductDto CreateProductDto() => new ProductDto
    {
        Id = Guid.NewGuid(),
        Name = "Contract Test Product",
        Description = "Product for contract testing",
        Price = 99.99m,
        StockQuantity = 50,
        CreatedAt = DateTime.UtcNow
    };

    public static OrderConfirmedEvent CreateOrderConfirmedEvent() => new OrderConfirmedEvent
    {
        OrderId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        TotalAmount = 199.99m,
        Items = new List<OrderItemEvent>
        {
            new OrderItemEvent
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Contract Test Product",
                Quantity = 2,
                UnitPrice = 99.99m
            }
        },
        Status = "Confirmed",
        OrderCreatedAt = DateTime.UtcNow,
        CorrelationId = "contract-test-correlation"
    };
}
```

### Validation Helpers
```csharp
public static class ContractValidationHelpers
{
    public static void ValidateProductDto(ProductDto product)
    {
        product.Should().NotBeNull();
        product.Id.Should().NotBeEmpty();
        product.Name.Should().NotBeNullOrEmpty();
        product.Price.Should().BeGreaterOrEqualTo(0);
        product.StockQuantity.Should().BeGreaterOrEqualTo(0);
    }

    public static void ValidateDomainEvent(DomainEvent domainEvent)
    {
        domainEvent.Should().NotBeNull();
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }
}
```

## Contract Documentation

### Automated Documentation
Contract tests serve as living documentation for API contracts:

```csharp
/// <summary>
/// Contract: ProductDto
/// Version: 1.0
/// Description: Product data transfer object for cross-service communication
/// Services: Inventory API ? Sales API
/// </summary>
[Fact]
public void ProductDto_Contract_v1_0_ShouldMaintainStructure()
{
    // Contract definition through test
}
```

### Schema Registry Integration
For future enhancements, consider integrating with schema registry:

```csharp
// Future: Schema registry validation
[Fact]
public void ProductDto_ShouldMatchSchemaRegistry()
{
    var schema = SchemaRegistry.GetSchema("ProductDto", "1.0");
    var product = CreateTestProduct();
    
    schema.Validate(product).Should().BeTrue();
}
```

## Best Practices

### Contract Test Guidelines

1. **Immutable Contracts**: Once published, contracts should not change
2. **Additive Changes**: Only add optional properties
3. **Deprecation Path**: Provide migration path for breaking changes
4. **Version Tagging**: Tag contract versions in tests
5. **Documentation**: Maintain contract documentation alongside tests

### Test Organization
```csharp
[Trait("Category", "Contract")]
[Trait("Service", "Inventory")]
[Trait("Version", "1.0")]
public class InventoryContractTests
{
    // Contract tests organized by service and version
}
```

### Error Handling
```csharp
[Fact]
public void ContractValidation_ShouldProvideDetailedErrors()
{
    // Provide detailed error messages for contract violations
    var validationResult = validator.Validate(invalidDto);
    validationResult.Errors.Should().NotBeEmpty("contract validation should provide specific error details");
}
```

## Integration with CI/CD

### Contract Testing Pipeline
```yaml
# Example pipeline step
- name: Contract Tests
  run: |
    dotnet test tests/contracts.tests/ \
      --filter "Category=Contract" \
      --logger "trx;LogFileName=contract-results.trx"
```

### Quality Gates
- All contract tests must pass before deployment
- Contract changes require explicit approval
- Breaking changes trigger major version increment

---

*Last Updated: January 2025*
*Version: 1.0.0*