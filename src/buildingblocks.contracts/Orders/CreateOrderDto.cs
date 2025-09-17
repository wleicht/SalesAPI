using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BuildingBlocks.Contracts.Orders
{
    /// <summary>
    /// Data Transfer Object for order creation requests.
    /// Represents the external contract for creating orders through the API.
    /// </summary>
    /// <remarks>
    /// Professional Contract Design Principles:
    /// 
    /// API Contract:
    /// - External facing contract for order creation
    /// - Clean separation from internal domain models
    /// - Flexible validation for different client scenarios
    /// - Backward compatibility considerations
    /// 
    /// Validation Strategy:
    /// - Basic data annotations for simple validation
    /// - FluentValidation for complex business rules
    /// - Client-friendly error messages
    /// - Support for partial data scenarios
    /// 
    /// This contract enables:
    /// - Consistent API interface across all consumers
    /// - Independent evolution of internal models
    /// - Clear documentation for API clients
    /// - Professional error handling and reporting
    /// </remarks>
    public class CreateOrderDto
    {
        /// <summary>
        /// Unique identifier of the customer placing the order.
        /// Can be provided by client or determined from authentication context.
        /// </summary>
        /// <example>f47ac10b-58cc-4372-a567-0e02b2c3d479</example>
        [Required(ErrorMessage = "Customer ID is required")]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// List of items to include in the order.
        /// Must contain at least one valid item with product and quantity information.
        /// </summary>
        /// <example>
        /// [
        ///   {
        ///     "productId": "123e4567-e89b-12d3-a456-426614174000",
        ///     "quantity": 2
        ///   }
        /// ]
        /// </example>
        [Required(ErrorMessage = "Order items are required")]
        [MinLength(1, ErrorMessage = "Order must contain at least one item")]
        public List<CreateOrderItemDto> Items { get; set; } = new();

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// If not provided, system will generate one automatically.
        /// </summary>
        /// <example>order-trace-2024-001</example>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Optional additional notes or instructions for the order.
        /// Can be used for special delivery instructions or customer comments.
        /// </summary>
        /// <example>Please deliver between 9 AM and 5 PM</example>
        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        /// <summary>
        /// Optional priority level for order processing.
        /// Defaults to 'Normal' if not specified.
        /// </summary>
        /// <example>Normal</example>
        public OrderPriority Priority { get; set; } = OrderPriority.Normal;

        /// <summary>
        /// Optional requested delivery date.
        /// If not provided, system will use default delivery schedule.
        /// </summary>
        /// <example>2024-12-25T10:00:00Z</example>
        public DateTime? RequestedDeliveryDate { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for individual order items.
    /// Represents each product and quantity in an order creation request.
    /// </summary>
    public class CreateOrderItemDto
    {
        /// <summary>
        /// Unique identifier of the product to order.
        /// Must reference an existing product in the system.
        /// </summary>
        /// <example>123e4567-e89b-12d3-a456-426614174000</example>
        [Required(ErrorMessage = "Product ID is required")]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity of the product to order.
        /// Must be a positive integer greater than zero.
        /// </summary>
        /// <example>2</example>
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
        public int Quantity { get; set; }

        /// <summary>
        /// Optional unit price for the item.
        /// If not provided, current product price will be used.
        /// Useful for quote-based or negotiated pricing scenarios.
        /// </summary>
        /// <example>29.99</example>
        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative")]
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Optional special instructions for this specific item.
        /// Can be used for customization requests or special handling.
        /// </summary>
        /// <example>Extra large size preferred</example>
        [MaxLength(200, ErrorMessage = "Item notes cannot exceed 200 characters")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Enumeration of order priority levels for processing queue management.
    /// </summary>
    public enum OrderPriority
    {
        /// <summary>
        /// Low priority - process when resources available
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority - standard processing queue
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority - expedited processing
        /// </summary>
        High = 2,

        /// <summary>
        /// Urgent priority - immediate processing required
        /// </summary>
        Urgent = 3
    }
}