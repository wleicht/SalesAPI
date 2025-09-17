namespace SalesApi.Domain.Entities
{
    /// <summary>
    /// Represents an individual line item within a customer order, containing product details,
    /// quantity, pricing information, and calculated totals. Serves as a value object within
    /// the Order aggregate, maintaining referential integrity and business rule enforcement.
    /// </summary>
    /// <remarks>
    /// The OrderItem entity encapsulates line-item specific logic including:
    /// 
    /// Business Responsibilities:
    /// - Product reference and snapshot data preservation
    /// - Quantity management and validation
    /// - Price calculation and total computation
    /// - Order relationship maintenance and integrity
    /// 
    /// Value Object Characteristics:
    /// - Immutable after creation (except quantity updates)
    /// - No independent lifecycle outside of parent Order
    /// - Equality based on product identity within order context
    /// - Snapshot of product information at time of order creation
    /// 
    /// Data Integrity:
    /// - Maintains product information as of order creation time
    /// - Preserves pricing information for audit and consistency
    /// - Calculates line totals automatically for accuracy
    /// - Validates business rules for quantity and pricing
    /// 
    /// The design ensures that orders maintain accurate historical information
    /// even if product details change after order creation, supporting audit
    /// requirements and pricing consistency for completed transactions.
    /// </remarks>
    public class OrderItem
    {
        /// <summary>
        /// Identifier of the parent order that contains this item.
        /// Establishes the relationship and ensures referential integrity.
        /// </summary>
        /// <value>Order identifier for relationship management</value>
        public Guid OrderId { get; private set; }

        /// <summary>
        /// Identifier of the product being ordered.
        /// References the product catalog while maintaining independence from product changes.
        /// </summary>
        /// <value>Product identifier for catalog reference and inventory management</value>
        public Guid ProductId { get; private set; }

        /// <summary>
        /// Name of the product at the time the order was created.
        /// Preserved as a snapshot to maintain order accuracy regardless of catalog changes.
        /// </summary>
        /// <value>Product name string for display and audit purposes</value>
        /// <remarks>
        /// Snapshot Strategy:
        /// - Captures product name at order creation time
        /// - Remains unchanged even if product name is updated in catalog
        /// - Provides consistent order history and reporting
        /// - Supports audit requirements and customer service
        /// 
        /// Business Benefits:
        /// - Accurate order confirmation and invoicing
        /// - Consistent customer communication
        /// - Historical accuracy for financial reporting
        /// - Protection against catalog data inconsistencies
        /// </remarks>
        public string ProductName { get; private set; } = string.Empty;

        /// <summary>
        /// Quantity of the product being ordered.
        /// Supports business logic for inventory allocation and total calculation.
        /// </summary>
        /// <value>Positive integer representing the number of units ordered</value>
        /// <remarks>
        /// Quantity Management:
        /// - Must be positive integer value
        /// - Can be updated through controlled domain methods
        /// - Used for inventory reservation and allocation
        /// - Drives total price calculation for the line item
        /// 
        /// Business Rules:
        /// - Minimum quantity is 1 (no zero or negative quantities)
        /// - Maximum quantity may be subject to business constraints
        /// - Changes require order to be in modifiable state
        /// - Updates trigger recalculation of order totals
        /// </remarks>
        public int Quantity { get; private set; }

        /// <summary>
        /// Price per unit of the product at the time the order was created.
        /// Preserved as a snapshot to ensure pricing consistency and audit compliance.
        /// </summary>
        /// <value>Decimal unit price for financial calculations</value>
        /// <remarks>
        /// Price Snapshot Strategy:
        /// - Captures current product price at order creation
        /// - Remains fixed regardless of subsequent price changes
        /// - Ensures order total consistency and customer trust
        /// - Supports audit requirements and financial reporting
        /// 
        /// Pricing Considerations:
        /// - May include discounts or promotional pricing
        /// - Excludes taxes (calculated separately)
        /// - Currency handling depends on system configuration
        /// - Precision maintained for financial accuracy
        /// </remarks>
        public decimal UnitPrice { get; private set; }

        /// <summary>
        /// Calculated total price for this line item (Quantity × UnitPrice).
        /// Supports both computed column in database and in-memory calculation.
        /// </summary>
        /// <value>Decimal total price for this order line item</value>
        /// <remarks>
        /// Implementation Strategy:
        /// - Has private setter to support Entity Framework computed columns
        /// - Public getter returns computed value if database value is not set
        /// - Database computed column takes precedence when available
        /// - Fallback to calculation ensures compatibility in all scenarios
        /// </remarks>
        private decimal _totalPrice;
        public decimal TotalPrice 
        { 
            get => _totalPrice != 0 ? _totalPrice : Quantity * UnitPrice;
            private set => _totalPrice = value;
        }

        /// <summary>
        /// Navigation property back to the parent order.
        /// Supports Entity Framework relationships and object navigation.
        /// </summary>
        /// <value>Order entity that contains this item</value>
        public virtual Order Order { get; private set; } = null!;

        /// <summary>
        /// Private constructor for Entity Framework and serialization support.
        /// Ensures proper initialization while preventing direct instantiation.
        /// </summary>
        private OrderItem() { }

        /// <summary>
        /// Creates a new order item with specified product details and pricing.
        /// Validates business rules and initializes all required properties.
        /// </summary>
        /// <param name="orderId">Identifier of the parent order</param>
        /// <param name="productId">Identifier of the product being ordered</param>
        /// <param name="productName">Name of the product for display purposes</param>
        /// <param name="quantity">Quantity of the product being ordered</param>
        /// <param name="unitPrice">Price per unit of the product</param>
        /// <exception cref="ArgumentException">Thrown when parameters violate business rules</exception>
        public OrderItem(Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
        {
            if (orderId == Guid.Empty)
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));

            if (productId == Guid.Empty)
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));

            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Product name cannot be null or empty", nameof(productName));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

            OrderId = orderId;
            ProductId = productId;
            ProductName = productName.Trim();
            Quantity = quantity;
            UnitPrice = unitPrice;
            // _totalPrice will be set by database computed column or calculated via getter
        }

        /// <summary>
        /// Updates the quantity of this order item while maintaining business rule compliance.
        /// Supports order modification scenarios within the order lifecycle constraints.
        /// </summary>
        /// <param name="newQuantity">New quantity value for this order item</param>
        /// <exception cref="ArgumentException">Thrown when new quantity violates business rules</exception>
        public void UpdateQuantity(int newQuantity)
        {
            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(newQuantity));

            Quantity = newQuantity;
            // Reset _totalPrice to force recalculation
            _totalPrice = 0;
        }

        /// <summary>
        /// Determines equality based on product identity within the same order.
        /// Supports business logic for duplicate product detection and validation.
        /// </summary>
        /// <param name="other">Other OrderItem to compare with</param>
        /// <returns>True if both items represent the same product in the same order</returns>
        public bool Equals(OrderItem? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return OrderId == other.OrderId && ProductId == other.ProductId;
        }

        /// <summary>
        /// Provides object equality comparison with type checking.
        /// Supports general object comparison scenarios and collection operations.
        /// </summary>
        /// <param name="obj">Object to compare with</param>
        /// <returns>True if objects are equal based on business identity</returns>
        public override bool Equals(object? obj) => Equals(obj as OrderItem);

        /// <summary>
        /// Generates hash code based on business identity for collection operations.
        /// Ensures consistent behavior in hash-based collections and operations.
        /// </summary>
        /// <returns>Hash code based on OrderId and ProductId</returns>
        public override int GetHashCode() => HashCode.Combine(OrderId, ProductId);

        /// <summary>
        /// Provides string representation for debugging and logging purposes.
        /// Includes key information for troubleshooting and operational visibility.
        /// </summary>
        /// <returns>Formatted string with order item details</returns>
        public override string ToString()
        {
            return $"OrderItem: {ProductName} (ID: {ProductId}) - Qty: {Quantity} @ {UnitPrice:C} = {TotalPrice:C}";
        }
    }
}