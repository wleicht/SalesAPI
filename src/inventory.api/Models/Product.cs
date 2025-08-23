using System;

namespace InventoryApi.Models
{
    /// <summary>
    /// Represents a product in the inventory system.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Unique identifier for the product.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the product.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Description of the product.
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Price of the product.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Stock quantity of the product.
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// Creation date of the product.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}