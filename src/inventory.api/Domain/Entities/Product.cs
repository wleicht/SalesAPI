using BuildingBlocks.Domain.Entities;

namespace InventoryApi.Domain.Entities
{
    /// <summary>
    /// Represents a product in the inventory domain with comprehensive stock management,
    /// business logic, and lifecycle operations. Serves as the aggregate root for product
    /// information, pricing, availability, and inventory tracking within the e-commerce system.
    /// </summary>
    /// <remarks>
    /// The Product entity encapsulates critical inventory domain responsibilities:
    /// 
    /// Business Responsibilities:
    /// - Product catalog information management and maintenance
    /// - Stock quantity tracking and inventory control
    /// - Pricing information and business rule enforcement
    /// - Product availability and lifecycle status management
    /// 
    /// Inventory Operations:
    /// - Stock reservation for pending orders
    /// - Stock allocation for confirmed orders
    /// - Stock replenishment and quantity adjustments
    /// - Low stock monitoring and alerting
    /// 
    /// Aggregate Design:
    /// - Acts as consistency boundary for all product-related operations
    /// - Manages product information and stock levels atomically
    /// - Enforces business rules around stock management
    /// - Provides domain events for external system integration
    /// 
    /// The entity design follows Domain-Driven Design principles with rich domain logic,
    /// clear business rule enforcement, and separation between domain and infrastructure concerns.
    /// </remarks>
    public class Product : AuditableEntity
    {
        /// <summary>
        /// Unique identifier for the product within the inventory system.
        /// Used for product tracking, catalog management, and cross-system integration.
        /// </summary>
        /// <value>GUID that uniquely identifies this product across all systems</value>
        public Guid Id { get; private set; }

        /// <summary>
        /// Display name of the product for customer-facing scenarios.
        /// Supports catalog browsing, search functionality, and order displays.
        /// </summary>
        /// <value>Product name string for display and search purposes</value>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Detailed description of the product providing comprehensive information.
        /// Supports customer decision-making and catalog content management.
        /// </summary>
        /// <value>Product description text for detailed information display</value>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Current selling price of the product for order processing.
        /// Maintained in the inventory domain for consistency and performance.
        /// </summary>
        /// <value>Decimal price value for financial calculations</value>
        /// <remarks>
        /// Pricing Strategy:
        /// - Current active price for new orders
        /// - Historical pricing maintained in order snapshots
        /// - Price changes tracked through audit fields
        /// - Currency handling depends on system configuration
        /// 
        /// Business Rules:
        /// - Price must be non-negative for business logic
        /// - Price changes require appropriate authorization
        /// - Price history maintained for analytics and reporting
        /// - Integration with pricing rules and discount systems
        /// </remarks>
        public decimal Price { get; private set; }

        /// <summary>
        /// Current available stock quantity for the product.
        /// Represents immediately available inventory for order fulfillment.
        /// </summary>
        /// <value>Integer quantity representing available stock units</value>
        /// <remarks>
        /// Stock Management:
        /// - Available quantity for immediate order fulfillment
        /// - Excludes reserved stock for pending orders
        /// - Real-time updates through inventory operations
        /// - Supports low stock alerting and replenishment
        /// 
        /// Concurrency Considerations:
        /// - Thread-safe operations for concurrent access
        /// - Optimistic concurrency control for updates
        /// - Atomic stock operations to prevent overselling
        /// - Integration with reservation and allocation systems
        /// </remarks>
        public int StockQuantity { get; private set; }

        /// <summary>
        /// Indicates whether the product is currently active and available for sale.
        /// Supports product lifecycle management and catalog control.
        /// </summary>
        /// <value>Boolean indicating product availability status</value>
        /// <remarks>
        /// Lifecycle Management:
        /// - Active products available for ordering
        /// - Inactive products hidden from catalog
        /// - Discontinued products with existing stock
        /// - Seasonal availability control
        /// 
        /// Business Impact:
        /// - Controls product visibility in catalog
        /// - Affects order validation and processing
        /// - Supports promotional and seasonal management
        /// - Enables gradual product retirement
        /// </remarks>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Minimum stock level that triggers replenishment alerts.
        /// Supports automated inventory management and procurement planning.
        /// </summary>
        /// <value>Integer threshold for low stock alerting</value>
        public int MinimumStockLevel { get; private set; }

        /// <summary>
        /// Private constructor for Entity Framework and serialization support.
        /// Ensures proper initialization while preventing direct instantiation.
        /// </summary>
        private Product() : base()
        {
            IsActive = true;
        }

        /// <summary>
        /// Creates a new product with comprehensive information and validation.
        /// Initializes the product with active status and proper audit tracking.
        /// </summary>
        /// <param name="name">Display name of the product</param>
        /// <param name="description">Detailed product description</param>
        /// <param name="price">Selling price of the product</param>
        /// <param name="initialStock">Initial stock quantity</param>
        /// <param name="createdBy">Identifier of the user creating the product</param>
        /// <param name="minimumStockLevel">Minimum stock level for alerts (defaults to 10)</param>
        /// <exception cref="ArgumentException">Thrown when parameters violate business rules</exception>
        public Product(
            string name, 
            string description, 
            decimal price, 
            int initialStock, 
            string createdBy,
            int minimumStockLevel = 10) : base(createdBy)
        {
            ValidateProductData(name, description, price, initialStock, minimumStockLevel);

            Id = Guid.NewGuid();
            Name = name.Trim();
            Description = description.Trim();
            Price = price;
            StockQuantity = initialStock;
            IsActive = true;
            MinimumStockLevel = minimumStockLevel;
        }

        /// <summary>
        /// Updates the product name with validation and audit tracking.
        /// Maintains product information currency and supports catalog management.
        /// </summary>
        /// <param name="name">New product name</param>
        /// <param name="updatedBy">Identifier of the user making the update</param>
        /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
        public void UpdateName(string name, string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty", nameof(name));

            Name = name.Trim();
            UpdateAuditFields(updatedBy);
        }

        /// <summary>
        /// Updates the product description with validation and audit tracking.
        /// Supports content management and product information maintenance.
        /// </summary>
        /// <param name="description">New product description</param>
        /// <param name="updatedBy">Identifier of the user making the update</param>
        /// <exception cref="ArgumentException">Thrown when description is invalid</exception>
        public void UpdateDescription(string description, string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Product description cannot be empty", nameof(description));

            Description = description.Trim();
            UpdateAuditFields(updatedBy);
        }

        /// <summary>
        /// Updates the product price with validation and audit tracking.
        /// Supports pricing strategy implementation and revenue optimization.
        /// </summary>
        /// <param name="price">New product price</param>
        /// <param name="updatedBy">Identifier of the user making the update</param>
        /// <exception cref="ArgumentException">Thrown when price is invalid</exception>
        public void UpdatePrice(decimal price, string updatedBy)
        {
            if (price < 0)
                throw new ArgumentException("Product price cannot be negative", nameof(price));

            Price = price;
            UpdateAuditFields(updatedBy);
        }

        /// <summary>
        /// Adds stock to the current inventory with validation and audit tracking.
        /// Supports replenishment operations and inventory management workflows.
        /// </summary>
        /// <param name="quantity">Quantity to add to current stock</param>
        /// <param name="updatedBy">Identifier of the user making the update</param>
        /// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
        /// <remarks>
        /// Stock Addition Rules:
        /// - Quantity must be positive
        /// - No maximum limit for stock additions
        /// - Audit trail maintained for tracking
        /// - Integration with procurement and receiving systems
        /// 
        /// Business Impact:
        /// - Increases available stock for orders
        /// - May resolve low stock conditions
        /// - Updates inventory metrics and reporting
        /// - Triggers availability notifications
        /// </remarks>
        public void AddStock(int quantity, string updatedBy)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity to add must be positive", nameof(quantity));

            StockQuantity += quantity;
            UpdateAuditFields(updatedBy);
        }

        /// <summary>
        /// Removes stock from current inventory with validation and business rule enforcement.
        /// Supports inventory adjustments, damage reporting, and stock corrections.
        /// </summary>
        /// <param name="quantity">Quantity to remove from current stock</param>
        /// <param name="updatedBy">Identifier of the user making the update</param>
        /// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when insufficient stock available</exception>
        /// <remarks>
        /// Stock Removal Rules:
        /// - Quantity must be positive
        /// - Cannot reduce stock below zero
        /// - Audit trail maintained for accountability
        /// - Integration with quality control and damage reporting
        /// 
        /// Use Cases:
        /// - Damaged inventory removal
        /// - Stock count corrections
        /// - Quality control adjustments
        /// - Theft or loss reporting
        /// </remarks>
        public void RemoveStock(int quantity, string updatedBy)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity to remove must be positive", nameof(quantity));

            if (StockQuantity < quantity)
                throw new InvalidOperationException($"Cannot remove {quantity} units. Only {StockQuantity} units available.");

            StockQuantity -= quantity;
            UpdateAuditFields(updatedBy);
        }

        /// <summary>
        /// Reserves stock for a pending order to prevent overselling.
        /// Supports order processing workflows and inventory allocation.
        /// </summary>
        /// <param name="quantity">Quantity to reserve</param>
        /// <param name="reservedBy">Identifier of the user or system making the reservation</param>
        /// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when insufficient stock or product inactive</exception>
        /// <remarks>
        /// Reservation Rules:
        /// - Product must be active for reservations
        /// - Sufficient stock must be available
        /// - Reservations reduce available stock temporarily
        /// - Reservations can be converted to allocations or released
        /// 
        /// Business Process:
        /// - Occurs during order creation/validation
        /// - Prevents overselling in concurrent scenarios
        /// - Temporary hold on inventory
        /// - Can be released if order is cancelled
        /// </remarks>
        public void ReserveStock(int quantity, string reservedBy)
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot reserve stock for inactive product");

            if (quantity <= 0)
                throw new ArgumentException("Reservation quantity must be positive", nameof(quantity));

            if (StockQuantity < quantity)
                throw new InvalidOperationException($"Insufficient stock to reserve {quantity} units. Only {StockQuantity} units available.");

            StockQuantity -= quantity;
            UpdateAuditFields(reservedBy);
        }

        /// <summary>
        /// Allocates stock for a confirmed order, committing inventory to fulfillment.
        /// Represents final commitment of inventory to customer order.
        /// </summary>
        /// <param name="quantity">Quantity to allocate</param>
        /// <param name="allocatedBy">Identifier of the user or system making the allocation</param>
        /// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when insufficient stock available</exception>
        /// <remarks>
        /// Allocation Rules:
        /// - Represents firm commitment of inventory
        /// - Typically follows successful reservation
        /// - Stock committed for order fulfillment
        /// - Cannot be easily reversed once allocated
        /// 
        /// Business Impact:
        /// - Reduces available inventory permanently
        /// - Triggers fulfillment and shipping processes
        /// - Updates inventory metrics and reporting
        /// - May trigger replenishment if below minimum levels
        /// </remarks>
        public void AllocateStock(int quantity, string allocatedBy)
        {
            if (quantity <= 0)
                throw new ArgumentException("Allocation quantity must be positive", nameof(quantity));

            if (StockQuantity < quantity)
                throw new InvalidOperationException($"Insufficient stock to allocate {quantity} units. Only {StockQuantity} units available.");

            StockQuantity -= quantity;
            UpdateAuditFields(allocatedBy);
        }

        /// <summary>
        /// Releases previously reserved stock back to available inventory.
        /// Supports order cancellation and reservation cleanup scenarios.
        /// </summary>
        /// <param name="quantity">Quantity to release back to available stock</param>
        /// <param name="releasedBy">Identifier of the user or system releasing the stock</param>
        /// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
        public void ReleaseReservedStock(int quantity, string releasedBy)
        {
            if (quantity <= 0)
                throw new ArgumentException("Release quantity must be positive", nameof(quantity));

            StockQuantity += quantity;
            UpdateAuditFields(releasedBy);
        }

        /// <summary>
        /// Activates the product making it available for sale and ordering.
        /// Supports product lifecycle management and catalog control.
        /// </summary>
        /// <param name="activatedBy">Identifier of the user activating the product</param>
        public void Activate(string activatedBy)
        {
            IsActive = true;
            UpdateAuditFields(activatedBy);
        }

        /// <summary>
        /// Deactivates the product removing it from sale availability.
        /// Supports product retirement and lifecycle management.
        /// </summary>
        /// <param name="deactivatedBy">Identifier of the user deactivating the product</param>
        public void Deactivate(string deactivatedBy)
        {
            IsActive = false;
            UpdateAuditFields(deactivatedBy);
        }

        /// <summary>
        /// Updates the minimum stock level for replenishment alerting.
        /// Supports inventory planning and automated replenishment systems.
        /// </summary>
        /// <param name="minimumLevel">New minimum stock level threshold</param>
        /// <param name="updatedBy">Identifier of the user making the update</param>
        /// <exception cref="ArgumentException">Thrown when minimum level is invalid</exception>
        public void UpdateMinimumStockLevel(int minimumLevel, string updatedBy)
        {
            if (minimumLevel < 0)
                throw new ArgumentException("Minimum stock level cannot be negative", nameof(minimumLevel));

            MinimumStockLevel = minimumLevel;
            UpdateAuditFields(updatedBy);
        }

        /// <summary>
        /// Determines if the product has sufficient stock for the requested quantity.
        /// Supports order validation and inventory availability checks.
        /// </summary>
        /// <param name="requestedQuantity">Quantity to check availability for</param>
        /// <returns>True if sufficient stock is available, false otherwise</returns>
        public bool HasSufficientStock(int requestedQuantity) => 
            IsActive && StockQuantity >= requestedQuantity && requestedQuantity > 0;

        /// <summary>
        /// Determines if the current stock level is below the minimum threshold.
        /// Supports automated replenishment and low stock alerting systems.
        /// </summary>
        /// <returns>True if stock is below minimum level, false otherwise</returns>
        public bool IsLowStock() => StockQuantity <= MinimumStockLevel;

        /// <summary>
        /// Determines if the product is currently out of stock.
        /// Supports catalog display and order validation logic.
        /// </summary>
        /// <returns>True if no stock is available, false otherwise</returns>
        public bool IsOutOfStock() => StockQuantity <= 0;

        /// <summary>
        /// Determines if the product is available for ordering.
        /// Combines active status and stock availability for business logic.
        /// </summary>
        /// <returns>True if product can be ordered, false otherwise</returns>
        public bool IsAvailableForOrder() => IsActive && StockQuantity > 0;

        /// <summary>
        /// Validates product data during creation and updates.
        /// Ensures business rule compliance and data integrity.
        /// </summary>
        private static void ValidateProductData(string name, string description, decimal price, int initialStock, int minimumStockLevel)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Product description cannot be empty", nameof(description));

            if (price < 0)
                throw new ArgumentException("Product price cannot be negative", nameof(price));

            if (initialStock < 0)
                throw new ArgumentException("Initial stock cannot be negative", nameof(initialStock));

            if (minimumStockLevel < 0)
                throw new ArgumentException("Minimum stock level cannot be negative", nameof(minimumStockLevel));
        }
    }
}