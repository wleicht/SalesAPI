using Microsoft.EntityFrameworkCore;
using InventoryApi.Domain.Entities;

namespace InventoryApi.Persistence;

/// <summary>
/// Represents the database context for inventory management including products and event processing.
/// Updated to use professional domain entities instead of simple models.
/// </summary>
public class InventoryDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="InventoryDbContext"/>.
    /// </summary>
    /// <param name="options">Database context options.</param>
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    /// <summary>
    /// Product set in the database using professional domain entities.
    /// </summary>
    public DbSet<Product> Products { get; set; }

    /// <summary>
    /// Processed events set for idempotency tracking.
    /// </summary>
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

    /// <summary>
    /// Stock reservations for managing inventory allocation.
    /// </summary>
    public DbSet<StockReservation> StockReservations { get; set; }

    /// <summary>
    /// Configure entity relationships and constraints.
    /// </summary>
    /// <param name="modelBuilder">Model builder instance.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product entity (Domain Entity)
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.Id)
                .ValueGeneratedNever() // Generated in domain
                .IsRequired();

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(p => p.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(p => p.StockQuantity)
                .IsRequired();

            entity.Property(p => p.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(p => p.MinimumStockLevel)
                .IsRequired()
                .HasDefaultValue(10);

            // Audit fields
            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.Property(p => p.UpdatedAt)
                .IsRequired(false);

            entity.Property(p => p.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(p => p.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            // Add indexes for performance
            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.CreatedAt);
            entity.HasIndex(p => p.IsActive);
            entity.HasIndex(p => new { p.IsActive, p.StockQuantity });

            // Shadow properties for concurrency
            entity.Property<byte[]>("RowVersion")
                .IsRowVersion();
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