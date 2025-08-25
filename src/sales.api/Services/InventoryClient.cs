using BuildingBlocks.Contracts.Products;
using System.Net.Http;

namespace SalesApi.Services
{
    /// <summary>
    /// HTTP client for communicating with the Inventory API.
    /// </summary>
    public interface IInventoryClient
    {
        /// <summary>
        /// Gets product details by product ID from the Inventory API.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Product details if found, null otherwise.</returns>
        Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets product details by product ID from the Inventory API.
        /// Alias for GetProductByIdAsync for backward compatibility.
        /// </summary>
        /// <param name="productId">The product identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Product details if found, null otherwise.</returns>
        Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of the Inventory API HTTP client.
    /// </summary>
    public class InventoryClient : IInventoryClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<InventoryClient> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryClient"/> class.
        /// </summary>
        /// <param name="httpClient">HTTP client instance.</param>
        /// <param name="logger">Logger instance.</param>
        public InventoryClient(HttpClient httpClient, ILogger<InventoryClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return await GetProductAsync(productId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 3;
            var delay = TimeSpan.FromSeconds(1);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Fetching product {ProductId} from Inventory API (attempt {Attempt}/{MaxRetries})", 
                        productId, attempt, maxRetries);

                    var response = await _httpClient.GetAsync($"products/{productId}", cancellationToken);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Product {ProductId} not found in Inventory API", productId);
                        return null;
                    }

                    response.EnsureSuccessStatusCode();

                    var product = await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken);
                    
                    _logger.LogInformation("Successfully fetched product {ProductId}: {ProductName}", 
                        productId, product?.Name);

                    return product;
                }
                catch (HttpRequestException ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "HTTP error on attempt {Attempt}/{MaxRetries} for product {ProductId}. Retrying in {Delay}ms", 
                        attempt, maxRetries, productId, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
                }
                catch (TaskCanceledException ex) when (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "Timeout on attempt {Attempt}/{MaxRetries} for product {ProductId}. Retrying in {Delay}ms", 
                        attempt, maxRetries, productId, delay.TotalMilliseconds);
                    
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching product {ProductId} on attempt {Attempt}/{MaxRetries}", 
                        productId, attempt, maxRetries);
                    
                    if (attempt == maxRetries)
                        throw;
                }
            }

            throw new HttpRequestException($"Failed to fetch product {productId} after {maxRetries} attempts");
        }
    }

    /// <summary>
    /// Extension methods for configuring the Inventory HTTP client.
    /// </summary>
    public static class InventoryClientExtensions
    {
        /// <summary>
        /// Adds the Inventory HTTP client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="baseAddress">Base address of the Inventory API.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddInventoryClient(this IServiceCollection services, string baseAddress)
        {
            services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
            {
                client.BaseAddress = new Uri(baseAddress);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }
    }
}