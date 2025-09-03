using InventoryApi.Application.Queries;
using InventoryApi.Application.DTOs;
using InventoryApi.Domain.Entities;
using InventoryApi.Domain.Repositories;
using InventoryApi.Domain.Services;
using Microsoft.Extensions.Logging;

namespace InventoryApi.Application.Handlers
{
    /// <summary>
    /// Application service responsible for handling inventory-related queries.
    /// Orchestrates query processing, data retrieval, and response formatting
    /// for inventory information requests in the inventory domain.
    /// </summary>
    /// <remarks>
    /// Handler Responsibilities:
    /// 
    /// Query Processing:
    /// - Processes read-only data requests
    /// - Optimizes data retrieval for specific use cases
    /// - Handles pagination and filtering requirements
    /// - Manages query performance and caching strategies
    /// 
    /// Data Projection:
    /// - Maps domain entities to appropriate DTOs
    /// - Applies projections for performance optimization
    /// - Handles data aggregation and statistics
    /// - Supports multiple response formats
    /// 
    /// Cross-Cutting Concerns:
    /// - Logging and monitoring for query operations
    /// - Error handling and exception management
    /// - Performance tracking and optimization
    /// - Caching strategy implementation
    /// </remarks>
    public class InventoryQueryHandler
    {
        private readonly IProductRepository _productRepository;
        private readonly IInventoryDomainService _inventoryDomainService;
        private readonly ILogger<InventoryQueryHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the InventoryQueryHandler.
        /// </summary>
        /// <param name="productRepository">Repository for product data access</param>
        /// <param name="inventoryDomainService">Domain service for inventory operations</param>
        /// <param name="logger">Logger for operation monitoring and troubleshooting</param>
        public InventoryQueryHandler(
            IProductRepository productRepository,
            IInventoryDomainService inventoryDomainService,
            ILogger<InventoryQueryHandler> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _inventoryDomainService = inventoryDomainService ?? throw new ArgumentNullException(nameof(inventoryDomainService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles retrieval of a single product by its unique identifier.
        /// Provides complete product information for display and management scenarios.
        /// </summary>
        /// <param name="query">Query containing product identifier</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Product DTO if found, null otherwise</returns>
        public async Task<ProductDto?> HandleAsync(
            GetProductByIdQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Retrieving product by ID | ProductId: {ProductId}",
                query.ProductId);

            try
            {
                var product = await _productRepository.GetByIdAsync(query.ProductId, cancellationToken);

                if (product == null)
                {
                    _logger.LogInformation(
                        "?? Product not found | ProductId: {ProductId}",
                        query.ProductId);
                    
                    return null;
                }

                _logger.LogInformation(
                    "? Product retrieved successfully | ProductId: {ProductId} | Name: {Name}",
                    product.Id, product.Name);

                return MapProductToDto(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error retrieving product | ProductId: {ProductId}",
                    query.ProductId);
                
                throw;
            }
        }

        /// <summary>
        /// Handles retrieval of multiple products by their identifiers.
        /// Supports batch operations and order processing scenarios.
        /// </summary>
        /// <param name="query">Query containing product identifiers</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of product DTOs</returns>
        public async Task<List<ProductDto>> HandleAsync(
            GetProductsByIdsQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Retrieving products by IDs | Count: {Count}",
                query.ProductIds.Count);

            try
            {
                var products = await _productRepository.GetByIdsAsync(query.ProductIds, cancellationToken);
                var productList = products.ToList();

                _logger.LogInformation(
                    "? Products retrieved successfully | Requested: {Requested} | Found: {Found}",
                    query.ProductIds.Count, productList.Count);

                return productList.Select(MapProductToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error retrieving products by IDs | Count: {Count}",
                    query.ProductIds.Count);
                
                throw;
            }
        }

        /// <summary>
        /// Handles retrieval of active products with pagination and optional search.
        /// Supports catalog browsing and product management scenarios.
        /// </summary>
        /// <param name="query">Query containing pagination and search criteria</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated result containing active products</returns>
        public async Task<PagedProductResultDto> HandleAsync(
            GetActiveProductsQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Retrieving active products | Page: {Page} | PageSize: {PageSize} | SearchTerm: {SearchTerm}",
                query.PageNumber, query.PageSize, query.SearchTerm);

            try
            {
                IEnumerable<Product> products;

                if (!string.IsNullOrEmpty(query.SearchTerm))
                {
                    products = await _productRepository.SearchByNameAsync(
                        query.SearchTerm, 
                        query.PageNumber, 
                        query.PageSize, 
                        cancellationToken);
                }
                else
                {
                    products = await _productRepository.GetActiveProductsAsync(
                        query.PageNumber, 
                        query.PageSize, 
                        cancellationToken);
                }

                var productList = products.ToList();
                var totalCount = await _productRepository.CountActiveProductsAsync(cancellationToken);

                _logger.LogInformation(
                    "? Active products retrieved | Count: {Count} | Total: {Total}",
                    productList.Count, totalCount);

                return new PagedProductResultDto
                {
                    Products = productList.Select(MapProductToDto).ToList(),
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
                    HasPreviousPage = query.PageNumber > 1,
                    HasNextPage = query.PageNumber < Math.Ceiling((double)totalCount / query.PageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error retrieving active products | SearchTerm: {SearchTerm}",
                    query.SearchTerm);
                
                throw;
            }
        }

        /// <summary>
        /// Handles retrieval of products with low stock levels.
        /// Supports inventory management and replenishment planning.
        /// </summary>
        /// <param name="query">Query containing low stock criteria</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of low stock products</returns>
        public async Task<List<ProductDto>> HandleAsync(
            GetLowStockProductsQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Retrieving low stock products | ActiveOnly: {ActiveOnly} | CustomThreshold: {CustomThreshold}",
                query.ActiveOnly, query.CustomThreshold);

            try
            {
                var products = await _productRepository.GetLowStockProductsAsync(cancellationToken);
                var productList = products.ToList();

                // Apply additional filters if specified
                if (query.ActiveOnly)
                {
                    productList = productList.Where(p => p.IsActive).ToList();
                }

                if (query.CustomThreshold.HasValue)
                {
                    productList = productList.Where(p => p.StockQuantity <= query.CustomThreshold.Value).ToList();
                }

                _logger.LogInformation(
                    "? Low stock products retrieved | Count: {Count}",
                    productList.Count);

                return productList.Select(MapProductToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error retrieving low stock products");
                
                throw;
            }
        }

        /// <summary>
        /// Handles retrieval of products that are currently out of stock.
        /// Supports urgent inventory management and customer service scenarios.
        /// </summary>
        /// <param name="query">Query containing out of stock criteria</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of out of stock products</returns>
        public async Task<List<ProductDto>> HandleAsync(
            GetOutOfStockProductsQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Retrieving out of stock products | ActiveOnly: {ActiveOnly}",
                query.ActiveOnly);

            try
            {
                var products = await _productRepository.GetOutOfStockProductsAsync(cancellationToken);
                var productList = products.ToList();

                // Apply active filter if specified
                if (query.ActiveOnly)
                {
                    productList = productList.Where(p => p.IsActive).ToList();
                }

                _logger.LogInformation(
                    "? Out of stock products retrieved | Count: {Count}",
                    productList.Count);

                return productList.Select(MapProductToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error retrieving out of stock products");
                
                throw;
            }
        }

        /// <summary>
        /// Handles product search by name with flexible matching.
        /// Supports catalog search and product discovery workflows.
        /// </summary>
        /// <param name="query">Query containing search criteria</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated result containing matching products</returns>
        public async Task<PagedProductResultDto> HandleAsync(
            SearchProductsQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Searching products | SearchTerm: {SearchTerm} | Page: {Page} | ActiveOnly: {ActiveOnly}",
                query.SearchTerm, query.PageNumber, query.ActiveOnly);

            try
            {
                var products = await _productRepository.SearchByNameAsync(
                    query.SearchTerm, 
                    query.PageNumber, 
                    query.PageSize, 
                    cancellationToken);

                var productList = products.ToList();

                // Apply active filter if specified
                if (query.ActiveOnly)
                {
                    productList = productList.Where(p => p.IsActive).ToList();
                }

                // For search, we estimate total count based on current page results
                var totalCount = productList.Count < query.PageSize ? 
                    (query.PageNumber - 1) * query.PageSize + productList.Count :
                    query.PageNumber * query.PageSize + 1; // Indicate there might be more

                _logger.LogInformation(
                    "? Product search completed | SearchTerm: {SearchTerm} | Found: {Found}",
                    query.SearchTerm, productList.Count);

                return new PagedProductResultDto
                {
                    Products = productList.Select(MapProductToDto).ToList(),
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
                    HasPreviousPage = query.PageNumber > 1,
                    HasNextPage = productList.Count == query.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error searching products | SearchTerm: {SearchTerm}",
                    query.SearchTerm);
                
                throw;
            }
        }

        /// <summary>
        /// Handles stock availability checking for multiple products.
        /// Supports order validation and availability checking scenarios.
        /// </summary>
        /// <param name="query">Query containing stock requirements</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Batch availability result with detailed information</returns>
        public async Task<BatchStockAvailabilityDto> HandleAsync(
            CheckStockAvailabilityQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Checking stock availability | ProductCount: {ProductCount}",
                query.StockRequirements.Count);

            try
            {
                var stockRequirements = query.StockRequirements.Select(r => new Domain.Services.StockRequirement
                {
                    ProductId = r.ProductId,
                    RequiredQuantity = r.RequiredQuantity
                }).ToList();

                // Use domain service for batch validation
                var validationResult = await _inventoryDomainService.ValidateStockAvailabilityAsync(
                    stockRequirements, 
                    cancellationToken);

                var availabilities = validationResult.ProductValidations.Select(pv => new StockAvailabilityDto
                {
                    ProductId = pv.ProductId,
                    ProductName = "Product", // TODO: Get actual product name
                    AvailableQuantity = pv.AvailableQuantity,
                    RequestedQuantity = pv.RequestedQuantity,
                    IsAvailable = pv.IsAvailable,
                    MaxAllocatable = pv.MaxAllocatable,
                    AvailabilityMessage = pv.Message
                }).ToList();

                _logger.LogInformation(
                    "? Stock availability checked | ProductCount: {ProductCount} | AllAvailable: {AllAvailable}",
                    query.StockRequirements.Count, validationResult.AllAvailable);

                return new BatchStockAvailabilityDto
                {
                    AllAvailable = validationResult.AllAvailable,
                    ProductAvailabilities = availabilities,
                    Summary = validationResult.Summary,
                    CheckedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error checking stock availability | ProductCount: {ProductCount}",
                    query.StockRequirements.Count);
                
                throw;
            }
        }

        /// <summary>
        /// Handles retrieval of inventory statistics and metrics.
        /// Supports business intelligence and operational reporting scenarios.
        /// </summary>
        /// <param name="query">Query containing statistics criteria</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Comprehensive inventory statistics</returns>
        public async Task<InventoryStatisticsDto> HandleAsync(
            GetInventoryStatisticsQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Calculating inventory statistics | ActiveOnly: {ActiveOnly} | Category: {Category}",
                query.ActiveOnly, query.Category);

            try
            {
                // Get active products count
                var activeProductsCount = await _productRepository.CountActiveProductsAsync(cancellationToken);
                
                // Get low stock products count
                var lowStockCount = await _productRepository.CountLowStockProductsAsync(cancellationToken);
                
                // Get total inventory value
                var totalValue = await _productRepository.CalculateTotalInventoryValueAsync(cancellationToken);

                // Get additional statistics by retrieving sample of products
                var sampleProducts = await _productRepository.GetActiveProductsAsync(1, 100, cancellationToken);
                var productList = sampleProducts.ToList();

                var outOfStockCount = productList.Count(p => p.IsOutOfStock());
                var totalProducts = query.ActiveOnly ? activeProductsCount : activeProductsCount + 100; // Estimate
                var totalStockUnits = productList.Sum(p => p.StockQuantity);
                var averagePrice = productList.Any() ? (decimal)productList.Average(p => (double)p.Price) : 0;
                var averageStock = productList.Any() ? (decimal)productList.Average(p => (double)p.StockQuantity) : 0;

                _logger.LogInformation(
                    "? Inventory statistics calculated | TotalProducts: {TotalProducts} | TotalValue: {TotalValue}",
                    totalProducts, totalValue);

                return new InventoryStatisticsDto
                {
                    TotalProducts = totalProducts,
                    ActiveProducts = activeProductsCount,
                    InactiveProducts = totalProducts - activeProductsCount,
                    LowStockProducts = lowStockCount,
                    OutOfStockProducts = outOfStockCount,
                    TotalInventoryValue = totalValue,
                    Currency = query.Category ?? "USD",
                    AverageProductPrice = averagePrice,
                    TotalStockUnits = totalStockUnits,
                    AverageStockLevel = averageStock,
                    LowStockPercentage = totalProducts > 0 ? (decimal)lowStockCount / totalProducts * 100 : 0,
                    OutOfStockPercentage = totalProducts > 0 ? (decimal)outOfStockCount / totalProducts * 100 : 0,
                    CalculatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error calculating inventory statistics");
                
                throw;
            }
        }

        /// <summary>
        /// Handles retrieval of replenishment recommendations.
        /// Supports proactive inventory management and procurement planning.
        /// </summary>
        /// <param name="query">Query containing replenishment criteria</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of replenishment recommendations</returns>
        public async Task<List<ReplenishmentRecommendationDto>> HandleAsync(
            GetReplenishmentRecommendationsQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Getting replenishment recommendations | MinPriority: {MinPriority} | MaxRecommendations: {MaxRecommendations}",
                query.MinimumPriority, query.MaxRecommendations);

            try
            {
                var recommendations = await _inventoryDomainService.GetReplenishmentRecommendationsAsync(cancellationToken);
                var recommendationList = recommendations.ToList();

                // Filter by minimum priority
                var filteredRecommendations = recommendationList
                    .Where(r => (int)r.Priority >= (int)query.MinimumPriority)
                    .Take(query.MaxRecommendations)
                    .ToList();

                var result = filteredRecommendations.Select(r => new ReplenishmentRecommendationDto
                {
                    ProductId = r.ProductId,
                    ProductName = r.ProductName,
                    CurrentStock = r.CurrentStock.Value,
                    MinimumStock = r.MinimumStock.Value,
                    RecommendedQuantity = r.RecommendedQuantity,
                    Priority = r.Priority.ToString(),
                    Reason = r.Reason,
                    EstimatedCost = r.RecommendedQuantity * 10m, // TODO: Get actual product price
                    DaysUntilStockOut = null // TODO: Calculate based on consumption patterns
                }).ToList();

                _logger.LogInformation(
                    "? Replenishment recommendations retrieved | Count: {Count}",
                    result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error getting replenishment recommendations");
                
                throw;
            }
        }

        /// <summary>
        /// Maps a domain Product entity to a ProductDto for API response.
        /// </summary>
        private static ProductDto MapProductToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Currency = "USD",
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                MinimumStockLevel = product.MinimumStockLevel,
                IsLowStock = product.IsLowStock(),
                IsOutOfStock = product.IsOutOfStock(),
                IsAvailableForOrder = product.IsAvailableForOrder(),
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt ?? product.CreatedAt,
                CreatedBy = product.CreatedBy ?? string.Empty,
                UpdatedBy = product.UpdatedBy ?? product.CreatedBy ?? string.Empty
            };
        }
    }
}