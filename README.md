# SalesAPI - Microservices Architecture

A comprehensive microservices-based e-commerce solution built with .NET 8, featuring inventory management, sales processing, and automated testing.

## ??? Architecture Overview

This solution implements a clean microservices architecture with the following components:

- **Inventory API** - Product catalog management with CRUD operations
- **Sales API** - Order processing and sales management  
- **Building Blocks Contracts** - Shared DTOs and contracts
- **Automated Tests** - Comprehensive endpoint testing

## ?? Project Structure

```
SalesAPI/
??? src/
?   ??? inventory.api/          # Inventory microservice
?   ??? sales.api/              # Sales microservice
?   ??? buildingblocks.contracts/  # Shared contracts and DTOs
??? tests/
?   ??? endpoint.tests/         # Integration tests
??? deploy/
?   ??? Dockerfile.sqlserver    # SQL Server container
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

### Option 1: Local SQL Server

If you have SQL Server installed locally:

1. **Update connection strings** in `src/inventory.api/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=InventoryDb;Integrated Security=true;TrustServerCertificate=True"
     }
   }
   ```

### Option 2: Docker SQL Server

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

### 3. Run Database Migrations

```bash
# Apply database migrations for Inventory API
dotnet ef database update --project src/inventory.api
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

**Or run both simultaneously in separate terminals.**

## ?? API Documentation

### Inventory API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/health` | Health check |
| `GET` | `/swagger` | API documentation |
| `POST` | `/products` | Create a new product |
| `GET` | `/products` | Get paginated products list |
| `GET` | `/products/{id}` | Get product by ID |

#### Example: Create Product
```bash
curl -X POST "http://localhost:5000/products" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sample Product",
    "description": "This is a sample product",
    "price": 29.99,
    "stockQuantity": 100
  }'
```

#### Example: Get Products with Pagination
```bash
curl "http://localhost:5000/products?page=1&pageSize=10"
```

### Sales API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/health` | Health check |
| `GET` | `/swagger` | API documentation |

## ?? Running Tests

The project includes comprehensive automated tests for all endpoints.

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
```

### Test Coverage

- ? Health check endpoints
- ? Swagger accessibility
- ? Product CRUD operations
- ? Input validation
- ? Error handling
- ? Pagination

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

### Environment Variables

You can override configuration using environment variables:

```bash
export ConnectionStrings__DefaultConnection="Server=your-server;Database=InventoryDb;..."
```

## ?? Data Models

### Product Model
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

### Product DTOs
- `CreateProductDto` - For creating new products
- `ProductDto` - For returning product data

## ??? Validation Rules

### Product Validation
- **Name**: Required, max 100 characters
- **Description**: Required, max 500 characters  
- **Price**: Must be ? 0
- **StockQuantity**: Must be ? 0

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

The application uses Serilog for structured logging to console. Logs include:
- Request/response information
- Error details
- Performance metrics

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

# Run the APIs
dotnet run --project src/inventory.api --urls "http://localhost:5000"
```

## ?? Troubleshooting

### Common Issues

#### Port Already in Use
```bash
# Find process using the port
netstat -ano | findstr :5000

# Kill the process (Windows)
taskkill /F /PID <process-id>
```

#### Database Connection Issues
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Ensure database exists: `dotnet ef database update`

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

## ?? Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request


## ?? Authors

- **Your Name** - *Initial work* - [wleicht](https://github.com/wleicht)

---

## ?? Current Status: v0.2

**Step 1 Complete**: Inventory basic CRUD with validation and tests
- ? EF Core configured with SQL Server
- ? Product CRUD operations
- ? Data validation with Data Annotations
- ? Comprehensive test suite (100% passing)
- ? API documentation with Swagger
- ? Health checks configured

**Next Step**: Sales basic functionality with HTTP validation