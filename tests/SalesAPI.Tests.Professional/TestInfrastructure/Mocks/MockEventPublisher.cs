using BuildingBlocks.Events.Infrastructure;
using BuildingBlocks.Events.Domain;
using Microsoft.Extensions.Logging;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Mocks
{
    /// <summary>
    /// Mock implementation of IEventPublisher for testing without RabbitMQ infrastructure.
    /// This implementation is used in test scenarios where event processing testing is not required.
    /// </summary>
    public class MockEventPublisher : IEventPublisher
    {
        private readonly ILogger<MockEventPublisher> _logger;
        private readonly List<DomainEvent> _publishedEvents = new();

        public MockEventPublisher(ILogger<MockEventPublisher> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets all events that were published during testing.
        /// </summary>
        public IReadOnlyList<DomainEvent> PublishedEvents => _publishedEvents.AsReadOnly();

        /// <summary>
        /// Mock implementation that logs event publishing for observability.
        /// </summary>
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var eventType = typeof(TEvent).Name;
            var correlationId = domainEvent.CorrelationId ?? "unknown";
            
            _publishedEvents.Add(domainEvent);
            
            _logger.LogInformation(
                "?? Mock event published: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId} | Data: {EventData}",
                eventType,
                domainEvent.EventId,
                correlationId,
                System.Text.Json.JsonSerializer.Serialize(domainEvent));
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Mock implementation for batch event publishing.
        /// </summary>
        public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var events = domainEvents.ToList();
            var eventType = typeof(TEvent).Name;
            
            _publishedEvents.AddRange(events);
            
            _logger.LogInformation(
                "?? Mock batch events published: {EventType} | Count: {EventCount}",
                eventType,
                events.Count);

            foreach (var domainEvent in events)
            {
                var correlationId = domainEvent.CorrelationId ?? "unknown";
                
                _logger.LogInformation(
                    "?? Mock batch event: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId}",
                    eventType,
                    domainEvent.EventId,
                    correlationId);
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clears all published events. Useful for test isolation.
        /// </summary>
        public void ClearPublishedEvents()
        {
            _publishedEvents.Clear();
        }

        /// <summary>
        /// Gets events of a specific type that were published.
        /// </summary>
        public IEnumerable<TEvent> GetPublishedEvents<TEvent>() where TEvent : DomainEvent
        {
            return _publishedEvents.OfType<TEvent>();
        }
    }
}