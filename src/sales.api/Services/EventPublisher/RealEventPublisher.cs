using BuildingBlocks.Events.Infrastructure;
using BuildingBlocks.Events.Domain;
using Rebus.Bus;
using Microsoft.Extensions.Logging;

namespace SalesApi.Services.EventPublisher
{
    /// <summary>
    /// Real implementation of IEventPublisher using Rebus for RabbitMQ event publishing.
    /// This replaces the MockEventPublisher to enable actual event-driven processing
    /// between Sales and Inventory services.
    /// </summary>
    /// <remarks>
    /// This implementation:
    /// - Publishes events to RabbitMQ using Rebus
    /// - Maintains observability with correlation tracking
    /// - Supports both single and batch event publishing
    /// - Handles failures with proper error logging
    /// 
    /// For Event-Driven Architecture, this enables:
    /// - Real OrderConfirmedEvent publishing to Inventory
    /// - Real OrderCancelledEvent publishing for compensation
    /// - Async stock deduction via event processing
    /// - Complete event-driven workflow testing
    /// </remarks>
    public class RealEventPublisher : IEventPublisher
    {
        private readonly IBus _bus;
        private readonly ILogger<RealEventPublisher> _logger;

        public RealEventPublisher(IBus bus, ILogger<RealEventPublisher> logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publishes a domain event to RabbitMQ via Rebus.
        /// </summary>
        /// <typeparam name="TEvent">Event type inheriting from DomainEvent</typeparam>
        /// <param name="domainEvent">Event data to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var eventType = typeof(TEvent).Name;
            var correlationId = domainEvent.CorrelationId ?? "unknown";
            
            try
            {
                _logger.LogInformation(
                    "?? Publishing event: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId}",
                    eventType,
                    domainEvent.EventId,
                    correlationId);

                // Publish to RabbitMQ via Rebus
                await _bus.Publish(domainEvent);

                _logger.LogInformation(
                    "? Event published successfully: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId}",
                    eventType,
                    domainEvent.EventId,
                    correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "? Failed to publish event: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId}",
                    eventType,
                    domainEvent.EventId,
                    correlationId);
                
                // Re-throw to allow calling code to handle the error
                throw;
            }
        }

        /// <summary>
        /// Publishes multiple domain events to RabbitMQ via Rebus.
        /// </summary>
        /// <typeparam name="TEvent">Event type inheriting from DomainEvent</typeparam>
        /// <param name="domainEvents">Collection of events to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var events = domainEvents.ToList();
            var eventType = typeof(TEvent).Name;
            
            if (!events.Any())
            {
                _logger.LogInformation("?? No events to publish for type: {EventType}", eventType);
                return;
            }

            _logger.LogInformation(
                "?? Publishing batch events: {EventType} | Count: {EventCount}",
                eventType,
                events.Count);

            var publishTasks = new List<Task>();
            
            foreach (var domainEvent in events)
            {
                var correlationId = domainEvent.CorrelationId ?? "unknown";
                
                _logger.LogDebug(
                    "?? Publishing batch event: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId}",
                    eventType,
                    domainEvent.EventId,
                    correlationId);

                // Add publish task to batch
                publishTasks.Add(_bus.Publish(domainEvent));
            }

            try
            {
                // Execute all publishes concurrently
                await Task.WhenAll(publishTasks);
                
                _logger.LogInformation(
                    "? Batch events published successfully: {EventType} | Count: {EventCount}",
                    eventType,
                    events.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "? Failed to publish batch events: {EventType} | Count: {EventCount}",
                    eventType,
                    events.Count);
                
                // Re-throw to allow calling code to handle the error
                throw;
            }
        }
    }
}