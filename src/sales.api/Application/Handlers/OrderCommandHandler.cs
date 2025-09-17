using SalesApi.Application.Commands;
using SalesApi.Application.DTOs;
using SalesApi.Application.Validators;
using SalesApi.Domain.Entities;
using SalesApi.Domain.Services;
using SalesApi.Domain.Repositories;
using SalesApi.Services;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MediatR;

namespace SalesApi.Application.Handlers
{
    /// <summary>
    /// MediatR Request Handler responsible for handling order-related commands.
    /// Enhanced with comprehensive validation, error handling, and business logic enforcement.
    /// </summary>
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderOperationResultDto>
    {
        private readonly IOrderDomainService _orderDomainService;
        private readonly IOrderRepository _orderRepository;
        private readonly IValidator<CreateOrderCommand> _validator;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the CreateOrderCommandHandler.
        /// </summary>
        public CreateOrderCommandHandler(
            IOrderDomainService orderDomainService,
            IOrderRepository orderRepository,
            IValidator<CreateOrderCommand> validator,
            ILogger<CreateOrderCommandHandler> logger)
        {
            _orderDomainService = orderDomainService ?? throw new ArgumentNullException(nameof(orderDomainService));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the creation of a new order with comprehensive validation and processing.
        /// </summary>
        public async Task<OrderOperationResultDto> Handle(
            CreateOrderCommand command, 
            CancellationToken cancellationToken = default)
        {
            var correlationId = command.CorrelationId ?? $"order-{Guid.NewGuid():N}";
            
            _logger.LogInformation(
                "?? Processing order creation | Customer: {CustomerId} | Items: {ItemCount} | User: {CreatedBy} | CorrelationId: {CorrelationId}",
                command.CustomerId, command.Items.Count, command.CreatedBy, correlationId);

            try
            {
                // Step 1: Comprehensive validation
                var validationResult = await ValidateCommandAsync(command);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "? Order creation validation failed | Customer: {CustomerId} | Errors: {Errors} | CorrelationId: {CorrelationId}",
                        command.CustomerId, 
                        string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")),
                        correlationId);

                    return OrderOperationResultDto.ValidationFailure(validationResult.Errors.Select(e => e.ErrorMessage));
                }

                // Step 2: Map command to domain service request
                var orderItems = command.Items.Select(item => new CreateOrderItemRequest
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName ?? string.Empty, // Will be enriched by domain service
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice // Will be enriched by domain service if zero
                }).ToList();

                _logger.LogInformation(
                    "?? Mapped command to domain request | ItemCount: {ItemCount} | CorrelationId: {CorrelationId}",
                    orderItems.Count, correlationId);

                // Step 3: Execute order creation through domain service
                var order = await _orderDomainService.CreateOrderAsync(
                    command.CustomerId,
                    orderItems,
                    command.CreatedBy,
                    correlationId,
                    cancellationToken);

                _logger.LogInformation(
                    "? Order created successfully | OrderId: {OrderId} | Customer: {CustomerId} | Total: {Total:C} | CorrelationId: {CorrelationId}",
                    order.Id, command.CustomerId, order.TotalAmount, correlationId);

                // Step 4: Map domain entity to DTO
                var orderDto = MapOrderToDto(order);
                return OrderOperationResultDto.Success(orderDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "?? Order creation failed due to invalid argument | Customer: {CustomerId} | Error: {Error} | CorrelationId: {CorrelationId}",
                    command.CustomerId, ex.Message, correlationId);

                return OrderOperationResultDto.Failure(ex.Message, "INVALID_ARGUMENT");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "?? Order creation failed due to business rule violation | Customer: {CustomerId} | Error: {Error} | CorrelationId: {CorrelationId}",
                    command.CustomerId, ex.Message, correlationId);

                return OrderOperationResultDto.Failure(ex.Message, "BUSINESS_RULE_VIOLATION");
            }
            catch (Exception ex) when (ex.GetType().Name == "ServiceUnavailableException")
            {
                _logger.LogError(ex,
                    "?? Order creation failed due to service unavailability | Customer: {CustomerId} | CorrelationId: {CorrelationId}",
                    command.CustomerId, correlationId);

                return OrderOperationResultDto.Failure(
                    "External service is temporarily unavailable. Please try again later.", 
                    "SERVICE_UNAVAILABLE");
            }
            catch (Exception ex) when (ex.GetType().Name == "ServiceTimeoutException")
            {
                _logger.LogError(ex,
                    "?? Order creation failed due to service timeout | Customer: {CustomerId} | CorrelationId: {CorrelationId}",
                    command.CustomerId, correlationId);

                return OrderOperationResultDto.Failure(
                    "Request timed out. Please try again later.", 
                    "TIMEOUT_ERROR");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Unexpected error during order creation | Customer: {CustomerId} | CorrelationId: {CorrelationId}",
                    command.CustomerId, correlationId);

                return OrderOperationResultDto.Failure(
                    "An unexpected error occurred while creating the order", 
                    "INTERNAL_ERROR");
            }
        }

        /// <summary>
        /// Validates the create order command using FluentValidation.
        /// </summary>
        private async Task<FluentValidation.Results.ValidationResult> ValidateCommandAsync(CreateOrderCommand command)
        {
            var validationResult = await _validator.ValidateAsync(command);
            return validationResult;
        }

        /// <summary>
        /// Maps a domain Order entity to an OrderDto for API response.
        /// Provides clean separation between domain and application layers.
        /// </summary>
        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = "USD", // TODO: Get from domain configuration
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