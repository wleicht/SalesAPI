# SalesAPI - Complete Microservices Architecture with JWT Authentication

A comprehensive microservices-based e-commerce solution built with .NET 8, featuring inventory management, sales processing, API Gateway with YARP reverse proxy, JWT authentication with role-based authorization, HTTP-based stock validation, and comprehensive automated testing.

## ??? Architecture Overview

This solution implements a complete microservices architecture with the following components:

- **API Gateway** - Unified entry point using YARP reverse proxy with JWT token issuer (Port 6000)
- **Inventory API** - Product catalog management with role-based authorization (Port 5000)
- **Sales API** - Order processing with customer authentication and real-time inventory validation (Port 5001)
- **Building Blocks Contracts** - Shared DTOs and contracts between services
- **Automated Tests** - Comprehensive endpoint, integration, authentication, and routing testing

## ?? Project Structure

```
SalesAPI/
??? src/
?   ??? gateway/                # API Gateway with YARP reverse proxy and JWT auth
?   ??? inventory.api/          # Inventory microservice with admin protection
?   ??? sales.api/              # Sales microservice with customer authentication
?   ??? buildingblocks.contracts/  # Shared contracts, DTOs, and auth models
??? tests/
?   ??? endpoint.tests/         # Unified integration tests for all APIs and auth
??? deploy/
?   ??? Dockerfile.sqlserver    # SQL Server container configuration
??? README.md
```

## ?? Service Communication Flow

```
Client Request ? API Gateway (6000) ? JWT Authentication ? Inventory API (5000)
                                   ?                   ? Sales API (5001)
                                   ?
                             Token Validation
                                   ?
Sales API ? HTTP Client ? Inventory API (Stock Validation)
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

## ?? Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (or Docker for containerized SQL Server)
- [Git](https://git-scm.com/)

### Clone the Repository

```bash
git clone https://github.com/wleicht/SalesAPI.git
cd SalesAPI
```

### Database Setup

#### Option 1: Local SQL Server

If you have SQL Server installed locally, update connection strings in both APIs:

**Inventory API** (`src/inventory.api/appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InventoryDb;Integrated Security=true;TrustServerCertificate=True"
  }
}
```

**Sales API** (`src/sales.api/appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SalesDb;Integrated Security=true;TrustServerCertificate=True"
  }
}
```

#### Option 2: Docker SQL Server

Use the provided Dockerfile to run SQL Server in a container:

```bash
# Run SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" -p 1433:1433 --name sqlserver-dev -d mcr.microsoft.com/mssql/server:2022-latest
```

## ????? Running the Complete System

### 1. Restore Dependencies

```bash
# Restore all projects
dotnet restore
```

### 2. Build the Solution

```bash
# Build all projects
dotnet build
```

### 3. Apply Database Migrations

```bash
# Apply database migrations for Inventory API
dotnet ef database update --project src/inventory.api

# Apply database migrations for Sales API
dotnet ef database update --project src/sales.api
```

### 4. Start All Services

You can start services individually or use the provided scripts:

#### Option A: Individual Services (Recommended for Development)
```bash
# Terminal 1: Start Inventory API
dotnet run --project src/inventory.api --urls "http://localhost:5000"

# Terminal 2: Start Sales API  
dotnet run --project src/sales.api --urls "http://localhost:5001"

# Terminal 3: Start API Gateway
dotnet run --project src/gateway --urls "http://localhost:6000"
```

#### Option B: Background Services (Quick Testing)
```bash
# Start all services in background (Windows PowerShell)
Start-Process powershell -ArgumentList "-Command", "dotnet run --project src/inventory.api --urls http://localhost:5000" -WindowStyle Hidden
Start-Process powershell -ArgumentList "-Command", "dotnet run --project src/sales.api --urls http://localhost:5001" -WindowStyle Hidden
Start-Process powershell -ArgumentList "-Command", "dotnet run --project src/gateway --urls http://localhost:6000" -WindowStyle Hidden
```

**Important:** All three services must be running for the complete system to function properly.

## ?? Authentication Workflow

### 1. Obtain JWT Token

```bash
# Login as admin
curl -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }'

# Response:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "role": "admin",
  "username": "admin"
}
```

### 2. Use Token for Protected Operations

```bash
# Create product (requires admin role)
curl -X POST "http://localhost:5000/products" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop",
    "description": "High-performance gaming laptop",
    "price": 1299.99,
    "stockQuantity": 50
  }'

# Create order (requires customer or admin role)
curl -X POST "http://localhost:5001/orders" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "items": [
      {
        "productId": "product-guid-from-inventory",
        "quantity": 2
      }
    ]
  }'
```

### 3. Access Open Endpoints (No Authentication Required)

```bash
# Read products (open access)
curl "http://localhost:5000/products"

# Read orders (open access)
curl "http://localhost:5001/orders"

# Health checks
curl "http://localhost:6000/health"
curl "http://localhost:5000/health"
curl "http://localhost:5001/health"
```

## ?? API Documentation

### Authentication Endpoints (Gateway - Port 6000)

| Method | Endpoint | Description | Authentication | Response |
|--------|----------|-------------|---------------|----------|
| `POST` | `/auth/token` | Generate JWT token | None | JWT token + user info |
| `GET` | `/auth/test-users` | Get available test users | None | List of test users |
| `GET` | `/health` | Gateway health check | None | Health status |
| `GET` | `/swagger` | Gateway API documentation | None | Swagger UI |

### Inventory API Endpoints (Via Gateway: `/inventory/*`)

| Method | Endpoint | Description | Authentication | Required Role |
|--------|----------|-------------|---------------|--------------|
| `GET` | `/products` | Get paginated products | None | Open access |
| `GET` | `/products/{id}` | Get product by ID | None | Open access |
| `POST` | `/products` | Create a new product | Required | `admin` |
| `PUT` | `/products/{id}` | Update existing product | Required | `admin` |
| `DELETE` | `/products/{id}` | Delete product | Required | `admin` |
| `GET` | `/health` | Health check | None | Open access |

### Sales API Endpoints (Via Gateway: `/sales/*`)

| Method | Endpoint | Description | Authentication | Required Role |
|--------|----------|-------------|---------------|--------------|
| `GET` | `/orders` | Get paginated orders | None | Open access |
| `GET` | `/orders/{id}` | Get order by ID | None | Open access |
| `POST` | `/orders` | Create order with stock validation | Required | `customer` or `admin` |
| `GET` | `/health` | Health check | None | Open access |

## ?? API Usage Examples

### Authentication Examples

#### Get Available Test Users
```bash
curl "http://localhost:6000/auth/test-users"
```

#### Login as Admin
```bash
curl -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }'
```

#### Login as Customer
```bash
curl -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "customer1",
    "password": "password123"
  }'
```

### Protected Operations Examples

#### Create Product (Admin Only)
```bash
curl -X POST "http://localhost:6000/inventory/products" \
  -H "Authorization: Bearer <admin-jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop",
    "description": "High-performance gaming laptop with RTX graphics",
    "price": 1299.99,
    "stockQuantity": 50
  }'
```

#### Create Order (Customer or Admin)
```bash
curl -X POST "http://localhost:6000/sales/orders" \
  -H "Authorization: Bearer <customer-jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "items": [
      {
        "productId": "product-guid-from-inventory",
        "quantity": 2
      }
    ]
  }'
```

### Open Access Examples

#### Get Products (No Authentication)
```bash
curl "http://localhost:6000/inventory/products?page=1&pageSize=10"
```

#### Get Orders (No Authentication)
```bash
curl "http://localhost:6000/sales/orders?page=1&pageSize=10"
```

## ?? Running Tests

The project includes comprehensive automated tests covering all APIs, authentication, and routing functionality.

### Run All Tests
```bash
dotnet test tests/endpoint.tests/endpoint.tests.csproj
```

### Run Specific Test Categories
```bash
# Authentication tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "AuthenticationTests"

# Gateway-specific tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "Gateway"

# Inventory API tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "InventoryApiTests"

# Sales API tests  
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "SalesApiTests"

# Product CRUD tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "ProductCrudTests"

# Order CRUD tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "OrderCrudTests"

# Gateway routing tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "GatewayRoutingTests"
```

### Current Test Coverage (44 Tests)

The project uses a **unified testing approach** with a single test project (`endpoint.tests`) containing comprehensive coverage:

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

## ?? Configuration

### JWT Configuration

All services use the same JWT configuration for token validation:

```json
{
  "Jwt": {
    "Key": "super-secret-key-for-jwt-token-generation-minimum-256-bits-required-for-security",
    "Issuer": "SalesAPI-Gateway",
    "Audience": "SalesAPI-Services",
    "ExpirationHours": 1
  }
}
```

### Connection Strings

**Inventory API** (`src/inventory.api/appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InventoryDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "super-secret-key-for-jwt-token-generation-minimum-256-bits-required-for-security",
    "Issuer": "SalesAPI-Gateway",
    "Audience": "SalesAPI-Services"
  }
}
```

**Sales API** (`src/sales.api/appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SalesDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"
  },
  "InventoryApi": {
    "BaseUrl": "http://localhost:5000/"
  },
  "Jwt": {
    "Key": "super-secret-key-for-jwt-token-generation-minimum-256-bits-required-for-security",
    "Issuer": "SalesAPI-Gateway",
    "Audience": "SalesAPI-Services"
  }
}
```

**API Gateway** (`src/gateway/appsettings.json`):
```json
{
  "Jwt": {
    "Key": "super-secret-key-for-jwt-token-generation-minimum-256-bits-required-for-security",
    "Issuer": "SalesAPI-Gateway",
    "Audience": "SalesAPI-Services",
    "ExpirationHours": 1
  },
  "ReverseProxy": {
    "Routes": {
      "inventory-route": {
        "ClusterId": "inventory-cluster",
        "Match": { "Path": "/inventory/{**catch-all}" },
        "Transforms": [{ "PathPattern": "/{**catch-all}" }]
      },
      "sales-route": {
        "ClusterId": "sales-cluster",
        "Match": { "Path": "/sales/{**catch-all}" },
        "Transforms": [{ "PathPattern": "/{**catch-all}" }]
      }
    }
  }
}
```

### Environment Variables

Override configuration using environment variables:

```bash
# Database connections
export ConnectionStrings__DefaultConnection="Server=your-server;Database=InventoryDb;..."

# JWT configuration
export Jwt__Key="your-secret-key"
export Jwt__Issuer="your-issuer"
export Jwt__Audience="your-audience"

# Service URLs
export InventoryApi__BaseUrl="http://your-inventory-api-url/"
```

## ?? Data Models

### Authentication Models
```csharp
public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public class LoginResponse
{
    public required string AccessToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public required string Role { get; set; }
    public required string Username { get; set; }
}
```

### Product Model (Inventory)
```csharp
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Order Models (Sales)
```csharp
public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Status { get; set; } // Pending, Confirmed, Cancelled
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<OrderItem> Items { get; set; }
}

public class OrderItem
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

## ??? Security & Validation

### JWT Token Security
- **Algorithm**: HMAC SHA-256
- **Expiration**: 1 hour
- **Claims**: Username, Role, JWT ID, Issue time
- **Validation**: Issuer, Audience, Lifetime, Signing key

### Input Validation Rules

#### Product Validation (Inventory API)
- **Name**: Required, max 100 characters
- **Description**: Required, max 500 characters  
- **Price**: Must be ? 0
- **StockQuantity**: Must be ? 0

#### Order Validation (Sales API)
- **CustomerId**: Required, valid GUID
- **Items**: Required, at least 1 item
- **ProductId**: Required, valid GUID
- **Quantity**: Must be ? 1

#### Authentication Validation
- **Username**: Required, max 50 characters
- **Password**: Required, 6-100 characters

## ?? Business Logic

### Order Processing Flow with Authentication
1. **Authenticate user** via `POST /auth/token`
2. **Receive order request** via `POST /sales/orders` with Bearer token
3. **Validate JWT token** and check customer/admin role
4. **Validate order data** using Data Annotations
5. **For each order item:**
   - Fetch product details from Inventory API
   - Validate product exists
   - Check sufficient stock quantity
   - Freeze unit price at current market price
6. **Calculate total amount** (sum of all items)
7. **Set order status** to "Confirmed" if all validations pass
8. **Save order** to database
9. **Return order details** with 201 Created status

### Authentication Flow
1. **User submits credentials** to `/auth/token`
2. **Gateway validates credentials** against in-memory user store
3. **Generate JWT token** with user claims (username, role)
4. **Return token** to client with user information
5. **Client includes token** in Authorization header for protected requests
6. **Services validate token** on each protected endpoint
7. **Check role-based permissions** before executing business logic

## ?? Error Handling

The system returns standardized error responses using RFC 7807 Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Unauthorized", 
  "status": 401,
  "detail": "JWT token is required for this operation."
}
```

### Authentication-Specific Error Codes
- **401 Unauthorized**: Missing or invalid JWT token
- **403 Forbidden**: Valid token but insufficient role permissions
- **400 Bad Request**: Invalid login credentials format
- **422 Unprocessable Entity**: Business logic validation failures

### Gateway-Specific Error Responses
- **404 Not Found**: Route not configured in gateway
- **503 Service Unavailable**: Backend service is down
- **502 Bad Gateway**: Backend service returned invalid response

## ?? Monitoring & Health Checks

### Health Check Endpoints

- **Gateway**: `GET http://localhost:6000/health`
- **Inventory (Direct)**: `GET http://localhost:5000/health`
- **Sales (Direct)**: `GET http://localhost:5001/health`
- **Inventory (Via Gateway)**: `GET http://localhost:6000/inventory/health`
- **Sales (Via Gateway)**: `GET http://localhost:6000/sales/health`

All health checks should return `200 OK` with response body: `"Healthy"`

### Authentication Status Information

```bash
# Get available test users for development
curl http://localhost:6000/auth/test-users

# Get gateway status and routing information
curl http://localhost:6000/gateway/status
curl http://localhost:6000/gateway/routes
```

### Logging

The applications use Serilog for structured logging. Logs include:
- **Gateway**: JWT token generation, authentication attempts, request routing
- **Inventory**: CRUD operations, authorization checks, validation errors
- **Sales**: Order processing, authentication validation, HTTP client communication
- **All Services**: Request/response information, error details, performance metrics

## ?? Development Workflow

### 1. Start Development Environment
```bash
# Option A: Start all services individually (recommended)
dotnet run --project src/inventory.api --urls "http://localhost:5000" &
dotnet run --project src/sales.api --urls "http://localhost:5001" &
dotnet run --project src/gateway --urls "http://localhost:6000" &
```

### 2. Authenticate for Testing
```bash
# Get admin token
export ADMIN_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r '.accessToken')

# Get customer token
export CUSTOMER_TOKEN=$(curl -s -X POST "http://localhost:6000/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"username":"customer1","password":"password123"}' | jq -r '.accessToken')
```

### 3. Test Protected Operations
```bash
# Test admin operations
curl -H "Authorization: Bearer $ADMIN_TOKEN" \
  "http://localhost:6000/inventory/products" -X POST \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Product","description":"Test","price":10.99,"stockQuantity":100}'

# Test customer operations
curl -H "Authorization: Bearer $CUSTOMER_TOKEN" \
  "http://localhost:6000/sales/orders" -X POST \
  -H "Content-Type: application/json" \
  -d '{"customerId":"123e4567-e89b-12d3-a456-426614174000","items":[{"productId":"123e4567-e89b-12d3-a456-426614174001","quantity":1}]}'
```

### 4. Run Tests
```bash
# Ensure all tests pass with unified test project
dotnet test tests/endpoint.tests/endpoint.tests.csproj

# Run authentication-specific tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "AuthenticationTests"
```

## ?? Troubleshooting

### Common Authentication Issues

#### Invalid JWT Token
```
HTTP 401 Unauthorized: "JWT token is required for this operation."
```
**Solution**: Ensure the Authorization header is set with `Bearer <token>`

#### Expired JWT Token
```
HTTP 401 Unauthorized: "Token has expired."
```
**Solution**: Request a new token via `/auth/token`

#### Insufficient Permissions
```
HTTP 403 Forbidden: "Insufficient permissions for this operation."
```
**Solution**: Use a token with the appropriate role (admin for products, customer for orders)

#### Authentication Service Unavailable
```
HTTP 503 Service Unavailable: "Authentication service is not available."
```
**Solution**: Ensure the Gateway service is running on port 6000

### Common Development Issues

#### JWT Configuration Mismatch
Ensure all services use the same JWT configuration (Key, Issuer, Audience)

#### Database Connection Issues
1. Verify SQL Server is running
2. Check connection strings in `appsettings.json`
3. Ensure databases exist:
   ```bash
   dotnet ef database update --project src/inventory.api
   dotnet ef database update --project src/sales.api
   ```

#### Service Communication Issues
1. Ensure all services are running on correct ports
2. Check service health endpoints
3. Verify JWT tokens are valid and not expired

## ?? Additional Resources

- [.NET 8 Documentation](https://docs.microsoft.com/dotnet/)
- [JWT.io - JWT Debugger](https://jwt.io/)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [ASP.NET Core Authentication](https://docs.microsoft.com/aspnet/core/security/authentication/)
- [xUnit Testing](https://xunit.net/)

## ?? Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Commit Message Convention
We follow conventional commits:
- `feat:` - New features
- `fix:` - Bug fixes
- `docs:` - Documentation updates
- `test:` - Test additions or modifications
- `refactor:` - Code refactoring
- `security:` - Security improvements

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Authors

- **Your Name** - *Initial work* - [wleicht](https://github.com/wleicht)

---

## ?? Current Status: v0.5

### ? **Step 1 Complete**: Inventory basic CRUD with validation and tests
- ? EF Core configured with SQL Server
- ? Product CRUD operations
- ? Data validation with Data Annotations
- ? Comprehensive test suite
- ? API documentation with Swagger
- ? Health checks configured

### ? **Step 2 Complete**: Sales basic functionality with HTTP validation
- ? Order management with CRUD operations
- ? Real-time stock validation via HTTP
- ? HTTP client with retry mechanism
- ? Order status management (Pending ? Confirmed)
- ? Price freezing at order time
- ? Comprehensive error handling
- ? Complete test coverage

### ? **Step 3 Complete**: API Gateway with YARP routing
- ? Unified entry point on port 6000
- ? YARP reverse proxy configuration
- ? Route mapping for inventory and sales services
- ? Health check integration for backend services
- ? Gateway-specific endpoints (status, routes)
- ? Comprehensive routing tests
- ? Error handling and service unavailability management

### ? **Step 4 Complete**: JWT Authentication with role-based protection
- ? JWT token generation and validation
- ? Role-based authorization (admin, customer)
- ? Protected endpoints with appropriate access levels
- ? Open access for read operations
- ? Development user accounts for testing
- ? Comprehensive authentication tests
- ? Security best practices implementation
- ? Unified testing approach with single test project

### **Next Step**: Advanced microservices patterns (Event-driven architecture, CQRS, or Service mesh)

## ?? System Architecture Diagram

```
???????????????????    ????????????????????    ???????????????????
?                 ?    ?                  ?    ?                 ?
?   Client Apps   ??????   API Gateway    ??????  Inventory API  ?
?                 ?    ?   (Port 6000)    ?    ?   (Port 5000)   ?
?                 ?    ?                  ?    ?                 ?
???????????????????    ?   YARP Reverse   ?    ???????????????????
                       ?     Proxy        ?              ?
                       ?                  ?              ?
                       ?   JWT Token      ?    ???????????????????
                       ?    Issuer        ?    ?                 ?
                       ?                  ??????   Sales API     ?
                       ?                  ?    ?   (Port 5001)   ?
                       ?                  ?    ?                 ?
                       ????????????????????    ???????????????????
                                ?                         ?
                                ?                         ? HTTP Client
                                ?                         ? (Stock Validation)
                       ????????????????????              ?
                       ?                  ?              ?
                       ?  Authentication  ?    ?????????????????????
                       ?      Flow        ?    ?                   ?
                       ?                  ?    ?  Inventory API    ?
                       ? 1. Login Request ?    ? (Direct Call for  ?
                       ? 2. JWT Token     ?    ? Stock Validation) ?
                       ? 3. Protected     ?    ?                   ?
                       ?    Operations    ?    ?????????????????????
                       ?                  ?
                       ????????????????????

Databases:                     Security:                Testing:
???????????????????    ???????????????????    ???????????????????
?   InventoryDb   ?    ?    SalesDb      ?    ?   Unified Tests ?
?  (Products)     ?    ?   (Orders)      ?    ?   44 Tests      ?
???????????????????    ???????????????????    ?  - Gateway      ?
                                              ?  - Auth         ?
JWT Development Users:                        ?  - CRUD         ?
???????????????????                          ?  - Integration  ?
?   admin         ?                          ?  - Security     ?
?   customer1     ?                          ???????????????????
?   customer2     ?
?   manager       ?
???????????????????
```

This architecture ensures scalability, maintainability, security, and clear separation of concerns while providing a unified API surface through the gateway with proper authentication and authorization controls. The unified testing approach ensures comprehensive coverage without project duplication.