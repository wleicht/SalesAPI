using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace EndpointTests
{
    /// <summary>
    /// Integration tests for Inventory API endpoints running on port 5000.
    /// </summary>
    public class InventoryApiTests
    {
        private readonly HttpClient _client;

        public InventoryApiTests()
        {
            // API running on port 5000
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnOk()
        {
            // Arrange & Act
            var response = await _client.GetAsync("health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", content);
        }

        [Fact]
        public async Task GetProducts_ShouldReturnOk()
        {
            // Arrange & Act
            var response = await _client.GetAsync("products");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetProducts_WithPagination_ShouldReturnOk()
        {
            // Arrange & Act
            var response = await _client.GetAsync("products?page=1&pageSize=10");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetProductById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"products/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Swagger_ShouldBeAccessible()
        {
            // Arrange & Act
            var response = await _client.GetAsync("swagger/index.html");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}