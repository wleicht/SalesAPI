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
    /// DbSet for managing stock reservations that prevent race conditions during order processing.
    /// Implements the Saga pattern for distributed transaction management across Sales and Inventory services.
    /// </summary>
    /// <remarks>
    /// Stock reservations provide a critical reliability mechanism for e-commerce operations:
    /// 
    /// Business Purpose:
    /// - Prevents overselling by temporarily allocating inventory during order processing
    /// - Implements atomic stock allocation to avoid race conditions in concurrent scenarios
    /// - Supports order cancellation and compensation workflows through reservation release
    /// - Enables sophisticated inventory management and demand planning analytics
    /// 
    /// Technical Implementation:
    /// - Reservations are created synchronously during order creation for immediate feedback
    /// - Status transitions are managed through asynchronous event processing
    /// - Database constraints ensure data consistency and prevent duplicate reservations
    /// - Audit trail capabilities support compliance and troubleshooting requirements
    /// 
    /// Integration Patterns:
    /// - HTTP Synchronous: Initial reservation creation with immediate availability validation
    /// - Event Asynchronous: Order confirmation/cancellation processing via domain events
    /// - Compensation Logic: Automatic reservation release for failed or cancelled orders
    /// - Monitoring Support: Comprehensive logging and metrics for operational visibility
    /// </remarks>
    public DbSet<StockReservation> StockReservations { get; set; }

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

        // Configure StockReservation entity
        modelBuilder.Entity<StockReservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.ReservedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);

            // Create indexes for performance
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ReservedAt);
        });
    }
}