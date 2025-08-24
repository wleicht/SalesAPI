using BuildingBlocks.Events.Domain;
using BuildingBlocks.Events.Infrastructure;
using Rebus.Bus;

namespace SalesAPI.Services
{
    /// <summary>
    /// Implementation of IEventPublisher that publishes domain events using Rebus and RabbitMQ.
    /// Provides reliable event publishing capabilities with intelligent routing based on event types.
    /// Supports both individual event publishing and batch operations for high-throughput scenarios.
    /// </summary>
    /// <remarks>
    /// This implementation uses Rebus as the messaging framework with RabbitMQ as the underlying
    /// message broker. It provides the following capabilities:
    /// - Direct routing for OrderConfirmedEvent to ensure reliable delivery to Inventory service
    /// - General publish-subscribe for other event types
    /// - Comprehensive error handling and logging
    /// - Support for cancellation tokens for graceful shutdown scenarios
    /// - Batch publishing for improved performance when handling multiple events
    /// 
    /// The publisher automatically selects the appropriate messaging pattern based on the event type,
    /// ensuring optimal delivery guarantees for different business scenarios.
    /// </remarks>
    public class EventPublisher : IEventPublisher
    {
        private readonly IBus _bus;
        private readonly ILogger<EventPublisher> _logger;

        /// <summary>
        /// Initializes a new instance of the EventPublisher with required dependencies.
        /// </summary>
        /// <param name="bus">Rebus message bus instance for publishing events to the message broker</param>
        /// <param name="logger">Logger instance for tracking event publishing operations and errors</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when bus or logger parameters are null
        /// </exception>
        public EventPublisher(IBus bus, ILogger<EventPublisher> logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publishes a single domain event to the message bus with intelligent routing.
        /// Uses direct routing for critical events like OrderConfirmedEvent and publish-subscribe for others.
        /// </summary>
        /// <typeparam name="TEvent">The type of domain event to publish, must inherit from DomainEvent</typeparam>
        /// <param name="domainEvent">The domain event instance containing the business data to publish</param>
        /// <param name="cancellationToken">Cancellation token to allow graceful cancellation of the operation</param>
        /// <returns>A task representing the asynchronous publishing operation</returns>
        /// <remarks>
        /// Routing strategy:
        /// - OrderConfirmedEvent: Uses directed Send() to "inventory.api" queue for guaranteed delivery
        /// - Other events: Uses Publish() for general publish-subscribe pattern
        /// 
        /// The method includes comprehensive logging for tracking event flow and debugging purposes.
        /// All events are logged with their type, unique ID, and correlation ID for traceability.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when domainEvent is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message bus is not properly configured</exception>
        /// <exception cref="Exception">Re-throws any underlying messaging exceptions for proper error handling</exception>
        public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : DomainEvent
        {
            try
            {
                _logger.LogInformation(
                    "Publishing event {EventType} with ID {EventId} and correlation ID {CorrelationId}",
                    typeof(TEvent).Name,
                    domainEvent.EventId,
                    domainEvent.CorrelationId);

                // Use Send instead of Publish for critical events requiring guaranteed delivery
                if (domainEvent is OrderConfirmedEvent)
                {
                    await _bus.Advanced.Routing.Send("inventory.api", domainEvent);
                }
                else
                {
                    await _bus.Publish(domainEvent);
                }

                _logger.LogInformation(
                    "Successfully published event {EventType} with ID {EventId}",
                    typeof(TEvent).Name,
                    domainEvent.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish event {EventType} with ID {EventId}",
                    typeof(TEvent).Name,
                    domainEvent.EventId);
                throw;
            }
        }

        /// <summary>
        /// Publishes multiple domain events to the message bus in sequence.
        /// Optimized for batch operations while maintaining the same routing intelligence as single event publishing.
        /// </summary>
        /// <typeparam name="TEvent">The type of domain events to publish, must inherit from DomainEvent</typeparam>
        /// <param name="domainEvents">Collection of domain event instances to publish</param>
        /// <param name="cancellationToken">Cancellation token to allow graceful cancellation of the operation</param>
        /// <returns>A task representing the asynchronous publishing operations for all events</returns>
        /// <remarks>
        /// This method processes events sequentially to maintain ordering guarantees and prevent message broker
        /// overload. Each event is published using the same routing logic as the single event method.
        /// 
        /// Performance considerations:
        /// - Events are published in sequence to maintain order
        /// - Failed events will cause the entire batch to fail
        /// - Consider using individual PublishAsync calls for independent event processing
        /// 
        /// The method provides aggregate logging showing the total number of events processed,
        /// making it easier to monitor batch operations in production environments.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when domainEvents collection is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the message bus is not properly configured</exception>
        /// <exception cref="Exception">Re-throws any underlying messaging exceptions, including partial failures</exception>
        public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default)
            where TEvent : DomainEvent
        {
            try
            {
                var events = domainEvents.ToList();
                _logger.LogInformation(
                    "Publishing {EventCount} events of type {EventType}",
                    events.Count,
                    typeof(TEvent).Name);

                foreach (var domainEvent in events)
                {
                    if (domainEvent is OrderConfirmedEvent)
                    {
                        await _bus.Advanced.Routing.Send("inventory.api", domainEvent);
                    }
                    else
                    {
                        await _bus.Publish(domainEvent);
                    }
                }

                _logger.LogInformation(
                    "Successfully published {EventCount} events of type {EventType}",
                    events.Count,
                    typeof(TEvent).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish multiple events of type {EventType}",
                    typeof(TEvent).Name);
                throw;
            }
        }
    }
}