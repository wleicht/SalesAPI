using BuildingBlocks.Events.Domain;
using BuildingBlocks.Events.Infrastructure;
using Rebus.Bus;

namespace InventoryApi.Services
{
    /// <summary>
    /// Event publisher implementation using Rebus for RabbitMQ integration.
    /// Publishes domain events to the message bus for consumption by other services.
    /// </summary>
    public class EventPublisher : IEventPublisher
    {
        private readonly IBus _bus;
        private readonly ILogger<EventPublisher> _logger;

        /// <summary>
        /// Initializes a new instance of the EventPublisher.
        /// </summary>
        /// <param name="bus">Rebus bus instance.</param>
        /// <param name="logger">Logger for the EventPublisher.</param>
        public EventPublisher(IBus bus, ILogger<EventPublisher> logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publishes a domain event to the message bus using Rebus.
        /// </summary>
        /// <typeparam name="TEvent">Type of the domain event.</typeparam>
        /// <param name="domainEvent">The domain event to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the publish operation.</returns>
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

                await _bus.Publish(domainEvent);

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
        /// Publishes multiple domain events to the message bus.
        /// </summary>
        /// <typeparam name="TEvent">Type of the domain events.</typeparam>
        /// <param name="domainEvents">Collection of events to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the publish operation.</returns>
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
                    await _bus.Publish(domainEvent);
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