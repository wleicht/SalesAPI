using Microsoft.Extensions.Logging;
using BuildingBlocks.Events.Domain;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Messaging
{
    /// <summary>
    /// Factory for creating messaging infrastructure for testing.
    /// Provides both real and fake message buses for different testing scenarios.
    /// </summary>
    public class TestMessagingFactory : IDisposable
    {
        private readonly List<IDisposable> _createdResources = new();
        private readonly ILogger<TestMessagingFactory> _logger;
        private readonly string _testIdentifier;
        private bool _disposed = false;
        
        public TestMessagingFactory(string testName, ILogger<TestMessagingFactory>? logger = null)
        {
            _logger = logger ?? new LoggerFactory().CreateLogger<TestMessagingFactory>();
            _testIdentifier = GenerateTestIdentifier(testName);
            
            _logger.LogInformation("Initializing messaging factory for test: {TestName} with identifier: {TestIdentifier}", 
                testName, _testIdentifier);
        }

        /// <summary>
        /// Creates a test-only fake bus for scenarios where real messaging is not needed.
        /// </summary>
        /// <returns>FakeBus instance for testing</returns>
        public FakeBus CreateFakeBus()
        {
            _logger.LogInformation("Creating fake bus for testing without real messaging infrastructure");
            var fakeBus = new FakeBus();
            _createdResources.Add(fakeBus);
            return fakeBus;
        }

        /// <summary>
        /// Creates a message collector that captures published events for verification.
        /// </summary>
        /// <returns>MessageCollector instance</returns>
        public MessageCollector CreateMessageCollector()
        {
            return new MessageCollector(_testIdentifier, _logger);
        }

        private string GenerateTestIdentifier(string testName)
        {
            var cleanTestName = testName.Replace(" ", "_")
                                       .Replace("-", "_")
                                       .Replace(".", "_");
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            
            return $"{cleanTestName}_{timestamp}_{uniqueId}";
        }

        public void Dispose()
        {
            if (_disposed) return;

            _logger.LogInformation("Disposing messaging factory. Cleaning up {ResourceCount} resources", 
                _createdResources.Count);

            // Dispose all created resources
            foreach (var resource in _createdResources)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing resource");
                }
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Collects messages published during tests for verification.
    /// </summary>
    public class MessageCollector
    {
        private readonly List<object> _collectedMessages = new();
        private readonly string _testIdentifier;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        public MessageCollector(string testIdentifier, ILogger logger)
        {
            _testIdentifier = testIdentifier;
            _logger = logger;
        }

        /// <summary>
        /// Records a message that was published.
        /// </summary>
        /// <param name="message">The message to record</param>
        public void RecordMessage(object message)
        {
            lock (_lock)
            {
                _collectedMessages.Add(message);
                _logger.LogDebug("Recorded message of type {MessageType} for test {TestIdentifier}", 
                    message.GetType().Name, _testIdentifier);
            }
        }

        /// <summary>
        /// Gets all messages of a specific type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>List of messages of the specified type</returns>
        public List<TMessage> GetMessages<TMessage>()
        {
            lock (_lock)
            {
                return _collectedMessages.OfType<TMessage>().ToList();
            }
        }

        /// <summary>
        /// Gets a specific event by ID.
        /// </summary>
        /// <typeparam name="TEvent">The event type</typeparam>
        /// <param name="eventId">The event ID to find</param>
        /// <returns>The event if found, null otherwise</returns>
        public TEvent? GetEvent<TEvent>(Guid eventId) where TEvent : DomainEvent
        {
            lock (_lock)
            {
                return _collectedMessages.OfType<TEvent>().FirstOrDefault(e => e.EventId == eventId);
            }
        }

        /// <summary>
        /// Gets the count of messages of a specific type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <returns>The count of messages</returns>
        public int GetMessageCount<TMessage>()
        {
            lock (_lock)
            {
                return _collectedMessages.OfType<TMessage>().Count();
            }
        }

        /// <summary>
        /// Clears all collected messages.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                var count = _collectedMessages.Count;
                _collectedMessages.Clear();
                _logger.LogDebug("Cleared {MessageCount} collected messages for test {TestIdentifier}", 
                    count, _testIdentifier);
            }
        }
    }

    /// <summary>
    /// Simple fake bus implementation for testing without real messaging infrastructure.
    /// Implements a minimal interface for testing purposes.
    /// </summary>
    public class FakeBus : IDisposable
    {
        private readonly List<object> _publishedMessages = new();
        private readonly object _lock = new object();

        /// <summary>
        /// Simulates publishing a message.
        /// </summary>
        /// <param name="eventMessage">The message to publish</param>
        /// <returns>Completed task</returns>
        public Task Publish(object eventMessage)
        {
            lock (_lock)
            {
                _publishedMessages.Add(eventMessage);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets all published messages of a specific type.
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <returns>List of published messages</returns>
        public List<T> GetPublishedMessages<T>()
        {
            lock (_lock)
            {
                return _publishedMessages.OfType<T>().ToList();
            }
        }

        /// <summary>
        /// Clears all published messages.
        /// </summary>
        public void ClearMessages()
        {
            lock (_lock)
            {
                _publishedMessages.Clear();
            }
        }

        /// <summary>
        /// Gets all published messages (for debugging).
        /// </summary>
        /// <returns>List of all messages</returns>
        public List<object> GetAllPublishedMessages()
        {
            lock (_lock)
            {
                return new List<object>(_publishedMessages);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _publishedMessages.Clear();
            }
        }
    }
}