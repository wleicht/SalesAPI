namespace SalesApi.Domain.ValueObjects
{
    /// <summary>
    /// Represents a monetary amount with currency information as an immutable value object.
    /// Provides type-safe money handling with arithmetic operations, currency validation,
    /// and business rule enforcement for financial calculations in the sales domain.
    /// </summary>
    /// <remarks>
    /// The Money value object addresses common financial calculation problems by:
    /// 
    /// Type Safety:
    /// - Prevents accidental mixing of different currencies
    /// - Compile-time safety for monetary operations
    /// - Clear distinction between monetary and numeric values
    /// - Explicit currency handling in all operations
    /// 
    /// Business Rules:
    /// - Enforces non-negative amounts for business contexts
    /// - Validates currency codes for consistency
    /// - Provides safe arithmetic operations with overflow protection
    /// - Maintains precision for financial accuracy requirements
    /// 
    /// Immutability:
    /// - Value objects cannot be modified after creation
    /// - All operations return new instances
    /// - Thread-safe by design
    /// - Prevents accidental state mutation
    /// 
    /// The design follows Domain-Driven Design principles for value objects
    /// and provides a robust foundation for financial calculations throughout
    /// the sales domain while preventing common monetary calculation errors.
    /// </remarks>
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        /// <summary>
        /// The monetary amount as a decimal value.
        /// Maintains precision required for financial calculations.
        /// </summary>
        /// <value>Decimal amount with appropriate precision for currency</value>
        public decimal Amount { get; }

        /// <summary>
        /// Three-letter ISO 4217 currency code (e.g., USD, EUR, GBP).
        /// Ensures currency consistency and enables proper formatting.
        /// </summary>
        /// <value>ISO 4217 currency code string</value>
        public string Currency { get; }

        /// <summary>
        /// Indicates whether this money instance represents zero amount.
        /// Useful for business logic and validation scenarios.
        /// </summary>
        public bool IsZero => Amount == 0m;

        /// <summary>
        /// Indicates whether this money instance represents a positive amount.
        /// Supports business rule validation and calculations.
        /// </summary>
        public bool IsPositive => Amount > 0m;

        /// <summary>
        /// Creates a new Money instance with the specified amount and currency.
        /// Validates business rules and ensures data integrity.
        /// </summary>
        /// <param name="amount">Monetary amount (must be non-negative)</param>
        /// <param name="currency">ISO 4217 currency code (defaults to USD)</param>
        /// <exception cref="ArgumentException">Thrown when amount is negative or currency is invalid</exception>
        /// <remarks>
        /// Validation Rules:
        /// - Amount must be non-negative (business rule for sales domain)
        /// - Currency must be a valid 3-character ISO code
        /// - Currency code is normalized to uppercase
        /// - Default currency is USD for backward compatibility
        /// 
        /// Business Rationale:
        /// - Negative amounts prevented to avoid business logic errors
        /// - Currency validation ensures consistent financial reporting
        /// - ISO 4217 compliance supports international operations
        /// - Default currency simplifies common usage scenarios
        /// </remarks>
        public Money(decimal amount, string currency = "USD")
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative in sales domain", nameof(amount));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency cannot be null or empty", nameof(currency));

            if (currency.Length != 3)
                throw new ArgumentException("Currency must be a 3-character ISO 4217 code", nameof(currency));

            Amount = amount;
            Currency = currency.ToUpperInvariant();
        }

        /// <summary>
        /// Adds two money amounts, ensuring currency compatibility.
        /// Returns a new Money instance with the combined amount.
        /// </summary>
        /// <param name="other">Money amount to add</param>
        /// <returns>New Money instance with combined amount</returns>
        /// <exception cref="InvalidOperationException">Thrown when currencies don't match</exception>
        /// <remarks>
        /// Currency Validation:
        /// - Both amounts must have the same currency
        /// - Result maintains the common currency
        /// - No automatic currency conversion performed
        /// - Explicit conversion required for different currencies
        /// 
        /// Arithmetic Safety:
        /// - Decimal arithmetic prevents floating-point errors
        /// - Overflow protection through decimal type limits
        /// - Result precision maintained for financial accuracy
        /// - Immutable operation pattern preserves original values
        /// </remarks>
        public Money Add(Money other)
        {
            ValidateCurrencyCompatibility(other, nameof(other));
            return new Money(Amount + other.Amount, Currency);
        }

        /// <summary>
        /// Subtracts another money amount from this instance.
        /// Ensures currency compatibility and non-negative results.
        /// </summary>
        /// <param name="other">Money amount to subtract</param>
        /// <returns>New Money instance with difference</returns>
        /// <exception cref="InvalidOperationException">Thrown when currencies don't match or result would be negative</exception>
        public Money Subtract(Money other)
        {
            ValidateCurrencyCompatibility(other, nameof(other));
            
            var result = Amount - other.Amount;
            if (result < 0)
                throw new InvalidOperationException("Subtraction would result in negative amount");

            return new Money(result, Currency);
        }

        /// <summary>
        /// Multiplies the money amount by a scalar factor.
        /// Useful for quantity-based calculations and discounts.
        /// </summary>
        /// <param name="factor">Multiplication factor (must be non-negative)</param>
        /// <returns>New Money instance with multiplied amount</returns>
        /// <exception cref="ArgumentException">Thrown when factor is negative</exception>
        public Money Multiply(decimal factor)
        {
            if (factor < 0)
                throw new ArgumentException("Factor cannot be negative", nameof(factor));

            return new Money(Amount * factor, Currency);
        }

        /// <summary>
        /// Divides the money amount by a scalar divisor.
        /// Maintains precision and prevents division by zero.
        /// </summary>
        /// <param name="divisor">Division factor (must be positive)</param>
        /// <returns>New Money instance with divided amount</returns>
        /// <exception cref="ArgumentException">Thrown when divisor is zero or negative</exception>
        public Money Divide(decimal divisor)
        {
            if (divisor <= 0)
                throw new ArgumentException("Divisor must be positive", nameof(divisor));

            return new Money(Amount / divisor, Currency);
        }

        /// <summary>
        /// Compares this money amount with another for equality.
        /// Both amount and currency must match for equality.
        /// </summary>
        /// <param name="other">Money instance to compare with</param>
        /// <returns>True if amounts and currencies are equal</returns>
        public bool Equals(Money other)
        {
            return Amount == other.Amount && Currency == other.Currency;
        }

        /// <summary>
        /// Compares this money amount with another for ordering.
        /// Requires matching currencies for valid comparison.
        /// </summary>
        /// <param name="other">Money instance to compare with</param>
        /// <returns>Comparison result (-1, 0, or 1)</returns>
        /// <exception cref="InvalidOperationException">Thrown when currencies don't match</exception>
        public int CompareTo(Money other)
        {
            ValidateCurrencyCompatibility(other, nameof(other));
            return Amount.CompareTo(other.Amount);
        }

        /// <summary>
        /// Provides object equality comparison with type checking.
        /// </summary>
        /// <param name="obj">Object to compare with</param>
        /// <returns>True if objects are equal</returns>
        public override bool Equals(object? obj) => obj is Money other && Equals(other);

        /// <summary>
        /// Generates hash code for collection operations.
        /// Based on both amount and currency for uniqueness.
        /// </summary>
        /// <returns>Hash code for this money instance</returns>
        public override int GetHashCode() => HashCode.Combine(Amount, Currency);

        /// <summary>
        /// Provides formatted string representation of the money amount.
        /// Uses culture-appropriate formatting for the currency.
        /// </summary>
        /// <returns>Formatted money string</returns>
        public override string ToString()
        {
            return $"{Amount:N2} {Currency}";
        }

        /// <summary>
        /// Provides culture-specific formatting for display purposes.
        /// Attempts to use proper currency formatting when available.
        /// </summary>
        /// <param name="formatProvider">Culture-specific format provider</param>
        /// <returns>Culture-formatted money string</returns>
        public string ToString(IFormatProvider formatProvider)
        {
            try
            {
                // Attempt to create culture-specific currency formatting
                var cultureInfo = formatProvider as System.Globalization.CultureInfo;
                if (cultureInfo != null)
                {
                    return Amount.ToString("C", cultureInfo) + $" ({Currency})";
                }
            }
            catch
            {
                // Fall back to default formatting if culture-specific formatting fails
            }

            return ToString();
        }

        /// <summary>
        /// Validates that two money instances have compatible currencies for operations.
        /// </summary>
        /// <param name="other">Money instance to validate</param>
        /// <param name="paramName">Parameter name for exception reporting</param>
        /// <exception cref="InvalidOperationException">Thrown when currencies are incompatible</exception>
        private void ValidateCurrencyCompatibility(Money other, string paramName)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException(
                    $"Cannot perform operation on different currencies: {Currency} and {other.Currency}");
        }

        // Operator overloads for convenient usage
        public static Money operator +(Money left, Money right) => left.Add(right);
        public static Money operator -(Money left, Money right) => left.Subtract(right);
        public static Money operator *(Money left, decimal right) => left.Multiply(right);
        public static Money operator *(decimal left, Money right) => right.Multiply(left);
        public static Money operator /(Money left, decimal right) => left.Divide(right);
        
        public static bool operator ==(Money left, Money right) => left.Equals(right);
        public static bool operator !=(Money left, Money right) => !left.Equals(right);
        public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
        public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
        public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
        public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

        /// <summary>
        /// Creates a zero money amount in the specified currency.
        /// Useful for initialization and calculation scenarios.
        /// </summary>
        /// <param name="currency">Currency for the zero amount</param>
        /// <returns>Zero money amount in specified currency</returns>
        public static Money Zero(string currency = "USD") => new Money(0, currency);

        /// <summary>
        /// Creates a money amount from an integer value.
        /// Provides convenient creation for whole currency units.
        /// </summary>
        /// <param name="amount">Integer amount</param>
        /// <param name="currency">Currency code</param>
        /// <returns>Money instance with specified amount and currency</returns>
        public static Money FromAmount(int amount, string currency = "USD") => new Money(amount, currency);

        /// <summary>
        /// Creates a money amount from a decimal value.
        /// Provides convenient creation with full precision support.
        /// </summary>
        /// <param name="amount">Decimal amount</param>
        /// <param name="currency">Currency code</param>
        /// <returns>Money instance with specified amount and currency</returns>
        public static Money FromAmount(decimal amount, string currency = "USD") => new Money(amount, currency);
    }
}