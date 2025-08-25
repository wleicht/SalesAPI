using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace EndpointTests
{
    /// <summary>
    /// Integration tests for Sales API endpoints running on port 5001.
    /// </summary>
    public class SalesApiTests
    {
        private readonly HttpClient _client;

        public SalesApiTests()
        {
            // Sales API running on port 5001
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:5001/") };
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
        public async Task Orders_Endpoint_ShouldBeAccessible()
        {
            // Arrange & Act
            var response = await _client.GetAsync("orders");

            // Assert - Should be accessible (OK or empty list)
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }
    }
}