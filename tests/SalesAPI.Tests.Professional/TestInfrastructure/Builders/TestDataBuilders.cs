using SalesApi.Models;
using InventoryApi.Models;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Builders
{
    /// <summary>
    /// Consolidated test data builders to eliminate duplication across test files.
    /// Enhanced to cover complex scenarios that were in removed test projects.
    /// </summary>
    public static class TestDataBuilders
    {
        public static class Orders
        {
            public static OrderBuilder NewOrder() => new OrderBuilder();
            
            /// <summary>
            /// Creates an order builder with complex scenarios for comprehensive testing
            /// </summary>
            public static OrderBuilder ComplexScenario() => new OrderBuilder()
                .WithMultipleItems()
                .WithComplexCalculations();

            /// <summary>
            /// Creates an order builder for high-value scenarios
            /// </summary>
            public static OrderBuilder HighValue() => new OrderBuilder()
                .WithItem("Premium Product", 1, 1000.00m)
                .WithItem("Luxury Item", 2, 750.00m);

            /// <summary>
            /// Creates an order builder for bulk order scenarios
            /// </summary>
            public static OrderBuilder BulkOrder() => new OrderBuilder()
                .WithItem("Bulk Item 1", 50, 10.00m)
                .WithItem("Bulk Item 2", 25, 20.00m)
                .WithItem("Bulk Item 3", 100, 5.00m);
        }

        public static class Products
        {
            public static ProductBuilder NewProduct() => new ProductBuilder();
            
            /// <summary>
            /// Creates a product builder for low stock scenarios
            /// </summary>
            public static ProductBuilder LowStock() => new ProductBuilder()
                .WithStock(1)
                .WithName("Low Stock Product");

            /// <summary>
            /// Creates a product builder for high value scenarios
            /// </summary>
            public static ProductBuilder HighValue() => new ProductBuilder()
                .WithPrice(1000.00m)
                .WithName("High Value Product")
                .WithDescription("Premium product with high value");

            /// <summary>
            /// Creates a product builder for zero stock scenarios (out of stock)
            /// </summary>
            public static ProductBuilder OutOfStock() => new ProductBuilder()
                .WithStock(0)
                .WithName("Out of Stock Product");

            /// <summary>
            /// Creates a product builder for bulk inventory scenarios
            /// </summary>
            public static ProductBuilder BulkInventory() => new ProductBuilder()
                .WithStock(1000)
                .WithPrice(5.00m)
                .WithName("Bulk Inventory Product");
        }
    }

    public class OrderBuilder
    {
        private Order _order;

        public OrderBuilder()
        {
            _order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>()
            };
        }

        public OrderBuilder WithId(Guid id)
        {
            _order.Id = id;
            return this;
        }

        public OrderBuilder WithCustomer(Guid customerId)
        {
            _order.CustomerId = customerId;
            return this;
        }

        public OrderBuilder WithStatus(string status)
        {
            _order.Status = status;
            return this;
        }

        public OrderBuilder WithItems(params OrderItem[] items)
        {
            _order.Items = items.ToList();
            _order.TotalAmount = _order.Items.Sum(i => i.TotalPrice);
            return this;
        }

        public OrderBuilder WithStandardItems()
        {
            var items = new[]
            {
                new OrderItem
                {
                    OrderId = _order.Id,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Standard Product 1",
                    Quantity = 2,
                    UnitPrice = 50.00m
                },
                new OrderItem
                {
                    OrderId = _order.Id,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Standard Product 2",
                    Quantity = 1,
                    UnitPrice = 75.00m
                }
            };

            return WithItems(items);
        }

        /// <summary>
        /// Adds multiple items with different price points and quantities
        /// </summary>
        public OrderBuilder WithMultipleItems()
        {
            return this
                .WithItem("Product A", 3, 25.00m)
                .WithItem("Product B", 1, 100.00m)
                .WithItem("Product C", 2, 45.50m)
                .WithItem("Product D", 5, 15.99m);
        }

        /// <summary>
        /// Creates an order with complex calculations for testing edge cases
        /// </summary>
        public OrderBuilder WithComplexCalculations()
        {
            return this
                .WithItem("Decimal Test Product", 3, 33.333m) // Tests decimal precision
                .WithItem("Large Quantity Product", 999, 1.01m) // Tests large quantities
                .WithItem("High Value Product", 1, 999.99m); // Tests high values
        }

        public OrderBuilder WithItem(string productName, int quantity, decimal unitPrice)
        {
            var item = new OrderItem
            {
                OrderId = _order.Id,
                ProductId = Guid.NewGuid(),
                ProductName = productName,
                Quantity = quantity,
                UnitPrice = unitPrice
            };

            _order.Items.Add(item);
            return this;
        }

        /// <summary>
        /// Sets the order creation date for testing temporal scenarios
        /// </summary>
        public OrderBuilder WithCreatedDate(DateTime createdAt)
        {
            _order.CreatedAt = createdAt;
            return this;
        }

        public Order Build()
        {
            _order.TotalAmount = _order.Items.Sum(i => i.TotalPrice);
            return _order;
        }
    }

    public class ProductBuilder
    {
        private Product _product;

        public ProductBuilder()
        {
            _product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Description = "A test product",
                Price = 99.99m,
                StockQuantity = 50,
                CreatedAt = DateTime.UtcNow
            };
        }

        public ProductBuilder WithId(Guid id)
        {
            _product.Id = id;
            return this;
        }

        public ProductBuilder WithName(string name)
        {
            _product.Name = name;
            return this;
        }

        public ProductBuilder WithPrice(decimal price)
        {
            _product.Price = price;
            return this;
        }

        public ProductBuilder WithStock(int quantity)
        {
            _product.StockQuantity = quantity;
            return this;
        }

        public ProductBuilder WithDescription(string description)
        {
            _product.Description = description;
            return this;
        }

        /// <summary>
        /// Creates a product with creation timestamp for testing
        /// </summary>
        public ProductBuilder WithCreatedDate(DateTime createdAt)
        {
            _product.CreatedAt = createdAt;
            return this;
        }

        /// <summary>
        /// Creates a product with invalid data for negative testing
        /// </summary>
        public ProductBuilder WithInvalidData()
        {
            _product.Name = ""; // Invalid empty name
            _product.Price = -1m; // Invalid negative price  
            _product.StockQuantity = -1; // Invalid negative stock
            return this;
        }

        public Product Build() => _product;
    }
}