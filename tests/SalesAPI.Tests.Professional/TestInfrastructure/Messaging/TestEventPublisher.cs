using BuildingBlocks.Events.Infrastructure;
using BuildingBlocks.Events.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Messaging
{
    /// <summary>
    /// Test implementation of IEventPublisher for testing without RabbitMQ infrastructure.
    /// This provides a clean test double that captures events for verification.
    /// </summary>
    /// <remarks>
    /// This replaces the production MockEventPublisher, providing:
    /// - Event capture for test assertions
    /// - Logging for debugging and observability
    /// - No external dependencies (RabbitMQ, Rebus)
    /// - Synchronous execution for predictable testing
    /// </remarks>
    public class TestEventPublisher : IEventPublisher
    {
        private readonly ILogger<TestEventPublisher> _logger;
        private readonly List<PublishedEvent> _publishedEvents = new();

        public TestEventPublisher(ILogger<TestEventPublisher> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all events that have been published during the test.
        /// </summary>
        public IReadOnlyList<PublishedEvent> PublishedEvents => _publishedEvents.AsReadOnly();

        /// <summary>
        /// Test implementation that captures events and logs them for observability.
        /// </summary>
        /// <typeparam name="TEvent">Event type inheriting from DomainEvent</typeparam>
        /// <param name="domainEvent">Event data to capture</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Completed task for synchronous test execution</returns>
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var eventType = typeof(TEvent).Name;
            var correlationId = domainEvent.CorrelationId ?? "test-unknown";
            
            // Capture event for test assertions
            var publishedEvent = new PublishedEvent
            {
                EventType = eventType,
                EventId = domainEvent.EventId,
                CorrelationId = correlationId,
                Event = domainEvent,
                PublishedAt = DateTime.UtcNow
            };
            
            _publishedEvents.Add(publishedEvent);
            
            _logger.LogInformation(
                "?? Test event published: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId}",
                eventType,
                domainEvent.EventId,
                correlationId);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Test implementation for batch event publishing.
        /// </summary>
        /// <typeparam name="TEvent">Event type inheriting from DomainEvent</typeparam>
        /// <param name="domainEvents">Collection of events to capture</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Completed task for synchronous test execution</returns>
        public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var events = domainEvents.ToList();
            var eventType = typeof(TEvent).Name;
            
            if (!events.Any())
            {
                _logger.LogInformation("?? No test events to publish for type: {EventType}", eventType);
                return Task.CompletedTask;
            }
            
            _logger.LogInformation(
                "?? Test batch events publishing: {EventType} | Count: {EventCount}",
                eventType,
                events.Count);

            foreach (var domainEvent in events)
            {
                var correlationId = domainEvent.CorrelationId ?? "test-unknown";
                
                var publishedEvent = new PublishedEvent
                {
                    EventType = eventType,
                    EventId = domainEvent.EventId,
                    CorrelationId = correlationId,
                    Event = domainEvent,
                    PublishedAt = DateTime.UtcNow
                };
                
                _publishedEvents.Add(publishedEvent);
                
                _logger.LogInformation(
                    "?? Test batch event: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId}",
                    eventType,
                    domainEvent.EventId,
                    correlationId);
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clears all captured events. Useful for test cleanup.
        /// </summary>
        public void ClearEvents()
        {
            _publishedEvents.Clear();
            _logger.LogInformation("?? Test events cleared");
        }

        /// <summary>
        /// Gets all events of a specific type that were published.
        /// </summary>
        /// <typeparam name="TEvent">Type of events to retrieve</typeparam>
        /// <returns>Collection of events of the specified type</returns>
        public IEnumerable<TEvent> GetEventsOfType<TEvent>() where TEvent : DomainEvent
        {
            return _publishedEvents
                .Where(e => e.Event is TEvent)
                .Select(e => (TEvent)e.Event);
        }

        /// <summary>
        /// Gets the count of events of a specific type that were published.
        /// </summary>
        /// <typeparam name="TEvent">Type of events to count</typeparam>
        /// <returns>Number of events of the specified type</returns>
        public int GetEventCountOfType<TEvent>() where TEvent : DomainEvent
        {
            return _publishedEvents.Count(e => e.Event is TEvent);
        }

        /// <summary>
        /// Verifies that an event of a specific type was published with the given predicate.
        /// </summary>
        /// <typeparam name="TEvent">Type of event to verify</typeparam>
        /// <param name="predicate">Condition the event must satisfy</param>
        /// <returns>True if an event matching the predicate was found</returns>
        public bool WasEventPublished<TEvent>(Func<TEvent, bool> predicate) where TEvent : DomainEvent
        {
            return GetEventsOfType<TEvent>().Any(predicate);
        }
    }

    /// <summary>
    /// Represents a captured published event for test verification.
    /// </summary>
    public class PublishedEvent
    {
        public string EventType { get; set; } = string.Empty;
        public Guid EventId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public DomainEvent Event { get; set; } = null!;
        public DateTime PublishedAt { get; set; }
    }
}