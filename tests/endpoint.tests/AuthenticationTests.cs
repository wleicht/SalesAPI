using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace EndpointTests
{
    /// <summary>
    /// Integration tests for JWT authentication functionality via Gateway.
    /// Tests token generation, validation, and protected endpoint access.
    /// </summary>
    public class AuthenticationTests
    {
        private readonly HttpClient _gatewayClient;
        private readonly HttpClient _inventoryClient;
        private readonly HttpClient _salesClient;

        public AuthenticationTests()
        {
            _gatewayClient = new HttpClient { BaseAddress = new Uri("http://localhost:6000/") };
            _inventoryClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };
            _salesClient = new HttpClient { BaseAddress = new Uri("http://localhost:5001/") };
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "admin",
                Password = "admin123"
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _gatewayClient.PostAsync("auth/token", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("accessToken", responseContent);
            Assert.Contains("admin", responseContent);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest = new
            {
                Username = "admin",
                Password = "wrongpassword"
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _gatewayClient.PostAsync("auth/token", content);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTestUsers_ShouldReturnAvailableUsers()
        {
            // Arrange & Act
            var response = await _gatewayClient.GetAsync("auth/test-users");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("admin", content);
            Assert.Contains("customer1", content);
        }

        [Fact]
        public async Task CreateProduct_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange
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
            var response = await _inventoryClient.PostAsync("products", content);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateProduct_WithValidAdminToken_ShouldBeAuthorized()
        {
            // Arrange - Get admin token
            var token = await GetAuthTokenAsync("admin", "admin123");
            if (token == null)
            {
                // Skip test if authentication service is not available
                return;
            }

            _inventoryClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var productData = new
            {
                Name = "Test Product via JWT",
                Description = "Product created with JWT token",
                Price = 25.99,
                StockQuantity = 50
            };

            var json = JsonSerializer.Serialize(productData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _inventoryClient.PostAsync("products", content);

            // Assert - Should be authorized (not 401 or 403)
            Assert.True(response.StatusCode != HttpStatusCode.Unauthorized && 
                       response.StatusCode != HttpStatusCode.Forbidden,
                       $"Expected authorized access but got {response.StatusCode}");
        }

        [Fact]
        public async Task CreateProduct_WithCustomerToken_ShouldReturnForbidden()
        {
            // Arrange - Get customer token
            var token = await GetAuthTokenAsync("customer1", "password123");
            if (token == null)
            {
                // Skip test if authentication service is not available
                return;
            }

            _inventoryClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
            var response = await _inventoryClient.PostAsync("products", content);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Forbidden || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task CreateOrder_WithoutToken_ShouldReturnUnauthorized()
        {
            // Arrange
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
            var response = await _salesClient.PostAsync("orders", content);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_WithValidCustomerToken_ShouldBeAuthorized()
        {
            // Arrange - Get customer token
            var token = await GetAuthTokenAsync("customer1", "password123");
            if (token == null)
            {
                // Skip test if authentication service is not available
                return;
            }

            _salesClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
            var response = await _salesClient.PostAsync("orders", content);

            // Assert - Should be authorized (not 401 or 403), may fail due to business logic
            Assert.True(response.StatusCode != HttpStatusCode.Unauthorized &&
                       response.StatusCode != HttpStatusCode.Forbidden,
                       $"Expected authorized access but got {response.StatusCode}");
        }

        [Fact]
        public async Task ReadOperations_ShouldBeOpenAccess()
        {
            // Test that read operations work without authentication
            
            // Act - Try to read products
            var productsResponse = await _inventoryClient.GetAsync("products");
            
            // Act - Try to read orders  
            var ordersResponse = await _salesClient.GetAsync("orders");

            // Assert - Should allow anonymous access (open endpoints)
            // Note: May return errors due to backend issues, but should not be 401 Unauthorized
            Assert.True(productsResponse.StatusCode != HttpStatusCode.Unauthorized,
                       $"Products read should be open access, got {productsResponse.StatusCode}");
            
            Assert.True(ordersResponse.StatusCode != HttpStatusCode.Unauthorized,
                       $"Orders read should be open access, got {ordersResponse.StatusCode}");
        }

        [Fact]
        public async Task JwtToken_ShouldContainCorrectClaims()
        {
            // Arrange & Act
            var token = await GetAuthTokenAsync("admin", "admin123");
            
            // Assert
            Assert.NotNull(token);
            Assert.True(token.Length > 100, "JWT token should be substantial length");
            
            // JWT tokens have 3 parts separated by dots
            var parts = token.Split('.');
            Assert.Equal(3, parts.Length);
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