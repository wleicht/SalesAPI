using SalesApi.Domain.Entities;
using InventoryApi.Domain.Entities;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Builders
{
    /// <summary>
    /// Consolidated test data builders using professional domain entities.
    /// Eliminates duplication and provides consistent test data across all test scenarios.
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

    /// <summary>
    /// Builder for creating Order domain entities in tests.
    /// Uses professional domain entity methods instead of direct property manipulation.
    /// </summary>
    public class OrderBuilder
    {
        private Guid _customerId;
        private string _createdBy;
        private List<(Guid productId, string productName, int quantity, decimal unitPrice)> _items;

        public OrderBuilder()
        {
            _customerId = Guid.NewGuid();
            _createdBy = "test-user";
            _items = new List<(Guid, string, int, decimal)>();
        }

        public OrderBuilder WithCustomer(Guid customerId)
        {
            _customerId = customerId;
            return this;
        }

        public OrderBuilder WithCreatedBy(string createdBy)
        {
            _createdBy = createdBy;
            return this;
        }

        public OrderBuilder WithStandardItems()
        {
            _items.Add((Guid.NewGuid(), "Standard Product 1", 2, 50.00m));
            _items.Add((Guid.NewGuid(), "Standard Product 2", 1, 75.00m));
            return this;
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
            _items.Add((Guid.NewGuid(), productName, quantity, unitPrice));
            return this;
        }

        public OrderBuilder WithItem(Guid productId, string productName, int quantity, decimal unitPrice)
        {
            _items.Add((productId, productName, quantity, unitPrice));
            return this;
        }

        /// <summary>
        /// Builds the Order using professional domain entity constructor and methods
        /// </summary>
        public Order Build()
        {
            var order = new Order(_customerId, _createdBy);
            
            // Add items using domain methods
            foreach (var (productId, productName, quantity, unitPrice) in _items)
            {
                order.AddItem(productId, productName, quantity, unitPrice, _createdBy);
            }

            return order;
        }

        /// <summary>
        /// Builds and confirms the order
        /// </summary>
        public Order BuildConfirmed()
        {
            var order = Build();
            order.Confirm(_createdBy);
            return order;
        }
    }

    /// <summary>
    /// Builder for creating Product domain entities in tests.
    /// Uses professional domain entity constructor instead of property manipulation.
    /// </summary>
    public class ProductBuilder
    {
        private string _name;
        private string _description;
        private decimal _price;
        private int _stockQuantity;
        private string _createdBy;
        private int _minimumStockLevel;

        public ProductBuilder()
        {
            _name = "Test Product";
            _description = "A test product";
            _price = 99.99m;
            _stockQuantity = 50;
            _createdBy = "test-user";
            _minimumStockLevel = 10;
        }

        public ProductBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public ProductBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        public ProductBuilder WithPrice(decimal price)
        {
            _price = price;
            return this;
        }

        public ProductBuilder WithStock(int quantity)
        {
            _stockQuantity = quantity;
            return this;
        }

        public ProductBuilder WithCreatedBy(string createdBy)
        {
            _createdBy = createdBy;
            return this;
        }

        public ProductBuilder WithMinimumStockLevel(int minimumLevel)
        {
            _minimumStockLevel = minimumLevel;
            return this;
        }

        /// <summary>
        /// Creates a product with invalid data for negative testing
        /// Note: This will throw exceptions when Build() is called, as expected for negative tests
        /// </summary>
        public ProductBuilder WithInvalidData()
        {
            _name = ""; // Invalid empty name
            _price = -1m; // Invalid negative price  
            _stockQuantity = -1; // Invalid negative stock
            return this;
        }

        /// <summary>
        /// Builds the Product using professional domain entity constructor
        /// </summary>
        public Product Build() => new Product(
            _name, 
            _description, 
            _price, 
            _stockQuantity, 
            _createdBy,
            _minimumStockLevel);
    }
}