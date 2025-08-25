# Integration Tests Documentation

## Overview

This document provides comprehensive documentation for all integration test suites in the SalesAPI solution. Integration tests validate end-to-end functionality, cross-service communication, and complete business workflows.

## Test Architecture

### Test Environment
- **Containerized Services**: Tests run against Docker containers
- **Real Dependencies**: Uses actual RabbitMQ, databases, and HTTP communication
- **Network Communication**: Tests real HTTP calls between services
- **Event Processing**: Validates actual message publishing and consumption

### Service Endpoints
| Service | Port | Base URL |
|---------|------|----------|
| Gateway | 6000 | http://localhost:6000 |
| Inventory API | 5000 | http://localhost:5000 |
| Sales API | 5001 | http://localhost:5001 |

## Test Suites (`endpoint.tests`)

### 1. Authentication Tests (`AuthenticationTests.cs`)

**Purpose**: Validates JWT authentication and authorization across all services.

#### Test Categories
- **Token Generation**: Tests JWT token creation
- **Authorization Flows**: Validates role-based access control
- **Token Validation**: Tests token expiration and validation
- **Cross-Service Authentication**: Validates token propagation

#### Key Test Cases

```csharp
[Fact]
public async Task Login_WithValidCredentials_ShouldReturnToken()
```
- **Purpose**: Tests successful authentication flow
- **Setup**: Valid username/password credentials
- **Actions**: POST to auth/token endpoint
- **Assertions**: 
  - HTTP 200 OK response
  - Valid JWT token returned
  - Token contains expected claims

```csharp
[Fact]
public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
```
- **Purpose**: Tests authentication failure handling
- **Setup**: Invalid credentials
- **Actions**: POST to auth/token endpoint
- **Assertions**: HTTP 401 Unauthorized response

```csharp
[Theory]
[InlineData("admin", "admin123", true)]
[InlineData("customer1", "password123", true)]
[InlineData("invalid", "wrong", false)]
public async Task Login_WithVariousCredentials_ShouldHandleAppropriately(string username, string password, bool shouldSucceed)
```
- **Purpose**: Tests multiple authentication scenarios
- **Test Data**: Various credential combinations
- **Assertions**: Appropriate responses for valid/invalid credentials

```csharp
[Fact]
public async Task AccessProtectedEndpoint_WithValidToken_ShouldSucceed()
```
- **Purpose**: Tests protected endpoint access with valid token
- **Setup**: Authenticated user token
- **Actions**: Access protected resource with Authorization header
- **Assertions**: Successful access to protected resource

```csharp
[Fact]
public async Task AccessProtectedEndpoint_WithoutToken_ShouldReturnUnauthorized()
```
- **Purpose**: Tests protected endpoint access without authentication
- **Actions**: Access protected resource without token
- **Assertions**: HTTP 401 Unauthorized response

```csharp
[Fact]
public async Task AccessAdminEndpoint_WithCustomerToken_ShouldReturnForbidden()
```
- **Purpose**: Tests role-based authorization
- **Setup**: Customer-level token
- **Actions**: Attempt to access admin-only endpoint
- **Assertions**: HTTP 403 Forbidden response

### 2. Product CRUD Tests (`ProductCrudTests.cs`)

**Purpose**: Tests complete product lifecycle operations through the API.

#### Test Categories
- **Create Operations**: Product creation with validation
- **Read Operations**: Product retrieval and querying
- **Update Operations**: Product modification
- **Delete Operations**: Product removal
- **Search & Filtering**: Product query capabilities

#### Key Test Cases

```csharp
[Fact]
public async Task CreateProduct_WithValidData_ShouldReturnCreatedProduct()
```
- **Purpose**: Tests successful product creation
- **Setup**: Valid product data and admin authentication
- **Actions**: POST to inventory/products endpoint
- **Assertions**:
  - HTTP 201 Created response
  - Product returned with generated ID
  - All properties persisted correctly

```csharp
[Fact]
public async Task CreateProduct_WithInvalidData_ShouldReturnValidationError()
```
- **Purpose**: Tests product creation validation
- **Setup**: Invalid product data (missing name, negative price)
- **Actions**: POST to inventory/products endpoint
- **Assertions**: 
  - HTTP 400 Bad Request response
  - Validation error messages returned

```csharp
[Fact]
public async Task GetProduct_WithValidId_ShouldReturnProduct()
```
- **Purpose**: Tests product retrieval by ID
- **Setup**: Existing product in database
- **Actions**: GET to inventory/products/{id}
- **Assertions**:
  - HTTP 200 OK response
  - Correct product data returned

```csharp
[Fact]
public async Task GetProduct_WithInvalidId_ShouldReturnNotFound()
```
- **Purpose**: Tests product retrieval with non-existent ID
- **Setup**: Random/non-existent product ID
- **Actions**: GET to inventory/products/{id}
- **Assertions**: HTTP 404 Not Found response

```csharp
[Fact]
public async Task UpdateProduct_WithValidData_ShouldUpdateSuccessfully()
```
- **Purpose**: Tests product update functionality
- **Setup**: Existing product and valid update data
- **Actions**: PUT to inventory/products/{id}
- **Assertions**:
  - HTTP 200 OK response
  - Product properties updated correctly
  - Changes persisted to database

```csharp
[Fact]
public async Task DeleteProduct_WithValidId_ShouldDeleteSuccessfully()
```
- **Purpose**: Tests product deletion
- **Setup**: Existing product
- **Actions**: DELETE to inventory/products/{id}
- **Assertions**:
  - HTTP 204 No Content response
  - Product no longer exists in database

```csharp
[Fact]
public async Task GetProducts_WithPagination_ShouldReturnPagedResults()
```
- **Purpose**: Tests product listing with pagination
- **Setup**: Multiple products in database
- **Actions**: GET to inventory/products?page=1&pageSize=10
- **Assertions**:
  - HTTP 200 OK response
  - Correct number of products returned
  - Pagination metadata included

### 3. Order CRUD Tests (`OrderCrudTests.cs`)

**Purpose**: Tests complete order lifecycle and business workflows.

#### Test Categories
- **Order Creation**: Order placement with stock validation
- **Order Retrieval**: Order querying and details
- **Order Processing**: Status transitions and workflows
- **Business Rules**: Order validation and constraints

#### Key Test Cases

```csharp
[Fact]
public async Task CreateOrder_WithValidProducts_ShouldCreateSuccessfully()
```
- **Purpose**: Tests successful order creation workflow
- **Setup**: 
  - Authenticated customer
  - Products with sufficient stock
  - Valid order request
- **Actions**: POST to sales/orders
- **Assertions**:
  - HTTP 201 Created response
  - Order created with Confirmed status
  - Stock reservations created
  - Order total calculated correctly

```csharp
[Fact]
public async Task CreateOrder_WithInsufficientStock_ShouldReturnError()
```
- **Purpose**: Tests order creation with stock validation
- **Setup**: 
  - Product with limited stock
  - Order quantity exceeding available stock
- **Actions**: POST to sales/orders
- **Assertions**:
  - HTTP 422 Unprocessable Entity response
  - Appropriate error message about insufficient stock
  - No order created

```csharp
[Fact]
public async Task CreateOrder_WithoutAuthentication_ShouldReturnUnauthorized()
```
- **Purpose**: Tests order creation authorization
- **Setup**: Valid order data without authentication token
- **Actions**: POST to sales/orders without Authorization header
- **Assertions**: HTTP 401 Unauthorized response

```csharp
[Fact]
public async Task GetOrder_WithValidId_ShouldReturnOrderDetails()
```
- **Purpose**: Tests order retrieval functionality
- **Setup**: Existing order in database
- **Actions**: GET to sales/orders/{id}
- **Assertions**:
  - HTTP 200 OK response
  - Complete order details returned
  - Order items included

```csharp
[Fact]
public async Task GetOrders_ShouldReturnOrderList()
```
- **Purpose**: Tests order listing functionality
- **Actions**: GET to sales/orders
- **Assertions**:
  - HTTP 200 OK response
  - List of orders returned
  - Orders sorted by creation date (descending)

### 4. Stock Reservation Tests (`StockReservationTests.cs`)

**Purpose**: Tests the advanced stock reservation system implementing the Saga pattern.

#### Test Categories
- **Reservation Creation**: Synchronous stock reservation
- **Payment Simulation**: Payment processing workflows
- **Event Processing**: Asynchronous reservation processing
- **Concurrency Control**: Race condition prevention
- **Compensation Logic**: Failure recovery mechanisms

#### Key Test Cases

```csharp
[Fact]
public async Task CreateOrderWithReservation_ShouldProcessSuccessfully()
```
- **Purpose**: Tests complete reservation-to-order workflow
- **Setup**: Product with sufficient stock
- **Test Flow**:
  1. Create product with 100 units
  2. Create order for 15 units
  3. Verify order confirmation and reservation creation
  4. Wait for event processing
  5. Verify stock deduction and reservation status update
- **Assertions**:
  - Order created with Confirmed status
  - Stock reservations created synchronously
  - OrderConfirmedEvent processed
  - Reservations converted from Reserved to Debited
  - Final stock reflects order quantity deduction (100 ? 85)

```csharp
[Fact]
public async Task CreateOrderWithPaymentFailure_ShouldReleaseReservations()
```
- **Purpose**: Tests compensation logic for payment failures
- **Setup**: Expensive product to trigger payment failure
- **Test Flow**:
  1. Create high-priced product ($2000)
  2. Attempt order creation (triggers payment failure)
  3. Verify order creation failure
  4. Wait for compensation event processing
  5. Verify stock consistency
- **Assertions**:
  - Payment failure detected
  - OrderCancelledEvent published
  - Stock reservations released
  - Stock quantity unchanged from original level

```csharp
[Fact]
public async Task ConcurrentOrderCreation_ShouldPreventOverselling()
```
- **Purpose**: Tests race condition prevention in concurrent scenarios
- **Setup**: Product with limited stock (20 units)
- **Test Flow**:
  1. Launch 4 concurrent orders of 6 units each
  2. Monitor order success/failure
  3. Wait for event processing
  4. Verify final stock consistency
- **Assertions**:
  - Maximum 3 orders succeed (3 × 6 = 18 ? 20)
  - At least 1 order succeeds
  - Stock deduction matches successful orders
  - No overselling occurs

```csharp
[Fact]
public async Task StockReservationApi_ShouldWorkCorrectly()
```
- **Purpose**: Tests direct stock reservation API functionality
- **Test Flow**:
  1. Create product with stock
  2. Create stock reservation directly via API
  3. Verify reservation details and status
  4. Query reservations by order ID
  5. Query specific reservation by ID
- **Assertions**:
  - Reservation created successfully
  - Reservation details accurate
  - Query operations work correctly
  - Audit trail maintained

### 5. Event-Driven Tests (`EventDrivenTests.cs`)

**Purpose**: Tests the complete event-driven architecture with real RabbitMQ integration.

#### Test Categories
- **Event Publishing**: OrderConfirmedEvent publication
- **Event Consumption**: Automatic event processing
- **Stock Processing**: Event-driven stock deduction
- **Correlation Tracking**: End-to-end correlation IDs
- **Idempotency**: Duplicate event handling

#### Key Test Cases

```csharp
[Fact]
public async Task CreateOrder_ShouldPublishEventAndDebitStock()
```
- **Purpose**: Tests complete event-driven stock deduction
- **Test Flow**:
  1. Create product with 100 units
  2. Create order for 5 units
  3. Wait for event processing
  4. Verify automatic stock deduction
- **Assertions**:
  - Order created successfully
  - OrderConfirmedEvent published to RabbitMQ
  - Event consumed by Inventory API
  - Stock automatically debited (100 ? 95)
  - Correlation ID maintained throughout

```csharp
[Fact]
public async Task CreateMultipleOrders_ShouldProcessAllEventsCorrectly()
```
- **Purpose**: Tests multiple concurrent event processing
- **Setup**: Multiple orders created simultaneously
- **Assertions**:
  - All events processed correctly
  - Stock deductions accurate for all orders
  - No event loss or duplication

```csharp
[Fact]
public async Task EventPublishing_ShouldMaintainCorrelationIds()
```
- **Purpose**: Tests correlation ID propagation through events
- **Setup**: Order creation with specific correlation ID
- **Assertions**:
  - Correlation ID present in published events
  - End-to-end tracing possible through correlation ID

### 6. Gateway Tests (`GatewayApiTests.cs`, `GatewayRoutingTests.cs`)

**Purpose**: Tests the API Gateway functionality and routing capabilities.

#### Gateway API Tests

```csharp
[Fact]
public async Task HealthCheck_ShouldReturnOk()
```
- **Purpose**: Tests gateway health endpoint
- **Actions**: GET to /health
- **Assertions**: HTTP 200 OK with "Healthy" response

```csharp
[Fact]
public async Task GatewayStatus_ShouldReturnStatusInformation()
```
- **Purpose**: Tests gateway status endpoint
- **Actions**: GET to /gateway/status
- **Assertions**:
  - HTTP 200 OK response
  - Status information includes "SalesAPI Gateway"
  - Health status included

```csharp
[Fact]
public async Task GatewayRoutes_ShouldReturnRoutingInformation()
```
- **Purpose**: Tests gateway route information endpoint
- **Actions**: GET to /gateway/routes
- **Assertions**:
  - HTTP 200 OK response
  - Route information includes Inventory API and Sales API
  - Route patterns displayed correctly

#### Gateway Routing Tests

```csharp
[Fact]
public async Task InventoryRoute_Products_ShouldRouteToInventoryApi()
```
- **Purpose**: Tests inventory route forwarding
- **Actions**: GET to /inventory/products via gateway
- **Assertions**: Request successfully routed to Inventory API

```csharp
[Fact]
public async Task SalesRoute_Orders_ShouldRouteToSalesApi()
```
- **Purpose**: Tests sales route forwarding
- **Actions**: GET to /sales/orders via gateway
- **Assertions**: Request successfully routed to Sales API

```csharp
[Fact]
public async Task NonExistentRoute_ShouldReturnNotFound()
```
- **Purpose**: Tests gateway handling of invalid routes
- **Actions**: GET to non-existent endpoint
- **Assertions**: HTTP 404 Not Found from gateway (YARP level)

### 7. Diagnostic Tests (`DiagnosticTests.cs`)

**Purpose**: Tests system monitoring, health checks, and observability features.

#### Key Test Cases

```csharp
[Fact]
public async Task AllServices_HealthChecks_ShouldReturnHealthy()
```
- **Purpose**: Tests health endpoints across all services
- **Actions**: GET to health endpoints for each service
- **Assertions**: All services report healthy status

```csharp
[Fact]
public async Task AllServices_Metrics_ShouldBeAccessible()
```
- **Purpose**: Tests Prometheus metrics endpoints
- **Actions**: GET to /metrics on each service
- **Assertions**: Metrics data accessible and properly formatted

```csharp
[Fact]
public async Task CorrelationId_ShouldPropagateAcrossServices()
```
- **Purpose**: Tests correlation ID propagation
- **Setup**: Request with custom correlation ID
- **Actions**: Cross-service operation
- **Assertions**: Correlation ID maintained throughout request chain

## Test Infrastructure

### Test Setup and Teardown

#### Base Test Classes
```csharp
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected HttpClient GatewayClient { get; private set; }
    protected HttpClient InventoryClient { get; private set; }
    protected HttpClient SalesClient { get; private set; }

    public async Task InitializeAsync()
    {
        // Setup HTTP clients for each service
        GatewayClient = new HttpClient { BaseAddress = new Uri("http://localhost:6000/") };
        InventoryClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
        SalesClient = new HttpClient { BaseAddress = new Uri("http://localhost:5001/") };
        
        // Wait for services to be ready
        await WaitForServicesAsync();
    }

    public async Task DisposeAsync()
    {
        GatewayClient?.Dispose();
        InventoryClient?.Dispose();
        SalesClient?.Dispose();
    }
}
```

#### Authentication Helper Methods
```csharp
protected async Task<string> GetAuthTokenAsync(string username, string password)
{
    var loginRequest = new { Username = username, Password = password };
    var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
    
    var response = await GatewayClient.PostAsync("auth/token", content);
    
    if (response.IsSuccessStatusCode)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var tokenData = JsonDocument.Parse(responseContent);
        return tokenData.RootElement.GetProperty("accessToken").GetString();
    }
    
    return null;
}
```

#### HTTP Client Extensions
```csharp
protected async Task<T> PostWithTokenAsync<T>(string endpoint, object data, string token)
{
    var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
    var client = GetClientForEndpoint(endpoint);
    
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    
    var response = await client.PostAsync(endpoint, content);
    response.EnsureSuccessStatusCode();
    
    var responseContent = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<T>(responseContent);
}
```

### Test Data Management

#### Unique Test Data
Each test creates unique data to prevent interference:

```csharp
protected async Task<ProductDto> CreateTestProductAsync(string name = null)
{
    name ??= $"Test Product {Guid.NewGuid()}";
    
    var productRequest = new
    {
        name = name,
        description = "Test Description",
        price = 99.99m,
        stockQuantity = 100
    };
    
    return await PostWithTokenAsync<ProductDto>("inventory/products", productRequest, await GetAdminTokenAsync());
}
```

#### Test Isolation
- Each test runs independently
- No shared state between tests
- Unique identifiers for all test entities
- Proper cleanup after test completion

### Retry and Resilience

#### Service Readiness Checks
```csharp
private async Task WaitForServicesAsync()
{
    var services = new[]
    {
        ("Gateway", GatewayClient, "health"),
        ("Inventory", InventoryClient, "health"),
        ("Sales", SalesClient, "health")
    };

    foreach (var (name, client, endpoint) in services)
    {
        await WaitForServiceAsync(name, client, endpoint);
    }
}

private async Task WaitForServiceAsync(string serviceName, HttpClient client, string endpoint)
{
    const int maxAttempts = 30;
    const int delayMs = 1000;

    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            var response = await client.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                return;
            }
        }
        catch (Exception)
        {
            // Service not ready yet
        }

        if (attempt < maxAttempts)
        {
            await Task.Delay(delayMs);
        }
    }

    throw new InvalidOperationException($"Service {serviceName} did not become ready within the expected time");
}
```

## Performance Considerations

### Test Execution Optimization
- **Parallel Execution**: Tests run in parallel where possible
- **Resource Sharing**: Reuse HTTP clients across tests
- **Efficient Waits**: Smart polling for service readiness
- **Cleanup**: Minimal but effective cleanup procedures

### Timing Considerations
- **Event Processing**: Allow sufficient time for async event processing
- **Service Startup**: Account for container startup time
- **Network Latency**: Buffer for HTTP communication delays

## Troubleshooting

### Common Issues

#### Service Not Ready
```csharp
// Issue: Test fails because service hasn't started
// Solution: Implement proper service readiness checks
await WaitForServicesAsync();
```

#### Authentication Failures
```csharp
// Issue: Authentication token expired or invalid
// Solution: Always get fresh tokens for each test
var token = await GetAuthTokenAsync("admin", "admin123");
```

#### Race Conditions
```csharp
// Issue: Events not processed before test verification
// Solution: Implement proper waiting mechanisms
await Task.Delay(5000); // Wait for event processing
```

#### Port Conflicts
```csharp
// Issue: Services running on unexpected ports
// Solution: Verify docker-compose configuration and port mappings
```

---

*Last Updated: January 2025*
*Version: 1.0.0*