using BuildingBlocks.Events.Domain;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Events.Infrastructure
{
    /// <summary>
    /// Backward compatibility adapter that bridges the existing IEventPublisher interface
    /// with the new IMessageBus abstraction. Enables gradual migration from the legacy
    /// event publishing interface to the more comprehensive message bus abstraction.
    /// </summary>
    /// <remarks>
    /// This adapter implements the Adapter Pattern to provide seamless integration between:
    /// 
    /// Legacy Integration:
    /// - Existing code using IEventPublisher interface
    /// - Current service implementations and dependencies
    /// - Established event publishing patterns and workflows
    /// - Testing infrastructure and mock implementations
    /// 
    /// Modern Infrastructure:
    /// - New IMessageBus abstraction with enhanced capabilities
    /// - Provider-agnostic messaging infrastructure
    /// - Comprehensive error handling and observability
    /// - Advanced features like batch processing and subscriptions
    /// 
    /// Migration Strategy:
    /// - Phase 1: Deploy adapter to maintain existing functionality
    /// - Phase 2: Gradually migrate services to use IMessageBus directly
    /// - Phase 3: Deprecate IEventPublisher after complete migration
    /// - Phase 4: Remove adapter and legacy interface
    /// 
    /// The adapter ensures zero breaking changes during the migration period
    /// while enabling new features through the enhanced IMessageBus interface.
    /// </remarks>
    public class MessageBusEventPublisherAdapter : IEventPublisher
    {
        private readonly IMessageBus _messageBus;
        private readonly ILogger<MessageBusEventPublisherAdapter> _logger;

        /// <summary>
        /// Initializes a new instance of the adapter with the underlying message bus implementation.
        /// </summary>
        /// <param name="messageBus">Message bus implementation to delegate operations to</param>
        /// <param name="logger">Logger for adapter operation monitoring and troubleshooting</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null</exception>
        public MessageBusEventPublisherAdapter(IMessageBus messageBus, ILogger<MessageBusEventPublisherAdapter> logger)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publishes a single domain event by delegating to the underlying message bus.
        /// Maintains complete compatibility with the legacy IEventPublisher interface
        /// while leveraging the enhanced capabilities of the modern message bus.
        /// </summary>
        /// <typeparam name="TEvent">Type of domain event inheriting from DomainEvent base class</typeparam>
        /// <param name="domainEvent">Domain event instance to publish</param>
        /// <param name="cancellationToken">Cancellation token for operation timeout</param>
        /// <returns>Task representing the asynchronous publishing operation</returns>
        /// <remarks>
        /// Delegation Strategy:
        /// 1. Validate that the event implements both DomainEvent and IDomainEvent
        /// 2. Log the adapter operation for migration monitoring
        /// 3. Delegate directly to IMessageBus.PublishAsync method
        /// 4. Maintain identical error handling and exception behavior
        /// 
        /// Compatibility Guarantees:
        /// - Identical method signature and behavior
        /// - Same exception types and error conditions
        /// - Preserved logging and monitoring patterns
        /// - No performance degradation from adaptation
        /// 
        /// Migration Benefits:
        /// - Enables gradual adoption of new infrastructure
        /// - Provides enhanced error handling and observability
        /// - Supports future feature additions through IMessageBus
        /// - Maintains testing compatibility during transition
        /// </remarks>
        public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            _logger.LogTrace(
                "?? Adapter delegating event publication: {EventType} | EventId: {EventId}",
                typeof(TEvent).Name, domainEvent.EventId);

            // Direct delegation to the message bus
            // The constraint ensures TEvent : DomainEvent, and DomainEvent : IDomainEvent
            await _messageBus.PublishAsync(domainEvent, cancellationToken);
        }

        /// <summary>
        /// Publishes multiple domain events by delegating to the underlying message bus batch operation.
        /// Provides enhanced batch processing capabilities while maintaining interface compatibility.
        /// </summary>
        /// <typeparam name="TEvent">Type of domain events inheriting from DomainEvent base class</typeparam>
        /// <param name="domainEvents">Collection of domain events to publish</param>
        /// <param name="cancellationToken">Cancellation token for operation timeout</param>
        /// <returns>Task representing the asynchronous batch publishing operation</returns>
        /// <remarks>
        /// Batch Processing Advantages:
        /// - Leverages optimized batch operations from IMessageBus
        /// - Improved performance for high-volume publishing scenarios
        /// - Enhanced error reporting with individual message tracking
        /// - Better resource utilization through connection reuse
        /// 
        /// Legacy Compatibility:
        /// - Maintains identical interface contract
        /// - Preserves error handling behavior
        /// - Supports existing batch publishing patterns
        /// - No changes required in calling code
        /// 
        /// Performance Improvements:
        /// - Reduced per-message overhead
        /// - Connection efficiency through batch operations
        /// - Comprehensive error aggregation and reporting
        /// - Enhanced observability for batch operations
        /// </remarks>
        public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var eventList = domainEvents?.ToList() ?? throw new ArgumentNullException(nameof(domainEvents));
            
            _logger.LogTrace(
                "?? Adapter delegating batch event publication: {EventType} | Count: {EventCount}",
                typeof(TEvent).Name, eventList.Count);

            // Direct delegation to the message bus batch operation
            await _messageBus.PublishManyAsync(eventList, cancellationToken);
        }
    }
}