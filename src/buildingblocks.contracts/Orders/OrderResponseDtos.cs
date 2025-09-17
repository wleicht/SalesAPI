using System;
using System.Collections.Generic;

namespace BuildingBlocks.Contracts.Orders
{
    /// <summary>
    /// Data Transfer Object for returning order details.
    /// Represents the complete order information in API responses.
    /// </summary>
    public class OrderDto
    {
        /// <summary>
        /// Unique identifier for the order.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Customer identifier who placed the order.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Current status of the order.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Total amount of the order.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Currency code for the order amount.
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Date and time when the order was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the order was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// User who created the order.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// User who last updated the order.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// List of items in the order.
        /// </summary>
        public List<OrderItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Data Transfer Object for returning order item details.
    /// Represents individual items within an order in API responses.
    /// </summary>
    public class OrderItemDto
    {
        /// <summary>
        /// Order identifier this item belongs to.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Product identifier for this order item.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name at the time of order.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of the product ordered.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Unit price of the product at the time of order.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total price for this order item (Quantity * UnitPrice).
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Currency code for the prices.
        /// </summary>
        public string Currency { get; set; } = "USD";
    }
}