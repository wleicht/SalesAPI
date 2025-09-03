using InventoryApi.Application.Commands;
using InventoryApi.Application.DTOs;
using InventoryApi.Domain.Entities;
using InventoryApi.Domain.Services;
using InventoryApi.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InventoryApi.Application.Handlers
{
    /// <summary>
    /// Application service responsible for handling inventory-related commands.
    /// Orchestrates command processing, domain service coordination, and cross-cutting concerns
    /// including validation, logging, and transaction management for inventory operations.
    /// </summary>
    /// <remarks>
    /// Handler Responsibilities:
    /// 
    /// Command Processing:
    /// - Validates command data and business rules
    /// - Coordinates domain service operations
    /// - Manages transaction boundaries and consistency
    /// - Handles error scenarios and exception management
    /// 
    /// Application Logic:
    /// - Maps commands to domain operations
    /// - Coordinates multiple domain services when needed
    /// - Implements application-specific business flows
    /// - Manages cross-cutting concerns (logging, monitoring)
    /// 
    /// Integration Points:
    /// - Domain service orchestration
    /// - Repository pattern implementation
    /// - Event publishing coordination
    /// - External service integration
    /// </remarks>
    public class InventoryCommandHandler
    {
        private readonly IInventoryDomainService _inventoryDomainService;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<InventoryCommandHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the InventoryCommandHandler.
        /// </summary>
        /// <param name="inventoryDomainService">Domain service for inventory operations</param>
        /// <param name="productRepository">Repository for product data access</param>
        /// <param name="logger">Logger for operation monitoring and troubleshooting</param>
        public InventoryCommandHandler(
            IInventoryDomainService inventoryDomainService,
            IProductRepository productRepository,
            ILogger<InventoryCommandHandler> logger)
        {
            _inventoryDomainService = inventoryDomainService ?? throw new ArgumentNullException(nameof(inventoryDomainService));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the creation of a new product with comprehensive validation and processing.
        /// Orchestrates the complete product creation workflow including validation and event publishing.
        /// </summary>
        /// <param name="command">Command containing product creation data</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Result containing created product or error information</returns>
        public async Task<InventoryOperationResultDto> HandleAsync(
            CreateProductCommand command, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "??? Starting product creation | Name: {Name} | Price: {Price} | InitialStock: {InitialStock}",
                command.Name, command.Price, command.InitialStock);

            try
            {
                // Validate command data
                var validationResult = ValidateCreateProductCommand(command);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "? Product creation validation failed | Name: {Name} | Errors: {Errors}",
                        command.Name, string.Join(", ", validationResult.Errors));

                    return InventoryOperationResultDto.ValidationFailure(validationResult.Errors);
                }

                // Create product entity
                var product = new Product(
                    command.Name,
                    command.Description,
                    command.Price,
                    command.InitialStock,
                    command.CreatedBy,
                    command.MinimumStockLevel);

                // Persist product
                var createdProduct = await _productRepository.AddAsync(product, cancellationToken);

                _logger.LogInformation(
                    "? Product created successfully | ProductId: {ProductId} | Name: {Name}",
                    createdProduct.Id, createdProduct.Name);

                // Map domain entity to DTO
                var productDto = MapProductToDto(createdProduct);
                return InventoryOperationResultDto.Success(productDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "?? Product creation failed due to invalid argument | Name: {Name}",
                    command.Name);

                return InventoryOperationResultDto.Failure(ex.Message, "INVALID_ARGUMENT");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "?? Product creation failed due to business rule violation | Name: {Name}",
                    command.Name);

                return InventoryOperationResultDto.Failure(ex.Message, "BUSINESS_RULE_VIOLATION");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during product creation | Name: {Name}",
                    command.Name);

                return InventoryOperationResultDto.Failure(
                    "An unexpected error occurred while creating the product", 
                    "INTERNAL_ERROR");
            }
        }

        /// <summary>
        /// Handles product updates with validation and business rule enforcement.
        /// Processes partial product modifications while maintaining data consistency.
        /// </summary>
        /// <param name="command">Command containing product update data</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Result containing updated product or error information</returns>
        public async Task<InventoryOperationResultDto> HandleAsync(
            UpdateProductCommand command, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Starting product update | ProductId: {ProductId} | UpdatedBy: {UpdatedBy}",
                command.ProductId, command.UpdatedBy);

            try
            {
                // Retrieve existing product
                var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning(
                        "? Product not found for update | ProductId: {ProductId}",
                        command.ProductId);

                    return InventoryOperationResultDto.Failure("Product not found", "PRODUCT_NOT_FOUND");
                }

                // Apply updates
                if (!string.IsNullOrEmpty(command.Name))
                {
                    product.UpdateName(command.Name, command.UpdatedBy);
                }

                if (!string.IsNullOrEmpty(command.Description))
                {
                    product.UpdateDescription(command.Description, command.UpdatedBy);
                }

                if (command.Price.HasValue)
                {
                    product.UpdatePrice(command.Price.Value, command.UpdatedBy);
                }

                if (command.MinimumStockLevel.HasValue)
                {
                    product.UpdateMinimumStockLevel(command.MinimumStockLevel.Value, command.UpdatedBy);
                }

                // Persist changes
                var updatedProduct = await _productRepository.UpdateAsync(product, cancellationToken);

                _logger.LogInformation(
                    "? Product updated successfully | ProductId: {ProductId}",
                    updatedProduct.Id);

                // Map domain entity to DTO
                var productDto = MapProductToDto(updatedProduct);
                return InventoryOperationResultDto.Success(productDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "?? Product update failed due to invalid argument | ProductId: {ProductId}",
                    command.ProductId);

                return InventoryOperationResultDto.Failure(ex.Message, "INVALID_ARGUMENT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during product update | ProductId: {ProductId}",
                    command.ProductId);

                return InventoryOperationResultDto.Failure(
                    "An unexpected error occurred while updating the product", 
                    "INTERNAL_ERROR");
            }
        }

        /// <summary>
        /// Handles stock reservation for pending orders with comprehensive validation.
        /// Orchestrates the stock reservation process through domain services.
        /// </summary>
        /// <param name="command">Command containing stock reservation data</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Result containing stock operation details or error information</returns>
        public async Task<StockOperationResultDto> HandleAsync(
            ReserveStockCommand command, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Starting stock reservation | ProductId: {ProductId} | Quantity: {Quantity} | OrderId: {OrderId}",
                command.ProductId, command.Quantity, command.OrderId);

            try
            {
                // Execute stock reservation through domain service
                var result = await _inventoryDomainService.ReserveStockAsync(
                    command.ProductId,
                    command.Quantity,
                    command.OrderId,
                    command.CustomerId,
                    command.ReservedBy,
                    command.CorrelationId,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "? Stock reserved successfully | ProductId: {ProductId} | Reserved: {Reserved} | Remaining: {Remaining}",
                        result.ProductId, result.ReservedQuantity, result.RemainingStock);

                    return StockOperationResultDto.Success(
                        result.ProductId,
                        result.RemainingStock.Value + result.ReservedQuantity, // Previous stock
                        result.RemainingStock.Value,
                        result.ReservedQuantity,
                        "RESERVATION");
                }

                _logger.LogWarning(
                    "?? Stock reservation failed | ProductId: {ProductId} | Error: {Error}",
                    command.ProductId, result.ErrorMessage);

                return StockOperationResultDto.Failure(command.ProductId, result.ErrorMessage ?? "Unknown error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during stock reservation | ProductId: {ProductId}",
                    command.ProductId);

                return StockOperationResultDto.Failure(
                    command.ProductId, 
                    "An unexpected error occurred while reserving stock");
            }
        }

        /// <summary>
        /// Handles stock allocation for confirmed orders.
        /// Converts reservations to firm allocations through domain services.
        /// </summary>
        /// <param name="command">Command containing stock allocation data</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Result containing stock operation details or error information</returns>
        public async Task<StockOperationResultDto> HandleAsync(
            AllocateStockCommand command, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Starting stock allocation | ProductId: {ProductId} | Quantity: {Quantity} | OrderId: {OrderId}",
                command.ProductId, command.Quantity, command.OrderId);

            try
            {
                // Execute stock allocation through domain service
                var result = await _inventoryDomainService.AllocateStockAsync(
                    command.ProductId,
                    command.Quantity,
                    command.OrderId,
                    command.CustomerId,
                    command.AllocatedBy,
                    command.CorrelationId,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "? Stock allocated successfully | ProductId: {ProductId} | Allocated: {Allocated} | Remaining: {Remaining} | LowStock: {LowStock}",
                        result.ProductId, result.AllocatedQuantity, result.RemainingStock, result.LowStockTriggered);

                    return StockOperationResultDto.Success(
                        result.ProductId,
                        result.RemainingStock.Value + result.AllocatedQuantity, // Previous stock
                        result.RemainingStock.Value,
                        result.AllocatedQuantity,
                        "ALLOCATION",
                        result.LowStockTriggered);
                }

                _logger.LogWarning(
                    "?? Stock allocation failed | ProductId: {ProductId} | Error: {Error}",
                    command.ProductId, result.ErrorMessage);

                return StockOperationResultDto.Failure(command.ProductId, result.ErrorMessage ?? "Unknown error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during stock allocation | ProductId: {ProductId}",
                    command.ProductId);

                return StockOperationResultDto.Failure(
                    command.ProductId, 
                    "An unexpected error occurred while allocating stock");
            }
        }

        /// <summary>
        /// Handles stock release for cancelled orders.
        /// Returns reserved stock to available inventory through domain services.
        /// </summary>
        /// <param name="command">Command containing stock release data</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Result containing stock operation details or error information</returns>
        public async Task<StockOperationResultDto> HandleAsync(
            ReleaseStockCommand command, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Starting stock release | ProductId: {ProductId} | Quantity: {Quantity} | OrderId: {OrderId}",
                command.ProductId, command.Quantity, command.OrderId);

            try
            {
                // Execute stock release through domain service
                var result = await _inventoryDomainService.ReleaseStockAsync(
                    command.ProductId,
                    command.Quantity,
                    command.OrderId,
                    command.CustomerId,
                    command.ReleasedBy,
                    command.ReleaseReason,
                    command.CorrelationId,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "? Stock released successfully | ProductId: {ProductId} | Released: {Released} | NewAvailable: {NewAvailable}",
                        result.ProductId, result.ReleasedQuantity, result.NewAvailableStock);

                    return StockOperationResultDto.Success(
                        result.ProductId,
                        result.NewAvailableStock.Value - result.ReleasedQuantity, // Previous stock
                        result.NewAvailableStock.Value,
                        result.ReleasedQuantity,
                        "RELEASE");
                }

                _logger.LogWarning(
                    "?? Stock release failed | ProductId: {ProductId} | Error: {Error}",
                    command.ProductId, result.ErrorMessage);

                return StockOperationResultDto.Failure(command.ProductId, result.ErrorMessage ?? "Unknown error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during stock release | ProductId: {ProductId}",
                    command.ProductId);

                return StockOperationResultDto.Failure(
                    command.ProductId, 
                    "An unexpected error occurred while releasing stock");
            }
        }

        /// <summary>
        /// Handles stock adjustments for inventory corrections.
        /// Processes positive and negative stock adjustments with audit trails.
        /// </summary>
        /// <param name="command">Command containing stock adjustment data</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Result containing stock operation details or error information</returns>
        public async Task<StockOperationResultDto> HandleAsync(
            AdjustStockCommand command, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Starting stock adjustment | ProductId: {ProductId} | Adjustment: {Adjustment} | Reason: {Reason}",
                command.ProductId, command.AdjustmentQuantity, command.AdjustmentReason);

            try
            {
                // Execute stock adjustment through domain service
                var result = await _inventoryDomainService.AdjustStockAsync(
                    command.ProductId,
                    command.AdjustmentQuantity,
                    command.AdjustmentReason,
                    command.AdjustedBy,
                    command.CorrelationId,
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "? Stock adjusted successfully | ProductId: {ProductId} | Previous: {Previous} | New: {New} | Adjustment: {Adjustment}",
                        result.ProductId, result.PreviousStock, result.NewStock, result.AdjustmentQuantity);

                    return StockOperationResultDto.Success(
                        result.ProductId,
                        result.PreviousStock.Value,
                        result.NewStock.Value,
                        Math.Abs(result.AdjustmentQuantity),
                        "ADJUSTMENT");
                }

                _logger.LogWarning(
                    "?? Stock adjustment failed | ProductId: {ProductId} | Error: {Error}",
                    command.ProductId, result.ErrorMessage);

                return StockOperationResultDto.Failure(command.ProductId, result.ErrorMessage ?? "Unknown error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during stock adjustment | ProductId: {ProductId}",
                    command.ProductId);

                return StockOperationResultDto.Failure(
                    command.ProductId, 
                    "An unexpected error occurred while adjusting stock");
            }
        }

        /// <summary>
        /// Validates the create product command data and business rules.
        /// </summary>
        private CommandValidationResult ValidateCreateProductCommand(CreateProductCommand command)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(command.Name))
                errors.Add("Product name is required");

            if (string.IsNullOrWhiteSpace(command.Description))
                errors.Add("Product description is required");

            if (command.Price < 0)
                errors.Add("Product price cannot be negative");

            if (command.InitialStock < 0)
                errors.Add("Initial stock cannot be negative");

            if (command.MinimumStockLevel < 0)
                errors.Add("Minimum stock level cannot be negative");

            if (string.IsNullOrWhiteSpace(command.CreatedBy))
                errors.Add("CreatedBy is required");

            return new CommandValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
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

    /// <summary>
    /// Represents the result of command validation.
    /// </summary>
    internal class CommandValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}