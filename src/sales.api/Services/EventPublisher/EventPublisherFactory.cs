using BuildingBlocks.Events.Infrastructure;
using SalesApi.Services.EventPublisher;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SalesApi.Services.EventPublisher
{
    /// <summary>
    /// Factory for creating appropriate event publisher implementations based on environment and configuration.
    /// </summary>
    public static class EventPublisherFactory
    {
        /// <summary>
        /// Creates an event publisher based on the specified configuration.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        /// <param name="useRealImplementation">Whether to use real or mock implementation</param>
        /// <returns>IEventPublisher instance</returns>
        public static IEventPublisher CreateEventPublisher(
            IServiceProvider serviceProvider, 
            bool useRealImplementation = true)
        {
            if (useRealImplementation)
            {
                return serviceProvider.GetRequiredService<RealEventPublisher>();
            }
            
            // For development/testing scenarios
            var logger = serviceProvider.GetRequiredService<ILogger<DevMockEventPublisher>>();
            return new DevMockEventPublisher(logger);
        }

        /// <summary>
        /// Creates an event publisher based on environment name.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        /// <param name="environmentName">Environment name (Development, Production, etc.)</param>
        /// <returns>IEventPublisher instance</returns>
        public static IEventPublisher CreateEventPublisher(
            IServiceProvider serviceProvider, 
            string environmentName)
        {
            var useReal = !string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
            return CreateEventPublisher(serviceProvider, useReal);
        }
    }

    /// <summary>
    /// Development-only mock event publisher for scenarios where RabbitMQ is not available.
    /// This should NEVER be used in production.
    /// </summary>
    internal class DevMockEventPublisher : IEventPublisher
    {
        private readonly ILogger<DevMockEventPublisher> _logger;

        public DevMockEventPublisher(ILogger<DevMockEventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
            where TEvent : BuildingBlocks.Events.Domain.DomainEvent
        {
            var eventType = typeof(TEvent).Name;
            var correlationId = domainEvent.CorrelationId ?? "unknown";
            
            _logger.LogWarning(
                "?? DEV MODE: Mock event published: {EventType} | EventId: {EventId} | CorrelationId: {CorrelationId}",
                eventType,
                domainEvent.EventId,
                correlationId);
            
            return Task.CompletedTask;
        }

        public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
            where TEvent : BuildingBlocks.Events.Domain.DomainEvent
        {
            var events = domainEvents.ToList();
            var eventType = typeof(TEvent).Name;
            
            _logger.LogWarning(
                "?? DEV MODE: Mock batch events published: {EventType} | Count: {EventCount}",
                eventType,
                events.Count);
            
            return Task.CompletedTask;
        }
    }
}