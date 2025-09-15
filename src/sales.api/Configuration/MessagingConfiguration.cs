using BuildingBlocks.Events.Infrastructure;
using BuildingBlocks.Events.Domain;
using SalesApi.Services.EventPublisher;
using Rebus.Config;
using Rebus.RabbitMq;
using Rebus.ServiceProvider;
using Microsoft.Extensions.Logging;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Professional messaging configuration supporting multiple environments
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
                    .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "sales.queue"))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(messagingConfig.Workers);
                        o.SetMaxParallelism(messagingConfig.Workers);
                    }));

                services.AddScoped<IEventPublisher, RealEventPublisher>();
            }
            else
            {
                // For environments where messaging is disabled (like CI/CD)
                services.AddScoped<IEventPublisher, NullEventPublisher>();
            }

            return services;
        }
    }

    /// <summary>
    /// Null Object pattern implementation for disabled messaging scenarios.
    /// Used only when messaging is explicitly disabled via configuration.
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
    /// Configuration options for messaging infrastructure.
    /// </summary>
    public class MessagingOptions
    {
        public bool Enabled { get; set; } = true;
        public string ConnectionString { get; set; } = string.Empty;
        public int Workers { get; set; } = 1;
    }
}