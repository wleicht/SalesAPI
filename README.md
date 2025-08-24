# SalesAPI - Complete Microservices Architecture

A comprehensive microservices-based e-commerce solution built with .NET 8, featuring inventory management, sales processing, API Gateway with YARP reverse proxy, HTTP-based stock validation, and comprehensive automated testing.

## ??? Architecture Overview

This solution implements a complete microservices architecture with the following components:

- **API Gateway** - Unified entry point using YARP reverse proxy (Port 6000)
- **Inventory API** - Product catalog management with full CRUD operations (Port 5000)
- **Sales API** - Order processing with real-time inventory validation (Port 5001)
- **Building Blocks Contracts** - Shared DTOs and contracts between services
- **Automated Tests** - Comprehensive endpoint, integration, and routing testing

## ?? Project Structure

```
SalesAPI/
??? src/
?   ??? gateway/                # API Gateway with YARP reverse proxy
?   ??? inventory.api/          # Inventory microservice
?   ??? sales.api/              # Sales microservice with order management
?   ??? buildingblocks.contracts/  # Shared contracts and DTOs
??? tests/
?   ??? endpoint.tests/         # Integration tests for all APIs and routing
??? deploy/
?   ??? Dockerfile.sqlserver    # SQL Server container configuration
??? README.md
```

## ?? Service Communication Flow

```
Client Request ? API Gateway (6000) ? Inventory API (5000)
                                   ? Sales API (5001)
                                   
Sales API ? HTTP Client ? Inventory API (Stock Validation)
```

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

## ?? API Documentation

### API Gateway Endpoints (Port 6000) - Primary Entry Point

| Method | Endpoint | Description | Routes To |
|--------|----------|-------------|-----------|
| `GET` | `/health` | Gateway health check | Gateway itself |
| `GET` | `/swagger` | Gateway API documentation | Gateway itself |
| `GET` | `/gateway/status` | Gateway status and info | Gateway itself |
| `GET` | `/gateway/routes` | Available routing information | Gateway itself |
| `GET` | `/inventory/*` | All inventory operations | Inventory API (5000) |
| `GET` | `/sales/*` | All sales operations | Sales API (5001) |

### Inventory API Endpoints (Via Gateway: `/inventory/*`)

| Method | Endpoint | Description | Direct URL | Via Gateway |
|--------|----------|-------------|-----------|-------------|
| `GET` | `/health` | Health check | `localhost:5000/health` | `localhost:6000/inventory/health` |
| `GET` | `/swagger` | API documentation | `localhost:5000/swagger` | `localhost:6000/inventory/swagger` |
| `POST` | `/products` | Create a new product | `localhost:5000/products` | `localhost:6000/inventory/products` |
| `GET` | `/products` | Get paginated products | `localhost:5000/products` | `localhost:6000/inventory/products` |
| `GET` | `/products/{id}` | Get product by ID | `localhost:5000/products/{id}` | `localhost:6000/inventory/products/{id}` |

### Sales API Endpoints (Via Gateway: `/sales/*`)

| Method | Endpoint | Description | Direct URL | Via Gateway |
|--------|----------|-------------|-----------|-------------|
| `GET` | `/health` | Health check | `localhost:5001/health` | `localhost:6000/sales/health` |
| `GET` | `/swagger` | API documentation | `localhost:5001/swagger` | `localhost:6000/sales/swagger` |
| `POST` | `/orders` | Create order with stock validation | `localhost:5001/orders` | `localhost:6000/sales/orders` |
| `GET` | `/orders` | Get paginated orders | `localhost:5001/orders` | `localhost:6000/sales/orders` |
| `GET` | `/orders/{id}` | Get order by ID | `localhost:5001/orders/{id}` | `localhost:6000/sales/orders/{id}` |

## ?? API Usage Examples

### Using the API Gateway (Recommended)

#### Create a Product via Gateway
```bash
curl -X POST "http://localhost:6000/inventory/products" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop",
    "description": "High-performance gaming laptop with RTX graphics",
    "price": 1299.99,
    "stockQuantity": 50
  }'
```

#### Get Products via Gateway
```bash
curl "http://localhost:6000/inventory/products?page=1&pageSize=10"
```

#### Create an Order via Gateway
```bash
curl -X POST "http://localhost:6000/sales/orders" \
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

#### Check System Health via Gateway
```bash
# Gateway health
curl "http://localhost:6000/health"

# Backend services health via gateway
curl "http://localhost:6000/inventory/health"
curl "http://localhost:6000/sales/health"

# Gateway routing information
curl "http://localhost:6000/gateway/status"
curl "http://localhost:6000/gateway/routes"
```

### Direct API Access (For Development/Debugging)

#### Direct Inventory API Access
```bash
curl "http://localhost:5000/products"
curl "http://localhost:5000/health"
```

#### Direct Sales API Access
```bash
curl "http://localhost:5001/orders"
curl "http://localhost:5001/health"
```

## ?? Running Tests

The project includes comprehensive automated tests covering all APIs and routing functionality.

### Run All Tests
```bash
dotnet test tests/endpoint.tests/endpoint.tests.csproj
```

### Run Specific Test Categories
```bash
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

### Current Test Coverage

#### Complete Test Suite (31 Tests)
- **Gateway API Tests** (4 tests) - Gateway health, status, routes, swagger
- **Gateway Routing Tests** (9 tests) - Route validation for all backend services
- **Inventory API Tests** (5 tests) - Health, swagger, product endpoints
- **Product CRUD Tests** (3 tests) - Product creation, validation, retrieval
- **Sales API Tests** (3 tests) - Health, swagger, order endpoints
- **Order CRUD Tests** (7 tests) - Order creation, validation, stock checking

## ?? Configuration

### Connection Strings

**Inventory API** (`src/inventory.api/appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InventoryDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
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
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "SalesApi.Services.InventoryClient": "Information"
    }
  }
}
```

**API Gateway** (`src/gateway/appsettings.json`):
```json
{
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
    },
    "Clusters": {
      "inventory-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://localhost:5000/" }
        }
      },
      "sales-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://localhost:5001/" }
        }
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

# Service URLs
export InventoryApi__BaseUrl="http://your-inventory-api-url/"

# YARP configuration
export ReverseProxy__Clusters__inventory-cluster__Destinations__destination1__Address="http://your-inventory-url/"
```

## ?? Data Models

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

## ??? Validation Rules

### Product Validation (Inventory API)
- **Name**: Required, max 100 characters
- **Description**: Required, max 500 characters  
- **Price**: Must be ? 0
- **StockQuantity**: Must be ? 0

### Order Validation (Sales API)
- **CustomerId**: Required, valid GUID
- **Items**: Required, at least 1 item
- **ProductId**: Required, valid GUID
- **Quantity**: Must be ? 1

## ?? Business Logic

### Order Processing Flow
1. **Receive order request** via `POST /sales/orders` (through Gateway)
2. **Validate order data** using Data Annotations
3. **For each order item:**
   - Fetch product details from Inventory API via HTTP
   - Validate product exists
   - Check sufficient stock quantity
   - Freeze unit price at current market price
4. **Calculate total amount** (sum of all items)
5. **Set order status** to "Confirmed" if all validations pass
6. **Save order** to database
7. **Return order details** with 201 Created status

### Gateway Routing Logic
1. **Receive client request** on port 6000
2. **Match request path** against configured routes
3. **Transform path** (remove `/inventory` or `/sales` prefix)
4. **Forward request** to appropriate backend service
5. **Return response** to client with original status codes
6. **Health check** backend services automatically

## ?? Error Handling

The system returns standardized error responses using RFC 7807 Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request", 
  "status": 400,
  "detail": "Name is required."
}
```

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

### Gateway Status Information

```bash
# Get gateway status and routing information
curl http://localhost:6000/gateway/status
curl http://localhost:6000/gateway/routes
```

### Logging

The applications use Serilog for structured logging. Logs include:
- **Gateway**: Request routing, backend health, YARP operations
- **Inventory**: CRUD operations, validation errors, database operations
- **Sales**: Order processing, HTTP client communication, stock validation
- **All Services**: Request/response information, error details, performance metrics

## ?? Development Workflow

### 1. Start Development Environment
```bash
# Option A: Start all services individually (recommended)
dotnet run --project src/inventory.api --urls "http://localhost:5000" &
dotnet run --project src/sales.api --urls "http://localhost:5001" &
dotnet run --project src/gateway --urls "http://localhost:6000" &

# Option B: Use your IDE to start multiple projects
```

### 2. Make Changes
```bash
# Edit code files
# Add new features or fix bugs
```

### 3. Run Tests
```bash
# Ensure all tests pass
dotnet test
```

### 4. Test via Gateway
```bash
# Test complete workflow via gateway
curl http://localhost:6000/gateway/status
curl http://localhost:6000/inventory/health
curl http://localhost:6000/sales/health
```

## ?? Troubleshooting

### Common Issues

#### Ports Already in Use
```bash
# Find processes using the ports
netstat -ano | findstr :5000
netstat -ano | findstr :5001
netstat -ano | findstr :6000

# Kill processes (Windows)
taskkill /F /PID <process-id>
```

#### Gateway Cannot Reach Backend Services
1. Verify all services are running on correct ports
2. Check gateway configuration in `appsettings.json`
3. Verify firewall settings allow local connections
4. Test direct backend access first

#### Database Connection Issues
1. Verify SQL Server is running
2. Check connection strings in `appsettings.json`
3. Ensure databases exist:
   ```bash
   dotnet ef database update --project src/inventory.api
   dotnet ef database update --project src/sales.api
   ```

#### YARP Routing Issues
1. Check route configuration in gateway `appsettings.json`
2. Verify path patterns match your requests
3. Test backend services directly first
4. Check gateway logs for routing errors

#### Build Errors
```bash
# Clean and rebuild entire solution
dotnet clean
dotnet build
```

### Health Check Commands

```bash
# Quick system health check
curl http://localhost:6000/health                    # Gateway
curl http://localhost:6000/inventory/health          # Inventory via Gateway  
curl http://localhost:6000/sales/health              # Sales via Gateway

# Direct service access (for debugging)
curl http://localhost:5000/health                    # Inventory Direct
curl http://localhost:5001/health                    # Sales Direct
```

## ?? Additional Resources

- [.NET 8 Documentation](https://docs.microsoft.com/dotnet/)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [ASP.NET Core Web APIs](https://docs.microsoft.com/aspnet/core/web-api/)
- [xUnit Testing](https://xunit.net/)
- [HTTP Client Best Practices](https://docs.microsoft.com/aspnet/core/fundamentals/http-requests)

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

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Authors

- **Your Name** - *Initial work* - [wleicht](https://github.com/wleicht)

---

## ?? Current Status: v0.4

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

### **Next Step**: JWT Authentication with role-based access control

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
                       ?                  ?    ???????????????????
                       ?                  ?    ?                 ?
                       ?                  ??????   Sales API     ?
                       ?                  ?    ?   (Port 5001)   ?
                       ?                  ?    ?                 ?
                       ????????????????????    ???????????????????
                                                         ?
                                                         ? HTTP Client
                                                         ? (Stock Validation)
                                                         ?
                                               ?????????????????????
                                               ?                   ?
                                               ?  Inventory API    ?
                                               ? (Direct Call for  ?
                                               ? Stock Validation) ?
                                               ?                   ?
                                               ?????????????????????

Databases:
???????????????????    ???????????????????
?   InventoryDb   ?    ?    SalesDb      ?
?  (Products)     ?    ?   (Orders)      ?
???????????????????    ???????????????????
```

This architecture ensures scalability, maintainability, and clear separation of concerns while providing a unified API surface through the gateway.