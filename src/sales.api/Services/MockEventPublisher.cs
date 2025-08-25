using BuildingBlocks.Events.Infrastructure;
using BuildingBlocks.Events.Domain;

namespace SalesApi.Services
{
    /// <summary>
    /// Mock implementation of IEventPublisher for basic observability testing.
    /// This allows the application to function without complex event infrastructure
    /// while still maintaining the observability patterns we're implementing.
    /// </summary>
    /// <remarks>
    /// In a complete implementation, this would be replaced with:
    /// - RabbitMQ event publisher
    /// - Azure Service Bus publisher
    /// - Kafka producer
    /// - Or other message broker integration
    /// 
    /// For Etapa 7 (Basic Observability), we focus on:
    /// - Correlation ID tracking
    /// - Structured logging
    /// - Prometheus metrics
    /// - Cross-service HTTP communication
    /// </remarks>
    public class MockEventPublisher : IEventPublisher
    {
        private readonly ILogger<MockEventPublisher> _logger;

        public MockEventPublisher(ILogger<MockEventPublisher> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Mock implementation that logs event publishing for observability.
        /// </summary>
        /// <typeparam name="TEvent">Event type inheriting from DomainEvent</typeparam>
        /// <param name="domainEvent">Event data to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Completed task</returns>
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var eventType = typeof(TEvent).Name;
            var correlationId = domainEvent.CorrelationId ?? "unknown";
            
            _logger.LogInformation(
                "???? Mock event published: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId} | Data: {EventData}",
                eventType,
                domainEvent.EventId,
                correlationId,
                System.Text.Json.JsonSerializer.Serialize(domainEvent));

            // In real implementation, this would:
            // 1. Serialize the event
            // 2. Send to message broker
            // 3. Handle failures and retries
            // 4. Return task representing the async operation
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Mock implementation for batch event publishing.
        /// </summary>
        /// <typeparam name="TEvent">Event type inheriting from DomainEvent</typeparam>
        /// <param name="domainEvents">Collection of events to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Completed task</returns>
        public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var events = domainEvents.ToList();
            var eventType = typeof(TEvent).Name;
            
            _logger.LogInformation(
                "???? Mock batch events published: {EventType} | Count: {EventCount}",
                eventType,
                events.Count);

            foreach (var domainEvent in events)
            {
                var correlationId = domainEvent.CorrelationId ?? "unknown";
                
                _logger.LogInformation(
                    "???? Mock batch event: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId}",
                    eventType,
                    domainEvent.EventId,
                    correlationId);
            }
            
            return Task.CompletedTask;
        }
    }
}