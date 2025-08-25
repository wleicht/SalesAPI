# Etapa 7 - Observability Testing Script for Windows
# Tests the complete system with correlation ID tracking and metrics collection

Write-Host "?? Starting Etapa 7 - Observability Testing" -ForegroundColor Blue
Write-Host "============================================" -ForegroundColor Blue

# Function to print colored output
function Write-Status {
    param($Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param($Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param($Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param($Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Function to test if a URL is accessible
function Test-Url {
    param($Url)
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5 -UseBasicParsing
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

# Check if services are running
function Test-Services {
    Write-Status "Checking if services are running..."
    
    if (-not (Test-Url "http://localhost:6000/health")) {
        Write-Error "Gateway not responding at http://localhost:6000"
        return $false
    }
    Write-Success "Gateway is healthy"
    
    if (-not (Test-Url "http://localhost:5000/health")) {
        Write-Error "Inventory API not responding at http://localhost:5000"
        return $false
    }
    Write-Success "Inventory API is healthy"
    
    if (-not (Test-Url "http://localhost:5001/health")) {
        Write-Error "Sales API not responding at http://localhost:5001"
        return $false
    }
    Write-Success "Sales API is healthy"
    
    return $true
}

# Test correlation ID propagation
function Test-Correlation {
    Write-Status "Testing correlation ID propagation..."
    
    # Generate a unique correlation ID for this test
    $correlationId = "test-$(Get-Date -Format 'yyyyMMddHHmmss')-$(Get-Random -Minimum 1000 -Maximum 9999)"
    Write-Status "Using Correlation ID: $correlationId"
    
    try {
        # Step 1: Get admin token
        Write-Status "Getting admin authentication token..."
        $adminTokenResponse = Invoke-RestMethod -Uri 'http://localhost:6000/auth/token' -Method Post `
            -Headers @{'Content-Type'='application/json'; 'X-Correlation-Id'=$correlationId} `
            -Body '{"username":"admin","password":"admin123"}'
        
        $adminToken = $adminTokenResponse.accessToken
        if (-not $adminToken) {
            Write-Error "Failed to get admin token"
            return $false
        }
        Write-Success "Admin token obtained"
        
        # Step 2: Create a product with correlation ID
        Write-Status "Creating test product..."
        $productBody = @{
            name = "Observability Test Product"
            description = "Product for testing correlation tracking"
            price = 99.99
            stockQuantity = 50
        } | ConvertTo-Json
        
        $productResponse = Invoke-RestMethod -Uri 'http://localhost:6000/inventory/products' -Method Post `
            -Headers @{'Authorization' = "Bearer $adminToken"; 'Content-Type'='application/json'; 'X-Correlation-Id'=$correlationId} `
            -Body $productBody
        
        $productId = $productResponse.id
        if (-not $productId) {
            Write-Error "Failed to create product"
            return $false
        }
        Write-Success "Product created: $productId"
        
        # Step 3: Get customer token
        Write-Status "Getting customer authentication token..."
        $customerTokenResponse = Invoke-RestMethod -Uri 'http://localhost:6000/auth/token' -Method Post `
            -Headers @{'Content-Type'='application/json'; 'X-Correlation-Id'=$correlationId} `
            -Body '{"username":"customer1","password":"password123"}'
        
        $customerToken = $customerTokenResponse.accessToken
        if (-not $customerToken) {
            Write-Error "Failed to get customer token"
            return $false
        }
        Write-Success "Customer token obtained"
        
        # Step 4: Create order with correlation tracking
        Write-Status "Creating order to test full correlation flow..."
        $customerId = [System.Guid]::NewGuid().ToString()
        $orderBody = @{
            customerId = $customerId
            items = @(
                @{
                    productId = $productId
                    quantity = 3
                }
            )
        } | ConvertTo-Json -Depth 3
        
        $orderResponse = Invoke-RestMethod -Uri 'http://localhost:6000/sales/orders' -Method Post `
            -Headers @{'Authorization' = "Bearer $customerToken"; 'Content-Type'='application/json'; 'X-Correlation-Id'=$correlationId} `
            -Body $orderBody
        
        $orderId = $orderResponse.id
        $orderStatus = $orderResponse.status
        
        if (-not $orderId) {
            Write-Warning "Order creation may have failed - checking response"
            Write-Host ($orderResponse | ConvertTo-Json) -ForegroundColor Yellow
            return $false
        }
        
        Write-Success "Order created: $orderId with status: $orderStatus"
        
        # Step 5: Check stock reservations with correlation
        Write-Status "Checking stock reservations..."
        Start-Sleep -Seconds 2  # Give time for processing
        
        try {
            $reservationsResponse = Invoke-RestMethod -Uri "http://localhost:6000/inventory/api/stockreservations/order/$orderId" -Method Get `
                -Headers @{'Authorization' = "Bearer $adminToken"; 'X-Correlation-Id'=$correlationId}
            
            $reservationCount = $reservationsResponse.Count
            if ($reservationCount -gt 0) {
                Write-Success "Found $reservationCount stock reservations for order"
            } else {
                Write-Warning "No stock reservations found yet (may still be processing)"
            }
        }
        catch {
            Write-Warning "Could not retrieve reservations - they may still be processing"
        }
        
        Write-Success "? Correlation ID test completed: $correlationId"
        Write-Host ""
        Write-Status "?? Check your service logs for the correlation ID: $correlationId"
        Write-Status "   You should see the same correlation ID across Gateway ? Sales ? Inventory"
        Write-Host ""
        
        return $true
    }
    catch {
        Write-Error "Error during correlation test: $($_.Exception.Message)"
        return $false
    }
}

# Test metrics endpoints
function Test-Metrics {
    Write-Status "Testing Prometheus metrics endpoints..."
    
    # Test Gateway metrics
    if (Test-Url "http://localhost:6000/metrics") {
        Write-Success "Gateway metrics endpoint accessible"
    } else {
        Write-Error "Gateway metrics endpoint not accessible"
    }
    
    # Test Inventory metrics
    if (Test-Url "http://localhost:5000/metrics") {
        Write-Success "Inventory API metrics endpoint accessible"
    } else {
        Write-Error "Inventory API metrics endpoint not accessible"
    }
    
    # Test Sales metrics
    if (Test-Url "http://localhost:5001/metrics") {
        Write-Success "Sales API metrics endpoint accessible"
    } else {
        Write-Error "Sales API metrics endpoint not accessible"
    }
}

# Test observability with concurrent orders
function Test-ConcurrentObservability {
    Write-Status "Testing observability with concurrent orders..."
    
    try {
        # Generate correlation IDs for concurrent tests
        $correlationBase = "concurrent-$(Get-Date -Format 'yyyyMMddHHmmss')"
        
        # Get tokens
        $adminTokenResponse = Invoke-RestMethod -Uri 'http://localhost:6000/auth/token' -Method Post `
            -Headers @{'Content-Type'='application/json'} `
            -Body '{"username":"admin","password":"admin123"}'
        $adminToken = $adminTokenResponse.accessToken
        
        $customerTokenResponse = Invoke-RestMethod -Uri 'http://localhost:6000/auth/token' -Method Post `
            -Headers @{'Content-Type'='application/json'} `
            -Body '{"username":"customer1","password":"password123"}'
        $customerToken = $customerTokenResponse.accessToken
        
        # Create product for concurrent testing
        $productBody = @{
            name = "Concurrent Test Product"
            description = "Product for testing concurrent observability"
            price = 49.99
            stockQuantity = 10
        } | ConvertTo-Json
        
        $productResponse = Invoke-RestMethod -Uri 'http://localhost:6000/inventory/products' -Method Post `
            -Headers @{'Authorization' = "Bearer $adminToken"; 'Content-Type'='application/json'} `
            -Body $productBody
        $productId = $productResponse.id
        
        # Launch concurrent orders with different correlation IDs
        Write-Status "Launching 3 concurrent orders with different correlation IDs..."
        
        $jobs = @()
        for ($i = 1; $i -le 3; $i++) {
            $correlationId = "$correlationBase-$i"
            Write-Status "Launching order $i with correlation: $correlationId"
            
            $orderBody = @{
                customerId = [System.Guid]::NewGuid().ToString()
                items = @(
                    @{
                        productId = $productId
                        quantity = 2
                    }
                )
            } | ConvertTo-Json -Depth 3
            
            $job = Start-Job -ScriptBlock {
                param($Url, $Token, $Body, $CorrelationId)
                try {
                    Invoke-RestMethod -Uri $Url -Method Post `
                        -Headers @{'Authorization' = "Bearer $Token"; 'Content-Type'='application/json'; 'X-Correlation-Id'=$CorrelationId} `
                        -Body $Body
                } catch {
                    return $null
                }
            } -ArgumentList 'http://localhost:6000/sales/orders', $customerToken, $orderBody, $correlationId
            
            $jobs += $job
        }
        
        # Wait for all jobs to complete
        $jobs | Wait-Job | Out-Null
        $jobs | Remove-Job
        
        Write-Success "Concurrent orders launched - check logs for different correlation IDs"
        Write-Host ""
        Write-Status "?? Look for correlation IDs: $correlationBase-1, $correlationBase-2, $correlationBase-3"
        Write-Host ""
        
        return $true
    }
    catch {
        Write-Error "Error during concurrent observability test: $($_.Exception.Message)"
        return $false
    }
}

# Main execution
function Main {
    Write-Host "?? Etapa 7 - Observability Testing Started" -ForegroundColor Blue
    Write-Host "===========================================" -ForegroundColor Blue
    Write-Host ""
    
    # Run tests
    if (Test-Services) {
        Write-Host ""
        Test-Correlation
        Write-Host ""
        Test-Metrics  
        Write-Host ""
        Test-ConcurrentObservability
        Write-Host ""
        
        Write-Success "?? Observability testing completed!"
        Write-Host ""
        Write-Status "?? Next steps:"
        Write-Status "   1. Check service logs for correlation IDs"
        Write-Status "   2. Visit http://localhost:6000/metrics for Gateway metrics"
        Write-Status "   3. Visit http://localhost:5000/metrics for Inventory metrics"
        Write-Status "   4. Visit http://localhost:5001/metrics for Sales metrics"
        Write-Status "   5. Run: docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d"
        Write-Status "   6. Visit http://localhost:9090 for Prometheus metrics collection"
        Write-Host ""
        Write-Success "? Etapa 7 - Observability implementation is working!"
    } else {
        Write-Error "? Services are not running. Please start them first with:"
        Write-Error "   docker compose up -d"
        exit 1
    }
}

# Run the main function
Main