using BuildingBlocks.Events.Infrastructure;
using BuildingBlocks.Events.Domain;

namespace SalesAPI.Services
{
    /// <summary>
    /// Dummy event publisher for testing purposes when MassTransit is not configured.
    /// This implementation logs events but doesn't actually publish them.
    /// </summary>
    public class DummyEventPublisher : IEventPublisher
    {
        private readonly ILogger<DummyEventPublisher> _logger;

        public DummyEventPublisher(ILogger<DummyEventPublisher> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs the event but doesn't publish it (dummy implementation).
        /// </summary>
        /// <typeparam name="TEvent">Type of the domain event.</typeparam>
        /// <param name="domainEvent">The domain event to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completed task.</returns>
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            _logger.LogInformation("Dummy event publisher: Would publish event of type {EventType}. Event: {@Event}", 
                typeof(TEvent).Name, domainEvent);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs multiple events but doesn't publish them (dummy implementation).
        /// </summary>
        /// <typeparam name="TEvent">Type of the domain events.</typeparam>
        /// <param name="domainEvents">Collection of events to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completed task.</returns>
        public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            foreach (var domainEvent in domainEvents)
            {
                _logger.LogInformation("Dummy event publisher: Would publish event of type {EventType}. Event: {@Event}", 
                    typeof(TEvent).Name, domainEvent);
            }
            
            return Task.CompletedTask;
        }
    }
}