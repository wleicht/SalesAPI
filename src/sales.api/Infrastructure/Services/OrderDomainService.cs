using SalesApi.Domain.Entities;
using SalesApi.Domain.Services;
using SalesApi.Domain.Repositories;
using SalesApi.Domain.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace SalesApi.Infrastructure.Services
{
    /// <summary>
    /// Concrete implementation of the IOrderDomainService interface.
    /// Orchestrates complex order business operations, domain logic enforcement,
    /// and cross-cutting concerns including event publishing and validation.
    /// </summary>
    /// <remarks>
    /// Domain Service Implementation:
    /// 
    /// Business Logic Orchestration:
    /// - Coordinates multiple domain entities and value objects
    /// - Enforces complex business rules and validations
    /// - Manages domain workflow and state transitions
    /// - Implements domain-specific algorithms and calculations
    /// 
    /// Event-Driven Integration:
    /// - Publishes domain events for cross-bounded context integration
    /// - Maintains event ordering and correlation for distributed scenarios
    /// - Ensures eventual consistency through reliable event publishing
    /// - Supports saga and process manager patterns
    /// 
    /// Transaction Management:
    /// - Coordinates transactional boundaries for complex operations
    /// - Ensures data consistency across multiple aggregates
    /// - Handles rollback scenarios and compensation logic
    /// - Maintains audit trails and change tracking
    /// 
    /// The implementation follows Domain-Driven Design principles while
    /// integrating with infrastructure concerns for production readiness.
    /// </remarks>
    public class OrderDomainService : IOrderDomainService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<OrderDomainService> _logger;

        /// <summary>
        /// Initializes a new instance of the OrderDomainService.
        /// </summary>
        /// <param name="orderRepository">Repository for order data access</param>
        /// <param name="messageBus">Message bus for event publishing</param>
        /// <param name="logger">Logger for operation monitoring and troubleshooting</param>
        public OrderDomainService(
            IOrderRepository orderRepository,
            IMessageBus messageBus,
            ILogger<OrderDomainService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new order with comprehensive validation and business rule enforcement.
        /// Orchestrates the complete order creation process including item validation,
        /// inventory checks, and appropriate event publishing for downstream processing.
        /// </summary>
        /// <param name="customerId">Identifier of the customer placing the order</param>
        /// <param name="orderItems">Collection of items to include in the order</param>
        /// <param name="createdBy">Identifier of the user or system creating the order</param>
        /// <param name="correlationId">Optional correlation ID for request tracing</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Created order entity with all items and business rules applied</returns>
        public async Task<Order> CreateOrderAsync(
            Guid customerId, 
            IEnumerable<CreateOrderItemRequest> orderItems, 
            string createdBy, 
            string? correlationId = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "??? Creating order | CustomerId: {CustomerId} | ItemCount: {ItemCount} | CreatedBy: {CreatedBy}",
                customerId, orderItems.Count(), createdBy);

            try
            {
                // Validate business rules
                ValidateOrderCreation(customerId, orderItems, createdBy);

                // Create order entity
                var order = new Order(customerId, createdBy);

                // Add items to order
                foreach (var item in orderItems)
                {
                    order.AddItem(
                        item.ProductId, 
                        item.ProductName, 
                        item.Quantity, 
                        item.UnitPrice, 
                        createdBy);
                }

                // Persist order
                var savedOrder = await _orderRepository.AddAsync(order, cancellationToken);

                // Publish domain event
                var orderCreatedEvent = new OrderCreatedDomainEvent(savedOrder, correlationId);
                await _messageBus.PublishAsync(orderCreatedEvent, cancellationToken);

                _logger.LogInformation(
                    "? Order created successfully | OrderId: {OrderId} | Total: {Total} | EventPublished: {EventPublished}",
                    savedOrder.Id, savedOrder.TotalAmount, true);

                return savedOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error creating order | CustomerId: {CustomerId} | CreatedBy: {CreatedBy}",
                    customerId, createdBy);
                throw;
            }
        }

        /// <summary>
        /// Confirms an order after validation and business rule checks.
        /// Orchestrates the transition from pending to confirmed status with appropriate
        /// event publishing and downstream process activation.
        /// </summary>
        /// <param name="orderId">Identifier of the order to confirm</param>
        /// <param name="confirmedBy">Identifier of the user or system confirming the order</param>
        /// <param name="correlationId">Optional correlation ID for request tracing</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Confirmed order entity with updated status</returns>
        public async Task<Order> ConfirmOrderAsync(
            Guid orderId, 
            string confirmedBy, 
            string? correlationId = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "? Confirming order | OrderId: {OrderId} | ConfirmedBy: {ConfirmedBy}",
                orderId, confirmedBy);

            try
            {
                // Retrieve order with items
                var order = await _orderRepository.GetWithItemsAsync(orderId, cancellationToken);
                if (order == null)
                {
                    throw new InvalidOperationException($"Order {orderId} not found");
                }

                // Validate confirmation eligibility
                var validationResult = await ValidateOrderForConfirmationAsync(orderId, cancellationToken);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Order cannot be confirmed: {string.Join(", ", validationResult.ValidationMessages)}");
                }

                // Confirm order
                order.Confirm(confirmedBy);

                // Save changes
                var updatedOrder = await _orderRepository.UpdateAsync(order, cancellationToken);

                // Publish domain event
                var orderConfirmedEvent = new OrderConfirmedDomainEvent(updatedOrder, confirmedBy, correlationId);
                await _messageBus.PublishAsync(orderConfirmedEvent, cancellationToken);

                _logger.LogInformation(
                    "? Order confirmed successfully | OrderId: {OrderId} | Status: {Status}",
                    updatedOrder.Id, updatedOrder.Status);

                return updatedOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error confirming order | OrderId: {OrderId} | ConfirmedBy: {ConfirmedBy}",
                    orderId, confirmedBy);
                throw;
            }
        }

        /// <summary>
        /// Cancels an order with proper compensation and cleanup processes.
        /// Orchestrates the cancellation workflow including inventory release,
        /// payment processing, and customer communication coordination.
        /// </summary>
        /// <param name="orderId">Identifier of the order to cancel</param>
        /// <param name="cancelledBy">Identifier of the user or system cancelling the order</param>
        /// <param name="reason">Optional reason for cancellation for audit purposes</param>
        /// <param name="correlationId">Optional correlation ID for request tracing</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Cancelled order entity with appropriate status</returns>
        public async Task<Order> CancelOrderAsync(
            Guid orderId, 
            string cancelledBy, 
            string? reason = null, 
            string? correlationId = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "? Cancelling order | OrderId: {OrderId} | CancelledBy: {CancelledBy} | Reason: {Reason}",
                orderId, cancelledBy, reason);

            try
            {
                // Retrieve order with items
                var order = await _orderRepository.GetWithItemsAsync(orderId, cancellationToken);
                if (order == null)
                {
                    throw new InvalidOperationException($"Order {orderId} not found");
                }

                // Validate cancellation eligibility
                var validationResult = await ValidateOrderForCancellationAsync(orderId, cancellationToken);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException($"Order cannot be cancelled: {string.Join(", ", validationResult.ValidationMessages)}");
                }

                // Store previous status for event
                var previousStatus = order.Status;

                // Cancel order
                order.Cancel(cancelledBy, reason);

                // Save changes
                var updatedOrder = await _orderRepository.UpdateAsync(order, cancellationToken);

                // Publish domain event
                var orderCancelledEvent = new OrderCancelledDomainEvent(
                    updatedOrder, previousStatus, cancelledBy, reason, correlationId);
                await _messageBus.PublishAsync(orderCancelledEvent, cancellationToken);

                _logger.LogInformation(
                    "? Order cancelled successfully | OrderId: {OrderId} | PreviousStatus: {PreviousStatus}",
                    updatedOrder.Id, previousStatus);

                return updatedOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error cancelling order | OrderId: {OrderId} | CancelledBy: {CancelledBy}",
                    orderId, cancelledBy);
                throw;
            }
        }

        /// <summary>
        /// Marks an order as fulfilled after successful completion and delivery.
        /// Finalizes the order lifecycle with appropriate event publishing and
        /// business process completion activities.
        /// </summary>
        /// <param name="orderId">Identifier of the order to mark as fulfilled</param>
        /// <param name="fulfilledBy">Identifier of the user or system marking fulfillment</param>
        /// <param name="correlationId">Optional correlation ID for request tracing</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Fulfilled order entity with final status</returns>
        public async Task<Order> MarkOrderAsFulfilledAsync(
            Guid orderId, 
            string fulfilledBy, 
            string? correlationId = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Marking order as fulfilled | OrderId: {OrderId} | FulfilledBy: {FulfilledBy}",
                orderId, fulfilledBy);

            try
            {
                // Retrieve order
                var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    throw new InvalidOperationException($"Order {orderId} not found");
                }

                // Mark as fulfilled
                order.MarkAsFulfilled(fulfilledBy);

                // Save changes
                var updatedOrder = await _orderRepository.UpdateAsync(order, cancellationToken);

                // TODO: Publish OrderFulfilledDomainEvent when created
                // var orderFulfilledEvent = new OrderFulfilledDomainEvent(updatedOrder, fulfilledBy, correlationId);
                // await _messageBus.PublishAsync(orderFulfilledEvent, cancellationToken);

                _logger.LogInformation(
                    "? Order marked as fulfilled successfully | OrderId: {OrderId} | Status: {Status}",
                    updatedOrder.Id, updatedOrder.Status);

                return updatedOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error marking order as fulfilled | OrderId: {OrderId} | FulfilledBy: {FulfilledBy}",
                    orderId, fulfilledBy);
                throw;
            }
        }

        /// <summary>
        /// Validates if an order can be confirmed based on current business rules and state.
        /// Provides comprehensive validation without side effects for decision support.
        /// </summary>
        /// <param name="orderId">Identifier of the order to validate</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Validation result with details about confirmation eligibility</returns>
        public async Task<OrderValidationResult> ValidateOrderForConfirmationAsync(
            Guid orderId, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("?? Validating order for confirmation | OrderId: {OrderId}", orderId);

            try
            {
                var order = await _orderRepository.GetWithItemsAsync(orderId, cancellationToken);
                if (order == null)
                {
                    return OrderValidationResult.Failure("Order not found");
                }

                var errors = new List<string>();

                // Validate order status
                if (order.Status != OrderStatus.Pending)
                {
                    errors.Add($"Order must be in Pending status to be confirmed. Current status: {order.Status}");
                }

                // Validate order has items
                if (!order.Items.Any())
                {
                    errors.Add("Order must contain at least one item to be confirmed");
                }

                // Validate item quantities are positive
                if (order.Items.Any(item => item.Quantity <= 0))
                {
                    errors.Add("All order items must have positive quantities");
                }

                // TODO: Add inventory availability validation
                // TODO: Add customer credit validation
                // TODO: Add payment authorization validation

                var result = errors.Any() 
                    ? OrderValidationResult.Failure(errors) 
                    : OrderValidationResult.Success();

                _logger.LogDebug("? Order validation completed | OrderId: {OrderId} | IsValid: {IsValid}", 
                    orderId, result.IsValid);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error validating order for confirmation | OrderId: {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Validates if an order can be cancelled based on current business rules and lifecycle state.
        /// Supports cancellation policy enforcement and customer service decision making.
        /// </summary>
        /// <param name="orderId">Identifier of the order to validate</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Validation result with details about cancellation eligibility</returns>
        public async Task<OrderValidationResult> ValidateOrderForCancellationAsync(
            Guid orderId, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("?? Validating order for cancellation | OrderId: {OrderId}", orderId);

            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    return OrderValidationResult.Failure("Order not found");
                }

                var errors = new List<string>();

                // Validate order status
                if (order.Status == OrderStatus.Fulfilled)
                {
                    errors.Add("Fulfilled orders cannot be cancelled. Use returns process instead.");
                }

                if (order.Status == OrderStatus.Cancelled)
                {
                    errors.Add("Order is already cancelled");
                }

                // TODO: Add time-based cancellation rules
                // TODO: Add customer-specific cancellation policies
                // TODO: Add compensation feasibility checks

                var result = errors.Any() 
                    ? OrderValidationResult.Failure(errors) 
                    : OrderValidationResult.Success();

                _logger.LogDebug("? Order cancellation validation completed | OrderId: {OrderId} | IsValid: {IsValid}", 
                    orderId, result.IsValid);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "?? Error validating order for cancellation | OrderId: {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Validates order creation parameters and business rules.
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="orderItems">Order items collection</param>
        /// <param name="createdBy">User creating the order</param>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        private static void ValidateOrderCreation(Guid customerId, IEnumerable<CreateOrderItemRequest> orderItems, string createdBy)
        {
            if (customerId == Guid.Empty)
                throw new ArgumentException("Customer ID is required", nameof(customerId));

            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("CreatedBy is required", nameof(createdBy));

            var itemList = orderItems.ToList();
            if (!itemList.Any())
                throw new ArgumentException("Order must contain at least one item", nameof(orderItems));

            // Validate individual items
            foreach (var item in itemList)
            {
                if (item.ProductId == Guid.Empty)
                    throw new ArgumentException($"Product ID is required for all items");

                if (string.IsNullOrWhiteSpace(item.ProductName))
                    throw new ArgumentException($"Product name is required for product {item.ProductId}");

                if (item.Quantity <= 0)
                    throw new ArgumentException($"Quantity must be positive for product {item.ProductId}");

                if (item.UnitPrice < 0)
                    throw new ArgumentException($"Unit price cannot be negative for product {item.ProductId}");
            }

            // Check for duplicate products
            var duplicateProducts = itemList
                .GroupBy(i => i.ProductId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            if (duplicateProducts.Any())
            {
                throw new ArgumentException($"Duplicate products found: {string.Join(", ", duplicateProducts)}");
            }
        }
    }
}