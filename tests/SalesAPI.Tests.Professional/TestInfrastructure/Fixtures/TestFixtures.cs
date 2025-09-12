using Microsoft.Extensions.Logging;
using SalesAPI.Tests.Professional.TestInfrastructure.Database;
using SalesAPI.Tests.Professional.TestInfrastructure.Messaging;
using SalesAPI.Tests.Professional.TestInfrastructure.WebApi;
using Xunit;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Fixtures
{
    /// <summary>
    /// Refactored database fixture using base class to eliminate duplication
    /// </summary>
    public class DatabaseFixture : BaseTestFixture
    {
        private TestDatabaseFactory? _databaseFactory;

        public TestDatabaseFactory DatabaseFactory => 
            ThrowIfNotInitialized(_databaseFactory, nameof(DatabaseFactory));

        protected override async Task InitializeInternalAsync()
        {
            _databaseFactory = new TestDatabaseFactory(LoggerFactory.CreateLogger<TestDatabaseFactory>());
            await Task.CompletedTask;
        }

        protected override async Task DisposeInternalAsync()
        {
            _databaseFactory?.Dispose();
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Refactored messaging fixture using base class to eliminate duplication
    /// </summary>
    public class MessagingFixture : BaseTestFixture
    {
        private TestMessagingFactory? _messagingFactory;

        public TestMessagingFactory MessagingFactory => 
            ThrowIfNotInitialized(_messagingFactory, nameof(MessagingFactory));

        protected override async Task InitializeInternalAsync()
        {
            _messagingFactory = new TestMessagingFactory(TestName, 
                LoggerFactory.CreateLogger<TestMessagingFactory>());
            await Task.CompletedTask;
        }

        protected override async Task DisposeInternalAsync()
        {
            _messagingFactory?.Dispose();
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Comprehensive fixture that combines database, messaging, and web API testing.
    /// Refactored to use base class and eliminate duplication.
    /// </summary>
    public class ScenarioFixture : BaseTestFixture
    {
        // Infrastructure components
        private TestDatabaseFactory? _databaseFactory;
        private TestMessagingFactory? _messagingFactory;
        private SalesApiTestServerFactory? _salesApiFactory;
        private InventoryApiTestServerFactory? _inventoryApiFactory;
        private GatewayApiTestServerFactory? _gatewayApiFactory;

        // Public properties for accessing infrastructure
        public TestDatabaseFactory DatabaseFactory => 
            ThrowIfNotInitialized(_databaseFactory, nameof(DatabaseFactory));

        public TestMessagingFactory MessagingFactory => 
            ThrowIfNotInitialized(_messagingFactory, nameof(MessagingFactory));

        public SalesApiTestServerFactory SalesApiFactory => 
            ThrowIfNotInitialized(_salesApiFactory, nameof(SalesApiFactory));

        public InventoryApiTestServerFactory InventoryApiFactory => 
            ThrowIfNotInitialized(_inventoryApiFactory, nameof(InventoryApiFactory));

        public GatewayApiTestServerFactory GatewayApiFactory => 
            ThrowIfNotInitialized(_gatewayApiFactory, nameof(GatewayApiFactory));

        protected override async Task InitializeInternalAsync()
        {
            // Initialize infrastructure in order
            _databaseFactory = new TestDatabaseFactory(LoggerFactory.CreateLogger<TestDatabaseFactory>());
            _messagingFactory = new TestMessagingFactory(TestName, LoggerFactory.CreateLogger<TestMessagingFactory>());
            
            _salesApiFactory = new SalesApiTestServerFactory(TestName, 
                LoggerFactory.CreateLogger<SalesApiTestServerFactory>());
                
            _inventoryApiFactory = new InventoryApiTestServerFactory(TestName, 
                LoggerFactory.CreateLogger<InventoryApiTestServerFactory>());
                
            _gatewayApiFactory = new GatewayApiTestServerFactory(TestName, 
                LoggerFactory.CreateLogger<GatewayApiTestServerFactory>());

            await Task.CompletedTask;
        }

        protected override async Task DisposeInternalAsync()
        {
            // Dispose in reverse order
            _gatewayApiFactory?.Dispose();
            _inventoryApiFactory?.Dispose();
            _salesApiFactory?.Dispose();
            _messagingFactory?.Dispose();
            _databaseFactory?.Dispose();

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Collection fixture for sharing database infrastructure across multiple test classes.
    /// </summary>
    [CollectionDefinition("Database Collection")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    /// <summary>
    /// Collection fixture for sharing messaging infrastructure across multiple test classes.
    /// </summary>
    [CollectionDefinition("Messaging Collection")]
    public class MessagingCollection : ICollectionFixture<MessagingFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    /// <summary>
    /// Collection fixture for sharing complete scenario infrastructure across multiple test classes.
    /// </summary>
    [CollectionDefinition("Scenario Collection")]
    public class ScenarioCollection : ICollectionFixture<ScenarioFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}