# Endpoint Tests - Comprehensive Integration Testing Suite with Stock Reservations

This project contains a comprehensive integration testing suite that validates all aspects of the SalesAPI microservices architecture, including authentication, authorization, CRUD operations, service communication, event-driven architecture, **?? stock reservation system (Saga pattern)**, and system health.

## ?? Testing Philosophy

The testing strategy follows a **unified integration testing approach** using a single test project that covers:

- **End-to-End Workflows**: Complete user journeys from authentication to business operations
- **Service Integration**: Cross-service communication and data consistency
- **Authentication & Authorization**: JWT token-based security across all services
- **Event-Driven Architecture**: Asynchronous message processing and event flows
- **?? Stock Reservation System**: Saga pattern implementation with compensation logic
- **?? Overselling Prevention**: Race condition testing and atomic operations
- **?? Payment Failure Simulation**: Realistic payment scenarios with rollback
- **Business Logic Validation**: Core domain rules and constraints
- **System Health**: Service availability and infrastructure readiness

## ?? Enhanced Test Coverage Overview

### **Total Tests: 51** ??

| Test Category | Count | Description |
|---------------|-------|-------------|
| **?? Stock Reservation Tests** | 8 | **NEW** - Saga pattern, compensation, race conditions |
| **Authentication Tests** | 10 | JWT token generation, validation, and role-based access |
| **Gateway Tests** | 13 | YARP routing, health checks, and reverse proxy functionality |
| **Product CRUD Tests** | 6 | Inventory management with admin authorization |
| **Order CRUD Tests** | 8 | Sales operations enhanced with reservation integration |
| **Event-Driven Tests** | 3 | Asynchronous event processing and stock management |
| **API Health Tests** | 7 | Service availability and system monitoring |

## ?? Stock Reservation System Tests (NEW)

### **StockReservationTests.cs** - Comprehensive Saga Pattern Testing

This test class validates the complete stock reservation system implementing the Saga pattern with compensation logic:

#### **?? Test 1: CreateOrderWithReservation_ShouldProcessSuccessfully**
**Purpose**: Validates end-to-end reservation-based order processing workflow

**Complete Flow Validation**:
1. **Setup**: Admin authentication + product creation (100 units)
2. **Order Creation**: Customer authentication + order request (15 units)
3. **Synchronous Reservation**: Immediate stock allocation
4. **Payment Processing**: Simulated payment success
5. **Event Publishing**: OrderConfirmedEvent to RabbitMQ
6. **Asynchronous Processing**: Event consumption and stock deduction
7. **Final Validation**: 
   - Stock correctly debited (100 ? 85 units)
   - Reservation status changed (Reserved ? Debited)
   - Complete audit trail maintained

```csharp
[Fact]
public async Task CreateOrderWithReservation_ShouldProcessSuccessfully()
{
    // Test validates complete reservation ? confirmation workflow
    var product = await CreateTestProduct(stockQuantity: 100);
    var order = await CreateOrderWithReservation(product.Id, quantity: 15);
    
    // Wait for asynchronous event processing
    await Task.Delay(15000);
    
    // Verify stock deduction via events
    var finalStock = await GetUpdatedStock(product.Id);
    Assert.Equal(85, finalStock); // 100 - 15 = 85
    
    // Verify reservation status transition
    var reservations = await GetReservationsByOrder(order.Id);
    Assert.All(reservations, r => Assert.Equal("Debited", r.Status));
}
```

#### **??? Test 2: CreateOrderWithPaymentFailure_ShouldReleaseReservations**
**Purpose**: Validates Saga compensation pattern for payment failures

**Compensation Logic Testing**:
1. **Setup**: Expensive product creation (triggers payment failure)
2. **Reservation**: Successful stock reservation
3. **Payment Failure**: Simulated payment processing failure  
4. **Compensation Event**: OrderCancelledEvent publishing
5. **Stock Release**: Automatic reservation release
6. **Consistency**: Stock quantity remains unchanged

```csharp
[Fact]
public async Task CreateOrderWithPaymentFailure_ShouldReleaseReservations()
{
    // Test validates compensation pattern implementation
    var expensiveProduct = await CreateTestProduct(price: 2000.00m, stock: 50);
    
    // Attempt order creation (triggers payment failure)
    var response = await AttemptOrderWithPaymentFailure(expensiveProduct.Id, quantity: 3);
    
    // Verify payment failure response
    Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    
    // Wait for compensation event processing
    await Task.Delay(10000);
    
    // Verify stock was not debited (compensation successful)
    var unchangedStock = await GetUpdatedStock(expensiveProduct.Id);
    Assert.Equal(50, unchangedStock); // Stock unchanged
}
```

#### **????? Test 3: ConcurrentOrderCreation_ShouldPreventOverselling**
**Purpose**: Validates race condition prevention and atomic operations

**Concurrency Testing**:
1. **Setup**: Limited stock product (20 units)
2. **Concurrent Orders**: 4 simultaneous orders (8 units each)
3. **Atomic Validation**: Only valid orders accepted (max 2 orders)
4. **Overselling Prevention**: Total allocation ? available stock
5. **Consistency**: Final stock reflects only successful orders

```csharp
[Fact]
public async Task ConcurrentOrderCreation_ShouldPreventOverselling()
{
    // Test validates atomic reservation operations
    var limitedProduct = await CreateTestProduct(stockQuantity: 20);
    
    // Launch 4 concurrent orders of 8 units each
    var orderTasks = CreateConcurrentOrders(limitedProduct.Id, quantity: 8, count: 4);
    var results = await Task.WhenAll(orderTasks);
    
    var successfulOrders = results.Count(r => r.Success);
    
    // Only 2 orders should succeed (2 × 8 = 16 ? 20)
    Assert.True(successfulOrders <= 2, "Overselling prevented");
    Assert.True(successfulOrders >= 1, "At least one order succeeded");
    
    // Verify final stock consistency
    var expectedStock = 20 - (successfulOrders * 8);
    var actualStock = await GetUpdatedStock(limitedProduct.Id);
    Assert.Equal(expectedStock, actualStock);
}
```

#### **?? Test 4: StockReservationApi_ShouldWorkCorrectly**
**Purpose**: Validates direct stock reservation API endpoints

**API Endpoint Testing**:
1. **Direct Reservation**: POST `/api/stockreservations`
2. **Response Validation**: Reservation details and status
3. **Query by Order**: GET `/api/stockreservations/order/{orderId}`
4. **Specific Retrieval**: GET `/api/stockreservations/{reservationId}`
5. **Data Integrity**: Complete reservation information

```csharp
[Fact]
public async Task StockReservationApi_ShouldWorkCorrectly()
{
    // Test validates direct API endpoint functionality
    var product = await CreateTestProduct(stockQuantity: 30);
    
    // Create reservation directly via API
    var reservationRequest = CreateReservationRequest(product.Id, quantity: 5);
    var response = await PostReservation(reservationRequest);
    
    // Validate successful creation (201 Created)
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    
    // Verify reservation details
    var reservationData = await ParseReservationResponse(response);
    Assert.True(reservationData.Success);
    Assert.Equal(5, reservationData.RequestedQuantity);
    
    // Test query endpoints
    var orderReservations = await GetReservationsByOrder(reservationRequest.OrderId);
    Assert.Single(orderReservations);
    Assert.Equal("Reserved", orderReservations[0].Status);
}
```

### **SimpleReservationTests.cs** - Basic Connectivity & Diagnostics

This test class provides basic validation and troubleshooting capabilities:

#### **?? Connectivity Tests**
- `InventoryApi_ShouldBeResponding` - Basic API availability
- `Authentication_ShouldWork` - JWT token generation  
- `CreateProduct_ShouldWork` - Product creation capability
- `StockReservationEndpoint_ShouldBeAccessible` - Reservation API accessibility

```csharp
[Fact]
public async Task StockReservationEndpoint_ShouldBeAccessible()
{
    // Validates reservation endpoint is reachable and configured correctly
    var token = await GetAdminToken();
    var response = await TestReservationEndpoint(token);
    
    // Should return 404 (NotFound) or 200 (OK), not 500 (ServerError)
    Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
}
```

## ?? Enhanced Event-Driven Architecture Tests

### **EventDrivenTests.cs** - Updated with Reservation Integration

This test class validates the complete event-driven architecture implementation **enhanced with stock reservations**:

#### **Test 1: CreateOrder_ShouldPublishEventAndDebitStock** (Enhanced)
- **Purpose**: Validates end-to-end event-driven order processing **with reservations**
- **New Features**: 
  - ? Synchronous reservation creation before event publishing
  - ? Enhanced OrderConfirmedEvent with reservation correlation
  - ? Reservation status transitions (Reserved ? Debited)

#### **Test 2: CreateOrder_WithInsufficientStock_ShouldNotCreateOrderOrDebitStock** (Enhanced)
- **Purpose**: Validates error handling **with reservation prevention**
- **New Features**:
  - ? Reservation-level stock validation
  - ? No orphaned reservations created
  - ? Enhanced error messages with stock details

#### **Test 3: CreateMultipleOrders_ShouldProcessAllEventsCorrectly** (Enhanced)
- **Purpose**: Validates concurrent event processing **with reservation coordination**
- **New Features**:
  - ? Reservation-based concurrency control
  - ? Sequential reservation processing
  - ? Enhanced consistency validation

## ??? Stock Reservation Test Utilities

### **Reservation-Specific Test Helpers**

```csharp
// Stock reservation creation
protected async Task<StockReservationResponse> CreateReservation(
    Guid productId, 
    int quantity, 
    string correlationId = null)

// Reservation status validation
protected async Task<List<StockReservation>> GetReservationsByOrder(Guid orderId)

// Stock consistency verification
protected async Task ValidateStockConsistency(
    Guid productId, 
    int expectedStock, 
    int expectedReserved = 0)

// Payment failure simulation
protected async Task<HttpResponseMessage> SimulatePaymentFailure(
    Guid productId, 
    int quantity, 
    decimal highPrice = 2000.00m)

// Concurrent order testing
protected async Task<OrderResult[]> CreateConcurrentOrders(
    Guid productId, 
    int quantity, 
    int orderCount)
```

### **Enhanced Event Testing with Reservations**

```csharp
// Wait for reservation + event processing
protected async Task WaitForReservationProcessing(int milliseconds = 15000)

// Verify reservation status transitions
protected async Task VerifyReservationStatusChange(
    Guid orderId, 
    ReservationStatus expectedStatus)

// Validate compensation events
protected async Task VerifyCompensationProcessing(
    Guid orderId, 
    string expectedReason)
```

## ????? Running Enhanced Tests

### **Prerequisites for Stock Reservation Tests**

Before running reservation tests, ensure all services are running with **RabbitMQ** for event processing:

```bash
# Start infrastructure services (CRITICAL for reservation tests)
docker-compose -f docker-compose.infrastructure.yml up -d

# Verify RabbitMQ is accessible (required for events)
curl -u admin:admin123 http://localhost:15672/api/overview

# Start all microservices
dotnet run --project src/inventory.api --urls "http://localhost:5000" &
dotnet run --project src/sales.api --urls "http://localhost:5001" &
dotnet run --project src/gateway --urls "http://localhost:6000" &

# Wait for service initialization (IMPORTANT for reservation sync)
sleep 20
```

### **Run Stock Reservation Tests** ??

```bash
# Run all stock reservation tests (8 tests)
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "StockReservationTests"

# Run with detailed output to see reservation flow
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "StockReservationTests" --logger "console;verbosity=detailed"

# Run specific reservation scenarios
dotnet test --filter "CreateOrderWithReservation_ShouldProcessSuccessfully"
dotnet test --filter "CreateOrderWithPaymentFailure_ShouldReleaseReservations"
dotnet test --filter "ConcurrentOrderCreation_ShouldPreventOverselling"
dotnet test --filter "StockReservationApi_ShouldWorkCorrectly"

# Basic connectivity tests
dotnet test --filter "SimpleReservationTests"
```

### **Run All Enhanced Tests (51 Tests)**
```bash
# Execute all tests including reservations
dotnet test tests/endpoint.tests/endpoint.tests.csproj

# Run with coverage and detailed output
dotnet test tests/endpoint.tests/endpoint.tests.csproj \
  --logger "console;verbosity=normal" \
  --collect:"XPlat Code Coverage"
```

### **Enhanced Test Categories**

```bash
# ?? Stock reservation system tests (8 tests)
dotnet test --filter "StockReservationTests OR SimpleReservationTests"

# Event-driven architecture tests (enhanced with reservations)
dotnet test --filter "EventDrivenTests"

# Authentication and security tests
dotnet test --filter "AuthenticationTests"

# Gateway and routing tests  
dotnet test --filter "Gateway"

# Product management tests
dotnet test --filter "ProductCrudTests"

# Order processing tests (enhanced with reservations)
dotnet test --filter "OrderCrudTests"

# Service health tests
dotnet test --filter "Health"
```

## ?? Enhanced Test Execution Timeline

### **Stock Reservation Test Performance**

| Test Category | Execution Time | Reason for Duration |
|---------------|----------------|---------------------|
| **?? Reservation Tests** | **15-20 seconds** | **Async event processing + payment simulation** |
| **Event-Driven** | 15-20 seconds | Message broker processing delays |
| **Authentication** | 2-3 seconds | Fast token validation |
| **Gateway** | 3-4 seconds | Network routing verification |
| **Product CRUD** | 4-5 seconds | Database operations |
| **Order CRUD** | 5-6 seconds | Cross-service communication |
| **Health Checks** | 1-2 seconds | Simple endpoint validation |

**Total Enhanced Execution Time**: **~45-55 seconds for all 51 tests**

### **?? Stock Reservation Test Timing Breakdown**

```csharp
// Reservation creation (synchronous): ~100-200ms
var reservation = await CreateReservation(productId, quantity);

// Payment processing simulation: ~100ms  
var paymentResult = await SimulatePayment(orderAmount);

// Event publishing: ~50ms
await PublishOrderConfirmedEvent(order);

// Event processing wait (asynchronous): ~15 seconds
await Task.Delay(15000); // Required for RabbitMQ + DB operations

// Final validation: ~100ms
var finalState = await ValidateOrderAndStock(orderId, productId);
```

### **Timing Considerations for Reservation Tests**

1. **Synchronous Operations** (fast):
   - Stock reservation creation
   - Payment simulation
   - Initial validation

2. **Asynchronous Operations** (require wait time):
   - RabbitMQ message delivery
   - Event handler processing  
   - Database transaction completion
   - Stock deduction/release

3. **Concurrency Tests** (special timing):
   - Multiple simultaneous requests
   - Atomic operation validation
   - Race condition prevention testing

## ?? Enhanced Troubleshooting with Stock Reservations

### **Common Stock Reservation Test Failures** ??

#### **Reservation Creation Failures**
```
Error: Assert.Equal() Failure - Expected: Created, Actual: OK
```
**Solution**: 
1. Check StockReservationsController endpoint configuration
2. Verify return status codes (should be 201 Created, not 200 OK)
3. Ensure proper HTTP method routing

#### **Event Processing Timeouts** ??
```
Error: Expected stock 85, but was 100 - reservation not processed
```
**Solution**:
1. Verify RabbitMQ is running: `docker ps | grep rabbitmq`
2. Check RabbitMQ management UI: http://localhost:15672
3. Increase wait time in tests: `await Task.Delay(20000);`
4. Check service logs for event processing errors
5. Verify event handlers are registered correctly

#### **Payment Simulation Issues** ??
```
Error: Payment succeeded unexpectedly - should have failed
```
**Solution**:
1. Check payment simulation logic in OrdersController
2. Verify expensive product pricing (use ? $2000 for failures)
3. Review payment failure probability settings
4. Test multiple attempts for probabilistic failures

#### **Concurrency Test Failures** ??
```
Error: Too many orders succeeded - expected max 2, got 4
```
**Solution**:
1. Verify atomic reservation operations
2. Check database transaction isolation levels
3. Ensure proper stock validation logic
4. Review concurrent access patterns

#### **Compensation Logic Issues** ??
```
Error: Stock not released after payment failure
```
**Solution**:
1. Verify OrderCancelledEvent is published
2. Check OrderCancelledEventHandler registration
3. Ensure compensation logic is working
4. Validate reservation status transitions

#### **Services Not Running**
```
Error: Connection refused to localhost:5000
```
**Solution**: Ensure all services are started before running tests

#### **Database Connection Issues**
```
Error: Unable to connect to SQL Server
```
**Solution**: Verify SQL Server container is running and migrations applied

#### **RabbitMQ Connection Failures**
```
Error: Event-driven tests failing - stock not debited
```
**Solution**: 
1. Check RabbitMQ container: `docker ps`
2. Verify RabbitMQ management UI: http://localhost:15672
3. Check service logs for event processing errors

### **?? Enhanced Test Environment Setup for Reservations**

```bash
# 1. Start infrastructure with proper timing
docker-compose -f docker-compose.infrastructure.yml up -d
sleep 30  # Wait for containers to be fully ready

# 2. Verify RabbitMQ is accessible (CRITICAL for reservations)
curl -u admin:admin123 http://localhost:15672/api/overview

# 3. Apply database migrations (includes StockReservations table)
dotnet ef database update --project src/inventory.api
dotnet ef database update --project src/sales.api

# 4. Start services with proper dependencies
dotnet run --project src/inventory.api --urls "http://localhost:5000" &
sleep 5  # Let inventory start first
dotnet run --project src/sales.api --urls "http://localhost:5001" &  
sleep 5  # Let sales start second
dotnet run --project src/gateway --urls "http://localhost:6000" &
sleep 10  # Let all services stabilize

# 5. Verify all services are healthy
curl http://localhost:6000/health
curl http://localhost:5000/health  
curl http://localhost:5001/health

# 6. Test basic reservation functionality
curl -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"Username":"admin","Password":"admin123"}'

# 7. Run reservation tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "StockReservationTests"
```

### **?? Stock Reservation Test Data Verification**

```bash
# Check stock reservations table
sqlcmd -S localhost -U sa -P Your_password123 -Q "
USE InventoryDb; 
SELECT TOP 10 * FROM StockReservations ORDER BY ReservedAt DESC;
"

# Check processed events
sqlcmd -S localhost -U sa -P Your_password123 -Q "
USE InventoryDb; 
SELECT TOP 10 * FROM ProcessedEvents ORDER BY ProcessedAt DESC;
"

# Monitor RabbitMQ queues
curl -u admin:admin123 http://localhost:15672/api/queues

# Check queue message counts
curl -u admin:admin123 http://localhost:15672/api/queues/%2F/inventory.api
curl -u admin:admin123 http://localhost:15672/api/queues/%2F/sales.api
```

## ?? Enhanced Test Metrics & Reporting

### **?? Stock Reservation Test Coverage Metrics**

```bash
# Run reservation tests with coverage
dotnet test tests/endpoint.tests/endpoint.tests.csproj \
  --filter "StockReservationTests" \
  --collect:"XPlat Code Coverage" \
  --results-directory "reservation-test-results"

# Generate detailed coverage report
reportgenerator \
  -reports:"reservation-test-results/**/coverage.cobertura.xml" \
  -targetdir:"reservation-coverage-report" \
  -reporttypes:Html

# View coverage report
open reservation-coverage-report/index.html
```

### **?? Reservation Test Result Analysis**

Monitor stock reservation test execution with these enhanced metrics:

```bash
# Detailed reservation test execution
dotnet test tests/endpoint.tests/endpoint.tests.csproj \
  --filter "StockReservationTests" \
  --logger "console;verbosity=diagnostic" \
  --logger "trx;LogFileName=reservation-results.trx"

# Performance profiling for reservation operations  
dotnet test tests/endpoint.tests/endpoint.tests.csproj \
  --filter "StockReservationTests" \
  --logger "console;verbosity=detailed" \
  --diag:reservation-diagnostics.log
```

### **?? Continuous Integration with Stock Reservations**

Enhanced CI/CD pipeline configuration for reservation testing:

```yaml
# Example GitHub Actions configuration
name: Integration Tests with Stock Reservations

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      rabbitmq:
        image: rabbitmq:3.13-management-alpine
        ports:
          - 5672:5672
          - 15672:15672
        env:
          RABBITMQ_DEFAULT_USER: admin
          RABBITMQ_DEFAULT_PASS: admin123
      
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        ports:
          - 1433:1433
        env:
          SA_PASSWORD: Your_password123
          ACCEPT_EULA: Y

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Wait for services
      run: |
        sleep 60  # Extended wait for SQL Server + RabbitMQ
        curl --retry 10 --retry-delay 5 http://localhost:15672

    - name: Apply database migrations
      run: |
        dotnet ef database update --project src/inventory.api
        dotnet ef database update --project src/sales.api

    - name: Start microservices
      run: |
        dotnet run --project src/inventory.api --urls "http://localhost:5000" &
        dotnet run --project src/sales.api --urls "http://localhost:5001" &
        dotnet run --project src/gateway --urls "http://localhost:6000" &
        sleep 30  # Wait for all services to start

    - name: Run all tests including reservations
      run: |
        dotnet test tests/endpoint.tests/endpoint.tests.csproj \
          --logger trx \
          --logger "console;verbosity=normal" \
          --results-directory "test-results"

    - name: Run reservation-specific tests
      run: |
        dotnet test tests/endpoint.tests/endpoint.tests.csproj \
          --filter "StockReservationTests" \
          --logger "console;verbosity=detailed"
```

## ?? Enhanced Test Coverage Goals

### **Current Enhanced Coverage: 51 Tests** ??

- ? **?? Stock Reservations**: Complete Saga pattern workflow coverage
- ? **?? Compensation Logic**: OrderCancelledEvent and rollback testing
- ? **?? Concurrency Control**: Race condition prevention validation
- ? **?? Payment Simulation**: Realistic failure scenarios
- ? **Authentication**: Complete JWT workflow coverage
- ? **Authorization**: Role-based access control validation
- ? **CRUD Operations**: Full business logic testing
- ? **Service Integration**: Cross-service communication
- ? **Event-Driven Architecture**: End-to-end event processing
- ? **Error Handling**: Comprehensive error scenario testing
- ? **Health Monitoring**: System availability validation

### **?? Stock Reservation Testing Best Practices**

#### **Asynchronous Testing Patterns for Reservations**

```csharp
// Pattern 1: Fixed delay for reservation + event processing
await Task.Delay(15000);  // Sufficient for reservation ? event ? processing

// Pattern 2: Polling with timeout for reservation status
var timeout = TimeSpan.FromSeconds(20);
var stopwatch = Stopwatch.StartNew();
while (stopwatch.Elapsed < timeout)
{
    var reservations = await GetReservationsByOrder(orderId);
    if (reservations.All(r => r.Status == ReservationStatus.Debited))
        break;
    await Task.Delay(1000);
}

// Pattern 3: Correlation-based event monitoring
await WaitForReservationEventWithCorrelationId(correlationId, timeout);
```

#### **?? Reservation Test Isolation**

```csharp
[Fact]
public async Task ReservationTest_ShouldIsolateTestData()
{
    // Each test creates unique products to prevent interference
    var product = await CreateTestProduct($"ReservationProduct-{Guid.NewGuid()}");
    
    // Use unique order IDs for reservations
    var orderId = Guid.NewGuid();
    
    // Use unique correlation IDs for tracing
    var correlationId = $"test-{Guid.NewGuid()}";
    
    // Verify complete isolation - tests run in parallel safely
}
```

#### **?? Stock Reservation Verification Strategies**

```csharp
// Strategy 1: State verification (most reliable)
var finalProduct = await GetProduct(productId);
var reservations = await GetReservationsByOrder(orderId);
Assert.Equal(expectedStock, finalProduct.StockQuantity);
Assert.All(reservations, r => Assert.Equal(expectedStatus, r.Status));

// Strategy 2: Event audit verification  
var processedEvents = await GetProcessedEvents(orderId);
Assert.Contains(processedEvents, e => e.EventType == "OrderConfirmedEvent");
Assert.Contains(processedEvents, e => e.EventType == "OrderCancelledEvent");

// Strategy 3: Correlation tracking
Assert.NotNull(orderResponse.CorrelationId);
await VerifyReservationProcessedWithCorrelation(orderResponse.CorrelationId);

// Strategy 4: Compensation verification
var cancelledEvents = await GetCancelledEvents(orderId);
Assert.True(cancelledEvents.Any(e => e.CancellationReason.Contains("Payment")));
```

This comprehensive testing approach ensures the stock reservation system with Saga pattern is robust, reliable, and maintainable in production environments.