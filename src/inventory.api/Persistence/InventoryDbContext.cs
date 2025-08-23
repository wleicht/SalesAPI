using Microsoft.EntityFrameworkCore;
using InventoryApi.Models;

namespace InventoryApi.Persistence;

/// <summary>
/// Represents the database context for products.
/// </summary>
public class InventoryDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="InventoryDbContext"/>.
    /// </summary>
    /// <param name="options">Database context options.</param>
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    /// <summary>
    /// Product set in the database.
    /// </summary>
    public DbSet<Product> Products { get; set; }
}