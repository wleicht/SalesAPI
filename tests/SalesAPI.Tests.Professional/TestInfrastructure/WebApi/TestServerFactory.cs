using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace SalesAPI.Tests.Professional.TestInfrastructure.WebApi
{
    /// <summary>
    /// Factory for creating HTTP clients for API testing.
    /// Provides simplified approach without WebApplicationFactory complexity.
    /// </summary>
    public class TestServerFactory : IDisposable
    {
        private readonly string _testName;
        private readonly ILogger<TestServerFactory> _logger;
        private readonly List<HttpClient> _createdClients = new();
        private bool _disposed = false;

        public TestServerFactory(string testName, ILogger<TestServerFactory>? logger = null)
        {
            _testName = testName;
            _logger = logger ?? new LoggerFactory().CreateLogger<TestServerFactory>();
            
            _logger.LogInformation("Creating test server factory for test name: {TestName}", testName);
        }

        /// <summary>
        /// Creates an HttpClient configured for API testing.
        /// Points to localhost APIs for integration testing.
        /// </summary>
        /// <param name="baseAddress">Base address for the API</param>
        /// <returns>Configured HttpClient</returns>
        public virtual HttpClient CreateApiClient(string baseAddress = "http://localhost:5000/")
        {
            var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
            
            // Configure default headers for JSON API
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            _createdClients.Add(client);
            
            _logger.LogInformation("Created API client for test server with base address: {BaseAddress}", baseAddress);
            return client;
        }

        /// <summary>
        /// Creates an authenticated HttpClient with the provided token.
        /// </summary>
        /// <param name="token">JWT token for authentication</param>
        /// <param name="baseAddress">Base address for the API</param>
        /// <returns>Configured HttpClient with authorization header</returns>
        public virtual HttpClient CreateAuthenticatedClient(string token, string baseAddress = "http://localhost:5000/")
        {
            var client = CreateApiClient(baseAddress);
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
            
            _logger.LogInformation("Created authenticated API client for test server");
            return client;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInformation("Disposing test server factory and {ClientCount} clients", _createdClients.Count);

            foreach (var client in _createdClients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing HTTP client");
                }
            }

            _createdClients.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Specialized test server factory for Sales API.
    /// </summary>
    public class SalesApiTestServerFactory : TestServerFactory
    {
        public SalesApiTestServerFactory(string testName, ILogger<SalesApiTestServerFactory>? logger = null) 
            : base(testName, logger ?? new LoggerFactory().CreateLogger<SalesApiTestServerFactory>()) { }

        /// <summary>
        /// Creates an API client specifically configured for Sales API.
        /// </summary>
        /// <returns>HttpClient configured for Sales API</returns>
        public override HttpClient CreateApiClient(string baseAddress = "http://localhost:5001/")
        {
            return base.CreateApiClient(baseAddress); // Sales API port
        }

        /// <summary>
        /// Creates an authenticated client for Sales API.
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Authenticated HttpClient for Sales API</returns>
        public override HttpClient CreateAuthenticatedClient(string token, string baseAddress = "http://localhost:5001/")
        {
            return base.CreateAuthenticatedClient(token, baseAddress);
        }
    }

    /// <summary>
    /// Specialized test server factory for Inventory API.
    /// </summary>
    public class InventoryApiTestServerFactory : TestServerFactory
    {
        public InventoryApiTestServerFactory(string testName, ILogger<InventoryApiTestServerFactory>? logger = null) 
            : base(testName, logger ?? new LoggerFactory().CreateLogger<InventoryApiTestServerFactory>()) { }

        /// <summary>
        /// Creates an API client specifically configured for Inventory API.
        /// </summary>
        /// <returns>HttpClient configured for Inventory API</returns>
        public override HttpClient CreateApiClient(string baseAddress = "http://localhost:5000/")
        {
            return base.CreateApiClient(baseAddress); // Inventory API port
        }

        /// <summary>
        /// Creates an authenticated client for Inventory API.
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Authenticated HttpClient for Inventory API</returns>
        public override HttpClient CreateAuthenticatedClient(string token, string baseAddress = "http://localhost:5000/")
        {
            return base.CreateAuthenticatedClient(token, baseAddress);
        }
    }

    /// <summary>
    /// Specialized test server factory for Gateway API.
    /// </summary>
    public class GatewayApiTestServerFactory : TestServerFactory
    {
        public GatewayApiTestServerFactory(string testName, ILogger<GatewayApiTestServerFactory>? logger = null) 
            : base(testName, logger ?? new LoggerFactory().CreateLogger<GatewayApiTestServerFactory>()) { }

        /// <summary>
        /// Creates an API client specifically configured for Gateway API.
        /// </summary>
        /// <returns>HttpClient configured for Gateway API</returns>
        public override HttpClient CreateApiClient(string baseAddress = "http://localhost:6000/")
        {
            return base.CreateApiClient(baseAddress); // Gateway API port
        }

        /// <summary>
        /// Creates an authenticated client for Gateway API.
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Authenticated HttpClient for Gateway API</returns>
        public override HttpClient CreateAuthenticatedClient(string token, string baseAddress = "http://localhost:6000/")
        {
            return base.CreateAuthenticatedClient(token, baseAddress);
        }
    }

    /// <summary>
    /// Helper class for common API testing operations.
    /// </summary>
    public static class ApiTestHelpers
    {
        /// <summary>
        /// Creates JSON content for HTTP requests.
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <returns>StringContent with JSON</returns>
        public static StringContent CreateJsonContent(object obj)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(obj, options);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Deserializes HTTP response content to specified type.
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="response">HTTP response</param>
        /// <returns>Deserialized object</returns>
        public static async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrEmpty(content))
                return default;
                
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<T>(content, options);
        }

        /// <summary>
        /// Creates a login request for authentication testing.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>Login request object</returns>
        public static object CreateLoginRequest(string username, string password)
        {
            return new { Username = username, Password = password };
        }

        /// <summary>
        /// Extracts JWT token from authentication response.
        /// </summary>
        /// <param name="authResponse">Authentication response</param>
        /// <returns>JWT token string</returns>
        public static async Task<string?> ExtractTokenAsync(HttpResponseMessage authResponse)
        {
            if (!authResponse.IsSuccessStatusCode)
                return null;

            var tokenResponse = await DeserializeResponseAsync<TokenResponse>(authResponse);
            return tokenResponse?.AccessToken;
        }

        /// <summary>
        /// Performs authentication and returns the token.
        /// </summary>
        /// <param name="client">HTTP client to use</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>JWT token or null if authentication failed</returns>
        public static async Task<string?> AuthenticateAsync(HttpClient client, string username, string password)
        {
            try
            {
                var loginRequest = CreateLoginRequest(username, password);
                var response = await client.PostAsync("auth/token", CreateJsonContent(loginRequest));
                
                if (response.IsSuccessStatusCode)
                {
                    return await ExtractTokenAsync(response);
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Response model for authentication token.
    /// </summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
    }
}