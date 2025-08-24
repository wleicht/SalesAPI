using System;
using System.ComponentModel.DataAnnotations;

namespace BuildingBlocks.Contracts.Products
{
    /// <summary>
    /// Data Transfer Object for creating a product.
    /// </summary>
    public class CreateProductDto
    {
        /// <summary>
        /// Name of the product.
        /// </summary>
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name must be at most 100 characters.")]
        public required string Name { get; set; }

        /// <summary>
        /// Description of the product.
        /// </summary>
        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, ErrorMessage = "Description must be at most 500 characters.")]
        public required string Description { get; set; }

        /// <summary>
        /// Price of the product.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0.")]
        public decimal Price { get; set; }

        /// <summary>
        /// Stock quantity of the product.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be greater than or equal to 0.")]
        public int StockQuantity { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for returning product details.
    /// </summary>
    public class ProductDto
    {
        /// <summary>
        /// Unique identifier for the product.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the product.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the product.
        /// </summary>
        public string Description { get; set; }

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
