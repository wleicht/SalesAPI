using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Contracts.Products;
using InventoryApi.Persistence;
using InventoryApi.Models;

namespace InventoryApi.Controllers
{
    /// <summary>
    /// Controller for managing products.
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
        /// Creates a new product.
        /// </summary>
        /// <param name="dto">Product creation data.</param>
        /// <returns>Created product details.</returns>
        [HttpPost]
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
        /// Gets a paginated list of products.
        /// </summary>
        /// <param name="page">Page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>Paginated list of products.</returns>
        [HttpGet]
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
        /// Gets product details by id.
        /// </summary>
        /// <param name="id">Product id.</param>
        /// <returns>Product details.</returns>
        [HttpGet("{id}")]
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
    }
}
