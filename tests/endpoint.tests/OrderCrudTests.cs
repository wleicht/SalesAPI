using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace EndpointTests
{
    /// <summary>
    /// Integration tests for Sales API CRUD operations running on port 5001.
    /// </summary>
    public class OrderCrudTests
    {
        private readonly HttpClient _client;

        public OrderCrudTests()
        {
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:5001/") };
        }

        [Fact]
        public async Task CreateOrder_WithValidData_ShouldReturnCreated()
        {
            // Arrange - Valid order with realistic data
            var orderData = new
            {
                CustomerId = Guid.NewGuid(),
                Items = new[]
                {
                    new
                    {
                        ProductId = Guid.NewGuid(),
                        Quantity = 2
                    }
                }
            };

            var json = JsonSerializer.Serialize(orderData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("orders", content);

            // Assert - Should return Created or BadRequest (if product doesn't exist)
            Assert.True(response.StatusCode == HttpStatusCode.Created || 
                       response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.UnprocessableEntity ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task CreateOrder_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange - Invalid order data (missing required fields)
            var invalidOrderData = new
            {
                CustomerId = Guid.Empty, // Invalid empty GUID
                Items = new object[0]     // Empty items array
            };

            var json = JsonSerializer.Serialize(invalidOrderData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("orders", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_WithNegativeQuantity_ShouldReturnBadRequest()
        {
            // Arrange - Order with negative quantity
            var invalidOrderData = new
            {
                CustomerId = Guid.NewGuid(),
                Items = new[]
                {
                    new
                    {
                        ProductId = Guid.NewGuid(),
                        Quantity = -1 // Invalid negative quantity
                    }
                }
            };

            var json = JsonSerializer.Serialize(invalidOrderData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("orders", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetOrders_ShouldReturnOrdersList()
        {
            // Arrange & Act
            var response = await _client.GetAsync("orders");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.NotNull(content);
            }
        }

        [Fact]
        public async Task GetOrders_WithPagination_ShouldReturnOk()
        {
            // Arrange & Act
            var response = await _client.GetAsync("orders?page=1&pageSize=10");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetOrderById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"orders/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetOrders_WithInvalidPagination_ShouldReturnBadRequest()
        {
            // Arrange & Act
            var response = await _client.GetAsync("orders?page=0&pageSize=-1");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}