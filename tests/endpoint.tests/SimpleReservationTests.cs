using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EndpointTests
{
    /// <summary>
    /// Simple integration tests for validating the stock reservation system.
    /// Tests basic functionality and connectivity before complex workflows.
    /// </summary>
    public class SimpleReservationTests
    {
        private readonly HttpClient _inventoryClient;
        private readonly HttpClient _gatewayClient;
        private readonly ITestOutputHelper _output;

        public SimpleReservationTests(ITestOutputHelper output)
        {
            _output = output;
            _inventoryClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
            _gatewayClient = new HttpClient { BaseAddress = new Uri("http://localhost:6000/") };
        }

        /// <summary>
        /// Simple test to verify that the inventory API is responding correctly.
        /// </summary>
        [Fact]
        public async Task InventoryApi_ShouldBeResponding()
        {
            _output.WriteLine("=== Testing Inventory API Connectivity ===");

            try
            {
                var response = await _inventoryClient.GetAsync("products");
                _output.WriteLine($"Inventory API Response: {response.StatusCode}");
                
                Assert.True(response.IsSuccessStatusCode, $"Expected success status, got {response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"Response content length: {content.Length}");
                
                Assert.False(string.IsNullOrEmpty(content), "Response content should not be empty");
                
                _output.WriteLine("? Inventory API is responding correctly");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"? Error connecting to Inventory API: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test to verify that we can authenticate and get a token.
        /// </summary>
        [Fact]
        public async Task Authentication_ShouldWork()
        {
            _output.WriteLine("=== Testing Authentication ===");

            try
            {
                var loginRequest = new { Username = "admin", Password = "admin123" };
                var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

                var response = await _gatewayClient.PostAsync("auth/token", content);
                _output.WriteLine($"Auth Response: {response.StatusCode}");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenData = JsonDocument.Parse(responseContent);
                var token = tokenData.RootElement.GetProperty("accessToken").GetString();

                Assert.False(string.IsNullOrEmpty(token), "Token should not be null or empty");
                _output.WriteLine("? Authentication successful");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"? Authentication failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test to verify that we can create a product with authentication.
        /// </summary>
        [Fact]
        public async Task CreateProduct_ShouldWork()
        {
            _output.WriteLine("=== Testing Product Creation ===");

            try
            {
                // Get auth token
                var loginRequest = new { Username = "admin", Password = "admin123" };
                var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
                var authResponse = await _gatewayClient.PostAsync("auth/token", loginContent);
                
                Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);
                
                var authResponseContent = await authResponse.Content.ReadAsStringAsync();
                var tokenData = JsonDocument.Parse(authResponseContent);
                var token = tokenData.RootElement.GetProperty("accessToken").GetString();

                // Create product
                var productRequest = new
                {
                    name = $"Simple Test Product {Guid.NewGuid()}",
                    description = "Simple test for product creation",
                    price = 29.99m,
                    stockQuantity = 10
                };

                var productContent = new StringContent(JsonSerializer.Serialize(productRequest), Encoding.UTF8, "application/json");
                
                _inventoryClient.DefaultRequestHeaders.Clear();
                _inventoryClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var productResponse = await _inventoryClient.PostAsync("products", productContent);
                _output.WriteLine($"Product Creation Response: {productResponse.StatusCode}");

                if (!productResponse.IsSuccessStatusCode)
                {
                    var errorContent = await productResponse.Content.ReadAsStringAsync();
                    _output.WriteLine($"Error content: {errorContent}");
                }

                Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

                var productResponseContent = await productResponse.Content.ReadAsStringAsync();
                var productData = JsonDocument.Parse(productResponseContent);
                var productId = productData.RootElement.GetProperty("id").GetString();

                Assert.False(string.IsNullOrEmpty(productId), "Product ID should not be null or empty");
                _output.WriteLine($"? Product created successfully: {productId}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"? Product creation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Test to verify that the stock reservation endpoint exists and is accessible.
        /// </summary>
        [Fact]
        public async Task StockReservationEndpoint_ShouldBeAccessible()
        {
            _output.WriteLine("=== Testing Stock Reservation Endpoint Accessibility ===");

            try
            {
                // Get auth token
                var loginRequest = new { Username = "admin", Password = "admin123" };
                var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
                var authResponse = await _gatewayClient.PostAsync("auth/token", loginContent);
                
                var authResponseContent = await authResponse.Content.ReadAsStringAsync();
                var tokenData = JsonDocument.Parse(authResponseContent);
                var token = tokenData.RootElement.GetProperty("accessToken").GetString();

                // Try to access the stock reservations endpoint (should return empty list or 404, not 500)
                _inventoryClient.DefaultRequestHeaders.Clear();
                _inventoryClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var testOrderId = Guid.NewGuid();
                var response = await _inventoryClient.GetAsync($"api/stockreservations/order/{testOrderId}");
                
                _output.WriteLine($"Stock Reservation Endpoint Response: {response.StatusCode}");

                // Should return 404 (not found) for non-existent order, not 500 (server error)
                Assert.True(
                    response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.OK,
                    $"Expected NotFound or OK, got {response.StatusCode}"
                );

                _output.WriteLine("? Stock Reservation endpoint is accessible");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"? Stock Reservation endpoint test failed: {ex.Message}");
                throw;
            }
        }
    }
}