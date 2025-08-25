using FluentValidation.TestHelper;
using BuildingBlocks.Contracts.Products;
using InventoryApi.Validation;
using Xunit;

namespace InventoryApi.Tests.Validation
{
    /// <summary>
    /// Unit tests for CreateProductDtoValidator.
    /// Tests validation rules for product creation without external dependencies.
    /// </summary>
    public class CreateProductDtoValidatorTests
    {
        private readonly CreateProductDtoValidator _validator;

        public CreateProductDtoValidatorTests()
        {
            _validator = new CreateProductDtoValidator();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Name_WhenEmpty_ShouldHaveValidationError(string name)
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = name,
                Description = "Valid description",
                Price = 10.00m,
                StockQuantity = 5
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Name is required.");
        }

        [Fact]
        public void Name_WhenTooLong_ShouldHaveValidationError()
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = new string('a', 101), // 101 characters
                Description = "Valid description",
                Price = 10.00m,
                StockQuantity = 5
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Name must be at most 100 characters.");
        }

        [Fact]
        public void Name_WhenValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = "Valid Product Name",
                Description = "Valid description",
                Price = 10.00m,
                StockQuantity = 5
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Description_WhenEmpty_ShouldHaveValidationError(string description)
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = "Valid Name",
                Description = description,
                Price = 10.00m,
                StockQuantity = 5
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Description is required.");
        }

        [Fact]
        public void Description_WhenTooLong_ShouldHaveValidationError()
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = "Valid Name",
                Description = new string('a', 501), // 501 characters
                Price = 10.00m,
                StockQuantity = 5
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Description must be at most 500 characters.");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-0.01)]
        public void Price_WhenNegative_ShouldHaveValidationError(decimal price)
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = "Valid Name",
                Description = "Valid description",
                Price = price,
                StockQuantity = 5
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Price)
                  .WithErrorMessage("Price must be greater than or equal to 0.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.01)]
        [InlineData(99.99)]
        [InlineData(1000)]
        public void Price_WhenValid_ShouldNotHaveValidationError(decimal price)
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = "Valid Name",
                Description = "Valid description",
                Price = price,
                StockQuantity = 5
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Price);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void StockQuantity_WhenNegative_ShouldHaveValidationError(int stockQuantity)
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = "Valid Name",
                Description = "Valid description",
                Price = 10.00m,
                StockQuantity = stockQuantity
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.StockQuantity)
                  .WithErrorMessage("Stock quantity must be greater than or equal to 0.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        public void StockQuantity_WhenValid_ShouldNotHaveValidationError(int stockQuantity)
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = "Valid Name",
                Description = "Valid description",
                Price = 10.00m,
                StockQuantity = stockQuantity
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.StockQuantity);
        }

        [Fact]
        public void ValidProduct_ShouldPassAllValidation()
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = "Premium Laptop",
                Description = "High-performance laptop for professional use",
                Price = 1299.99m,
                StockQuantity = 25
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void InvalidProduct_ShouldHaveMultipleValidationErrors()
        {
            // Arrange
            var dto = new CreateProductDto
            {
                Name = "", // Invalid
                Description = "", // Invalid
                Price = -10, // Invalid
                StockQuantity = -5 // Invalid
            };

            // Act & Assert
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.Description);
            result.ShouldHaveValidationErrorFor(x => x.Price);
            result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
        }
    }
}