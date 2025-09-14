using Rebus.Config;
using Rebus.RabbitMq;
using Rebus.ServiceProvider;

namespace SalesApi.Configuration
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
                .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "sales.queue"))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                }));

            return services;
        }
    }
}