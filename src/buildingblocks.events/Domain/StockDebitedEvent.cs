namespace BuildingBlocks.Events.Domain
{
    /// <summary>
    /// Represents a single stock deduction operation performed during order fulfillment.
    /// Provides comprehensive audit trail information for inventory movements and financial reconciliation.
    /// </summary>
    /// <remarks>
    /// This class captures the complete before-and-after state of a stock deduction operation,
    /// enabling full audit trails and supporting business intelligence requirements. Each
    /// StockDeduction instance represents an atomic operation that either succeeded completely
    /// or failed without affecting inventory levels.
    /// 
    /// Audit Trail Purpose:
    /// - Financial reconciliation and accounting
    /// - Inventory discrepancy investigation
    /// - Business intelligence and analytics
    /// - Regulatory compliance reporting
    /// - Customer service order tracking
    /// </remarks>
    public class StockDeduction
    {
        /// <summary>
        /// Unique identifier of the product whose stock was debited.
        /// References the Product entity in the Inventory database for cross-referencing operations.
        /// </summary>
        /// <value>A GUID that uniquely identifies the product across the system</value>
        /// <remarks>
        /// This identifier enables precise tracking of stock movements at the product level
        /// and supports detailed inventory reporting and analysis. It also facilitates
        /// reconciliation with external systems and audit processes.
        /// </remarks>
        public required Guid ProductId { get; set; }

        /// <summary>
        /// Human-readable name of the product at the time of stock deduction.
        /// Provides contextual information for audit trails and customer service operations.
        /// </summary>
        /// <value>The product name as it existed when the deduction was performed</value>
        /// <remarks>
        /// Capturing the product name at the time of deduction ensures audit trail integrity
        /// even if product names are subsequently changed. This snapshot approach provides
        /// historical accuracy for compliance and reporting purposes.
        /// </remarks>
        public required string ProductName { get; set; }

        /// <summary>
        /// Exact quantity that was successfully debited from the product's stock level.
        /// Represents the number of units removed from available inventory.
        /// </summary>
        /// <value>A positive integer representing the units debited from stock</value>
        /// <remarks>
        /// This value should always match the requested quantity from the corresponding
        /// OrderItemEvent unless the deduction failed due to insufficient stock. In case
        /// of partial availability, this field would reflect the actual amount debited.
        /// </remarks>
        public required int QuantityDebited { get; set; }

        /// <summary>
        /// Stock quantity level immediately before the deduction operation was performed.
        /// Provides the baseline for calculating the impact of the stock movement.
        /// </summary>
        /// <value>A non-negative integer representing the stock level before deduction</value>
        /// <remarks>
        /// This baseline value is critical for:
        /// - Audit trail verification (PreviousStock - QuantityDebited = NewStock)
        /// - Inventory movement analysis and trending
        /// - Detecting and investigating stock discrepancies
        /// - Financial reconciliation and cost accounting
        /// </remarks>
        public required int PreviousStock { get; set; }

        /// <summary>
        /// Stock quantity level after the deduction operation was completed.
        /// Represents the updated inventory level following the stock movement.
        /// </summary>
        /// <value>A non-negative integer representing the stock level after deduction</value>
        /// <remarks>
        /// Mathematical Relationship: NewStock = PreviousStock - QuantityDebited
        /// 
        /// This field enables:
        /// - Immediate verification of calculation accuracy
        /// - Real-time inventory level reporting
        /// - Stock replenishment trigger point analysis
        /// - Business rule validation (e.g., minimum stock levels)
        /// </remarks>
        public required int NewStock { get; set; }
    }

    /// <summary>
    /// Domain event published when stock deduction operations have been completed for an order.
    /// Provides comprehensive confirmation, audit trail, and status information for inventory movements
    /// triggered by order confirmation events.
    /// </summary>
    /// <remarks>
    /// The StockDebitedEvent serves as the definitive record of inventory processing results
    /// following an OrderConfirmedEvent. This event enables:
    /// 
    /// Business Capabilities:
    /// - Order status updates and customer notifications
    /// - Financial reconciliation and cost accounting
    /// - Inventory reporting and business intelligence
    /// - Audit trail maintenance and compliance reporting
    /// - Exception handling and error resolution workflows
    /// 
    /// Integration Patterns:
    /// This event completes the request-response pattern initiated by OrderConfirmedEvent:
    /// 1. Sales service publishes OrderConfirmedEvent
    /// 2. Inventory service processes stock deductions
    /// 3. Inventory service publishes StockDebitedEvent (this event)
    /// 4. Sales service receives confirmation and updates order status
    /// 
    /// Error Handling:
    /// The event supports both successful and failed scenarios, enabling proper
    /// error recovery and compensation workflows when stock deductions cannot
    /// be completed as requested.
    /// 
    /// Idempotency:
    /// This event, like all domain events, includes unique identification and
    /// correlation tracking to ensure exactly-once processing semantics.
    /// </remarks>
    public class StockDebitedEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the order that triggered the stock deduction operations.
        /// Links this event back to the original OrderConfirmedEvent for complete audit trail correlation.
        /// </summary>
        /// <value>A GUID that uniquely identifies the order across all services</value>
        /// <remarks>
        /// This correlation enables:
        /// - End-to-end order processing tracking
        /// - Customer service order status inquiries  
        /// - Financial reconciliation between sales and inventory
        /// - Business intelligence and order fulfillment analytics
        /// - Error investigation and root cause analysis
        /// </remarks>
        public required Guid OrderId { get; set; }

        /// <summary>
        /// Comprehensive collection of all stock deduction operations performed for this order.
        /// Each element represents the complete before-and-after state of a single product's stock adjustment.
        /// </summary>
        /// <value>A collection of StockDeduction objects with complete audit information</value>
        /// <remarks>
        /// Processing Characteristics:
        /// - One StockDeduction per unique product in the original order
        /// - Maintains chronological order of processing when possible
        /// - Includes both successful and failed deduction attempts
        /// - Empty collection indicates order had no valid items for processing
        /// 
        /// Usage Scenarios:
        /// - Detailed audit trail generation for compliance
        /// - Financial cost calculation and accounting entries
        /// - Inventory movement reporting and analytics
        /// - Customer service order detail inquiries
        /// - Exception investigation and error resolution
        /// </remarks>
        public required ICollection<StockDeduction> StockDeductions { get; set; }

        /// <summary>
        /// Calculated count of individual stock deduction operations included in this event.
        /// Provides immediate access to the scope of inventory processing without collection enumeration.
        /// </summary>
        /// <value>A non-negative integer representing the number of products processed</value>
        /// <remarks>
        /// This computed property enables:
        /// - Quick assessment of order complexity and processing scope
        /// - Performance monitoring and capacity planning metrics
        /// - Business intelligence reporting without detailed enumeration
        /// - Validation that all expected items were processed
        /// 
        /// Note: This count includes both successful and failed deduction attempts,
        /// providing the total scope of processing rather than just successful operations.
        /// </remarks>
        public int TotalItemsProcessed => StockDeductions?.Count ?? 0;

        /// <summary>
        /// Indicates whether all requested stock deductions were completed successfully.
        /// Provides immediate status assessment for order fulfillment and exception handling workflows.
        /// </summary>
        /// <value>True if all deductions succeeded; false if any deduction failed</value>
        /// <remarks>
        /// Success Criteria:
        /// - All requested quantities were available and debited
        /// - No database errors occurred during processing
        /// - All business validation rules were satisfied
        /// 
        /// Failure Scenarios:
        /// - Insufficient stock for one or more products
        /// - Product not found in inventory database
        /// - Database transaction failures or timeouts
        /// - Business rule violations (e.g., restricted products)
        /// 
        /// When false, consumers should examine ErrorMessage and individual
        /// StockDeduction records to determine appropriate compensation actions.
        /// </remarks>
        public required bool AllDeductionsSuccessful { get; set; }

        /// <summary>
        /// Human-readable error message describing any failures that occurred during stock deduction processing.
        /// Provides actionable information for error resolution and customer communication.
        /// </summary>
        /// <value>Descriptive error message or null/empty if all operations succeeded</value>
        /// <remarks>
        /// Error Message Content:
        /// - Specific product names and availability issues
        /// - System error descriptions for technical failures
        /// - Business rule violation explanations
        /// - Guidance for resolution when possible
        /// 
        /// Usage Guidelines:
        /// - Customer service representatives for order inquiries
        /// - Automated notification systems for customer updates
        /// - Operations teams for inventory management
        /// - Development teams for system troubleshooting
        /// 
        /// The message is designed to be informative while remaining
        /// appropriate for both technical and business stakeholders.
        /// </remarks>
        public string? ErrorMessage { get; set; }
    }
}