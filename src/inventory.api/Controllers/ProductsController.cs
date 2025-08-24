using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Contracts.Products;
using InventoryApi.Persistence;
using InventoryApi.Models;

namespace InventoryApi.Controllers
{
    /// <summary>
    /// Controller for managing products with role-based authorization.
    /// Read operations are open, write operations require admin role.
    /// </summary>
    [ApiController]
    [Route("products")]
    public class ProductsController : ControllerBase
    {
        private readonly InventoryDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of <see cref="ProductsController"/>.
        /// </summary>
        /// <param name="dbContext">Inventory database context.</param>
        public ProductsController(InventoryDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Creates a new product. Requires admin role.
        /// </summary>
        /// <param name="dto">Product creation data.</param>
        /// <returns>Created product details.</returns>
        /// <response code="201">Product created successfully.</response>
        /// <response code="400">Invalid product data.</response>
        /// <response code="401">Unauthorized - JWT token required.</response>
        /// <response code="403">Forbidden - Admin role required.</response>
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ProductDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto dto)
        {
            // Check ModelState for Data Annotations validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    StockQuantity = dto.StockQuantity,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Products.Add(product);
                await _dbContext.SaveChangesAsync();

                var result = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    CreatedAt = product.CreatedAt
                };
                return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 500, title: "An error occurred while creating the product.", detail: ex.Message));
            }
        }

        /// <summary>
        /// Gets a paginated list of products. Open access - no authentication required.
        /// </summary>
        /// <param name="page">Page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>Paginated list of products.</returns>
        /// <response code="200">Returns paginated product list.</response>
        /// <response code="400">Invalid pagination parameters.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 400, title: "Invalid pagination parameters."));

            var products = await _dbContext.Products
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(product => new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    CreatedAt = product.CreatedAt
                })
                .ToListAsync();

            return Ok(products);
        }

        /// <summary>
        /// Gets product details by id. Open access - no authentication required.
        /// </summary>
        /// <param name="id">Product id.</param>
        /// <returns>Product details.</returns>
        /// <response code="200">Returns product details.</response>
        /// <response code="404">Product not found.</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product == null)
                return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 404, title: "Product not found."));

            var result = new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CreatedAt = product.CreatedAt
            };
            return Ok(result);
        }

        /// <summary>
        /// Updates an existing product. Requires admin role.
        /// </summary>
        /// <param name="id">Product ID to update.</param>
        /// <param name="dto">Updated product data.</param>
        /// <returns>Updated product details.</returns>
        /// <response code="200">Product updated successfully.</response>
        /// <response code="400">Invalid product data.</response>
        /// <response code="401">Unauthorized - JWT token required.</response>
        /// <response code="403">Forbidden - Admin role required.</response>
        /// <response code="404">Product not found.</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var product = await _dbContext.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 404, title: "Product not found."));
                }

                product.Name = dto.Name;
                product.Description = dto.Description;
                product.Price = dto.Price;
                product.StockQuantity = dto.StockQuantity;

                await _dbContext.SaveChangesAsync();

                var result = new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    CreatedAt = product.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 500, title: "An error occurred while updating the product.", detail: ex.Message));
            }
        }

        /// <summary>
        /// Deletes a product. Requires admin role.
        /// </summary>
        /// <param name="id">Product ID to delete.</param>
        /// <returns>No content if successful.</returns>
        /// <response code="204">Product deleted successfully.</response>
        /// <response code="401">Unauthorized - JWT token required.</response>
        /// <response code="403">Forbidden - Admin role required.</response>
        /// <response code="404">Product not found.</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteProduct(Guid id)
        {
            try
            {
                var product = await _dbContext.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 404, title: "Product not found."));
                }

                _dbContext.Products.Remove(product);
                await _dbContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: 500, title: "An error occurred while deleting the product.", detail: ex.Message));
            }
        }
    }
}
