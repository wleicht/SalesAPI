using System;
using System.ComponentModel.DataAnnotations;

namespace SalesApi.Models
{
    /// <summary>
    /// Represents an order in the sales system.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Unique identifier for the order.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Customer identifier who placed the order.
        /// </summary>
        [Required]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Current status of the order (Pending, Confirmed, Cancelled).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Total amount of the order.
        /// </summary>
        [Required]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Date and time when the order was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Navigation property for order items.
        /// </summary>
        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    /// <summary>
    /// Represents an item within an order.
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// Order identifier this item belongs to.
        /// </summary>
        [Required]
        public Guid OrderId { get; set; }

        /// <summary>
        /// Product identifier for this order item.
        /// </summary>
        [Required]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name at the time of order (snapshot).
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of the product ordered.
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// Unit price of the product at the time of order (snapshot).
        /// </summary>
        [Required]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Calculated total price for this line item (Quantity × UnitPrice).
        /// Provides immediate access to the line total for calculations and validation.
        /// </summary>
        public decimal TotalPrice => Quantity * UnitPrice;

        /// <summary>
        /// Navigation property back to the order.
        /// </summary>
        public virtual Order Order { get; set; } = null!;
    }
}