using SalesApi.Application.Commands;
using SalesApi.Application.DTOs;
using SalesApi.Domain.Entities;
using SalesApi.Domain.Services;
using SalesApi.Domain.Repositories;
using Microsoft.Extensions.Logging;
using MediatR;

namespace SalesApi.Application.Handlers
{
    /// <summary>
    /// MediatR Request Handler responsible for handling order-related commands.
    /// Orchestrates command processing, domain service coordination, and cross-cutting concerns
    /// including validation, logging, and transaction management for order operations.
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
    /// 
    /// The handler follows Application Service patterns from Domain-Driven Design
    /// and provides clean separation between API controllers and domain logic.
    /// </remarks>
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderOperationResultDto>
    {
        private readonly IOrderDomainService _orderDomainService;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the CreateOrderCommandHandler.
        /// </summary>
        /// <param name="orderDomainService">Domain service for order operations</param>
        /// <param name="orderRepository">Repository for order data access</param>
        /// <param name="logger">Logger for operation monitoring and troubleshooting</param>
        public CreateOrderCommandHandler(
            IOrderDomainService orderDomainService,
            IOrderRepository orderRepository,
            ILogger<CreateOrderCommandHandler> logger)
        {
            _orderDomainService = orderDomainService ?? throw new ArgumentNullException(nameof(orderDomainService));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the creation of a new order with comprehensive validation and processing.
        /// Orchestrates the complete order creation workflow including item validation,
        /// inventory coordination, and appropriate event publishing.
        /// </summary>
        /// <param name="command">Command containing order creation data</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Result containing created order or error information</returns>
        /// <remarks>
        /// Creation Process:
        /// 1. Validate command data and business rules
        /// 2. Enrich order items with product information (name, price)
        /// 3. Map command to domain service requests
        /// 4. Execute order creation through domain service
        /// 5. Handle success and error scenarios
        /// 6. Return appropriate result with order information
        /// 
        /// Error Handling:
        /// - Command validation errors
        /// - Product information retrieval errors
        /// - Business rule violations
        /// - Domain service exceptions
        /// - Infrastructure failures
        /// 
        /// Logging and Monitoring:
        /// - Operation start and completion logging
        /// - Performance metrics and timing
        /// - Error scenarios and exception details
        /// - Business process tracking
        /// </remarks>
        public async Task<OrderOperationResultDto> Handle(
            CreateOrderCommand command, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Starting order creation for Customer: {CustomerId} | Items: {ItemCount} | CorrelationId: {CorrelationId}",
                command.CustomerId, command.Items.Count, command.CorrelationId);

            try
            {
                // Validate command data
                var validationResult = ValidateCreateOrderCommand(command);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "? Order creation validation failed for Customer: {CustomerId} | Errors: {Errors}",
                        command.CustomerId, string.Join(", ", validationResult.Errors));

                    return OrderOperationResultDto.ValidationFailure(validationResult.Errors);
                }

                // Map command to domain service request (product info will be enriched by domain service)
                var orderItems = command.Items.Select(item => new CreateOrderItemRequest
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName, // May be empty - will be enriched
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice // May be 0 - will be enriched
                }).ToList();

                // Execute order creation through domain service
                var order = await _orderDomainService.CreateOrderAsync(
                    command.CustomerId,
                    orderItems,
                    command.CreatedBy,
                    command.CorrelationId,
                    cancellationToken);

                _logger.LogInformation(
                    "? Order created successfully | OrderId: {OrderId} | Customer: {CustomerId} | Total: {Total}",
                    order.Id, command.CustomerId, order.TotalAmount);

                // Map domain entity to DTO
                var orderDto = MapOrderToDto(order);
                return OrderOperationResultDto.Success(orderDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "? Order creation failed due to invalid argument | Customer: {CustomerId}",
                    command.CustomerId);

                return OrderOperationResultDto.Failure(ex.Message, "INVALID_ARGUMENT");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "? Order creation failed due to business rule violation | Customer: {CustomerId}",
                    command.CustomerId);

                return OrderOperationResultDto.Failure(ex.Message, "BUSINESS_RULE_VIOLATION");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during order creation | Customer: {CustomerId}",
                    command.CustomerId);

                return OrderOperationResultDto.Failure(
                    "An unexpected error occurred while creating the order", 
                    "INTERNAL_ERROR");
            }
        }

        /// <summary>
        /// Validates the create order command data and business rules.
        /// Ensures command contains valid and complete information for order creation.
        /// </summary>
        /// <param name="command">Command to validate</param>
        /// <returns>Validation result with success status and error details</returns>
        private CommandValidationResult ValidateCreateOrderCommand(CreateOrderCommand command)
        {
            var errors = new List<string>();

            // Validate customer ID
            if (command.CustomerId == Guid.Empty)
                errors.Add("Customer ID is required");

            // Validate created by
            if (string.IsNullOrWhiteSpace(command.CreatedBy))
                errors.Add("CreatedBy is required");

            // Validate items
            if (!command.Items.Any())
                errors.Add("Order must contain at least one item");

            foreach (var item in command.Items)
            {
                if (item.ProductId == Guid.Empty)
                    errors.Add($"Product ID is required for all items");

                if (string.IsNullOrWhiteSpace(item.ProductName))
                    errors.Add($"Product name is required for product {item.ProductId}");

                if (item.Quantity <= 0)
                    errors.Add($"Quantity must be positive for product {item.ProductId}");

                if (item.UnitPrice < 0)
                    errors.Add($"Unit price cannot be negative for product {item.ProductId}");
            }

            // Check for duplicate products
            var duplicateProducts = command.Items
                .GroupBy(i => i.ProductId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var productId in duplicateProducts)
            {
                errors.Add($"Duplicate product found: {productId}");
            }

            return new CommandValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// Maps a domain Order entity to an OrderDto for API response.
        /// Provides clean separation between domain and application layers.
        /// </summary>
        /// <param name="order">Domain order entity</param>
        /// <returns>Order DTO for API response</returns>
        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = "USD", // TODO: Get from domain or configuration
                Items = order.Items.Select(item => new OrderItemDto
                {
                    OrderId = item.OrderId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Currency = "USD"
                }).ToList(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt,
                CreatedBy = order.CreatedBy ?? string.Empty,
                UpdatedBy = order.UpdatedBy ?? order.CreatedBy ?? string.Empty
            };
        }
    }

    /// <summary>
    /// MediatR Request Handler for order confirmation commands.
    /// </summary>
    public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, OrderOperationResultDto>
    {
        private readonly IOrderDomainService _orderDomainService;
        private readonly ILogger<ConfirmOrderCommandHandler> _logger;

        public ConfirmOrderCommandHandler(
            IOrderDomainService orderDomainService,
            ILogger<ConfirmOrderCommandHandler> logger)
        {
            _orderDomainService = orderDomainService;
            _logger = logger;
        }

        public async Task<OrderOperationResultDto> Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "? Starting order confirmation | OrderId: {OrderId} | ConfirmedBy: {ConfirmedBy}",
                command.OrderId, command.ConfirmedBy);

            try
            {
                // Execute order confirmation through domain service
                var order = await _orderDomainService.ConfirmOrderAsync(
                    command.OrderId,
                    command.ConfirmedBy,
                    command.CorrelationId,
                    cancellationToken);

                _logger.LogInformation(
                    "? Order confirmed successfully | OrderId: {OrderId} | Total: {Total}",
                    order.Id, order.TotalAmount);

                // Map domain entity to DTO
                var orderDto = MapOrderToDto(order);
                return OrderOperationResultDto.Success(orderDto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "? Order confirmation failed | OrderId: {OrderId}",
                    command.OrderId);

                return OrderOperationResultDto.Failure(ex.Message, "CONFIRMATION_FAILED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during order confirmation | OrderId: {OrderId}",
                    command.OrderId);

                return OrderOperationResultDto.Failure(
                    "An unexpected error occurred while confirming the order", 
                    "INTERNAL_ERROR");
            }
        }

        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = "USD",
                Items = order.Items.Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Currency = "USD"
                }).ToList(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt,
                CreatedBy = order.CreatedBy ?? string.Empty,
                UpdatedBy = order.UpdatedBy ?? order.CreatedBy ?? string.Empty
            };
        }
    }

    /// <summary>
    /// MediatR Request Handler for order cancellation commands.
    /// </summary>
    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderOperationResultDto>
    {
        private readonly IOrderDomainService _orderDomainService;
        private readonly ILogger<CancelOrderCommandHandler> _logger;

        public CancelOrderCommandHandler(
            IOrderDomainService orderDomainService,
            ILogger<CancelOrderCommandHandler> logger)
        {
            _orderDomainService = orderDomainService;
            _logger = logger;
        }

        public async Task<OrderOperationResultDto> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "? Starting order cancellation | OrderId: {OrderId} | CancelledBy: {CancelledBy} | Reason: {Reason}",
                command.OrderId, command.CancelledBy, command.Reason);

            try
            {
                // Execute order cancellation through domain service
                var order = await _orderDomainService.CancelOrderAsync(
                    command.OrderId,
                    command.CancelledBy,
                    command.Reason,
                    command.CorrelationId,
                    cancellationToken);

                _logger.LogInformation(
                    "? Order cancelled successfully | OrderId: {OrderId}",
                    order.Id);

                // Map domain entity to DTO
                var orderDto = MapOrderToDto(order);
                return OrderOperationResultDto.Success(orderDto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "? Order cancellation failed | OrderId: {OrderId}",
                    command.OrderId);

                return OrderOperationResultDto.Failure(ex.Message, "CANCELLATION_FAILED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during order cancellation | OrderId: {OrderId}",
                    command.OrderId);

                return OrderOperationResultDto.Failure(
                    "An unexpected error occurred while cancelling the order", 
                    "INTERNAL_ERROR");
            }
        }

        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = "USD",
                Items = order.Items.Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Currency = "USD"
                }).ToList(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt,
                CreatedBy = order.CreatedBy ?? string.Empty,
                UpdatedBy = order.UpdatedBy ?? order.CreatedBy ?? string.Empty
            };
        }
    }

    /// <summary>
    /// MediatR Request Handler for order fulfillment commands.
    /// </summary>
    public class MarkOrderAsFulfilledCommandHandler : IRequestHandler<MarkOrderAsFulfilledCommand, OrderOperationResultDto>
    {
        private readonly IOrderDomainService _orderDomainService;
        private readonly ILogger<MarkOrderAsFulfilledCommandHandler> _logger;

        public MarkOrderAsFulfilledCommandHandler(
            IOrderDomainService orderDomainService,
            ILogger<MarkOrderAsFulfilledCommandHandler> logger)
        {
            _orderDomainService = orderDomainService;
            _logger = logger;
        }

        public async Task<OrderOperationResultDto> Handle(MarkOrderAsFulfilledCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "?? Starting order fulfillment marking | OrderId: {OrderId} | FulfilledBy: {FulfilledBy}",
                command.OrderId, command.FulfilledBy);

            try
            {
                // Execute order fulfillment through domain service
                var order = await _orderDomainService.MarkOrderAsFulfilledAsync(
                    command.OrderId,
                    command.FulfilledBy,
                    command.CorrelationId,
                    cancellationToken);

                _logger.LogInformation(
                    "? Order marked as fulfilled successfully | OrderId: {OrderId}",
                    order.Id);

                // Map domain entity to DTO
                var orderDto = MapOrderToDto(order);
                return OrderOperationResultDto.Success(orderDto);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "? Order fulfillment marking failed | OrderId: {OrderId}",
                    command.OrderId);

                return OrderOperationResultDto.Failure(ex.Message, "FULFILLMENT_FAILED");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during order fulfillment marking | OrderId: {OrderId}",
                    command.OrderId);

                return OrderOperationResultDto.Failure(
                    "An unexpected error occurred while marking order as fulfilled", 
                    "INTERNAL_ERROR");
            }
        }

        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = "USD",
                Items = order.Items.Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Currency = "USD"
                }).ToList(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt,
                CreatedBy = order.CreatedBy ?? string.Empty,
                UpdatedBy = order.UpdatedBy ?? order.CreatedBy ?? string.Empty
            };
        }
    }

    /// <summary>
    /// Represents the result of command validation.
    /// Provides validation status and error details for command processing.
    /// </summary>
    internal class CommandValidationResult
    {
        /// <summary>
        /// Indicates whether the command is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Collection of validation error messages.
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
}