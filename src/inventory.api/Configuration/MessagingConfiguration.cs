using BuildingBlocks.Events.Infrastructure;
using BuildingBlocks.Events.Domain;
using Rebus.Config;
using Rebus.RabbitMq;
using Rebus.ServiceProvider;
using InventoryApi.Services;
using Microsoft.Extensions.Logging;

namespace InventoryApi.Configuration
{
    /// <summary>
    /// Professional messaging configuration for Inventory API supporting multiple environments
    /// without fake implementations in production code.
    /// </summary>
    public static class MessagingConfiguration
    {
        public static IServiceCollection AddMessagingServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var messagingConfig = configuration.GetSection("Messaging").Get<MessagingOptions>() ?? new MessagingOptions();
            
            if (messagingConfig.Enabled)
            {
                var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ")
                    ?? ConfigurationKeys.Defaults.RabbitMqConnection;

                services.AddRebus((configure, serviceProvider) => configure
                    .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "inventory.queue"))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(messagingConfig.Workers);
                        o.SetMaxParallelism(messagingConfig.Workers);
                    }));

                // Register all handlers from this assembly
                services.AutoRegisterHandlersFromAssemblyOf<OrderConfirmedEventHandler>();
                
                // Register real event publisher if needed
                services.AddScoped<IEventPublisher, InventoryEventPublisher>();
            }
            else
            {
                // For environments where messaging is disabled (like CI/CD)
                services.AddScoped<IEventPublisher, NullEventPublisher>();
            }

            return services;
        }

        public static async Task<WebApplication> SubscribeToEventsAsync(this WebApplication app)
        {
            var messagingConfig = app.Configuration.GetSection("Messaging").Get<MessagingOptions>() ?? new MessagingOptions();
            
            if (messagingConfig.Enabled)
            {
                using var scope = app.Services.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<Rebus.Bus.IBus>();
                await bus.Subscribe<OrderConfirmedEvent>();
            }

            return app;
        }
    }

    /// <summary>
    /// Null Object pattern implementation for disabled messaging scenarios.
    /// </summary>
    internal class NullEventPublisher : IEventPublisher
    {
        private readonly ILogger<NullEventPublisher> _logger;

        public NullEventPublisher(ILogger<NullEventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            _logger.LogWarning("?? Messaging disabled - Event not published: {EventType}", typeof(TEvent).Name);
            return Task.CompletedTask;
        }

        public Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            _logger.LogWarning("?? Messaging disabled - Events not published: {EventType}, Count: {Count}", 
                typeof(TEvent).Name, domainEvents.Count());
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Real event publisher for Inventory API.
    /// </summary>
    internal class InventoryEventPublisher : IEventPublisher
    {
        private readonly Rebus.Bus.IBus _bus;
        private readonly ILogger<InventoryEventPublisher> _logger;

        public InventoryEventPublisher(Rebus.Bus.IBus bus, ILogger<InventoryEventPublisher> logger)
        {
            _bus = bus;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            try
            {
                _logger.LogInformation("?? Publishing event: {EventType} | EventId: {EventId}", 
                    typeof(TEvent).Name, domainEvent.EventId);
                    
                await _bus.Publish(domainEvent);
                
                _logger.LogInformation("? Event published successfully: {EventType} | EventId: {EventId}", 
                    typeof(TEvent).Name, domainEvent.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to publish event: {EventType} | EventId: {EventId}", 
                    typeof(TEvent).Name, domainEvent.EventId);
                throw;
            }
        }

        public async Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default) 
            where TEvent : DomainEvent
        {
            var events = domainEvents.ToList();
            
            try
            {
                _logger.LogInformation("?? Publishing batch events: {EventType} | Count: {Count}", 
                    typeof(TEvent).Name, events.Count);
                    
                foreach (var domainEvent in events)
                {
                    await _bus.Publish(domainEvent);
                }
                
                _logger.LogInformation("? Batch events published successfully: {EventType} | Count: {Count}", 
                    typeof(TEvent).Name, events.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Failed to publish batch events: {EventType} | Count: {Count}", 
                    typeof(TEvent).Name, events.Count);
                throw;
            }
        }
    }

    /// <summary>
    /// Configuration options for messaging infrastructure.
    /// </summary>
    public class MessagingOptions
    {
        public bool Enabled { get; set; } = true;
        public string ConnectionString { get; set; } = string.Empty;
        public int Workers { get; set; } = 1;
    }
}