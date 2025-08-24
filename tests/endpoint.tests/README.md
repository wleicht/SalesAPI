# Endpoint Tests - Port 5000

This project contains automated tests to validate API endpoints running on **port 5000**.

## How to run the tests

### Prerequisites
1. Make sure the API is running on port 5000:
   ```bash
   dotnet run --project src/inventory.api --urls "http://localhost:5000"
   ```

2. Or configure in `appsettings.json` or `launchSettings.json` to run on port 5000.

### Execute tests
```bash
# Run all tests
dotnet test tests/endpoint.tests/endpoint.tests.csproj

# Run only a specific test file
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "InventoryApiTests"
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "SalesApiTests" 
dotnet test tests/endpoint.tests/endpoint.tests.csproj --filter "ProductCrudTests"
```

## Tests included

### InventoryApiTests
- ? Health check endpoint
- ? Get products endpoint
- ? Get products with pagination
- ? Get product by invalid ID (should return 404)
- ? Swagger accessibility

### SalesApiTests
- ? Health check endpoint
- ? Swagger accessibility

### ProductCrudTests
- ? Create product with valid data
- ? Create product with invalid data (should return 400)
- ? Get products list

## Notes
- All tests assume the API is running on `http://localhost:5000`
- Tests are independent and can be executed in any order
- For endpoints that require valid data, tests verify both success and failure scenarios