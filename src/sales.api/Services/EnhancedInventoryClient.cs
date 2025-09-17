using BuildingBlocks.Contracts.Products;
using System.Net;
using System.Text.Json;

namespace SalesApi.Services
{
    /// <summary>
    /// Enhanced HTTP client for communicating with the Inventory API.
    /// Implements resilience patterns including retry and timeout handling.
    /// </summary>
    public class EnhancedInventoryClient : IInventoryClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EnhancedInventoryClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public EnhancedInventoryClient(HttpClient httpClient, ILogger<EnhancedInventoryClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <inheritdoc />
        public async Task<ProductDto?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return await GetProductAsync(productId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            if (productId == Guid.Empty)
            {
                _logger.LogWarning("Invalid product ID provided: {ProductId}", productId);
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));
            }

            var correlationId = $"inv-{Guid.NewGuid():N}";
            const int maxRetries = 3;
            const int delayMs = 1000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "?? Fetching product from Inventory API | ProductId: {ProductId} | Attempt: {Attempt}/{MaxRetries} | CorrelationId: {CorrelationId}",
                        productId, attempt, maxRetries, correlationId);

                    var request = new HttpRequestMessage(HttpMethod.Get, $"products/{productId}");
                    request.Headers.Add("X-Correlation-Id", correlationId);

                    using var response = await _httpClient.SendAsync(request, cancellationToken);
                    return await ProcessInventoryResponse(response, productId, correlationId, cancellationToken);
                }
                catch (HttpRequestException ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex,
                        "?? HTTP error on attempt {Attempt}/{MaxRetries} for product {ProductId}. Retrying in {Delay}ms | CorrelationId: {CorrelationId}",
                        attempt, maxRetries, productId, delayMs * attempt, correlationId);
                    
                    await Task.Delay(delayMs * attempt, cancellationToken); // Linear backoff
                }
                catch (TaskCanceledException ex) when (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning(ex,
                        "?? Timeout on attempt {Attempt}/{MaxRetries} for product {ProductId}. Retrying in {Delay}ms | CorrelationId: {CorrelationId}",
                        attempt, maxRetries, productId, delayMs * attempt, correlationId);
                    
                    await Task.Delay(delayMs * attempt, cancellationToken); // Linear backoff
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "?? Error occurred while fetching product {ProductId} on attempt {Attempt}/{MaxRetries} | CorrelationId: {CorrelationId}",
                        productId, attempt, maxRetries, correlationId);
                    
                    if (attempt == maxRetries)
                    {
                        throw new InvalidOperationException($"Failed to fetch product {productId} after {maxRetries} attempts", ex);
                    }
                }
            }

            throw new InvalidOperationException($"Failed to fetch product {productId} after {maxRetries} attempts");
        }

        private async Task<ProductDto?> ProcessInventoryResponse(
            HttpResponseMessage response,
            Guid productId,
            string correlationId,
            CancellationToken cancellationToken)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "?? Product not found in Inventory API | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
                    productId, correlationId);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "? Inventory API returned error | ProductId: {ProductId} | StatusCode: {StatusCode} | Error: {Error} | CorrelationId: {CorrelationId}",
                    productId, response.StatusCode, errorContent, correlationId);
                
                throw new InvalidOperationException($"Inventory API returned error: {response.StatusCode}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var product = JsonSerializer.Deserialize<ProductDto>(jsonContent, _jsonOptions);

            if (product != null)
            {
                _logger.LogInformation(
                    "? Product fetched successfully | ProductId: {ProductId} | Name: {ProductName} | Price: {Price} | Stock: {Stock} | CorrelationId: {CorrelationId}",
                    productId, product.Name, product.Price, product.StockQuantity, correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "?? Product data could not be deserialized | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
                    productId, correlationId);
            }

            return product;
        }
    }
}