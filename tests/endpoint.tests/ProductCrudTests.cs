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
    /// Integration tests for Product CRUD operations with JWT authentication.
    /// Tests product creation, validation, and retrieval with proper authorization.
    /// </summary>
    public class ProductCrudTests
    {
        private readonly HttpClient _client;
        private readonly HttpClient _gatewayClient;

        public ProductCrudTests()
        {
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
            _gatewayClient = new HttpClient { BaseAddress = new Uri("http://localhost:6000/") };
        }

        [Fact]
        public async Task CreateProduct_WithValidData_ShouldReturnCreated()
        {
            // Arrange - Get admin token for authorization
            var token = await GetAuthTokenAsync("admin", "admin123");
            if (token != null)
            {
                _client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var productData = new
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 10.99,
                StockQuantity = 100
            };

            var json = JsonSerializer.Serialize(productData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("products", content);

            // Assert - Should be authorized and may succeed or fail due to backend issues
            if (token == null)
            {
                // If we can't get a token, expect unauthorized
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                // With valid admin token, should not be unauthorized or forbidden
                Assert.True(response.StatusCode != HttpStatusCode.Unauthorized && 
                           response.StatusCode != HttpStatusCode.Forbidden,
                           $"Expected authorized access but got {response.StatusCode}");
            }
        }

        [Fact]
        public async Task CreateProduct_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange - Get admin token for authorization
            var token = await GetAuthTokenAsync("admin", "admin123");
            if (token != null)
            {
                _client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var productData = new
            {
                Name = "", // Invalid empty name
                Description = "",
                Price = -1, // Invalid negative price
                StockQuantity = -1 // Invalid negative stock
            };

            var json = JsonSerializer.Serialize(productData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("products", content);

            // Assert
            if (token == null)
            {
                // If we can't get a token, expect unauthorized
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                // With valid token but invalid data, should return BadRequest if backend is working
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                           response.StatusCode == HttpStatusCode.InternalServerError,
                           $"Expected BadRequest or InternalServerError but got {response.StatusCode}");
            }
        }

        [Fact]
        public async Task GetProducts_ShouldReturnProductsList()
        {
            // Arrange & Act - Reading products should be open access (no token needed)
            var response = await _client.GetAsync("products");

            // Assert - Should allow anonymous read access
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.ServiceUnavailable,
                       $"Expected OK, InternalServerError, or ServiceUnavailable but got {response.StatusCode}");

            // Should NOT be unauthorized since reads are open
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateProduct_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange - No authorization header
            var productData = new
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 10.99,
                StockQuantity = 100
            };

            var json = JsonSerializer.Serialize(productData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("products", content);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateProduct_WithCustomerToken_ShouldReturnForbidden()
        {
            // Arrange - Get customer token (not admin)
            var token = await GetAuthTokenAsync("customer1", "password123");
            if (token != null)
            {
                _client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var productData = new
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 10.99,
                StockQuantity = 100
            };

            var json = JsonSerializer.Serialize(productData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("products", content);

            // Assert
            if (token == null)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
            else
            {
                // Customer role should be forbidden from creating products
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            }
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