using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EndpointTests
{
    /// <summary>
    /// Diagnostic tests to identify specific issues with the reservation system.
    /// </summary>
    public class DiagnosticTests
    {
        private readonly HttpClient _inventoryClient;
        private readonly HttpClient _salesClient;
        private readonly HttpClient _gatewayClient;
        private readonly ITestOutputHelper _output;

        public DiagnosticTests(ITestOutputHelper output)
        {
            _output = output;
            _inventoryClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
            _salesClient = new HttpClient { BaseAddress = new Uri("http://localhost:5001/") };
            _gatewayClient = new HttpClient { BaseAddress = new Uri("http://localhost:6000/") };
        }

        /// <summary>
        /// Test to check if the Sales API is functioning correctly for order creation.
        /// This will help identify where the InternalServerError is coming from.
        /// </summary>
        [Fact]
        public async Task DiagnoseOrderCreationError()
        {
            _output.WriteLine("=== Diagnosing Order Creation Error ===");

            try
            {
                // Step 1: Get admin token for product creation
                _output.WriteLine("Step 1: Getting admin token...");
                var loginRequest = new { Username = "admin", Password = "admin123" };
                var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
                var authResponse = await _gatewayClient.PostAsync("auth/token", loginContent);
                
                if (!authResponse.IsSuccessStatusCode)
                {
                    var errorContent = await authResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"? Auth failed: {authResponse.StatusCode} - {errorContent}");
                    throw new Exception($"Authentication failed: {authResponse.StatusCode}");
                }
                
                var authData = JsonDocument.Parse(await authResponse.Content.ReadAsStringAsync());
                var adminToken = authData.RootElement.GetProperty("accessToken").GetString();
                _output.WriteLine("? Admin token obtained");

                // Step 2: Create a simple product first
                _output.WriteLine("Step 2: Creating test product...");
                var productRequest = new
                {
                    name = $"Diagnostic Product {Guid.NewGuid()}",
                    description = "Product for diagnostic testing",
                    price = 50.0m,
                    stockQuantity = 10
                };

                _inventoryClient.DefaultRequestHeaders.Clear();
                _inventoryClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

                var productContent = new StringContent(JsonSerializer.Serialize(productRequest), Encoding.UTF8, "application/json");
                var productResponse = await _inventoryClient.PostAsync("products", productContent);
                
                if (!productResponse.IsSuccessStatusCode)
                {
                    var errorContent = await productResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"? Product creation failed: {productResponse.StatusCode} - {errorContent}");
                    throw new Exception($"Product creation failed: {productResponse.StatusCode}");
                }

                var productData = JsonDocument.Parse(await productResponse.Content.ReadAsStringAsync());
                var productId = Guid.Parse(productData.RootElement.GetProperty("id").GetString()!);
                _output.WriteLine($"? Product created: {productId}");

                // Step 3: Get customer token
                _output.WriteLine("Step 3: Getting customer token...");
                var customerLoginRequest = new { Username = "customer1", Password = "password123" };
                var customerLoginContent = new StringContent(JsonSerializer.Serialize(customerLoginRequest), Encoding.UTF8, "application/json");
                var customerAuthResponse = await _gatewayClient.PostAsync("auth/token", customerLoginContent);
                
                if (!customerAuthResponse.IsSuccessStatusCode)
                {
                    var errorContent = await customerAuthResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"? Customer auth failed: {customerAuthResponse.StatusCode} - {errorContent}");
                    throw new Exception($"Customer authentication failed: {customerAuthResponse.StatusCode}");
                }
                
                var customerAuthData = JsonDocument.Parse(await customerAuthResponse.Content.ReadAsStringAsync());
                var customerToken = customerAuthData.RootElement.GetProperty("accessToken").GetString();
                _output.WriteLine("? Customer token obtained");

                // Step 4: Try to create an order and capture detailed error
                _output.WriteLine("Step 4: Attempting order creation...");
                var orderRequest = new
                {
                    customerId = Guid.NewGuid(),
                    items = new[]
                    {
                        new
                        {
                            productId = productId,
                            quantity = 2
                        }
                    }
                };

                _salesClient.DefaultRequestHeaders.Clear();
                _salesClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {customerToken}");

                var orderContent = new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json");
                var orderResponse = await _salesClient.PostAsync("orders", orderContent);
                
                _output.WriteLine($"Order response status: {orderResponse.StatusCode}");

                if (!orderResponse.IsSuccessStatusCode)
                {
                    var errorContent = await orderResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"? Order creation failed: {orderResponse.StatusCode}");
                    _output.WriteLine($"Error details: {errorContent}");
                    
                    // Don't throw here - we want to capture the error details
                    _output.WriteLine("Order creation failed as expected - this is what we're diagnosing");
                }
                else
                {
                    var orderData = JsonDocument.Parse(await orderResponse.Content.ReadAsStringAsync());
                    var orderId = orderData.RootElement.GetProperty("id").GetString();
                    _output.WriteLine($"? Order created successfully: {orderId}");
                }

            }
            catch (Exception ex)
            {
                _output.WriteLine($"? Diagnostic test failed: {ex.Message}");
                _output.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Test to diagnose the stock reservation endpoint specifically.
        /// </summary>
        [Fact]
        public async Task DiagnoseStockReservationEndpoint()
        {
            _output.WriteLine("=== Diagnosing Stock Reservation Endpoint ===");

            try
            {
                // Get admin token
                var loginRequest = new { Username = "admin", Password = "admin123" };
                var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
                var authResponse = await _gatewayClient.PostAsync("auth/token", loginContent);
                
                var authData = JsonDocument.Parse(await authResponse.Content.ReadAsStringAsync());
                var token = authData.RootElement.GetProperty("accessToken").GetString();
                _output.WriteLine("? Token obtained");

                // Test GET endpoint first
                _output.WriteLine("Testing GET /api/stockreservations/order/{guid}...");
                _inventoryClient.DefaultRequestHeaders.Clear();
                _inventoryClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var testOrderId = Guid.NewGuid();
                var getResponse = await _inventoryClient.GetAsync($"api/stockreservations/order/{testOrderId}");
                
                _output.WriteLine($"GET response status: {getResponse.StatusCode}");
                var getContent = await getResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"GET response content: {getContent}");

                // Test POST endpoint
                _output.WriteLine("Testing POST /api/stockreservations...");
                var reservationRequest = new
                {
                    orderId = Guid.NewGuid(),
                    correlationId = Guid.NewGuid().ToString(),
                    items = new[]
                    {
                        new
                        {
                            productId = Guid.NewGuid(),
                            quantity = 1
                        }
                    }
                };

                var postContent = new StringContent(JsonSerializer.Serialize(reservationRequest), Encoding.UTF8, "application/json");
                var postResponse = await _inventoryClient.PostAsync("api/stockreservations", postContent);
                
                _output.WriteLine($"POST response status: {postResponse.StatusCode}");
                var postResponseContent = await postResponse.Content.ReadAsStringAsync();
                _output.WriteLine($"POST response content: {postResponseContent}");

                _output.WriteLine("? Stock reservation endpoint diagnosis complete");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"? Stock reservation diagnosis failed: {ex.Message}");
                throw;
            }
        }
    }
}