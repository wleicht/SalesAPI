# Endpoint Tests - Comprehensive Integration Testing Suite

This project contains a comprehensive integration testing suite that validates all aspects of the SalesAPI microservices architecture, including authentication, authorization, CRUD operations, service communication, event-driven architecture, and system health.

## ?? Testing Philosophy

The testing strategy follows a **unified integration testing approach** using a single test project that covers:

- **End-to-End Workflows**: Complete user journeys from authentication to business operations
- **Service Integration**: Cross-service communication and data consistency
- **Authentication & Authorization**: JWT token-based security across all services
- **Event-Driven Architecture**: Asynchronous message processing and event flows
- **Business Logic Validation**: Core domain rules and constraints
- **System Health**: Service availability and infrastructure readiness

## ?? Test Coverage Overview

### **Total Tests: 47**

| Test Category | Count | Description |
|---------------|-------|-------------|
| **Authentication Tests** | 10 | JWT token generation, validation, and role-based access |
| **Gateway Tests** | 13 | YARP routing, health checks, and reverse proxy functionality |
| **Product CRUD Tests** | 6 | Inventory management with admin authorization |
| **Order CRUD Tests** | 8 | Sales operations with customer authentication |
| **Event-Driven Tests** | 3 | ? Asynchronous event processing and stock management |
| **API Health Tests** | 7 | Service availability and system monitoring |

## ?? Event-Driven Architecture Tests

### **EventDrivenTests.cs** ? NEW

This test class validates the complete event-driven architecture implementation:

#### **Test 1: CreateOrder_ShouldPublishEventAndDebitStock**
- **Purpose**: Validates end-to-end event-driven order processing
- **Flow**:
  1. Authenticate as admin and create product with initial stock
  2. Authenticate as customer and create order
  3. Verify `OrderConfirmedEvent` is published to RabbitMQ
  4. Confirm automatic stock deduction via event consumption
  5. Validate final stock quantity reflects the order

```csharp
[Fact]
public async Task CreateOrder_ShouldPublishEventAndDebitStock()
{
    // Arrange - Create product with stock
    var product = await CreateTestProduct(stockQuantity: 100);
    
    // Act - Create order (triggers event)
    var order = await CreateTestOrder(product.Id, quantity: 5);
    
    // Wait for event processing
    await Task.Delay(3000);
    
    // Assert - Stock was automatically debited
    var updatedProduct = await GetProduct(product.Id);
    Assert.Equal(95, updatedProduct.StockQuantity); // 100 - 5 = 95
}
```

#### **Test 2: CreateOrder_WithInsufficientStock_ShouldNotCreateOrderOrDebitStock**
- **Purpose**: Validates error handling when stock is insufficient
- **Flow**:
  1. Create product with low stock (2 units)
  2. Attempt to order more than available (10 units)
  3. Verify order creation fails with 422 Unprocessable Entity
  4. Confirm stock remains unchanged (no events processed)

```csharp
[Fact]
public async Task CreateOrder_WithInsufficientStock_ShouldNotCreateOrderOrDebitStock()
{
    // Arrange - Create product with low stock
    var product = await CreateTestProduct(stockQuantity: 2);
    
    // Act - Try to order more than available
    var response = await AttemptOrderCreation(product.Id, quantity: 10);
    
    // Assert - Order rejected and stock unchanged
    Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    
    var unchangedProduct = await GetProduct(product.Id);
    Assert.Equal(2, unchangedProduct.StockQuantity); // Stock unchanged
}
```

#### **Test 3: CreateMultipleOrders_ShouldProcessAllEventsCorrectly**
- **Purpose**: Validates concurrent event processing and consistency
- **Flow**:
  1. Create product with sufficient stock (50 units)
  2. Create multiple orders sequentially (3, 7, 2 units)
  3. Wait for all events to be processed
  4. Verify final stock reflects all deductions (50 - 12 = 38)

```csharp
[Fact]
public async Task CreateMultipleOrders_ShouldProcessAllEventsCorrectly()
{
    // Arrange - Create product with sufficient stock
    var product = await CreateTestProduct(stockQuantity: 50);
    
    // Act - Create multiple orders
    await CreateTestOrder(product.Id, quantity: 3);
    await CreateTestOrder(product.Id, quantity: 7);
    await CreateTestOrder(product.Id, quantity: 2);
    
    // Wait for all events to be processed
    await Task.Delay(10000);
    
    // Assert - All deductions applied correctly
    var finalProduct = await GetProduct(product.Id);
    Assert.Equal(38, finalProduct.StockQuantity); // 50 - 3 - 7 - 2 = 38
}
```

## ?? Authentication Tests

### **AuthenticationTests.cs**

Comprehensive JWT authentication and authorization testing:

#### Core Authentication Features
- **Token Generation**: Validates JWT token creation with valid credentials
- **Token Validation**: Ensures invalid/expired tokens are rejected
- **Role-Based Access**: Verifies admin/customer role enforcement
- **Open Access**: Confirms public endpoints work without authentication

#### Key Test Cases
```csharp
[Fact]
public async Task GenerateToken_WithValidCredentials_ShouldReturnJwtToken()

[Fact]
public async Task AccessProtectedEndpoint_WithValidToken_ShouldSucceed()

[Fact]
public async Task AccessProtectedEndpoint_WithoutToken_ShouldReturn401()

[Fact]
public async Task AccessAdminEndpoint_WithCustomerToken_ShouldReturn403()
```

## ?? Gateway Tests

### **GatewayApiTests.cs** & **GatewayRoutingTests.cs**

Tests for YARP reverse proxy and routing functionality:

#### Gateway Features Tested
- **Service Health**: Gateway health endpoint availability
- **Route Configuration**: Dynamic route discovery and configuration
- **Backend Routing**: Request forwarding to appropriate services
- **Error Handling**: Graceful handling of backend service failures

#### Key Test Cases
```csharp
[Fact]
public async Task GetHealth_ShouldReturnHealthy()

[Fact]
public async Task GetRoutes_ShouldReturnConfiguredRoutes()

[Fact]
public async Task RouteToInventory_ShouldForwardToInventoryService()

[Fact]
public async Task RouteToSales_ShouldForwardToSalesService()
```

## ?? Product CRUD Tests

### **ProductCrudTests.cs** & **InventoryApiTests.cs**

Comprehensive testing of inventory management functionality:

#### Features Tested
- **Admin Authorization**: Product creation/update requires admin role
- **Open Access**: Product reading available without authentication
- **Data Validation**: Input validation and error handling
- **Business Rules**: Price and stock quantity constraints

#### Key Test Cases
```csharp
[Fact]
public async Task CreateProduct_WithAdminToken_ShouldCreateProduct()

[Fact]
public async Task CreateProduct_WithoutToken_ShouldReturn401()

[Fact]
public async Task GetProducts_WithoutAuthentication_ShouldReturnProducts()

[Fact]
public async Task UpdateProduct_WithAdminToken_ShouldUpdateProduct()
```

## ?? Order CRUD Tests

### **OrderCrudTests.cs** & **SalesApiTests.cs**

Testing of sales operations and order management:

#### Features Tested
- **Customer Authorization**: Order creation requires customer/admin role
- **Stock Validation**: Real-time inventory checking via HTTP
- **Order Processing**: Complete order workflow with business logic
- **Price Freezing**: Unit prices captured at order time

#### Key Test Cases
```csharp
[Fact]
public async Task CreateOrder_WithValidCustomerToken_ShouldCreateOrder()

[Fact]
public async Task CreateOrder_WithInsufficientStock_ShouldReturnError()

[Fact]
public async Task GetOrders_WithoutAuthentication_ShouldReturnOrders()

[Fact]
public async Task CreateOrder_ShouldValidateStockAvailability()
```

## ?? API Health Tests

Health monitoring and service availability testing across all services:

#### Features Tested
- **Service Health**: Individual service health endpoints
- **Gateway Health**: Unified health checking through gateway
- **Infrastructure**: Database and external dependency health
- **Swagger Documentation**: API documentation availability

## ?? Running the Tests

### **Prerequisites**

Before running tests, ensure all services are running:

```bash
# Start infrastructure services
docker-compose -f docker-compose.infrastructure.yml up -d

# Start all microservices
dotnet run --project src/inventory.api --urls "http://localhost:5000" &
dotnet run --project src/sales.api --urls "http://localhost:5001" &
dotnet run --project src/gateway --urls "http://localhost:6000" &
```

### **Run All Tests**
```bash
# Execute all 47 tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj

# Run with detailed output
dotnet test tests/endpoint.tests/endpoint.tests.csproj --logger "console;verbosity=normal"
```

### **Run Specific Test Categories**

```bash
# Event-driven architecture tests (NEW)
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "EventDrivenTests"

# Authentication and security tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "AuthenticationTests"

# Gateway and routing tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "Gateway"

# Product management tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "ProductCrudTests"

# Order processing tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "OrderCrudTests"

# Service health tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "Health"
```

### **Run Tests with Coverage**
```bash
# Install coverage tools
dotnet tool install --global dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test tests/endpoint.tests/endpoint.tests.csproj --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

## ?? Test Configuration

### **Test Base Classes**

The tests use shared base classes for common functionality:

```csharp
public class BaseIntegrationTest
{
    protected readonly HttpClient _gatewayClient;
    protected readonly HttpClient _inventoryClient;
    protected readonly HttpClient _salesClient;
    
    protected async Task<string?> GetAuthTokenAsync(string username, string password)
    protected async Task<T> GetAsync<T>(string endpoint)
    protected async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
}
```

### **Test Data Management**

Each test creates its own isolated test data:

```csharp
protected async Task<ProductDto> CreateTestProduct(
    string name = "Test Product",
    decimal price = 99.99m,
    int stockQuantity = 100)

protected async Task<OrderDto> CreateTestOrder(
    Guid productId,
    int quantity,
    string customerRole = "customer1")
```

### **Event Testing Utilities**

Specialized utilities for event-driven testing:

```csharp
protected async Task WaitForEventProcessing(int milliseconds = 3000)
protected async Task VerifyStockDeduction(Guid productId, int expectedStock)
protected async Task VerifyOrderEventPublished(Guid orderId)
```

## ?? Test Execution Timeline

### **Typical Test Run Performance**

| Test Category | Execution Time | Description |
|---------------|----------------|-------------|
| **Authentication** | ~2-3 seconds | Fast token validation tests |
| **Gateway** | ~3-4 seconds | Network routing verification |
| **Product CRUD** | ~4-5 seconds | Database operations |
| **Order CRUD** | ~5-6 seconds | Cross-service communication |
| **Event-Driven** | ~15-20 seconds | ? Asynchronous processing wait times |
| **Health Checks** | ~1-2 seconds | Simple endpoint validation |

**Total Execution Time**: ~30-40 seconds for all 47 tests

### **Event-Driven Test Timing**

Event-driven tests require additional wait time for asynchronous processing:

```csharp
// Wait for event processing
await Task.Delay(3000);  // Single event processing
await Task.Delay(10000); // Multiple concurrent events
```

This ensures events are fully processed before assertions are made.

## ?? Troubleshooting Tests

### **Common Test Failures**

#### **Services Not Running**
```
Error: Connection refused to localhost:5000
```
**Solution**: Ensure all services are started before running tests

#### **Database Connection Issues**
```
Error: Unable to connect to SQL Server
```
**Solution**: Verify SQL Server container is running and connection strings are correct

#### **RabbitMQ Connection Failures** ?
```
Error: Event-driven tests failing - stock not debited
```
**Solution**: 
1. Check RabbitMQ container: `docker ps`
2. Verify RabbitMQ management UI: http://localhost:15672
3. Check service logs for event processing errors

#### **Authentication Token Expiry**
```
Error: 401 Unauthorized during test execution
```
**Solution**: Tests automatically generate fresh tokens, but verify JWT configuration

#### **Event Processing Timeout** ?
```
Error: Expected stock 95, but was 100
```
**Solution**: 
1. Increase wait time in event tests
2. Check event handler logs for processing errors
3. Verify event publishing is working correctly

### **Test Environment Setup**

```bash
# Verify all services are healthy
curl http://localhost:6000/health
curl http://localhost:5000/health  
curl http://localhost:5001/health

# Check RabbitMQ is accessible
curl -u admin:admin123 http://localhost:15672/api/overview

# Verify databases are accessible
dotnet ef database update --project src/inventory.api
dotnet ef database update --project src/sales.api
```

## ?? Test Metrics & Reporting

### **Test Result Analysis**

Monitor test execution with these metrics:

```bash
# Test execution summary
dotnet test tests/endpoint.tests/endpoint.tests.csproj --logger trx

# Performance profiling
dotnet test tests/endpoint.tests/endpoint.tests.csproj --logger "console;verbosity=diagnostic"

# Test result reporting
dotnet test tests/endpoint.tests/endpoint.tests.csproj --results-directory "test-results"
```

### **Continuous Integration**

The tests are designed for CI/CD pipeline integration:

```yaml
# Example GitHub Actions configuration
- name: Run Integration Tests
  run: |
    docker-compose -f docker-compose.infrastructure.yml up -d
    sleep 30  # Wait for services to start
    dotnet test tests/endpoint.tests/endpoint.tests.csproj --logger trx
```

## ?? Test Coverage Goals

### **Current Coverage: 47 Tests**

- ? **Authentication**: Complete JWT workflow coverage
- ? **Authorization**: Role-based access control validation
- ? **CRUD Operations**: Full business logic testing
- ? **Service Integration**: Cross-service communication
- ? **Event-Driven Architecture**: ? End-to-end event processing
- ? **Error Handling**: Comprehensive error scenario testing
- ? **Health Monitoring**: System availability validation

### **Future Test Enhancements**

- **Performance Tests**: Load testing for high-volume scenarios
- **Chaos Engineering**: Failure injection and recovery testing
- **Security Tests**: Penetration testing and vulnerability scanning
- **Contract Tests**: API contract validation between services
- **End-to-End Browser Tests**: Complete user journey automation

## ?? Event-Driven Testing Best Practices

### **Asynchronous Testing Patterns**

```csharp
// Pattern 1: Fixed delay for event processing
await Task.Delay(3000);

// Pattern 2: Polling with timeout
var timeout = TimeSpan.FromSeconds(10);
var stopwatch = Stopwatch.StartNew();
while (stopwatch.Elapsed < timeout)
{
    var product = await GetProduct(productId);
    if (product.StockQuantity == expectedStock)
        break;
    await Task.Delay(500);
}

// Pattern 3: Event monitoring with correlation ID
await WaitForEventWithCorrelationId(correlationId, timeout);
```

### **Event Test Isolation**

Each event test creates isolated test data to prevent interference:

```csharp
[Fact]
public async Task EventTest_ShouldIsolateTestData()
{
    // Each test creates unique products
    var product = await CreateTestProduct($"Product-{Guid.NewGuid()}");
    
    // Use unique customer IDs
    var customerId = Guid.NewGuid();
    
    // Verify isolation
    // Tests run in parallel without interference
}
```

### **Event Verification Strategies**

```csharp
// Strategy 1: State verification (most common)
var finalProduct = await GetProduct(productId);
Assert.Equal(expectedStock, finalProduct.StockQuantity);

// Strategy 2: Event log verification
var processedEvents = await GetProcessedEvents(orderId);
Assert.Contains(processedEvents, e => e.EventType == "OrderConfirmedEvent");

// Strategy 3: Correlation tracking
Assert.NotNull(orderResponse.CorrelationId);
await VerifyEventProcessedWithCorrelation(orderResponse.CorrelationId);
```

This comprehensive testing approach ensures the event-driven architecture is robust, reliable, and maintainable.