using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BuildingBlocks.Contracts.Orders
{
    /// <summary>
    /// Data Transfer Object for creating an order.
    /// </summary>
    public class CreateOrderDto
    {
        /// <summary>
        /// Customer identifier who is placing the order.
        /// </summary>
        [Required(ErrorMessage = "Customer ID is required.")]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// List of items to be included in the order.
        /// </summary>
        [Required(ErrorMessage = "Order items are required.")]
        [MinLength(1, ErrorMessage = "At least one order item is required.")]
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Data Transfer Object for creating an order item.
    /// </summary>
    public class CreateOrderItemDto
    {
        /// <summary>
        /// Product identifier for the order item.
        /// </summary>
        [Required(ErrorMessage = "Product ID is required.")]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity of the product to order.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for returning order details.
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
        /// Date and time when the order was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// List of items in the order.
        /// </summary>
        public List<OrderItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Data Transfer Object for returning order item details.
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
    }
}