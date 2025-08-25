# Unit Tests Documentation

## Overview

This document provides detailed documentation for all unit test suites in the SalesAPI solution. Unit tests focus on testing individual components in isolation to ensure correct business logic implementation.

## Test Suites

### 1. Inventory API Tests (`inventory.api.tests`)

#### 1.1 Model Tests

##### StockReservationTests.cs
**Purpose**: Validates the StockReservation domain model behavior and business rules.

**Test Categories**:
- **Property Validation**: Ensures model properties behave correctly
- **Business Rules**: Validates domain-specific business logic
- **State Transitions**: Tests valid reservation status changes

**Key Test Cases**:

```csharp
[Fact]
public void StockReservation_ShouldInitializeWithCorrectDefaults()
```
- **Purpose**: Verifies default property values are set correctly
- **Assertions**: 
  - Id is generated
  - Status defaults to Reserved
  - ReservedAt is set to current UTC time
  - ProcessedAt is null initially

```csharp
[Fact]
public void StockReservation_ShouldValidateRequiredProperties()
```
- **Purpose**: Ensures required properties throw validation errors when missing
- **Test Data**: Invalid StockReservation instances
- **Assertions**: Validation attributes work correctly

```csharp
[Theory]
[InlineData(ReservationStatus.Reserved, ReservationStatus.Debited)]
[InlineData(ReservationStatus.Reserved, ReservationStatus.Released)]
public void StockReservation_ShouldAllowValidStatusTransitions(ReservationStatus from, ReservationStatus to)
```
- **Purpose**: Validates allowed status transitions in reservation lifecycle
- **Test Data**: Valid status transition combinations
- **Assertions**: Status transitions are permitted and ProcessedAt is updated

#### 1.2 Validation Tests

##### CreateProductDtoValidatorTests.cs
**Purpose**: Tests the FluentValidation rules for product creation DTOs.

**Test Categories**:
- **Required Field Validation**: Tests mandatory field enforcement
- **Format Validation**: Validates data format requirements
- **Business Rule Validation**: Tests domain-specific validation rules
- **Edge Case Validation**: Tests boundary conditions

**Key Test Cases**:

```csharp
[Fact]
public void CreateProductDto_ShouldValidateRequiredName()
```
- **Purpose**: Ensures product name is required
- **Test Data**: CreateProductDto with null/empty name
- **Assertions**: Validation error for missing name

```csharp
[Theory]
[InlineData("")]
[InlineData("   ")]
[InlineData(null)]
public void CreateProductDto_ShouldRejectInvalidNames(string invalidName)
```
- **Purpose**: Tests various invalid name scenarios
- **Test Data**: Empty, whitespace, and null strings
- **Assertions**: Appropriate validation messages

```csharp
[Theory]
[InlineData(-1)]
[InlineData(-0.01)]
public void CreateProductDto_ShouldRejectNegativePrice(decimal invalidPrice)
```
- **Purpose**: Validates price cannot be negative
- **Test Data**: Various negative price values
- **Assertions**: Validation error for negative prices

```csharp
[Theory]
[InlineData(0)]
[InlineData(1)]
[InlineData(999999)]
public void CreateProductDto_ShouldAcceptValidStockQuantity(int validQuantity)
```
- **Purpose**: Ensures valid stock quantities are accepted
- **Test Data**: Range of valid stock values
- **Assertions**: No validation errors for valid quantities

#### 1.3 Integration Tests

##### InventoryDbContextTests.cs
**Purpose**: Tests Entity Framework database operations and context behavior.

**Test Categories**:
- **CRUD Operations**: Create, Read, Update, Delete functionality
- **Entity Relationships**: Tests entity associations and navigation properties
- **Concurrency Control**: Validates optimistic concurrency handling
- **Transaction Management**: Tests database transaction behavior

**Key Test Cases**:

```csharp
[Fact]
public async Task AddProduct_ShouldPersistToDatabase()
```
- **Purpose**: Validates product creation and persistence
- **Setup**: In-memory database with test product
- **Actions**: Add product to context and save changes
- **Assertions**: Product is persisted with correct properties

```csharp
[Fact]
public async Task UpdateProductStock_ShouldModifyQuantity()
```
- **Purpose**: Tests stock quantity updates
- **Setup**: Product with initial stock
- **Actions**: Modify stock quantity and save
- **Assertions**: Stock change is persisted correctly

```csharp
[Fact]
public async Task AddStockReservation_ShouldCreateReservationRecord()
```
- **Purpose**: Validates stock reservation creation
- **Setup**: In-memory database
- **Actions**: Create and save stock reservation
- **Assertions**: Reservation is saved with correct default values

```csharp
[Fact]
public async Task QueryReservationsByOrder_ShouldReturnCorrectResults()
```
- **Purpose**: Tests reservation querying functionality
- **Setup**: Multiple reservations for different orders
- **Actions**: Query reservations by order ID
- **Assertions**: Correct reservations returned, proper filtering

```csharp
[Fact]
public async Task ConcurrentStockUpdate_ShouldMaintainConsistency()
```
- **Purpose**: Tests concurrent stock operations
- **Setup**: Product with initial stock
- **Actions**: Simulate concurrent reservation creation
- **Assertions**: Data consistency maintained, no race conditions

```csharp
[Fact]
public async Task UpdateReservationStatus_ShouldModifyStatus()
```
- **Purpose**: Tests reservation status updates
- **Setup**: Reservation in Reserved status
- **Actions**: Update status to Debited with ProcessedAt timestamp
- **Assertions**: Status and timestamp updated correctly

### 2. Sales API Tests (`sales.api.tests`)

#### 2.1 Model Tests

##### OrderTests.cs
**Purpose**: Validates the Order domain model behavior and business calculations.

**Test Categories**:
- **Property Initialization**: Tests default values and initialization
- **Business Calculations**: Validates total amount calculations
- **Validation Rules**: Tests model validation attributes
- **State Management**: Tests order status transitions

**Key Test Cases**:

```csharp
[Fact]
public void Order_ShouldInitializeWithCorrectDefaults()
```
- **Purpose**: Verifies order default property values
- **Assertions**: 
  - Id is generated
  - CreatedAt is set to current UTC time
  - Status defaults appropriately
  - Items collection is initialized

```csharp
[Fact]
public void Order_TotalAmount_ShouldCalculateCorrectly()
```
- **Purpose**: Tests total amount calculation from order items
- **Setup**: Order with multiple items of different prices/quantities
- **Assertions**: Total amount equals sum of all item totals

```csharp
[Theory]
[InlineData("Pending")]
[InlineData("Confirmed")]
[InlineData("Cancelled")]
public void Order_ShouldAcceptValidStatus(string validStatus)
```
- **Purpose**: Validates accepted order status values
- **Test Data**: Various valid status strings
- **Assertions**: Status assignment succeeds without errors

##### OrderItemTests.cs
**Purpose**: Tests the OrderItem domain model and price calculations.

**Test Categories**:
- **Price Calculations**: Tests total price computation
- **Property Validation**: Validates required properties
- **Business Rules**: Tests quantity and pricing rules

**Key Test Cases**:

```csharp
[Fact]
public void OrderItem_TotalPrice_ShouldCalculateCorrectly()
```
- **Purpose**: Validates total price calculation (quantity × unit price)
- **Setup**: OrderItem with specific quantity and unit price
- **Assertions**: TotalPrice property returns correct calculation

```csharp
[Theory]
[InlineData(1, 10.00, 10.00)]
[InlineData(2, 15.50, 31.00)]
[InlineData(5, 9.99, 49.95)]
public void OrderItem_TotalPrice_ShouldHandleVariousScenarios(int quantity, decimal unitPrice, decimal expectedTotal)
```
- **Purpose**: Tests price calculation with various input combinations
- **Test Data**: Different quantity/price combinations
- **Assertions**: Calculated total matches expected value

```csharp
[Fact]
public void OrderItem_ShouldValidateRequiredProperties()
```
- **Purpose**: Ensures required properties are enforced
- **Test Data**: OrderItem with missing required values
- **Assertions**: Validation errors for missing properties

#### 2.2 Validation Tests

##### OrderDtoValidationTests.cs
**Purpose**: Tests FluentValidation rules for order-related DTOs.

**Test Categories**:
- **DTO Structure Validation**: Tests CreateOrderDto validation rules
- **Nested Object Validation**: Validates order item validation
- **Business Rule Validation**: Tests domain-specific order rules
- **Cross-Field Validation**: Tests validation across multiple properties

**Key Test Cases**:

```csharp
[Fact]
public void CreateOrderDto_ShouldValidateRequiredCustomerId()
```
- **Purpose**: Ensures customer ID is required for order creation
- **Test Data**: CreateOrderDto with empty/null customer ID
- **Assertions**: Validation error for missing customer ID

```csharp
[Fact]
public void CreateOrderDto_ShouldValidateOrderItemsNotEmpty()
```
- **Purpose**: Validates that orders must contain at least one item
- **Test Data**: CreateOrderDto with empty items collection
- **Assertions**: Validation error for empty order

```csharp
[Theory]
[InlineData(0)]
[InlineData(-1)]
public void CreateOrderItemDto_ShouldRejectInvalidQuantity(int invalidQuantity)
```
- **Purpose**: Tests quantity validation rules
- **Test Data**: Zero and negative quantities
- **Assertions**: Validation errors for invalid quantities

```csharp
[Fact]
public void CreateOrderItemDto_ShouldValidateRequiredProductId()
```
- **Purpose**: Ensures product ID is required for order items
- **Test Data**: OrderItemDto with empty product ID
- **Assertions**: Validation error for missing product ID

#### 2.3 Integration Tests

##### SalesDbContextTests.cs
**Purpose**: Tests Entity Framework operations for the Sales database context.

**Test Categories**:
- **Entity Persistence**: Tests order and order item persistence
- **Relationship Mapping**: Validates entity relationships
- **Query Operations**: Tests complex queries and projections
- **Transaction Behavior**: Validates transaction handling

**Key Test Cases**:

```csharp
[Fact]
public async Task AddOrder_ShouldPersistWithItems()
```
- **Purpose**: Tests order creation with associated order items
- **Setup**: Order with multiple order items
- **Actions**: Save order with items to database
- **Assertions**: Order and all items are persisted correctly

```csharp
[Fact]
public async Task GetOrderWithItems_ShouldLoadNavigationProperties()
```
- **Purpose**: Tests eager loading of order items
- **Setup**: Order with items in database
- **Actions**: Query order with Include for items
- **Assertions**: Order items are loaded correctly

```csharp
[Fact]
public async Task UpdateOrderStatus_ShouldPersistChanges()
```
- **Purpose**: Tests order status updates
- **Setup**: Order with initial status
- **Actions**: Update order status and save
- **Assertions**: Status change is persisted

```csharp
[Fact]
public async Task DeleteOrder_ShouldRemoveOrderAndItems()
```
- **Purpose**: Tests cascade delete behavior
- **Setup**: Order with associated items
- **Actions**: Delete order from context
- **Assertions**: Order and items are removed (if cascade configured)

```csharp
[Fact]
public async Task QueryOrdersByCustomer_ShouldReturnCorrectResults()
```
- **Purpose**: Tests customer-specific order queries
- **Setup**: Orders for multiple customers
- **Actions**: Query orders by specific customer ID
- **Assertions**: Only orders for specified customer returned

```csharp
[Fact]
public async Task GetOrdersByDateRange_ShouldFilterCorrectly()
```
- **Purpose**: Tests date-based order filtering
- **Setup**: Orders with different creation dates
- **Actions**: Query orders within specific date range
- **Assertions**: Only orders within range returned

## Test Data Management

### Test Data Builders
Each test suite uses builder patterns for creating test data:

```csharp
public class ProductBuilder
{
    public static Product Create() => new Product
    {
        Id = Guid.NewGuid(),
        Name = "Test Product",
        Description = "Test Description",
        Price = 99.99m,
        StockQuantity = 100
    };
}
```

### Test Database Setup
Integration tests use in-memory databases for isolation:

```csharp
public class TestDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
    }
}
```

## Naming Conventions

### Test Method Naming
- **Pattern**: `[MethodUnderTest]_Should[ExpectedBehavior]_When[Condition]`
- **Example**: `CreateProduct_ShouldReturnValidationError_WhenNameIsEmpty`

### Test Class Naming
- **Pattern**: `[ClassUnderTest]Tests`
- **Example**: `ProductValidatorTests`

## Assertions Guidelines

### Use Descriptive Assertions
```csharp
// Good
result.Should().NotBeNull("because the product should be created successfully");
result.Name.Should().Be(expectedName, "because the name should match the input");

// Avoid
Assert.NotNull(result);
Assert.Equal(expectedName, result.Name);
```

### Test One Thing at a Time
```csharp
// Good - focused test
[Fact]
public void Product_ShouldValidateName()
{
    var product = new Product { Name = "" };
    var result = validator.Validate(product);
    result.Should().HaveValidationErrorFor(p => p.Name);
}

// Avoid - testing multiple things
[Fact]
public void Product_ShouldValidateAllProperties()
{
    // Testing name, price, stock in one test
}
```

## Performance Considerations

### Fast Test Execution
- Use in-memory databases for integration tests
- Mock external dependencies
- Minimize test setup and teardown overhead

### Resource Management
- Dispose of database contexts properly
- Clean up test data between tests
- Use `IDisposable` pattern for test fixtures

---

*Last Updated: January 2025*
*Version: 1.0.0*