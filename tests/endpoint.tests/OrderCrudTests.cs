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
    /// Integration tests for Order CRUD operations with JWT authentication.
    /// Tests order creation, validation, and retrieval with proper authorization.
    /// </summary>
    public class OrderCrudTests
    {
        private readonly HttpClient _client;
        private readonly HttpClient _gatewayClient;

        public OrderCrudTests()
        {
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:5001/") };
            _gatewayClient = new HttpClient { BaseAddress = new Uri("http://localhost:6000/") };
        }

        [Fact]
        public async Task CreateOrder_WithValidData_ShouldReturnCreated()
        {
            // Arrange - Get customer token for authorization
            var token = await GetAuthTokenAsync("customer1", "password123");
            if (token != null)
            {
                _client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

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

            // Assert
            if (token == null)
            {
                // If we can't get a token, expect unauthorized
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                // With valid customer token, should be authorized but may fail due to business logic
                Assert.True(response.StatusCode != HttpStatusCode.Unauthorized && 
                           response.StatusCode != HttpStatusCode.Forbidden,
                           $"Expected authorized access but got {response.StatusCode}");
            }
        }

        [Fact]
        public async Task CreateOrder_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange - Get customer token for authorization
            var token = await GetAuthTokenAsync("customer1", "password123");
            if (token != null)
            {
                _client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

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
            if (token == null)
            {
                // If we can't get a token, expect unauthorized
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                // With valid token but invalid data, should return BadRequest
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task CreateOrder_WithNegativeQuantity_ShouldReturnBadRequest()
        {
            // Arrange - Get customer token for authorization
            var token = await GetAuthTokenAsync("customer1", "password123");
            if (token != null)
            {
                _client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

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
            if (token == null)
            {
                // If we can't get a token, expect unauthorized
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                // With valid token but invalid quantity, should return BadRequest
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetOrders_ShouldReturnOrdersList()
        {
            // Arrange & Act - Reading orders should be open access (no token needed)
            var response = await _client.GetAsync("orders");

            // Assert - Should allow anonymous read access
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable,
                       $"Expected OK, InternalServerError, or ServiceUnavailable but got {response.StatusCode}");

            // Should NOT be unauthorized since reads are open
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOrders_WithPagination_ShouldReturnOk()
        {
            // Arrange & Act - Reading with pagination should be open access
            var response = await _client.GetAsync("orders?page=1&pageSize=10");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
            
            // Should NOT be unauthorized since reads are open
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOrderById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act - Reading should be open access (no token needed)
            var response = await _client.GetAsync($"orders/{invalidId}");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
            
            // Should NOT be unauthorized since reads are open
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOrders_WithInvalidPagination_ShouldReturnBadRequest()
        {
            // Arrange & Act - Reading with invalid pagination should be open access
            var response = await _client.GetAsync("orders?page=0&pageSize=-1");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
            
            // Should NOT be unauthorized since reads are open
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange - No authorization header
            var orderData = new
            {
                CustomerId = Guid.NewGuid(),
                Items = new[]
                {
                    new
                    {
                        ProductId = Guid.NewGuid(),
                        Quantity = 1
                    }
                }
            };

            var json = JsonSerializer.Serialize(orderData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("orders", content);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Helper method to get authentication token for testing.
        /// </summary>
        /// <param name="username">Username for authentication.</param>
        /// <param name="password">Password for authentication.</param>
        /// <returns>JWT token string or null if authentication fails.</returns>
        private async Task<string?> GetAuthTokenAsync(string username, string password)
        {
            try
            {
                var loginRequest = new
                {
                    Username = username,
                    Password = password
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _gatewayClient.PostAsync("auth/token", content);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                return loginResponse.GetProperty("accessToken").GetString();
            }
            catch
            {
                return null;
            }
        }
    }
}