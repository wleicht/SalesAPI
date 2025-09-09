using SalesApi.Domain.Entities;
using SalesApi.Domain.Services;
using SalesApi.Application.DTOs;
using MediatR;

namespace SalesApi.Application.Commands
{
    /// <summary>
    /// Represents a command to create a new order in the sales system.
    /// Encapsulates all the necessary data for order creation with validation attributes
    /// and business logic enforcement for the order creation workflow.
    /// </summary>
    /// <remarks>
    /// Command Design Principles:
    /// 
    /// CQRS Implementation:
    /// - Represents an intent to change system state
    /// - Contains all necessary data for the operation
    /// - Immutable after creation to ensure command integrity
    /// - Clear separation between commands and queries
    /// 
    /// Validation Strategy:
    /// - Data validation at command level
    /// - Business rule validation in domain services
    /// - Cross-cutting validation through decorators
    /// - Comprehensive error handling and reporting
    /// 
    /// Use Case Alignment:
    /// - Directly maps to "Create Order" use case
    /// - Contains complete order context
    /// - Supports single responsibility principle
    /// - Enables clear audit trails and logging
    /// 
    /// The command serves as the entry point for order creation
    /// and provides a clean contract for application service consumption.
    /// </remarks>
    public record CreateOrderCommand : IRequest<OrderOperationResultDto>
    {
        /// <summary>
        /// Unique identifier of the customer placing the order.
        /// Must reference an existing and active customer in the system.
        /// </summary>
        /// <value>Customer identifier for order attribution and customer service</value>
        public Guid CustomerId { get; init; }

        /// <summary>
        /// Collection of items to include in the order.
        /// Must contain at least one item with valid product references and quantities.
        /// </summary>
        /// <value>List of order items with product and quantity information</value>
        public List<CreateOrderItemCommand> Items { get; init; } = new();

        /// <summary>
        /// Optional correlation identifier for request tracing across distributed systems.
        /// Enables end-to-end tracking and debugging in microservices architecture.
        /// </summary>
        /// <value>Correlation ID for distributed tracing and monitoring</value>
        public string? CorrelationId { get; init; }

        /// <summary>
        /// Identifier of the user or system creating the order.
        /// Required for audit trails and authorization validation.
        /// </summary>
        /// <value>User identifier for audit and authorization purposes</value>
        public string CreatedBy { get; init; } = string.Empty;
    }

    /// <summary>
    /// Represents a command to create an individual order item within an order.
    /// Contains product reference, quantity, and pricing information for order line items.
    /// </summary>
    /// <remarks>
    /// Order Item Design:
    /// - Immutable data structure for command integrity
    /// - Contains product snapshot information
    /// - Validates quantity and pricing constraints
    /// - Supports order composition and validation
    /// 
    /// Business Rules:
    /// - Product must exist and be available
    /// - Quantity must be positive and within limits
    /// - Pricing information maintained for consistency
    /// - Product name captured for audit and display
    /// </remarks>
    public record CreateOrderItemCommand
    {
        /// <summary>
        /// Unique identifier of the product being ordered.
        /// Must reference an existing and available product in the inventory.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// Name of the product for display and audit purposes.
        /// Captured at order creation time for historical accuracy.
        /// </summary>
        public string ProductName { get; init; } = string.Empty;

        /// <summary>
        /// Quantity of the product being ordered.
        /// Must be a positive integer within business rule limits.
        /// </summary>
        public int Quantity { get; init; }

        /// <summary>
        /// Unit price of the product at the time of order creation.
        /// Captured for pricing consistency and audit compliance.
        /// </summary>
        public decimal UnitPrice { get; init; }
    }

    /// <summary>
    /// Represents a command to confirm an existing order for fulfillment processing.
    /// Triggers the transition from pending to confirmed status with business validation.
    /// </summary>
    /// <remarks>
    /// Confirmation Process:
    /// - Validates order eligibility for confirmation
    /// - Ensures inventory availability and allocation
    /// - Triggers fulfillment and payment workflows
    /// - Updates order status and audit information
    /// 
    /// Business Impact:
    /// - Commits inventory to the customer order
    /// - Initiates fulfillment and shipping processes
    /// - Updates business metrics and reporting
    /// - Triggers customer communication workflows
    /// </remarks>
    public record ConfirmOrderCommand : IRequest<OrderOperationResultDto>
    {
        /// <summary>
        /// Unique identifier of the order to confirm.
        /// </summary>
        public Guid OrderId { get; init; }

        /// <summary>
        /// Identifier of the user or system confirming the order.
        /// </summary>
        public string ConfirmedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a command to cancel an existing order with appropriate compensation.
    /// Handles order cancellation scenarios including inventory release and customer notification.
    /// </summary>
    /// <remarks>
    /// Cancellation Workflow:
    /// - Validates cancellation eligibility based on order status
    /// - Determines compensation requirements (inventory, payment)
    /// - Updates order status and audit information
    /// - Triggers cleanup and customer communication processes
    /// 
    /// Compensation Scenarios:
    /// - Pending orders: Simple status change
    /// - Confirmed orders: Inventory release and payment processing
    /// - Partial fulfillment: Complex compensation with reversals
    /// </remarks>
    public record CancelOrderCommand : IRequest<OrderOperationResultDto>
    {
        /// <summary>
        /// Unique identifier of the order to cancel.
        /// </summary>
        public Guid OrderId { get; init; }

        /// <summary>
        /// Identifier of the user or system cancelling the order.
        /// </summary>
        public string CancelledBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional reason for the cancellation for audit and customer service.
        /// </summary>
        public string? Reason { get; init; }

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a command to mark an order as fulfilled after successful completion.
    /// Finalizes the order lifecycle and triggers completion workflows.
    /// </summary>
    /// <remarks>
    /// Fulfillment Completion:
    /// - Validates order is eligible for fulfillment marking
    /// - Updates final order status and completion timestamp
    /// - Triggers customer satisfaction and feedback workflows
    /// - Updates business metrics and performance tracking
    /// </remarks>
    public record MarkOrderAsFulfilledCommand : IRequest<OrderOperationResultDto>
    {
        /// <summary>
        /// Unique identifier of the order to mark as fulfilled.
        /// </summary>
        public Guid OrderId { get; init; }

        /// <summary>
        /// Identifier of the user or system marking fulfillment.
        /// </summary>
        public string FulfilledBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }
}