using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace EndpointTests
{
    /// <summary>
    /// Integration tests for event-driven architecture functionality.
    /// Tests order creation with event publishing and stock deduction.
    /// </summary>
    public class EventDrivenTests
    {
        private readonly HttpClient _gatewayClient;
        private readonly HttpClient _inventoryClient;
        private readonly HttpClient _salesClient;

        public EventDrivenTests()
        {
            _gatewayClient = new HttpClient { BaseAddress = new Uri("http://localhost:6000/") };
            _inventoryClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
            _salesClient = new HttpClient { BaseAddress = new Uri("http://localhost:5001/") };
        }

        /// <summary>
        /// Tests end-to-end order processing with event-driven stock deduction.
        /// </summary>
        [Fact]
        public async Task CreateOrder_ShouldPublishEventAndDebitStock()
        {
            // Arrange - Get admin token for product creation
            var adminToken = await GetAuthTokenAsync("admin", "admin123");
            Assert.NotNull(adminToken);

            // Create a product with initial stock
            var productRequest = new
            {
                name = "Event Test Product",
                description = "Product for testing event-driven stock deduction",
                price = 99.99m,
                stockQuantity = 100
            };

            _inventoryClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", adminToken);

            var productResponse = await _inventoryClient.PostAsync("products", 
                new StringContent(JsonSerializer.Serialize(productRequest), Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

            var productContent = await productResponse.Content.ReadAsStringAsync();
            var productData = JsonDocument.Parse(productContent);
            var productId = productData.RootElement.GetProperty("id").GetGuid();
            var initialStock = productData.RootElement.GetProperty("stockQuantity").GetInt32();

            // Get customer token for order creation
            var customerToken = await GetAuthTokenAsync("customer1", "password123");
            Assert.NotNull(customerToken);

            // Create order that should trigger event
            var orderRequest = new
            {
                customerId = Guid.NewGuid(),
                items = new[]
                {
                    new
                    {
                        productId = productId,
                        quantity = 5
                    }
                }
            };

            _salesClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", customerToken);

            // Act - Create order (should publish OrderConfirmedEvent)
            var orderResponse = await _salesClient.PostAsync("orders",
                new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json"));

            // Assert - Order creation successful
            Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

            var orderContent = await orderResponse.Content.ReadAsStringAsync();
            var orderData = JsonDocument.Parse(orderContent);
            var orderId = orderData.RootElement.GetProperty("id").GetGuid();
            var orderStatus = orderData.RootElement.GetProperty("status").GetString();

            Assert.Equal("Confirmed", orderStatus);

            // Wait for event processing (async operation)
            await Task.Delay(3000);

            // Verify stock was debited by checking product again
            _inventoryClient.DefaultRequestHeaders.Authorization = null; // Use open access
            var updatedProductResponse = await _inventoryClient.GetAsync($"products/{productId}");
            Assert.Equal(HttpStatusCode.OK, updatedProductResponse.StatusCode);

            var updatedProductContent = await updatedProductResponse.Content.ReadAsStringAsync();
            var updatedProductData = JsonDocument.Parse(updatedProductContent);
            var finalStock = updatedProductData.RootElement.GetProperty("stockQuantity").GetInt32();

            // Assert - Stock was debited (initial 100 - ordered 5 = 95)
            Assert.Equal(initialStock - 5, finalStock);
            Assert.Equal(95, finalStock);
        }

        /// <summary>
        /// Tests order creation with insufficient stock doesn't debit inventory.
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithInsufficientStock_ShouldNotCreateOrderOrDebitStock()
        {
            // Arrange - Get admin token
            var adminToken = await GetAuthTokenAsync("admin", "admin123");
            Assert.NotNull(adminToken);

            // Create product with low stock
            var productRequest = new
            {
                name = "Low Stock Product",
                description = "Product with insufficient stock for testing",
                price = 25.00m,
                stockQuantity = 2
            };

            _inventoryClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", adminToken);

            var productResponse = await _inventoryClient.PostAsync("products",
                new StringContent(JsonSerializer.Serialize(productRequest), Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

            var productContent = await productResponse.Content.ReadAsStringAsync();
            var productData = JsonDocument.Parse(productContent);
            var productId = productData.RootElement.GetProperty("id").GetGuid();
            var initialStock = productData.RootElement.GetProperty("stockQuantity").GetInt32();

            // Get customer token
            var customerToken = await GetAuthTokenAsync("customer1", "password123");
            Assert.NotNull(customerToken);

            // Try to order more than available stock
            var orderRequest = new
            {
                customerId = Guid.NewGuid(),
                items = new[]
                {
                    new
                    {
                        productId = productId,
                        quantity = 10 // More than available (2)
                    }
                }
            };

            _salesClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", customerToken);

            // Act - Try to create order with insufficient stock
            var orderResponse = await _salesClient.PostAsync("orders",
                new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json"));

            // Assert - Order creation should fail
            Assert.Equal(HttpStatusCode.UnprocessableEntity, orderResponse.StatusCode);

            // Verify stock was NOT debited
            _inventoryClient.DefaultRequestHeaders.Authorization = null;
            var updatedProductResponse = await _inventoryClient.GetAsync($"products/{productId}");
            Assert.Equal(HttpStatusCode.OK, updatedProductResponse.StatusCode);

            var updatedProductContent = await updatedProductResponse.Content.ReadAsStringAsync();
            var updatedProductData = JsonDocument.Parse(updatedProductContent);
            var finalStock = updatedProductData.RootElement.GetProperty("stockQuantity").GetInt32();

            // Stock should remain unchanged
            Assert.Equal(initialStock, finalStock);
            Assert.Equal(2, finalStock);
        }

        /// <summary>
        /// Tests multiple orders with event processing work correctly.
        /// </summary>
        [Fact]
        public async Task CreateMultipleOrders_ShouldProcessAllEventsCorrectly()
        {
            // Arrange - Get tokens
            var adminToken = await GetAuthTokenAsync("admin", "admin123");
            var customerToken = await GetAuthTokenAsync("customer1", "password123");
            Assert.NotNull(adminToken);
            Assert.NotNull(customerToken);

            // Create product with sufficient stock
            var productRequest = new
            {
                name = "Bulk Order Product",
                description = "Product for testing multiple orders",
                price = 15.99m,
                stockQuantity = 50
            };

            _inventoryClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", adminToken);

            var productResponse = await _inventoryClient.PostAsync("products",
                new StringContent(JsonSerializer.Serialize(productRequest), Encoding.UTF8, "application/json"));

            var productContent = await productResponse.Content.ReadAsStringAsync();
            var productData = JsonDocument.Parse(productContent);
            var productId = productData.RootElement.GetProperty("id").GetGuid();

            _salesClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", customerToken);

            // Act - Create multiple orders sequentially to avoid race conditions
            var orderRequests = new[]
            {
                new { customerId = Guid.NewGuid(), items = new[] { new { productId, quantity = 3 } } },
                new { customerId = Guid.NewGuid(), items = new[] { new { productId, quantity = 7 } } },
                new { customerId = Guid.NewGuid(), items = new[] { new { productId, quantity = 2 } } }
            };

            // Create orders one by one and wait for each to be processed
            foreach (var request in orderRequests)
            {
                var orderResponse = await _salesClient.PostAsync("orders",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
                
                Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
                
                // Wait a bit between orders to ensure sequential processing
                await Task.Delay(1000);
            }

            // Wait longer for all events to be processed
            await Task.Delay(10000);

            // Verify final stock (50 - 3 - 7 - 2 = 38)
            _inventoryClient.DefaultRequestHeaders.Authorization = null;
            var finalProductResponse = await _inventoryClient.GetAsync($"products/{productId}");
            var finalProductContent = await finalProductResponse.Content.ReadAsStringAsync();
            var finalProductData = JsonDocument.Parse(finalProductContent);
            var finalStock = finalProductData.RootElement.GetProperty("stockQuantity").GetInt32();

            Assert.Equal(38, finalStock);
        }

        /// <summary>
        /// Helper method to get authentication token.
        /// </summary>
        private async Task<string?> GetAuthTokenAsync(string username, string password)
        {
            var loginRequest = new { Username = username, Password = password };
            var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

            var response = await _gatewayClient.PostAsync("auth/token", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenData = JsonDocument.Parse(responseContent);
                return tokenData.RootElement.GetProperty("accessToken").GetString();
            }

            return null;
        }
    }
}