using SalesApi.Domain.Entities;
using SalesApi.Domain.DomainEvents;
using SalesApi.Domain.Repositories;

namespace SalesApi.Domain.Services
{
    /// <summary>
    /// Defines the contract for order domain services responsible for complex business operations
    /// that don't naturally belong to a single entity. Orchestrates order lifecycle management
    /// with cross-cutting concerns including validation, event publishing, and external integration.
    /// </summary>
    public interface IOrderDomainService
    {
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
        /// <remarks>
        /// Business Process Steps:
        /// 1. Validate customer and order item data
        /// 2. Apply business rules and constraints
        /// 3. Create order entity with proper state
        /// 4. Persist order to repository with transaction management
        /// 5. Publish OrderCreatedDomainEvent for downstream processing
        /// 
        /// Validation Rules:
        /// - Customer must exist and be in good standing
        /// - All products must be available and active
        /// - Order items must have valid quantities and pricing
        /// - Business rules for minimum/maximum order values
        /// 
        /// Event Integration:
        /// - OrderCreatedDomainEvent published after successful creation
        /// - Correlation ID propagated for distributed tracing
        /// - Event contains complete order context for autonomous processing
        /// - Enables inventory planning and customer communication workflows
        /// </remarks>
        Task<Order> CreateOrderAsync(
            Guid customerId, 
            IEnumerable<CreateOrderItemRequest> orderItems, 
            string createdBy, 
            string? correlationId = null, 
            CancellationToken cancellationToken = default);

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
        /// <remarks>
        /// Confirmation Process:
        /// 1. Retrieve order and validate current state
        /// 2. Validate business rules for confirmation
        /// 3. Update order status to confirmed
        /// 4. Persist changes with audit trail
        /// 5. Publish OrderConfirmedDomainEvent for fulfillment activation
        /// 
        /// Business Rules:
        /// - Order must be in pending status
        /// - All order items must be available
        /// - Customer account must be in good standing
        /// - Payment authorization must be successful (if applicable)
        /// 
        /// Downstream Impact:
        /// - Triggers inventory allocation processes
        /// - Activates fulfillment and shipping workflows
        /// - Initiates customer communication sequences
        /// - Updates business metrics and reporting systems
        /// </remarks>
        Task<Order> ConfirmOrderAsync(
            Guid orderId, 
            string confirmedBy, 
            string? correlationId = null, 
            CancellationToken cancellationToken = default);

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
        /// <remarks>
        /// Cancellation Process:
        /// 1. Retrieve order and validate cancellation eligibility
        /// 2. Determine compensation requirements based on order status
        /// 3. Update order status to cancelled with audit information
        /// 4. Persist changes with cancellation details
        /// 5. Publish OrderCancelledDomainEvent for compensation processing
        /// 
        /// Compensation Scenarios:
        /// - Pending orders: Simple status change with minimal compensation
        /// - Confirmed orders: Inventory release and payment processing required
        /// - Partially fulfilled orders: Complex compensation with partial reversals
        /// - Business rule validation for cancellation eligibility
        /// 
        /// Event-Driven Compensation:
        /// - OrderCancelledDomainEvent triggers inventory release
        /// - Payment system processes refunds or authorization voids
        /// - Customer service systems update communication workflows
        /// - Analytics systems update metrics and reporting data
        /// </remarks>
        Task<Order> CancelOrderAsync(
            Guid orderId, 
            string cancelledBy, 
            string? reason = null, 
            string? correlationId = null, 
            CancellationToken cancellationToken = default);

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
        /// <remarks>
        /// Fulfillment Process:
        /// 1. Validate order is eligible for fulfillment completion
        /// 2. Update order status to fulfilled with audit information
        /// 3. Persist final order state with completion timestamp
        /// 4. Publish order fulfillment events for completion workflows
        /// 
        /// Business Rules:
        /// - Order must be in confirmed status
        /// - All order items must be delivered or completed
        /// - Customer satisfaction requirements met
        /// - Final payment processing completed
        /// 
        /// Completion Activities:
        /// - Customer communication for order completion
        /// - Analytics and reporting system updates
        /// - Customer satisfaction and feedback workflows
        /// - Business metrics and performance tracking
        /// </remarks>
        Task<Order> MarkOrderAsFulfilledAsync(
            Guid orderId, 
            string fulfilledBy, 
            string? correlationId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if an order can be confirmed based on current business rules and state.
        /// Provides comprehensive validation without side effects for decision support.
        /// </summary>
        /// <param name="orderId">Identifier of the order to validate</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Validation result with details about confirmation eligibility</returns>
        /// <remarks>
        /// Validation Categories:
        /// - Order state validation (must be pending)
        /// - Customer account validation (good standing, credit limits)
        /// - Product availability validation (inventory levels, active status)
        /// - Business rule validation (minimum order values, restrictions)
        /// 
        /// Result Information:
        /// - Boolean eligibility result
        /// - Detailed validation messages for UI display
        /// - Specific rule violations for troubleshooting
        /// - Recommendations for resolution where applicable
        /// 
        /// Use Cases:
        /// - UI validation before confirmation submission
        /// - Batch processing eligibility checks
        /// - Business rule compliance verification
        /// - Customer service validation scenarios
        /// </remarks>
        Task<OrderValidationResult> ValidateOrderForConfirmationAsync(
            Guid orderId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if an order can be cancelled based on current business rules and lifecycle state.
        /// Supports cancellation policy enforcement and customer service decision making.
        /// </summary>
        /// <param name="orderId">Identifier of the order to validate</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Validation result with details about cancellation eligibility</returns>
        /// <remarks>
        /// Cancellation Policies:
        /// - Time-based cancellation windows and restrictions
        /// - Status-based cancellation eligibility rules
        /// - Customer-specific cancellation policies and limits
        /// - Product-specific cancellation restrictions
        /// 
        /// Validation Scope:
        /// - Order lifecycle state compatibility
        /// - Business policy compliance
        /// - Customer account standing and history
        /// - Compensation feasibility and impact
        /// 
        /// Decision Support:
        /// - Customer service representative guidance
        /// - Automated cancellation workflow decisions
        /// - Policy compliance verification
        /// - Business impact assessment
        /// </remarks>
        Task<OrderValidationResult> ValidateOrderForCancellationAsync(
            Guid orderId, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a request to create an order item with product and quantity information.
    /// Provides a data transfer structure for order creation scenarios with validation support.
    /// </summary>
    /// <remarks>
    /// Request Design:
    /// - Simple data structure for API and service layer integration
    /// - Validation attributes for data integrity
    /// - Serialization support for remote service calls
    /// - Business rule enforcement through validation
    /// </remarks>
    public class CreateOrderItemRequest
    {
        /// <summary>
        /// Identifier of the product to include in the order.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Name of the product for display and validation purposes.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of the product to order.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Unit price of the product at time of order creation.
        /// </summary>
        public decimal UnitPrice { get; set; }
    }

    /// <summary>
    /// Represents the result of order validation operations with detailed feedback.
    /// Provides comprehensive information for decision making and user interface display.
    /// </summary>
    /// <remarks>
    /// Validation Result Design:
    /// - Boolean success indicator for programmatic decisions
    /// - Detailed messages for user interface display
    /// - Structured error information for troubleshooting
    /// - Extensible design for additional validation metadata
    /// </remarks>
    public class OrderValidationResult
    {
        /// <summary>
        /// Indicates whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Collection of validation messages providing detailed feedback.
        /// </summary>
        public List<string> ValidationMessages { get; set; } = new();

        /// <summary>
        /// Optional error code for specific validation failure scenarios.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static OrderValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with error messages.
        /// </summary>
        /// <param name="messages">Validation error messages</param>
        /// <param name="errorCode">Optional error code</param>
        public static OrderValidationResult Failure(IEnumerable<string> messages, string? errorCode = null) => 
            new() 
            { 
                IsValid = false, 
                ValidationMessages = messages.ToList(), 
                ErrorCode = errorCode 
            };

        /// <summary>
        /// Creates a failed validation result with a single error message.
        /// </summary>
        /// <param name="message">Validation error message</param>
        /// <param name="errorCode">Optional error code</param>
        public static OrderValidationResult Failure(string message, string? errorCode = null) => 
            Failure(new[] { message }, errorCode);
    }
}