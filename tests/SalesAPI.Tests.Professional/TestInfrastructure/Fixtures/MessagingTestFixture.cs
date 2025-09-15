using BuildingBlocks.Events.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SalesAPI.Tests.Professional.TestInfrastructure.Mocks;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Fixtures
{
    /// <summary>
    /// Test fixture providing messaging infrastructure for tests.
    /// Replaces production messaging with controlled test doubles.
    /// </summary>
    public class MessagingTestFixture : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        public IServiceProvider ServiceProvider => _serviceProvider;
        public MockEventPublisher MockEventPublisher { get; private set; }

        public MessagingTestFixture()
        {
            var services = new ServiceCollection();
            
            services.AddLogging(builder => builder.AddConsole());
            
            var tempServiceProvider = services.BuildServiceProvider();
            MockEventPublisher = new MockEventPublisher(
                tempServiceProvider.GetRequiredService<ILogger<MockEventPublisher>>());
            
            services.AddSingleton<IEventPublisher>(MockEventPublisher);
            
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Clears all published events for test isolation.
        /// </summary>
        public void Reset()
        {
            MockEventPublisher?.ClearPublishedEvents();
        }

        public void Dispose()
        {
            MockEventPublisher?.ClearPublishedEvents();
            _serviceProvider?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}