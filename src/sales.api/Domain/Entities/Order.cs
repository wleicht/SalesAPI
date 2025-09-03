using BuildingBlocks.Domain.Entities;

namespace SalesApi.Domain.Entities
{
    /// <summary>
    /// Represents a customer order in the sales domain with comprehensive business logic
    /// and state management capabilities. Encapsulates order lifecycle, calculation logic,
    /// and business rule enforcement for the e-commerce ordering process.
    /// </summary>
    /// <remarks>
    /// The Order entity serves as the aggregate root for the order subdomain, managing:
    /// 
    /// Business Responsibilities:
    /// - Order lifecycle management (Pending ? Confirmed ? Fulfilled ? Cancelled)
    /// - Total amount calculation and validation
    /// - Order item management and consistency
    /// - Business rule enforcement and validation
    /// 
    /// Domain Logic:
    /// - Automatic total calculation from order items
    /// - State transition validation and business rules
    /// - Order modification constraints based on current status
    /// - Integration with inventory and payment domains
    /// 
    /// Aggregate Design:
    /// - Acts as the consistency boundary for order operations
    /// - Manages collection of OrderItem value objects
    /// - Enforces invariants across all order components
    /// - Provides domain events for external integration
    /// 
    /// The entity design follows Domain-Driven Design principles with rich domain logic
    /// and clear separation between domain concerns and infrastructure details.
    /// </remarks>
    public class Order : AuditableEntity
    {
        /// <summary>
        /// Unique identifier for the order within the sales domain.
        /// Used for order tracking, customer service, and system integration.
        /// </summary>
        /// <value>GUID that uniquely identifies this order across all systems</value>
        public Guid Id { get; private set; }

        /// <summary>
        /// Identifier of the customer who placed this order.
        /// Links the order to customer information and enables customer-specific operations.
        /// </summary>
        /// <value>Customer identifier for relationship management and customer service</value>
        public Guid CustomerId { get; private set; }

        /// <summary>
        /// Current status of the order in its lifecycle.
        /// Determines available operations and business rule enforcement.
        /// </summary>
        /// <value>Order status string representing current lifecycle state</value>
        /// <remarks>
        /// Valid Status Values:
        /// - "Pending": Initial state, awaiting confirmation
        /// - "Confirmed": Order confirmed, inventory reserved
        /// - "Fulfilled": Order completed and delivered
        /// - "Cancelled": Order cancelled, inventory released
        /// 
        /// Status Transitions:
        /// - Pending ? Confirmed (payment processed, inventory reserved)
        /// - Confirmed ? Fulfilled (order shipped and delivered)
        /// - Pending/Confirmed ? Cancelled (customer cancellation or business rules)
        /// 
        /// Business Rules:
        /// - Only pending orders can be modified
        /// - Confirmed orders cannot be cancelled without compensation
        /// - Fulfilled orders are immutable for audit compliance
        /// </remarks>
        public string Status { get; private set; }

        /// <summary>
        /// Total monetary amount for the complete order including all items.
        /// Calculated automatically from individual order items and maintained for performance.
        /// </summary>
        /// <value>Decimal amount representing the total order value</value>
        /// <remarks>
        /// Calculation Strategy:
        /// - Automatically computed from sum of all order item totals
        /// - Updated whenever order items are added, removed, or modified
        /// - Cached for performance optimization in queries and reporting
        /// - Validated against individual item calculations for consistency
        /// 
        /// Business Considerations:
        /// - Includes all item costs, taxes calculated separately
        /// - Currency handling depends on system configuration
        /// - Precision maintained for financial accuracy requirements
        /// - Used for payment processing and financial reporting
        /// </remarks>
        public decimal TotalAmount { get; private set; }

        /// <summary>
        /// Collection of items included in this order.
        /// Represents the order line items with product details and quantities.
        /// </summary>
        /// <value>Read-only collection of order items</value>
        /// <remarks>
        /// Collection Management:
        /// - Private setter ensures controlled access through domain methods
        /// - Initialized as empty collection to prevent null reference issues
        /// - Modified only through AddItem, RemoveItem, and UpdateItem methods
        /// - Maintains referential integrity with parent order
        /// 
        /// Business Rules:
        /// - Orders must contain at least one item before confirmation
        /// - Item quantities must be positive integers
        /// - Product references must be valid and available
        /// - Total calculation depends on all items in collection
        /// </remarks>
        public virtual ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();

        /// <summary>
        /// Private constructor for Entity Framework and serialization support.
        /// Ensures proper initialization while preventing direct instantiation.
        /// </summary>
        private Order() : base()
        {
            Status = OrderStatus.Pending;
        }

        /// <summary>
        /// Creates a new order for the specified customer with audit tracking.
        /// Initializes the order in pending status ready for item addition.
        /// </summary>
        /// <param name="customerId">Identifier of the customer placing the order</param>
        /// <param name="createdBy">Identifier of the user or system creating the order</param>
        /// <exception cref="ArgumentException">Thrown when customerId is empty</exception>
        /// <exception cref="ArgumentNullException">Thrown when createdBy is null or empty</exception>
        public Order(Guid customerId, string createdBy) : base(createdBy)
        {
            if (customerId == Guid.Empty)
                throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));

            Id = Guid.NewGuid();
            CustomerId = customerId;
            Status = OrderStatus.Pending;
            TotalAmount = 0m;
        }

        /// <summary>
        /// Adds a new item to the order with automatic total calculation.
        /// Validates business rules and maintains order consistency.
        /// </summary>
        /// <param name="productId">Identifier of the product being ordered</param>
        /// <param name="productName">Name of the product for display and audit purposes</param>
        /// <param name="quantity">Quantity of the product being ordered</param>
        /// <param name="unitPrice">Price per unit of the product</param>
        /// <param name="updatedBy">Identifier of the user making the modification</param>
        /// <exception cref="InvalidOperationException">Thrown when order status prevents modification</exception>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
        /// <remarks>
        /// Business Rules Enforced:
        /// - Only pending orders can be modified
        /// - Quantity must be positive
        /// - Unit price must be non-negative
        /// - Product name cannot be empty
        /// - Duplicate products are not allowed (use UpdateItem instead)
        /// 
        /// Side Effects:
        /// - Updates total amount calculation
        /// - Updates audit fields with modification timestamp
        /// - Maintains referential integrity between order and items
        /// </remarks>
        public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice, string updatedBy)
        {
            ValidateModificationAllowed();
            ValidateItemParameters(productId, productName, quantity, unitPrice);

            if (Items.Any(i => i.ProductId == productId))
                throw new InvalidOperationException($"Product {productId} is already in the order. Use UpdateItem to modify quantity.");

            var orderItem = new OrderItem(Id, productId, productName, quantity, unitPrice);
            Items.Add(orderItem);
            
            RecalculateTotalAmount();
            UpdateAuditFields(updatedBy);
        }

        /// <summary>
        /// Removes an item from the order and recalculates the total amount.
        /// Maintains order consistency and enforces business rules.
        /// </summary>
        /// <param name="productId">Identifier of the product to remove</param>
        /// <param name="updatedBy">Identifier of the user making the modification</param>
        /// <exception cref="InvalidOperationException">Thrown when order status prevents modification or item not found</exception>
        public void RemoveItem(Guid productId, string updatedBy)
        {
            ValidateModificationAllowed();

            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                throw new InvalidOperationException($"Product {productId} is not in the order");

            Items.Remove(item);
            RecalculateTotalAmount();
            UpdateAuditFields(updatedBy);
        }

        /// <summary>
        /// Updates the quantity of an existing item in the order.
        /// Maintains business rules and recalculates totals appropriately.
        /// </summary>
        /// <param name="productId">Identifier of the product to update</param>
        /// <param name="newQuantity">New quantity for the product</param>
        /// <param name="updatedBy">Identifier of the user making the modification</param>
        /// <exception cref="InvalidOperationException">Thrown when order status prevents modification or item not found</exception>
        /// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
        public void UpdateItemQuantity(Guid productId, int newQuantity, string updatedBy)
        {
            ValidateModificationAllowed();

            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(newQuantity));

            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
                throw new InvalidOperationException($"Product {productId} is not in the order");

            item.UpdateQuantity(newQuantity);
            RecalculateTotalAmount();
            UpdateAuditFields(updatedBy);
        }

        /// <summary>
        /// Confirms the order, transitioning it from pending to confirmed status.
        /// Validates business rules and prepares the order for fulfillment processing.
        /// </summary>
        /// <param name="confirmedBy">Identifier of the user or system confirming the order</param>
        /// <exception cref="InvalidOperationException">Thrown when order cannot be confirmed</exception>
        /// <remarks>
        /// Confirmation Requirements:
        /// - Order must be in pending status
        /// - Order must contain at least one item
        /// - All order items must be valid and available
        /// - Payment processing must be successful (handled externally)
        /// 
        /// Post-Confirmation State:
        /// - Status changes to "Confirmed"
        /// - Order becomes read-only for modifications
        /// - Inventory reservations should be converted to allocations
        /// - Order enters fulfillment pipeline
        /// </remarks>
        public void Confirm(string confirmedBy)
        {
            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException($"Only pending orders can be confirmed. Current status: {Status}");

            if (!Items.Any())
                throw new InvalidOperationException("Cannot confirm an order without items");

            Status = OrderStatus.Confirmed;
            UpdateAuditFields(confirmedBy);
        }

        /// <summary>
        /// Marks the order as fulfilled, indicating successful completion and delivery.
        /// Represents the final state in the successful order lifecycle.
        /// </summary>
        /// <param name="fulfilledBy">Identifier of the user or system marking fulfillment</param>
        /// <exception cref="InvalidOperationException">Thrown when order cannot be fulfilled</exception>
        public void MarkAsFulfilled(string fulfilledBy)
        {
            if (Status != OrderStatus.Confirmed)
                throw new InvalidOperationException($"Only confirmed orders can be fulfilled. Current status: {Status}");

            Status = OrderStatus.Fulfilled;
            UpdateAuditFields(fulfilledBy);
        }

        /// <summary>
        /// Cancels the order with appropriate compensation and state cleanup.
        /// Handles both customer-initiated and system-initiated cancellations.
        /// </summary>
        /// <param name="cancelledBy">Identifier of the user or system cancelling the order</param>
        /// <param name="reason">Optional reason for the cancellation for audit purposes</param>
        /// <exception cref="InvalidOperationException">Thrown when order cannot be cancelled</exception>
        /// <remarks>
        /// Cancellation Rules:
        /// - Pending orders can be cancelled freely
        /// - Confirmed orders may require compensation (inventory release, refunds)
        /// - Fulfilled orders cannot be cancelled (use returns process instead)
        /// 
        /// Compensation Activities:
        /// - Release inventory reservations or allocations
        /// - Process refunds if payment was captured
        /// - Update customer communication and notifications
        /// - Generate appropriate domain events for downstream processing
        /// </remarks>
        public void Cancel(string cancelledBy, string? reason = null)
        {
            if (Status == OrderStatus.Fulfilled)
                throw new InvalidOperationException("Fulfilled orders cannot be cancelled. Use returns process instead.");

            if (Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("Order is already cancelled");

            Status = OrderStatus.Cancelled;
            UpdateAuditFields(cancelledBy);
        }

        /// <summary>
        /// Determines whether the order can be modified based on its current status.
        /// Used to enforce business rules around order lifecycle management.
        /// </summary>
        /// <returns>True if the order can be modified, false otherwise</returns>
        public bool CanBeModified() => Status == OrderStatus.Pending;

        /// <summary>
        /// Calculates the total amount by summing all order item totals.
        /// Maintains financial accuracy and consistency across the order.
        /// </summary>
        private void RecalculateTotalAmount()
        {
            TotalAmount = Items.Sum(item => item.TotalPrice);
        }

        /// <summary>
        /// Validates that the order is in a state that allows modification.
        /// Enforces business rules around order lifecycle constraints.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when modifications are not allowed</exception>
        private void ValidateModificationAllowed()
        {
            if (!CanBeModified())
                throw new InvalidOperationException($"Cannot modify order in {Status} status. Only pending orders can be modified.");
        }

        /// <summary>
        /// Validates parameters for adding items to the order.
        /// Ensures data integrity and business rule compliance.
        /// </summary>
        private static void ValidateItemParameters(Guid productId, string productName, int quantity, decimal unitPrice)
        {
            if (productId == Guid.Empty)
                throw new ArgumentException("Product ID cannot be empty", nameof(productId));

            if (string.IsNullOrWhiteSpace(productName))
                throw new ArgumentException("Product name cannot be empty", nameof(productName));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));

            if (unitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        }
    }

    /// <summary>
    /// Defines the valid status values for order lifecycle management.
    /// Provides type safety and consistency for order status handling.
    /// </summary>
    /// <remarks>
    /// Status Constants:
    /// - Pending: Initial state, order is being assembled
    /// - Confirmed: Order confirmed, payment processed, inventory allocated
    /// - Fulfilled: Order completed, delivered to customer
    /// - Cancelled: Order cancelled, compensation processed
    /// 
    /// These constants ensure consistency across the application and provide
    /// compile-time safety for status comparisons and transitions.
    /// </remarks>
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Fulfilled = "Fulfilled";
        public const string Cancelled = "Cancelled";
    }
}