using System;

namespace InventoryApi.Domain.ValueObjects
{
    /// <summary>
    /// Represents a stock quantity as an immutable value object with business rule enforcement.
    /// Provides type-safe stock operations, validation, and business logic encapsulation
    /// for inventory management scenarios in the e-commerce domain.
    /// </summary>
    /// <remarks>
    /// The StockQuantity value object addresses inventory management concerns by:
    /// 
    /// Type Safety:
    /// - Prevents accidental mixing of different quantity types
    /// - Compile-time safety for inventory operations
    /// - Clear distinction between stock quantities and other numeric values
    /// - Explicit validation for business rule compliance
    /// 
    /// Business Rules:
    /// - Enforces non-negative quantities for inventory contexts
    /// - Provides safe arithmetic operations with overflow protection
    /// - Validates quantity limits for business requirements
    /// - Maintains consistency across inventory operations
    /// 
    /// Immutability:
    /// - Value objects cannot be modified after creation
    /// - All operations return new instances
    /// - Thread-safe by design for concurrent access
    /// - Prevents accidental state mutation in business logic
    /// 
    /// The design follows Domain-Driven Design principles for value objects
    /// and provides robust foundation for inventory operations while preventing
    /// common stock management errors and business rule violations.
    /// </remarks>
    public readonly struct StockQuantity : IEquatable<StockQuantity>, IComparable<StockQuantity>
    {
        /// <summary>
        /// The actual quantity value as an integer.
        /// Represents discrete units of inventory stock.
        /// </summary>
        /// <value>Integer quantity value with business rule validation</value>
        public int Value { get; }

        /// <summary>
        /// Indicates whether this stock quantity represents zero stock.
        /// Useful for out-of-stock conditions and business logic.
        /// </summary>
        public bool IsZero => Value == 0;

        /// <summary>
        /// Indicates whether this stock quantity represents available stock.
        /// Supports inventory availability checks and business decisions.
        /// </summary>
        public bool IsAvailable => Value > 0;

        /// <summary>
        /// Creates a new StockQuantity instance with the specified value.
        /// Validates business rules and ensures data integrity.
        /// </summary>
        /// <param name="value">Stock quantity value (must be non-negative)</param>
        /// <exception cref="ArgumentException">Thrown when value is negative</exception>
        /// <remarks>
        /// Validation Rules:
        /// - Value must be non-negative (business rule for inventory domain)
        /// - No upper limit enforced at value object level
        /// - Business logic should handle reasonable upper limits
        /// - Zero is valid for out-of-stock scenarios
        /// 
        /// Business Rationale:
        /// - Negative stock prevented to avoid inventory calculation errors
        /// - Zero stock allowed for valid out-of-stock conditions
        /// - Positive values represent available inventory
        /// - Simple integer arithmetic for performance and clarity
        /// </remarks>
        public StockQuantity(int value)
        {
            if (value < 0)
                throw new ArgumentException("Stock quantity cannot be negative", nameof(value));

            Value = value;
        }

        /// <summary>
        /// Checks if there is sufficient stock for the requested quantity.
        /// Supports inventory allocation and reservation business logic.
        /// </summary>
        /// <param name="requestedQuantity">Quantity needed for operation</param>
        /// <returns>True if sufficient stock is available, false otherwise</returns>
        /// <remarks>
        /// Sufficiency Logic:
        /// - Current stock must be greater than or equal to requested quantity
        /// - Requested quantity must be positive for valid check
        /// - Zero requested quantity is considered sufficient
        /// - Handles edge cases for business rule compliance
        /// 
        /// Use Cases:
        /// - Order validation before stock reservation
        /// - Inventory allocation feasibility checks
        /// - Stock availability verification
        /// - Business rule validation in domain services
        /// </remarks>
        public bool IsSufficient(int requestedQuantity) => 
            requestedQuantity >= 0 && Value >= requestedQuantity;

        /// <summary>
        /// Checks if there is sufficient stock for the requested StockQuantity.
        /// Provides type-safe comparison with another StockQuantity value.
        /// </summary>
        /// <param name="requestedQuantity">StockQuantity needed for operation</param>
        /// <returns>True if sufficient stock is available, false otherwise</returns>
        public bool IsSufficient(StockQuantity requestedQuantity) => 
            Value >= requestedQuantity.Value;

        /// <summary>
        /// Adds the specified quantity to current stock.
        /// Returns a new StockQuantity instance with the combined amount.
        /// </summary>
        /// <param name="quantity">Quantity to add (must be non-negative)</param>
        /// <returns>New StockQuantity instance with increased amount</returns>
        /// <exception cref="ArgumentException">Thrown when quantity is negative</exception>
        /// <exception cref="OverflowException">Thrown when result exceeds integer limits</exception>
        /// <remarks>
        /// Addition Rules:
        /// - Added quantity must be non-negative
        /// - Result must not exceed integer maximum value
        /// - Immutable operation pattern preserves original value
        /// - Overflow protection for large stock additions
        /// 
        /// Use Cases:
        /// - Stock replenishment operations
        /// - Inventory receiving and putaway
        /// - Stock adjustment corrections
        /// - Return-to-stock processing
        /// </remarks>
        public StockQuantity Add(int quantity)
        {
            if (quantity < 0)
                throw new ArgumentException("Added quantity cannot be negative", nameof(quantity));

            checked
            {
                return new StockQuantity(Value + quantity);
            }
        }

        /// <summary>
        /// Adds another StockQuantity to current stock.
        /// Provides type-safe addition with overflow protection.
        /// </summary>
        /// <param name="other">StockQuantity to add</param>
        /// <returns>New StockQuantity instance with combined amounts</returns>
        public StockQuantity Add(StockQuantity other)
        {
            checked
            {
                return new StockQuantity(Value + other.Value);
            }
        }

        /// <summary>
        /// Subtracts the specified quantity from current stock.
        /// Returns a new StockQuantity instance with the reduced amount.
        /// </summary>
        /// <param name="quantity">Quantity to subtract (must be non-negative)</param>
        /// <returns>New StockQuantity instance with decreased amount</returns>
        /// <exception cref="ArgumentException">Thrown when quantity is negative or exceeds available stock</exception>
        /// <remarks>
        /// Subtraction Rules:
        /// - Subtracted quantity must be non-negative
        /// - Result cannot be negative (business rule)
        /// - Sufficient stock must be available for subtraction
        /// - Immutable operation pattern preserves original value
        /// 
        /// Use Cases:
        /// - Stock allocation and reservation
        /// - Order fulfillment processing
        /// - Stock adjustment corrections
        /// - Damage or loss reporting
        /// </remarks>
        public StockQuantity Subtract(int quantity)
        {
            if (quantity < 0)
                throw new ArgumentException("Subtracted quantity cannot be negative", nameof(quantity));

            if (Value < quantity)
                throw new ArgumentException($"Cannot subtract {quantity} from {Value}. Insufficient stock.", nameof(quantity));

            return new StockQuantity(Value - quantity);
        }

        /// <summary>
        /// Subtracts another StockQuantity from current stock.
        /// Provides type-safe subtraction with business rule validation.
        /// </summary>
        /// <param name="other">StockQuantity to subtract</param>
        /// <returns>New StockQuantity instance with reduced amount</returns>
        public StockQuantity Subtract(StockQuantity other)
        {
            if (Value < other.Value)
                throw new ArgumentException($"Cannot subtract {other.Value} from {Value}. Insufficient stock.");

            return new StockQuantity(Value - other.Value);
        }

        /// <summary>
        /// Compares this stock quantity with another for equality.
        /// Based solely on the quantity value.
        /// </summary>
        /// <param name="other">StockQuantity instance to compare with</param>
        /// <returns>True if quantities are equal</returns>
        public bool Equals(StockQuantity other) => Value == other.Value;

        /// <summary>
        /// Compares this stock quantity with another for ordering.
        /// Enables sorting and comparison operations.
        /// </summary>
        /// <param name="other">StockQuantity instance to compare with</param>
        /// <returns>Comparison result (-1, 0, or 1)</returns>
        public int CompareTo(StockQuantity other) => Value.CompareTo(other.Value);

        /// <summary>
        /// Provides object equality comparison with type checking.
        /// </summary>
        /// <param name="obj">Object to compare with</param>
        /// <returns>True if objects are equal</returns>
        public override bool Equals(object? obj) => obj is StockQuantity other && Equals(other);

        /// <summary>
        /// Generates hash code for collection operations.
        /// Based on the quantity value for uniqueness.
        /// </summary>
        /// <returns>Hash code for this stock quantity instance</returns>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// Provides string representation for debugging and logging.
        /// Shows quantity value with appropriate formatting.
        /// </summary>
        /// <returns>Formatted stock quantity string</returns>
        public override string ToString() => $"{Value} units";

        // Operator overloads for convenient usage
        public static StockQuantity operator +(StockQuantity left, int right) => left.Add(right);
        public static StockQuantity operator +(StockQuantity left, StockQuantity right) => left.Add(right);
        public static StockQuantity operator -(StockQuantity left, int right) => left.Subtract(right);
        public static StockQuantity operator -(StockQuantity left, StockQuantity right) => left.Subtract(right);
        
        public static bool operator ==(StockQuantity left, StockQuantity right) => left.Equals(right);
        public static bool operator !=(StockQuantity left, StockQuantity right) => !left.Equals(right);
        public static bool operator <(StockQuantity left, StockQuantity right) => left.CompareTo(right) < 0;
        public static bool operator <=(StockQuantity left, StockQuantity right) => left.CompareTo(right) <= 0;
        public static bool operator >(StockQuantity left, StockQuantity right) => left.CompareTo(right) > 0;
        public static bool operator >=(StockQuantity left, StockQuantity right) => left.CompareTo(right) >= 0;

        // Implicit conversion operators for convenience
        public static implicit operator int(StockQuantity stockQuantity) => stockQuantity.Value;
        public static implicit operator StockQuantity(int value) => new(value);

        /// <summary>
        /// Creates a zero stock quantity for initialization scenarios.
        /// Represents out-of-stock condition.
        /// </summary>
        /// <returns>StockQuantity with zero value</returns>
        public static StockQuantity Zero => new(0);

        /// <summary>
        /// Creates a stock quantity from an integer value.
        /// Provides convenient creation with validation.
        /// </summary>
        /// <param name="value">Integer stock value</param>
        /// <returns>StockQuantity instance with specified value</returns>
        public static StockQuantity FromValue(int value) => new(value);

        /// <summary>
        /// Calculates the maximum quantity that can be allocated from available stock.
        /// Supports partial allocation scenarios and business logic.
        /// </summary>
        /// <param name="requestedQuantity">Desired quantity for allocation</param>
        /// <returns>Maximum allocatable quantity (limited by available stock)</returns>
        /// <remarks>
        /// Allocation Logic:
        /// - Returns requested quantity if sufficient stock available
        /// - Returns available stock if requested exceeds available
        /// - Returns zero if no stock available
        /// - Handles negative requests as zero allocation
        /// 
        /// Use Cases:
        /// - Partial order fulfillment scenarios
        /// - Stock allocation optimization
        /// - Inventory constraint handling
        /// - Business logic for stock shortages
        /// </remarks>
        public int MaxAllocatable(int requestedQuantity)
        {
            if (requestedQuantity <= 0) return 0;
            return Math.Min(Value, requestedQuantity);
        }
    }
}