using FluentAssertions;
using InventoryApi.Domain.Entities;
using SalesAPI.Tests.Professional.TestInfrastructure.Builders;
using Xunit;

namespace SalesAPI.Tests.Professional.Domain.Tests.Models
{
    /// <summary>
    /// Pure domain logic tests for Product entity.
    /// Tests business rules and calculations without infrastructure dependencies.
    /// Tests use domain methods instead of direct property manipulation.
    /// </summary>
    public class ProductTests
    {
        [Fact]
        public void CreateProduct_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var name = "Test Product";
            var description = "A test product for unit testing";
            var price = 99.99m;
            var stockQuantity = 50;
            
            // Act - Using domain entity constructor
            var product = new Product(name, description, price, stockQuantity, "test-user");
            
            // Assert
            product.Name.Should().Be(name);
            product.Description.Should().Be(description);
            product.Price.Should().Be(price);
            product.StockQuantity.Should().Be(stockQuantity);
            product.IsActive.Should().BeTrue();
        }

        [Fact]
        public void RemoveStock_WithSufficientQuantity_ShouldReduceStock()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            var removeAmount = 25;
            
            // Act - Using domain method
            product.RemoveStock(removeAmount, "test-user");
            
            // Assert
            product.StockQuantity.Should().Be(75);
        }

        [Fact]
        public void RemoveStock_ExactAmount_ShouldResultInZeroStock()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 50, "test-user");
            var removeAmount = 50;
            
            // Act
            product.RemoveStock(removeAmount, "test-user");
            
            // Assert
            product.StockQuantity.Should().Be(0);
        }

        [Fact]
        public void AddStock_ShouldIncreaseStock()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 25, "test-user");
            var addAmount = 75;
            
            // Act - Using domain method
            product.AddStock(addAmount, "test-user");
            
            // Assert
            product.StockQuantity.Should().Be(100);
        }

        [Fact]
        public void HasSufficientStock_WithEnoughQuantity_ShouldReturnTrue()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            var requestedQuantity = 50;
            
            // Act - Using domain method
            var hasSufficientStock = product.HasSufficientStock(requestedQuantity);
            
            // Assert
            hasSufficientStock.Should().BeTrue();
        }

        [Fact]
        public void HasSufficientStock_WithInsufficientQuantity_ShouldReturnFalse()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 25, "test-user");
            var requestedQuantity = 50;
            
            // Act
            var hasSufficientStock = product.HasSufficientStock(requestedQuantity);
            
            // Assert
            hasSufficientStock.Should().BeFalse();
        }

        [Fact]
        public void HasSufficientStock_WithExactQuantity_ShouldReturnTrue()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 50, "test-user");
            var requestedQuantity = 50;
            
            // Act
            var hasSufficientStock = product.HasSufficientStock(requestedQuantity);
            
            // Assert
            hasSufficientStock.Should().BeTrue();
        }

        [Fact]
        public void ReserveStock_WithSufficientQuantity_ShouldReduceStock()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            var reserveAmount = 25;
            
            // Act - Using domain method
            product.ReserveStock(reserveAmount, "test-user");
            
            // Assert
            product.StockQuantity.Should().Be(75);
        }

        [Fact]
        public void AllocateStock_WithSufficientQuantity_ShouldReduceStock()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            var allocateAmount = 30;
            
            // Act - Using domain method
            product.AllocateStock(allocateAmount, "test-user");
            
            // Assert
            product.StockQuantity.Should().Be(70);
        }

        [Fact]
        public void UpdatePrice_ShouldChangeProductPrice()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            var newPrice = 75.00m;
            
            // Act - Using domain method
            product.UpdatePrice(newPrice, "test-user");
            
            // Assert
            product.Price.Should().Be(newPrice);
        }

        [Fact]
        public void UpdateName_ShouldChangeProductName()
        {
            // Arrange
            var product = new Product("Old Name", "Description", 50.00m, 100, "test-user");
            var newName = "New Product Name";
            
            // Act - Using domain method
            product.UpdateName(newName, "test-user");
            
            // Assert
            product.Name.Should().Be(newName);
        }

        [Fact]
        public void Deactivate_ShouldMakeProductInactive()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            
            // Act - Using domain method
            product.Deactivate("test-user");
            
            // Assert
            product.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Activate_DeactivatedProduct_ShouldMakeProductActive()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            product.Deactivate("test-user");
            
            // Act - Using domain method
            product.Activate("test-user");
            
            // Assert
            product.IsActive.Should().BeTrue();
        }

        [Fact]
        public void IsOutOfStock_WithZeroStock_ShouldReturnTrue()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 1, "test-user");
            product.RemoveStock(1, "test-user"); // Make it zero
            
            // Act - Using domain method
            var isOutOfStock = product.IsOutOfStock();
            
            // Assert
            isOutOfStock.Should().BeTrue();
        }

        [Fact]
        public void IsLowStock_BelowMinimumLevel_ShouldReturnTrue()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 50, "test-user", minimumStockLevel: 20);
            product.RemoveStock(40, "test-user"); // Leaves 10, below minimum of 20
            
            // Act - Using domain method
            var isLowStock = product.IsLowStock();
            
            // Assert
            isLowStock.Should().BeTrue();
        }

        [Fact]
        public void IsAvailableForOrder_ActiveWithStock_ShouldReturnTrue()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            
            // Act - Using domain method
            var isAvailable = product.IsAvailableForOrder();
            
            // Assert
            isAvailable.Should().BeTrue();
        }

        [Fact]
        public void IsAvailableForOrder_InactiveWithStock_ShouldReturnFalse()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            product.Deactivate("test-user");
            
            // Act - Using domain method
            var isAvailable = product.IsAvailableForOrder();
            
            // Assert
            isAvailable.Should().BeFalse();
        }

        [Fact]
        public void RemoveStock_MoreThanAvailable_ShouldThrowException()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 10, "test-user");
            
            // Act & Assert
            product.Invoking(p => p.RemoveStock(15, "test-user"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot remove 15 units. Only 10 units available*");
        }

        [Fact]
        public void ReserveStock_OnInactiveProduct_ShouldThrowException()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            product.Deactivate("test-user");
            
            // Act & Assert
            product.Invoking(p => p.ReserveStock(10, "test-user"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot reserve stock for inactive product");
        }

        [Fact]
        public void ReleaseReservedStock_ShouldIncreaseStock()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            product.ReserveStock(20, "test-user"); // Stock becomes 80
            
            // Act - Using domain method
            product.ReleaseReservedStock(20, "test-user");
            
            // Assert
            product.StockQuantity.Should().Be(100); // Back to original
        }

        [Theory]
        [InlineData(100, 10, true)]
        [InlineData(100, 100, true)]
        [InlineData(100, 101, false)]
        [InlineData(0, 1, false)]
        [InlineData(50, 25, true)]
        public void HasSufficientStock_WithVariousQuantities_ShouldReturnExpectedResult(
            int currentStock, int requestedQuantity, bool expectedResult)
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, currentStock, "test-user");
            
            // Act
            var hasStock = product.HasSufficientStock(requestedQuantity);
            
            // Assert
            hasStock.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateProduct_WithNegativePrice_ShouldThrowException()
        {
            // Act & Assert
            var action = () => new Product("Test Product", "Description", -10.00m, 100, "test-user");
            
            action.Should().Throw<ArgumentException>()
                .WithMessage("Product price cannot be negative*");
        }

        [Fact]
        public void CreateProduct_WithEmptyName_ShouldThrowException()
        {
            // Act & Assert
            var action = () => new Product("", "Description", 50.00m, 100, "test-user");
            
            action.Should().Throw<ArgumentException>()
                .WithMessage("Product name cannot be empty*");
        }

        [Fact]
        public void AddStock_WithZeroQuantity_ShouldThrowException()
        {
            // Arrange
            var product = new Product("Test Product", "Description", 50.00m, 100, "test-user");
            
            // Act & Assert
            product.Invoking(p => p.AddStock(0, "test-user"))
                .Should().Throw<ArgumentException>()
                .WithMessage("Quantity to add must be positive*");
        }
    }
}