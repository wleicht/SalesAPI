using Microsoft.EntityFrameworkCore;
using SalesApi.Domain.Entities;
using BuildingBlocks.Events.Domain;
using BuildingBlocks.Domain.Entities;

namespace SalesApi.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework database context for the Sales API domain.
    /// Provides data access capabilities and manages entity relationships
    /// for order processing and sales domain operations.
    /// </summary>
    /// <remarks>
    /// Context Responsibilities:
    /// 
    /// Entity Management:
    /// - Configures entity mappings and relationships
    /// - Manages database schema and migrations
    /// - Provides DbSet properties for entity access
    /// - Handles concurrency and transaction management
    /// 
    /// Data Access Patterns:
    /// - Repository pattern implementation support
    /// - Unit of Work pattern through SaveChanges
    /// - Change tracking for audit and optimization
    /// - Query optimization and performance tuning
    /// 
    /// Domain Integration:
    /// - Domain event publishing integration
    /// - Audit field management and timestamps
    /// - Business rule enforcement at data layer
    /// - Cross-cutting concerns integration
    /// 
    /// The context follows Entity Framework best practices and supports
    /// the domain-driven design patterns implemented in the sales domain.
    /// </remarks>
    public class SalesDbContext : DbContext
    {
        /// <summary>
        /// Database set for Order entities and related operations.
        /// Provides CRUD operations and query capabilities for orders.
        /// </summary>
        public DbSet<Order> Orders { get; set; } = null!;

        /// <summary>
        /// Database set for OrderItem entities and related operations.
        /// Provides access to order line items and composition management.
        /// </summary>
        public DbSet<OrderItem> OrderItems { get; set; } = null!;

        /// <summary>
        /// Initializes a new instance of the SalesDbContext.
        /// </summary>
        /// <param name="options">Configuration options for the context</param>
        public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Configures entity mappings, relationships, and database schema.
        /// Defines the domain model structure and constraints at the database level.
        /// </summary>
        /// <param name="modelBuilder">Model builder for entity configuration</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Orders table
            ConfigureOrder(modelBuilder);

            // Configure OrderItems table
            ConfigureOrderItem(modelBuilder);

            // Configure relationships
            ConfigureRelationships(modelBuilder);

            // Configure indexes for performance
            ConfigureIndexes(modelBuilder);
        }

        /// <summary>
        /// Configures the Order entity mapping and constraints.
        /// </summary>
        /// <param name="modelBuilder">Model builder for configuration</param>
        private static void ConfigureOrder(ModelBuilder modelBuilder)
        {
            var orderEntity = modelBuilder.Entity<Order>();

            // Table configuration
            orderEntity.ToTable("Orders");

            // Primary key
            orderEntity.HasKey(o => o.Id);

            // Properties configuration
            orderEntity.Property(o => o.Id)
                .ValueGeneratedNever() // Generated in domain
                .IsRequired();

            orderEntity.Property(o => o.CustomerId)
                .IsRequired();

            orderEntity.Property(o => o.Status)
                .HasMaxLength(50)
                .IsRequired();

            orderEntity.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Audit fields
            orderEntity.Property(o => o.CreatedAt)
                .IsRequired();

            orderEntity.Property(o => o.UpdatedAt)
                .IsRequired(false);

            orderEntity.Property(o => o.CreatedBy)
                .HasMaxLength(255)
                .IsRequired();

            orderEntity.Property(o => o.UpdatedBy)
                .HasMaxLength(255)
                .IsRequired(false);

            // Shadow properties for concurrency
            orderEntity.Property<byte[]>("RowVersion")
                .IsRowVersion();
        }

        /// <summary>
        /// Configures the OrderItem entity mapping and constraints.
        /// </summary>
        /// <param name="modelBuilder">Model builder for configuration</param>
        private static void ConfigureOrderItem(ModelBuilder modelBuilder)
        {
            var orderItemEntity = modelBuilder.Entity<OrderItem>();

            // Table configuration
            orderItemEntity.ToTable("OrderItems");

            // Composite primary key (OrderId + ProductId)
            orderItemEntity.HasKey(oi => new { oi.OrderId, oi.ProductId });

            // Properties configuration
            orderItemEntity.Property(oi => oi.OrderId)
                .IsRequired();

            orderItemEntity.Property(oi => oi.ProductId)
                .IsRequired();

            orderItemEntity.Property(oi => oi.ProductName)
                .HasMaxLength(255)
                .IsRequired();

            orderItemEntity.Property(oi => oi.Quantity)
                .IsRequired();

            orderItemEntity.Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Computed column for TotalPrice
            orderItemEntity.Property(oi => oi.TotalPrice)
                .HasComputedColumnSql("[Quantity] * [UnitPrice]", stored: false);
        }

        /// <summary>
        /// Configures entity relationships and navigation properties.
        /// </summary>
        /// <param name="modelBuilder">Model builder for configuration</param>
        private static void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            // Order -> OrderItems (One-to-Many)
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        /// <summary>
        /// Configures database indexes for performance optimization.
        /// </summary>
        /// <param name="modelBuilder">Model builder for configuration</param>
        private static void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            // Order indexes
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CustomerId)
                .HasDatabaseName("IX_Orders_CustomerId");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status)
                .HasDatabaseName("IX_Orders_Status");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CreatedAt)
                .HasDatabaseName("IX_Orders_CreatedAt");

            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.CustomerId, o.Status })
                .HasDatabaseName("IX_Orders_CustomerId_Status");

            // OrderItem indexes
            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.ProductId)
                .HasDatabaseName("IX_OrderItems_ProductId");
        }

        /// <summary>
        /// Overrides SaveChanges to handle audit fields and domain events.
        /// Automatically manages created/updated timestamps and user tracking.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Number of entities written to the database</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Update audit fields for tracked entities
            UpdateAuditFields();

            // TODO: Publish domain events here
            // var domainEvents = GetDomainEvents();
            // await PublishDomainEvents(domainEvents);

            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Updates audit fields for entities being tracked.
        /// Automatically sets CreatedAt/UpdatedAt timestamps.
        /// </summary>
        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries<AuditableEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var now = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    // For new entities, set creation time if not already set
                    // The AuditableEntity constructor handles this automatically
                }
                else if (entry.State == EntityState.Modified)
                {
                    // For modified entities, update the audit fields
                    entry.Entity.UpdateAuditFields("system"); // TODO: Get actual user context
                }
            }
        }

        /// <summary>
        /// Retrieves domain events from tracked entities for publishing.
        /// Supports event-driven architecture patterns.
        /// </summary>
        /// <returns>Collection of domain events to publish</returns>
        private IEnumerable<IDomainEvent> GetDomainEvents()
        {
            // TODO: Implement domain event collection
            // This would collect events from aggregates before saving
            return Enumerable.Empty<IDomainEvent>();
        }
    }
}