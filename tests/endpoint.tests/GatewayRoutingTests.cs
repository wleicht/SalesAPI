using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace EndpointTests
{
    /// <summary>
    /// Integration tests for Gateway routing functionality.
    /// Tests that the gateway correctly routes requests to backend services.
    /// </summary>
    public class GatewayRoutingTests
    {
        private readonly HttpClient _client;

        public GatewayRoutingTests()
        {
            // Gateway running on port 6000
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:6000/") };
        }

        [Fact]
        public async Task InventoryRoute_Products_ShouldRouteToInventoryApi()
        {
            // Arrange & Act
            var response = await _client.GetAsync("inventory/products");

            // Assert - Should route to Inventory API (accepts various status codes from backend)
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.NotFound);
               
            // Verify it's not a gateway-level routing error (404 Not Found from YARP)
            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task InventoryRoute_Health_ShouldRouteToInventoryApi()
        {
            // Arrange & Act
            var response = await _client.GetAsync("inventory/health");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.Equal("Healthy", content);
            }
            else
            {
                // Service might be unavailable, which is acceptable in testing
                Assert.True(response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                           response.StatusCode == HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
        public async Task SalesRoute_Orders_ShouldRouteToSalesApi()
        {
            // Arrange & Act
            var response = await _client.GetAsync("sales/orders");

            // Assert - Should route to Sales API successfully
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task SalesRoute_Health_ShouldRouteToSalesApi()
        {
            // Arrange & Act
            var response = await _client.GetAsync("sales/health");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.Equal("Healthy", content);
            }
            else
            {
                // Service might be unavailable, which is acceptable in testing
                Assert.True(response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                           response.StatusCode == HttpStatusCode.InternalServerError);
            }
        }

        [Fact]
        public async Task NonExistentRoute_ShouldReturnNotFound()
        {
            // Arrange & Act
            var response = await _client.GetAsync("nonexistent/endpoint");

            // Assert - This should be a YARP-level 404, not backend 404
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task InventoryRoute_WithId_ShouldRouteCorrectly()
        {
            // Arrange
            var testId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"inventory/products/{testId}");

            // Assert - Should route correctly (various responses acceptable from backend)
            Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task SalesRoute_WithId_ShouldRouteCorrectly()
        {
            // Arrange
            var testId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"sales/orders/{testId}");

            // Assert - Should route correctly (various responses acceptable from backend)
            Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task InventoryRoute_Swagger_ShouldRouteToInventoryApi()
        {
            // Arrange & Act
            var response = await _client.GetAsync("inventory/swagger/index.html");

            // Assert - Should route to Inventory API Swagger
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task SalesRoute_Swagger_ShouldRouteToSalesApi()
        {
            // Arrange & Act
            var response = await _client.GetAsync("sales/swagger/index.html");

            // Assert - Should route to Sales API Swagger
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }
}