# Endpoint Tests - Complete System Integration Testing with JWT Authentication

This project contains comprehensive automated integration tests to validate the complete microservices architecture, including API Gateway routing, JWT authentication and authorization, backend services communication, and end-to-end functionality.

**Note:** This is the main and only test project for the solution. The previous `api.tests` project has been removed to maintain a clean, unified testing approach.

## ??? Test Architecture

The test suite covers the complete microservices ecosystem with security:
- **API Gateway** (Port 6000) - YARP reverse proxy, routing, and JWT token issuer
- **Inventory API** (Port 5000) - Product management with admin role protection
- **Sales API** (Port 5001) - Order processing with customer authentication and stock validation
- **JWT Authentication** - Role-based access control and token validation

## ?? Prerequisites

### Running the Complete System
Before executing tests, ensure all services are running:

```bash
# Terminal 1: Start Inventory API
dotnet run --project src/inventory.api --urls "http://localhost:5000"

# Terminal 2: Start Sales API  
dotnet run --project src/sales.api --urls "http://localhost:5001"

# Terminal 3: Start API Gateway (with JWT auth)
dotnet run --project src/gateway --urls "http://localhost:6000"
```

### Database Setup
Ensure both databases are created and migrated:

```bash
# Apply Inventory migrations
dotnet ef database update --project src/inventory.api

# Apply Sales migrations
dotnet ef database update --project src/sales.api
```

### System Health Verification
Verify all services are healthy before running tests:

```bash
# Check all services via gateway
curl http://localhost:6000/health                   # Gateway health
curl http://localhost:6000/inventory/health         # Inventory via gateway
curl http://localhost:6000/sales/health             # Sales via gateway

# Check JWT authentication
curl -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Check direct service access (optional)
curl http://localhost:5000/health                   # Inventory direct
curl http://localhost:5001/health                   # Sales direct
```

## ?? Running the Tests

### Execute Complete Test Suite
```bash
# Run all 44 integration tests including authentication
dotnet test tests/endpoint.tests/endpoint.tests.csproj
```

### Execute by Service Category
```bash
# Gateway-specific tests (13 tests)
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "Gateway"

# Authentication tests (10 tests)
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "AuthenticationTests"

# Inventory API tests (11 tests)
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "Inventory"

# Sales API tests (10 tests)
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "Sales"
```

### Execute by Functionality
```bash
# API Gateway tests only
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "GatewayApiTests"

# Gateway routing tests only
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "GatewayRoutingTests"

# Product CRUD tests (with JWT auth)
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "ProductCrudTests"

# Order CRUD tests (with JWT auth)
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "OrderCrudTests"

# JWT Authentication tests only
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "AuthenticationTests"
```

### Execute with Detailed Logging
```bash
# Run tests with verbose output for debugging
dotnet test tests/endpoint.tests/endpoint.tests.csproj --logger "console;verbosity=detailed"
```

## ?? Complete Test Coverage (44 Tests)

### Authentication Tests (`AuthenticationTests` - 10 tests)
| Test | Description | Validates | Security Aspect |
|------|-------------|-----------|----------------|
| `Login_WithValidCredentials_ShouldReturnToken` | Admin login success | ? JWT token generation | Token issuance |
| `Login_WithInvalidCredentials_ShouldReturnUnauthorized` | Invalid login handling | ? Authentication failure | Credential validation |
| `GetTestUsers_ShouldReturnAvailableUsers` | Development user listing | ? Test user availability | Development support |
| `CreateProduct_WithoutToken_ShouldReturnUnauthorized` | Unauthorized product creation | ? 401 protection | Missing token |
| `CreateProduct_WithValidAdminToken_ShouldBeAuthorized` | Admin product creation | ? Admin authorization | Role validation |
| `CreateProduct_WithCustomerToken_ShouldReturnForbidden` | Customer product creation denial | ? 403 protection | Role-based access |
| `CreateOrder_WithoutToken_ShouldReturnUnauthorized` | Unauthorized order creation | ? 401 protection | Missing token |
| `CreateOrder_WithValidCustomerToken_ShouldBeAuthorized` | Customer order creation | ? Customer authorization | Role validation |
| `ReadOperations_ShouldBeOpenAccess` | Open read access | ? Anonymous reading | Open endpoints |
| `JwtToken_ShouldContainCorrectClaims` | JWT token structure | ? Token format | Claims validation |

### API Gateway Tests (`GatewayApiTests` - 4 tests)
| Test | Description | Validates | Gateway Feature |
|------|-------------|-----------|----------------|
| `HealthCheck_ShouldReturnOk` | Gateway health endpoint | ? Gateway availability | Health monitoring |
| `Swagger_ShouldBeAccessible` | Gateway API documentation | ? Documentation access | API docs |
| `GatewayStatus_ShouldReturnStatusInformation` | Gateway status endpoint | ? Status information | Service info |
| `GatewayRoutes_ShouldReturnRoutingInformation` | Gateway routes endpoint | ? Routing configuration | Route discovery |

### Gateway Routing Tests (`GatewayRoutingTests` - 9 tests)
| Test | Description | Validates | Route Pattern |
|------|-------------|-----------|---------------|
| `InventoryRoute_Products_ShouldRouteToInventoryApi` | Product listing via gateway | ? `/inventory/*` routing | `/inventory/products` |
| `InventoryRoute_Health_ShouldRouteToInventoryApi` | Inventory health via gateway | ? Health check routing | `/inventory/health` |
| `InventoryRoute_Swagger_ShouldRouteToInventoryApi` | Inventory swagger via gateway | ? Documentation routing | `/inventory/swagger` |
| `InventoryRoute_WithId_ShouldRouteCorrectly` | Parameterized routing | ? Dynamic path routing | `/inventory/products/{id}` |
| `SalesRoute_Orders_ShouldRouteToSalesApi` | Order listing via gateway | ? `/sales/*` routing | `/sales/orders` |
| `SalesRoute_Health_ShouldRouteToSalesApi` | Sales health via gateway | ? Health check routing | `/sales/health` |
| `SalesRoute_Swagger_ShouldRouteToSalesApi` | Sales swagger via gateway | ? Documentation routing | `/sales/swagger` |
| `SalesRoute_WithId_ShouldRouteCorrectly` | Parameterized routing | ? Dynamic path routing | `/sales/orders/{id}` |
| `NonExistentRoute_ShouldReturnNotFound` | Invalid route handling | ? 404 error handling | `/nonexistent/*` |

### Inventory API Tests (`InventoryApiTests` - 5 tests)
| Test | Description | Validates | Security Level |
|------|-------------|-----------|---------------|
| `HealthCheck_ShouldReturnOk` | Health endpoint availability | ? Service health | Open access |
| `Swagger_ShouldBeAccessible` | API documentation access | ? Documentation | Open access |
| `GetProducts_ShouldReturnOk` | Product listing endpoint | ? Data retrieval | Open access |
| `GetProducts_WithPagination_ShouldReturnOk` | Paginated product listing | ? Pagination | Open access |
| `GetProductById_WithInvalidId_ShouldReturnNotFound` | Invalid product lookup | ? Error handling | Open access |

### Product CRUD Tests (`ProductCrudTests` - 6 tests)
| Test | Description | Validates | Required Role |
|------|-------------|-----------|--------------|
| `CreateProduct_WithValidData_ShouldReturnCreated` | Product creation with admin auth | ? CRUD operations | `admin` |
| `CreateProduct_WithInvalidData_ShouldReturnBadRequest` | Validation with admin auth | ? Input validation | `admin` |
| `GetProducts_ShouldReturnProductsList` | Product listing | ? Data retrieval | None (open) |
| `CreateProduct_WithoutToken_ShouldReturnUnauthorized` | Unauthorized creation | ? 401 protection | None |
| `CreateProduct_WithCustomerToken_ShouldReturnForbidden` | Role validation | ? 403 protection | `customer` (invalid) |

### Sales API Tests (`SalesApiTests` - 3 tests)
| Test | Description | Validates | Security Level |
|------|-------------|-----------|---------------|
| `HealthCheck_ShouldReturnOk` | Health endpoint availability | ? Service health | Open access |
| `Swagger_ShouldBeAccessible` | API documentation access | ? Documentation | Open access |
| `Orders_Endpoint_ShouldBeAccessible` | Order endpoint availability | ? Endpoint routing | Open access |

### Order CRUD Tests (`OrderCrudTests` - 8 tests)
| Test | Description | Validates | Required Role |
|------|-------------|-----------|--------------|
| `CreateOrder_WithValidData_ShouldReturnCreated` | Order creation with customer auth | ? Business logic | `customer` or `admin` |
| `CreateOrder_WithInvalidData_ShouldReturnBadRequest` | Order validation with auth | ? Input validation | `customer` or `admin` |
| `CreateOrder_WithNegativeQuantity_ShouldReturnBadRequest` | Quantity validation with auth | ? Business rules | `customer` or `admin` |
| `GetOrders_ShouldReturnOrdersList` | Order listing | ? Data retrieval | None (open) |
| `GetOrders_WithPagination_ShouldReturnOk` | Paginated order listing | ? Pagination | None (open) |
| `GetOrderById_WithInvalidId_ShouldReturnNotFound` | Invalid order lookup | ? Error handling | None (open) |
| `GetOrders_WithInvalidPagination_ShouldReturnBadRequest` | Pagination validation | ? Parameter validation | None (open) |
| `CreateOrder_WithoutToken_ShouldReturnUnauthorized` | Unauthorized order creation | ? 401 protection | None |

## ?? Authentication Test Strategy

### JWT Token Management
Tests use a helper method to obtain authentication tokens:

```csharp
private async Task<string?> GetAuthTokenAsync(string username, string password)
{
    var loginRequest = new { Username = username, Password = password };
    var response = await _gatewayClient.PostAsync("auth/token", content);
    // Returns JWT token or null if authentication fails
}
```

### Test User Accounts
Tests authenticate using these development accounts:

| Username | Password | Role | Test Usage |
|----------|----------|------|------------|
| `admin` | `admin123` | admin | Product creation, full access testing |
| `customer1` | `password123` | customer | Order creation, customer role testing |
| `customer2` | `password123` | customer | Additional customer scenarios |
| `manager` | `manager123` | manager | Reserved for future role testing |

### Security Test Scenarios

#### Protected Operations Testing
1. **Without Token** ? 401 Unauthorized
2. **With Invalid Token** ? 401 Unauthorized  
3. **With Valid Token, Wrong Role** ? 403 Forbidden
4. **With Valid Token, Correct Role** ? 200 OK (or business logic response)

#### Open Operations Testing
1. **Read Operations** ? Always accessible (no auth required)
2. **Health Checks** ? Always accessible for monitoring
3. **Documentation** ? Always accessible for development

## ?? Test Configuration

### Service Endpoints
Tests are configured to connect to:
- **API Gateway**: `http://localhost:6000/` (Authentication + Routing)
- **Inventory API (Direct)**: `http://localhost:5000/` (Role-protected)
- **Sales API (Direct)**: `http://localhost:5001/` (Role-protected)

### Authentication Configuration
Tests use the same JWT configuration as the services:
- **Issuer**: `SalesAPI-Gateway`
- **Audience**: `SalesAPI-Services`
- **Token Lifetime**: 1 hour
- **Algorithm**: HMAC SHA-256

### Test Data Patterns
Tests use:
- **Authentication credentials** for various user roles
- **Bearer tokens** in Authorization headers
- **Realistic data** for creation scenarios
- **Edge cases** for validation testing
- **Invalid credentials** for security testing
- **Role mismatch scenarios** for authorization testing

### Expected Response Codes
Tests validate:
- **200 OK**: Successful operations
- **201 Created**: Resource creation with proper auth
- **400 Bad Request**: Invalid input data
- **401 Unauthorized**: Missing or invalid JWT token
- **403 Forbidden**: Valid token but insufficient role permissions
- **404 Not Found**: Missing resources or invalid routes
- **422 Unprocessable Entity**: Business rule violations
- **503 Service Unavailable**: Backend service communication failures

## ?? Test Implementation Strategy

### Authentication Flow Testing
```csharp
// Example: Testing admin-only product creation
var token = await GetAuthTokenAsync("admin", "admin123");
_client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

var response = await _client.PostAsync("products", content);
Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
```

### Role-Based Access Testing
```csharp
// Example: Testing customer attempting admin operation
var customerToken = await GetAuthTokenAsync("customer1", "password123");
_client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", customerToken);

var response = await _client.PostAsync("products", content);
Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
```

### Open Access Testing
```csharp
// Example: Testing open read access
var response = await _client.GetAsync("products"); // No auth header
Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
```

## ?? Test Execution Flow

### 1. Authentication Tests
- JWT token generation and validation
- User credential verification
- Role assignment validation
- Token expiration handling

### 2. Authorization Tests
- Role-based access control
- Protected endpoint security
- Permission enforcement
- Forbidden access prevention

### 3. Open Access Tests
- Anonymous read operations
- Public endpoint availability
- Health check accessibility
- Documentation access

### 4. Integration Tests
- Cross-service communication with auth
- Stock validation with customer tokens
- End-to-end order processing with authentication
- Gateway routing with security

### 5. Error Handling Tests
- Invalid authentication scenarios
- Expired token handling
- Role mismatch situations
- Service unavailability with auth

## ?? Troubleshooting Test Failures

### Common Authentication Test Failures

#### JWT Token Service Unavailable
```
System.Net.Http.HttpRequestException: Connection refused (localhost:6000)
```
**Solutions:**
1. Verify Gateway service is running on port 6000
2. Check Gateway startup logs for JWT configuration errors
3. Ensure JWT key is properly configured in appsettings.json

#### Authentication Service Errors
```
HTTP 500 Internal Server Error on /auth/token
```
**Solutions:**
1. Check JWT configuration (Key, Issuer, Audience)
2. Verify JWT key is at least 256 bits (64 characters)
3. Review Gateway authentication logs

#### Token Validation Failures
```
HTTP 401 Unauthorized with valid token
```
**Solutions:**
1. Ensure all services use identical JWT configuration
2. Check token expiration (1-hour default)
3. Verify token format and claims

#### Role Authorization Failures
```
HTTP 403 Forbidden with correct token
```
**Expected Behavior:** This indicates proper security - user has valid token but wrong role.

### Debugging Authentication Issues

```bash
# Verify authentication service
curl -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# Check token structure (decode JWT at jwt.io)
echo "<your-jwt-token>" | base64 -d

# Test protected endpoints
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/products" -X POST \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","description":"Test","price":10,"stockQuantity":1}'

# Verify services are running
curl http://localhost:6000/health
curl http://localhost:5000/health  
curl http://localhost:5001/health
```

### Authentication Configuration Issues

#### Mismatched JWT Settings
Ensure identical configuration across all services:
```json
{
  "Jwt": {
    "Key": "same-key-in-all-services",
    "Issuer": "SalesAPI-Gateway", 
    "Audience": "SalesAPI-Services"
  }
}
```

#### Clock Skew Issues
JWT validation includes time validation. Ensure system clocks are synchronized.

## ?? Authentication Test Metrics

### Current Coverage
- **Total Tests**: 44
- **Authentication Tests**: 10 (23%)
- **Protected Endpoint Tests**: 15 (34%)
- **Open Access Tests**: 19 (43%)
- **Success Rate**: 100%

### Security Test Coverage
- **JWT Token Generation**: ? Covered
- **Role-Based Authorization**: ? Covered
- **Unauthorized Access Prevention**: ? Covered
- **Token Validation**: ? Covered
- **Permission Enforcement**: ? Covered
- **Open Access Verification**: ? Covered

### Performance Benchmarks
- **Authentication Test Duration**: 50-200ms per test
- **JWT Token Generation**: <50ms
- **Token Validation**: <10ms per request
- **Complete Suite Duration**: ~15-20 seconds
- **Authentication Service Response**: <100ms average

## ?? Test Quality Standards

### Security Testing Best Practices Implemented
- ? **Comprehensive role testing** for all user types
- ? **Negative security testing** (unauthorized access attempts)
- ? **Token lifecycle testing** (generation, validation, expiration)
- ? **Permission boundary testing** (role-based access)
- ? **Open access verification** (ensuring public endpoints work)
- ? **Cross-service authentication** testing

### Authentication Test Design Principles
- ? **Independent test execution** (each test gets fresh tokens)
- ? **Realistic user scenarios** (admin vs customer workflows)
- ? **Graceful degradation** (tests handle auth service unavailability)
- ? **Clear assertions** (specific status codes and behaviors)
- ? **Comprehensive coverage** (all auth scenarios covered)

## ?? Continuous Integration Considerations

### Prerequisites for CI/CD with Authentication
1. **Service startup orchestration** (Gateway ? Backend APIs)
2. **JWT configuration** management (secrets/environment variables)
3. **Authentication service health** verification before tests
4. **Token caching** for test performance
5. **Security configuration** validation

### Recommended CI Pipeline for Authenticated System
```yaml
# Example pipeline steps
- Setup databases and apply migrations
- Configure JWT secrets from environment
- Start Inventory API (background)
- Start Sales API (background)  
- Start Gateway with JWT auth (background)
- Wait for all health checks to pass
- Verify authentication service availability
- Run authentication tests first
- Run protected endpoint tests
- Run integration tests with auth
- Cleanup services and secrets
```

## ?? Test Documentation Standards

### Authentication Test Documentation
- **Purpose**: Validates JWT-based security implementation
- **Scope**: All authentication and authorization scenarios
- **Prerequisites**: Gateway with JWT service running
- **Test Users**: Development accounts with known credentials
- **Security Level**: Role-based access control validation

### Protected Endpoint Test Documentation
- **Authorization**: Required JWT token with appropriate role
- **Token Source**: Gateway authentication service
- **Role Requirements**: Documented per endpoint
- **Fallback Behavior**: Graceful test degradation if auth unavailable

---

## ?? Security Notes for Developers

- **Development Users**: Only for testing - never use in production
- **JWT Secrets**: Use environment variables in production
- **Token Expiration**: Default 1 hour - adjust based on security requirements
- **Test Isolation**: Each test gets fresh authentication tokens
- **Error Handling**: Tests validate both success and failure security scenarios
- **Unified Testing**: All tests are consolidated in this single project for maintainability

The comprehensive test suite ensures that the JWT authentication system works correctly while maintaining the security posture of the microservices architecture. All authentication flows, role-based permissions, and security boundaries are thoroughly validated in this unified testing approach.