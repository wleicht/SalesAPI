using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace EndpointTests
{
    /// <summary>
    /// Integration tests for Gateway API endpoints running on port 6000.
    /// Tests the reverse proxy functionality and routing capabilities.
    /// </summary>
    public class GatewayApiTests
    {
        private readonly HttpClient _client;

        public GatewayApiTests()
        {
            // Gateway running on port 6000
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:6000/") };
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
        public async Task GatewayStatus_ShouldReturnStatusInformation()
        {
            // Arrange & Act
            var response = await _client.GetAsync("gateway/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("SalesAPI Gateway", content);
            Assert.Contains("Healthy", content);
        }

        [Fact]
        public async Task GatewayRoutes_ShouldReturnRoutingInformation()
        {
            // Arrange & Act
            var response = await _client.GetAsync("gateway/routes");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Inventory API", content);
            Assert.Contains("Sales API", content);
            Assert.Contains("/inventory/", content);
            Assert.Contains("/sales/", content);
        }
    }
}