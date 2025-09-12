using FluentAssertions;
using InventoryApi.Models;
using SalesAPI.Tests.Professional.TestInfrastructure.Builders;
using Xunit;

namespace SalesAPI.Tests.Professional.Domain.Tests.Models
{
    /// <summary>
    /// Pure domain logic tests for Product entity.
    /// Tests business rules and calculations without infrastructure dependencies.
    /// Refactored to use TestDataBuilders to eliminate code duplication.
    /// </summary>
    public class ProductTests
    {
        [Fact]
        public void CreateProduct_WithValidData_ShouldInitializeCorrectly()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var name = "Test Product";
            var description = "A test product for unit testing";
            var price = 99.99m;
            var stockQuantity = 50;
            
            // Act - Using TestDataBuilder instead of manual construction
            var product = TestDataBuilders.Products.NewProduct()
                .WithId(productId)
                .WithName(name)
                .WithDescription(description)
                .WithPrice(price)
                .WithStock(stockQuantity)
                .Build();
            
            // Assert
            product.Id.Should().Be(productId);
            product.Name.Should().Be(name);
            product.Description.Should().Be(description);
            product.Price.Should().Be(price);
            product.StockQuantity.Should().Be(stockQuantity);
        }

        [Fact]
        public void DebitStock_WithSufficientQuantity_ShouldReduceStock()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(100)
                .Build();
            var debitAmount = 25;
            
            // Act - Simulating stock debit business logic
            product.StockQuantity -= debitAmount;
            
            // Assert
            product.StockQuantity.Should().Be(75);
        }

        [Fact]
        public void DebitStock_ExactAmount_ShouldResultInZeroStock()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(50)
                .Build();
            var debitAmount = 50;
            
            // Act
            product.StockQuantity -= debitAmount;
            
            // Assert
            product.StockQuantity.Should().Be(0);
        }

        [Fact]
        public void CreditStock_ShouldIncreaseStock()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(25)
                .Build();
            var creditAmount = 75;
            
            // Act - Simulating stock replenishment business logic
            product.StockQuantity += creditAmount;
            
            // Assert
            product.StockQuantity.Should().Be(100);
        }

        [Fact]
        public void HasSufficientStock_WithEnoughQuantity_ShouldReturnTrue()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(100)
                .Build();
            var requestedQuantity = 50;
            
            // Act - Business logic for stock availability check
            var hasSufficientStock = product.StockQuantity >= requestedQuantity;
            
            // Assert
            hasSufficientStock.Should().BeTrue();
        }

        [Fact]
        public void HasSufficientStock_WithInsufficientQuantity_ShouldReturnFalse()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(25)
                .Build();
            var requestedQuantity = 50;
            
            // Act
            var hasSufficientStock = product.StockQuantity >= requestedQuantity;
            
            // Assert
            hasSufficientStock.Should().BeFalse();
        }

        [Fact]
        public void HasSufficientStock_WithExactQuantity_ShouldReturnTrue()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(50)
                .Build();
            var requestedQuantity = 50;
            
            // Act
            var hasSufficientStock = product.StockQuantity >= requestedQuantity;
            
            // Assert
            hasSufficientStock.Should().BeTrue();
        }

        [Fact]
        public void CalculateValue_ShouldMultiplyPriceByStock()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithPrice(29.99m)
                .WithStock(100)
                .Build();
            
            // Act - Business logic for inventory value calculation
            var totalValue = product.Price * product.StockQuantity;
            
            // Assert
            totalValue.Should().Be(2999.00m);
        }

        [Fact]
        public void UpdatePrice_ShouldChangeProductPrice()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithPrice(50.00m)
                .Build();
            var newPrice = 75.00m;
            
            // Act
            product.Price = newPrice;
            
            // Assert
            product.Price.Should().Be(newPrice);
        }

        [Fact]
        public void Product_WithZeroStock_ShouldHandleCorrectly()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(0)
                .Build();
            
            // Act & Assert
            product.StockQuantity.Should().Be(0);
            
            // Business logic checks
            var hasSufficientStockForAny = product.StockQuantity >= 1;
            hasSufficientStockForAny.Should().BeFalse();
            
            var totalValue = product.Price * product.StockQuantity;
            totalValue.Should().Be(0);
        }

        [Fact]
        public void Product_WithHighStock_ShouldHandleCorrectly()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(1_000_000)
                .Build();
            
            // Act & Assert
            product.StockQuantity.Should().Be(1_000_000);
            
            // Business logic for large quantities
            var canFulfillLargeOrder = product.StockQuantity >= 500_000;
            canFulfillLargeOrder.Should().BeTrue();
        }

        [Fact]
        public void Product_WithDecimalPrice_ShouldHandlePrecision()
        {
            // Arrange
            var precisePrice = 33.333333m;
            var product = TestDataBuilders.Products.NewProduct()
                .WithPrice(precisePrice)
                .WithStock(3)
                .Build();
            
            // Act
            var totalValue = product.Price * product.StockQuantity;
            
            // Assert
            product.Price.Should().Be(33.333333m);
            totalValue.Should().Be(99.999999m);
        }

        [Fact]
        public void MultipleStockOperations_ShouldMaintainCorrectBalance()
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(100)
                .Build();
            
            // Act - Simulate multiple operations
            product.StockQuantity -= 30; // Debit 30
            product.StockQuantity += 20; // Credit 20
            product.StockQuantity -= 15; // Debit 15
            product.StockQuantity += 5;  // Credit 5
            
            // Assert
            product.StockQuantity.Should().Be(80); // 100 - 30 + 20 - 15 + 5
        }

        [Theory]
        [InlineData(100, 10, true)]
        [InlineData(100, 100, true)]
        [InlineData(100, 101, false)]
        [InlineData(0, 1, false)]
        [InlineData(50, 25, true)]
        public void StockAvailabilityCheck_WithVariousQuantities_ShouldReturnExpectedResult(
            int currentStock, int requestedQuantity, bool expectedResult)
        {
            // Arrange
            var product = TestDataBuilders.Products.NewProduct()
                .WithStock(currentStock)
                .Build();
            
            // Act
            var hasStock = product.StockQuantity >= requestedQuantity;
            
            // Assert
            hasStock.Should().Be(expectedResult);
        }

        // Note: Removed duplicated helper method CreateTestProduct()
        // This is now replaced by TestDataBuilders.Products pattern
    }
}