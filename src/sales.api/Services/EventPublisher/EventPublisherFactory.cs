using BuildingBlocks.Events.Infrastructure;
using SalesApi.Services.EventPublisher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SalesApi.Services.EventPublisher
{
    /// <summary>
    /// Production-ready factory for event publisher implementations.
    /// Provides environment-specific configurations without fake implementations in production code.
    /// </summary>
    public static class EventPublisherFactory
    {
        /// <summary>
        /// Creates the appropriate event publisher based on configuration.
        /// Always returns production implementations - testing uses separate infrastructure.
        /// </summary>
        public static IEventPublisher CreateEventPublisher(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<RealEventPublisher>();
        }

        /// <summary>
        /// Registers event publisher services in the DI container.
        /// </summary>
        public static IServiceCollection AddEventPublisher(this IServiceCollection services)
        {
            services.AddScoped<IEventPublisher, RealEventPublisher>();
            return services;
        }
    }
}