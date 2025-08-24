using Microsoft.EntityFrameworkCore;
using InventoryApi.Models;

namespace InventoryApi.Persistence;

/// <summary>
/// Represents the database context for inventory management including products and event processing.
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

    /// <summary>
    /// Processed events set for idempotency tracking.
    /// </summary>
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

    /// <summary>
    /// Configure entity relationships and constraints.
    /// </summary>
    /// <param name="modelBuilder">Model builder instance.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Description).IsRequired().HasMaxLength(500);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.StockQuantity).IsRequired();
            entity.Property(p => p.CreatedAt).IsRequired();

            // Add index for performance
            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.CreatedAt);
        });

        // Configure ProcessedEvent entity
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(pe => pe.Id);
            entity.Property(pe => pe.EventId).IsRequired();
            entity.Property(pe => pe.EventType).IsRequired().HasMaxLength(200);
            entity.Property(pe => pe.ProcessedAt).IsRequired();
            entity.Property(pe => pe.CorrelationId).HasMaxLength(100);
            entity.Property(pe => pe.ProcessingDetails).HasMaxLength(1000);

            // Create unique index on EventId to enforce idempotency
            entity.HasIndex(pe => pe.EventId).IsUnique();
            
            // Add index for querying by OrderId and ProcessedAt
            entity.HasIndex(pe => pe.OrderId);
            entity.HasIndex(pe => pe.ProcessedAt);
        });
    }
}