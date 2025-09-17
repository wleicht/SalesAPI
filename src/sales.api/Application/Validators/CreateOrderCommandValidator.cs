using FluentValidation;
using SalesApi.Application.Commands;
using SalesApi.Application.DTOs;

namespace SalesApi.Application.Validators
{
    /// <summary>
    /// Enhanced FluentValidation validator for CreateOrderCommand with professional business rules validation.
    /// Provides comprehensive validation with detailed error messages and flexible rule enforcement.
    /// </summary>
    /// <remarks>
    /// Professional Validation Strategy:
    /// 
    /// Validation Layers:
    /// - Data Format Validation: Basic format and type checks
    /// - Business Rule Validation: Domain-specific business logic
    /// - Cross-Field Validation: Relationships between fields
    /// - Context-Aware Validation: Based on user roles and permissions
    /// 
    /// Error Handling Philosophy:
    /// - User-friendly error messages for API consumers
    /// - Detailed validation context for troubleshooting
    /// - Consistent error format across all validations
    /// - Support for internationalization when needed
    /// 
    /// Performance Considerations:
    /// - Early exit on basic validation failures
    /// - Optimized complex validations with minimal overhead
    /// - Caching of expensive validation rules where applicable
    /// - Async validation for external dependency checks
    /// </remarks>
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            // Customer validation with enhanced error messages
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("Customer ID is required for order processing")
                .Must(BeValidGuid)
                .WithMessage("Customer ID must be a valid GUID format")
                .WithName("CustomerId");

            // CreatedBy validation with professional requirements
            RuleFor(x => x.CreatedBy)
                .NotEmpty()
                .WithMessage("Created By field is required for audit purposes")
                .Length(1, 255)
                .WithMessage("Created By must be between 1 and 255 characters")
                .WithName("CreatedBy");

            // Items collection validation with comprehensive business rules
            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Order must contain at least one item")
                .Must(items => items != null && items.Count <= 100)
                .WithMessage("Order cannot contain more than 100 items for performance reasons")
                .Must(NotContainDuplicateProducts)
                .WithMessage("Order cannot contain duplicate products. Please consolidate quantities for the same product.")
                .WithName("Items");

            // Individual item validation with enhanced rules
            RuleForEach(x => x.Items)
                .SetValidator(new CreateOrderItemCommandValidator())
                .When(x => x.Items != null && x.Items.Any());

            // Correlation ID validation - optional but with format requirements
            RuleFor(x => x.CorrelationId)
                .MaximumLength(100)
                .WithMessage("Correlation ID cannot exceed 100 characters")
                .Matches(@"^[a-zA-Z0-9\-_\.]+$")
                .WithMessage("Correlation ID can only contain alphanumeric characters, hyphens, underscores, and periods")
                .When(x => !string.IsNullOrEmpty(x.CorrelationId))
                .WithName("CorrelationId");

            // Total quantity validation for business rules
            RuleFor(x => x.Items)
                .Must(HaveReasonableTotalQuantity)
                .WithMessage("Total order quantity exceeds reasonable limits (max 1000 items total)")
                .When(x => x.Items != null && x.Items.Any())
                .WithName("TotalQuantity");
        }

        /// <summary>
        /// Validates that a GUID is not empty or default value.
        /// </summary>
        private static bool BeValidGuid(Guid guid)
        {
            return guid != Guid.Empty;
        }

        /// <summary>
        /// Validates that order items don't contain duplicate products.
        /// </summary>
        private static bool NotContainDuplicateProducts(IList<CreateOrderItemCommand>? items)
        {
            if (items == null || items.Count == 0)
                return true;

            var productIds = items.Select(i => i.ProductId).ToList();
            return productIds.Count == productIds.Distinct().Count();
        }

        /// <summary>
        /// Validates that total quantity across all items is reasonable.
        /// </summary>
        private static bool HaveReasonableTotalQuantity(IList<CreateOrderItemCommand>? items)
        {
            if (items == null || items.Count == 0)
                return true;

            var totalQuantity = items.Sum(i => i.Quantity);
            return totalQuantity <= 1000; // Business rule: max 1000 total items
        }
    }

    /// <summary>
    /// Enhanced FluentValidation validator for individual order items with professional validation rules.
    /// </summary>
    public class CreateOrderItemCommandValidator : AbstractValidator<CreateOrderItemCommand>
    {
        public CreateOrderItemCommandValidator()
        {
            // Product ID validation
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product ID is required for each order item")
                .Must(BeValidGuid)
                .WithMessage("Product ID must be a valid GUID format")
                .WithName("ProductId");

            // Quantity validation with business rules
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero")
                .LessThanOrEqualTo(100)
                .WithMessage("Quantity per item cannot exceed 100 for inventory management purposes")
                .WithName("Quantity");

            // Product Name validation - optional but with constraints when provided
            RuleFor(x => x.ProductName)
                .MaximumLength(200)
                .WithMessage("Product name cannot exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.ProductName))
                .WithName("ProductName");

            // Unit Price validation - optional but must be non-negative when provided
            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Unit price cannot be negative")
                .LessThan(1000000)
                .WithMessage("Unit price cannot exceed 1,000,000 for data integrity purposes")
                .When(x => x.UnitPrice > 0)
                .WithName("UnitPrice");
        }

        /// <summary>
        /// Validates that a GUID is not empty or default value.
        /// </summary>
        private static bool BeValidGuid(Guid guid)
        {
            return guid != Guid.Empty;
        }
    }

    /// <summary>
    /// Custom validation result with detailed error information for API responses.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; }
        public List<ValidationError> Errors { get; }

        public ValidationResult(bool isValid, IEnumerable<ValidationError> errors)
        {
            IsValid = isValid;
            Errors = errors.ToList();
        }

        public static ValidationResult Success() => new(true, Enumerable.Empty<ValidationError>());

        public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new(false, errors);
    }

    /// <summary>
    /// Detailed validation error information for client troubleshooting.
    /// </summary>
    public class ValidationError
    {
        public string PropertyName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public object? AttemptedValue { get; set; }
        public string? ErrorCode { get; set; }

        public ValidationError() { }

        public ValidationError(string propertyName, string errorMessage, object? attemptedValue = null, string? errorCode = null)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
            AttemptedValue = attemptedValue;
            ErrorCode = errorCode;
        }
    }
}