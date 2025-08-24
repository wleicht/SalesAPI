using Microsoft.EntityFrameworkCore;
using SalesApi.Models;

namespace SalesApi.Persistence
{
    /// <summary>
    /// Database context for the Sales API.
    /// </summary>
    public class SalesDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SalesDbContext"/> class.
        /// </summary>
        /// <param name="options">Database context options.</param>
        public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the Orders database set.
        /// </summary>
        public DbSet<Order> Orders { get; set; } = null!;

        /// <summary>
        /// Gets or sets the OrderItems database set.
        /// </summary>
        public DbSet<OrderItem> OrderItems { get; set; } = null!;

        /// <summary>
        /// Configures the model relationships and constraints.
        /// </summary>
        /// <param name="modelBuilder">The model builder instance.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => new { e.OrderId, e.ProductId });
                entity.Property(e => e.ProductName).HasMaxLength(100);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                
                // Configure relationship
                entity.HasOne(e => e.Order)
                      .WithMany(e => e.Items)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}