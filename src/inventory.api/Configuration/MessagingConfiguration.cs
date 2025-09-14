using Rebus.Config;
using Rebus.RabbitMq;
using Rebus.ServiceProvider;
using BuildingBlocks.Events.Domain;
using InventoryApi.Services;

namespace InventoryApi.Configuration
{
    /// <summary>
    /// Extension methods for configuring Rebus messaging services.
    /// </summary>
    public static class MessagingConfiguration
    {
        public static IServiceCollection AddMessagingServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ")
                ?? ConfigurationKeys.Defaults.RabbitMqConnection;

            services.AddRebus((configure, serviceProvider) => configure
                .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "inventory.queue"))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                }));

            // Register all handlers from this assembly
            services.AutoRegisterHandlersFromAssemblyOf<OrderConfirmedEventHandler>();

            return services;
        }

        public static async Task<WebApplication> SubscribeToEventsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<Rebus.Bus.IBus>();
            await bus.Subscribe<OrderConfirmedEvent>();

            return app;
        }
    }
}