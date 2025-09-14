using SalesApi.Domain.Repositories;
using SalesApi.Domain.Services;
using SalesApi.Infrastructure.Repositories;
using SalesApi.Infrastructure.Services;
using BuildingBlocks.Events.Infrastructure;
using SalesAPI.Services;
using MediatR;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Extension methods for configuring business domain services.
    /// </summary>
    public static class DomainServicesConfiguration
    {
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            // Register domain services and repositories
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderDomainService, OrderDomainService>();

            // Register MediatR for CQRS pattern
            services.AddMediatR(cfg => 
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            });

            // Register event publisher for event-driven architecture
            services.AddScoped<IEventPublisher, EventPublisher>();

            return services;
        }
    }
}