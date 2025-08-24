# SalesAPI - Microservices Architecture

A comprehensive microservices-based e-commerce solution built with .NET 8, featuring inventory management, sales processing, HTTP-based stock validation, and automated testing.

## ??? Architecture Overview

This solution implements a clean microservices architecture with the following components:

- **Inventory API** - Product catalog management with full CRUD operations
- **Sales API** - Order processing with real-time inventory validation  
- **Building Blocks Contracts** - Shared DTOs and contracts between services
- **Automated Tests** - Comprehensive endpoint and integration testing

## ?? Project Structure

```
SalesAPI/
??? src/
?   ??? inventory.api/          # Inventory microservice
?   ??? sales.api/              # Sales microservice with order management
?   ??? buildingblocks.contracts/  # Shared contracts and DTOs
??? tests/
?   ??? endpoint.tests/         # Integration tests for both APIs
??? deploy/
?   ??? Dockerfile.sqlserver    # SQL Server container configuration
??? README.md
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
# Build and run SQL Server container
docker build -f deploy/Dockerfile.sqlserver -t sqlserver-dev .
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" -p 1433:1433 --name sqlserver-dev -d sqlserver-dev

# Or use the official image directly
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_password123" -p 1433:1433 --name sqlserver-dev -d mcr.microsoft.com/mssql/server:2022-latest
```

## ????? Running the APIs

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

### 4. Start the APIs

#### Inventory API (Port 5000)
```bash
dotnet run --project src/inventory.api --urls "http://localhost:5000"
```

#### Sales API (Port 5001)
```bash
dotnet run --project src/sales.api --urls "http://localhost:5001"
```

**Important:** Both APIs must be running for the Sales API to validate inventory stock via HTTP calls.

## ?? API Documentation

### Inventory API Endpoints (Port 5000)

| Method | Endpoint | Description | Status |
|--------|----------|-------------|---------|
| `GET` | `/health` | Health check | ? |
| `GET` | `/swagger` | API documentation | ? |
| `POST` | `/products` | Create a new product | ? |
| `GET` | `/products` | Get paginated products list | ? |
| `GET` | `/products/{id}` | Get product by ID | ? |

### Sales API Endpoints (Port 5001)

| Method | Endpoint | Description | Status |
|--------|----------|-------------|---------|
| `GET` | `/health` | Health check | ? |
| `GET` | `/swagger` | API documentation | ? |
| `POST` | `/orders` | Create order with stock validation | ? |
| `GET` | `/orders` | Get paginated orders list | ? |
| `GET` | `/orders/{id}` | Get order by ID | ? |

## ?? API Usage Examples

### Create a Product (Inventory API)
```bash
curl -X POST "http://localhost:5000/products" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop",
    "description": "High-performance gaming laptop with RTX graphics",
    "price": 1299.99,
    "stockQuantity": 50
  }'
```

### Get Products with Pagination
```bash
curl "http://localhost:5000/products?page=1&pageSize=10"
```

### Create an Order (Sales API)
```bash
curl -X POST "http://localhost:5001/orders" \
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

### Get Orders with Pagination
```bash
curl "http://localhost:5001/orders?page=1&pageSize=10"
```

## ?? Running Tests

The project includes comprehensive automated tests for all endpoints and business logic.

### Run All Tests
```bash
dotnet test tests/endpoint.tests/endpoint.tests.csproj
```

### Run Specific Test Categories
```bash
# Inventory API tests only
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "InventoryApiTests"

# Sales API tests only  
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "SalesApiTests"

# Product CRUD tests only
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "ProductCrudTests"

# Order CRUD tests only
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "OrderCrudTests"
```

### Test Coverage

#### Inventory API Tests
- ? Health check endpoints
- ? Swagger accessibility
- ? Product CRUD operations
- ? Input validation
- ? Error handling
- ? Pagination

#### Sales API Tests
- ? Health check endpoints
- ? Swagger accessibility
- ? Order creation with stock validation
- ? HTTP communication with Inventory API
- ? Order retrieval and pagination
- ? Error handling and validation

## ?? Configuration

### Connection Strings

Update connection strings in the respective `appsettings.json` files:

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

### Environment Variables

You can override configuration using environment variables:

```bash
export ConnectionStrings__DefaultConnection="Server=your-server;Database=InventoryDb;..."
export InventoryApi__BaseUrl="http://your-inventory-api-url/"
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
1. **Receive order request** via `POST /orders`
2. **Validate order data** using Data Annotations
3. **For each order item:**
   - Fetch product details from Inventory API
   - Validate product exists
   - Check sufficient stock quantity
   - Freeze unit price at current market price
4. **Calculate total amount** (sum of all items)
5. **Set order status** to "Confirmed" if all validations pass
6. **Save order** to database
7. **Return order details** with 201 Created status

### Error Handling
- **400 Bad Request**: Invalid input data
- **422 Unprocessable Entity**: Insufficient stock
- **503 Service Unavailable**: Inventory API communication failure

## ?? Error Handling

The APIs return standardized error responses using RFC 7807 Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request", 
  "status": 400,
  "detail": "Name is required."
}
```

## ?? Monitoring & Health Checks

### Health Check Endpoints

- **Inventory API**: `GET http://localhost:5000/health`
- **Sales API**: `GET http://localhost:5001/health`

Both should return `200 OK` with response body: `"Healthy"`

### Logging

The applications use Serilog for structured logging to console. Logs include:
- Request/response information
- HTTP client communication details
- Error details with correlation
- Performance metrics
- Business logic flow

## ?? Development Workflow

### 1. Make Changes
```bash
# Edit code files
# Add new features or fix bugs
```

### 2. Run Tests
```bash
# Ensure all tests pass
dotnet test
```

### 3. Build and Run
```bash
# Build the solution
dotnet build

# Run both APIs (in separate terminals)
dotnet run --project src/inventory.api --urls "http://localhost:5000"
dotnet run --project src/sales.api --urls "http://localhost:5001"
```

## ?? Troubleshooting

### Common Issues

#### Port Already in Use
```bash
# Find process using the port
netstat -ano | findstr :5000
netstat -ano | findstr :5001

# Kill the process (Windows)
taskkill /F /PID <process-id>
```

#### Database Connection Issues
1. Verify SQL Server is running
2. Check connection strings in `appsettings.json`
3. Ensure databases exist: 
   ```bash
   dotnet ef database update --project src/inventory.api
   dotnet ef database update --project src/sales.api
   ```

#### HTTP Communication Issues
1. Ensure Inventory API is running on port 5000
2. Check `InventoryApi:BaseUrl` configuration in Sales API
3. Verify network connectivity between services

#### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

## ?? Additional Resources

- [.NET 8 Documentation](https://docs.microsoft.com/dotnet/)
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


## ?? Authors

- **Your Name** - *Initial work* - [wleicht](https://github.com/wleicht)

---

## ?? Current Status: v0.3

### ? **Step 1 Complete**: Inventory basic CRUD with validation and tests
- ? EF Core configured with SQL Server
- ? Product CRUD operations
- ? Data validation with Data Annotations
- ? Comprehensive test suite (100% passing)
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

### **Next Step**: API Gateway with YARP for unified entry point

## ?? API Communication Flow

```
Client Request ? Sales API ? HTTP Client ? Inventory API
                     ?              ?             ?
                Order Creation   Stock Check   Product Data
                     ?              ?             ?
                Order Confirmed ? Valid Stock ? Product Found
```

This architecture ensures real-time stock validation while maintaining service independence and scalability.