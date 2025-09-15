using SalesApi.Domain.Repositories;
using SalesApi.Domain.Services;
using SalesApi.Infrastructure.Repositories;
using SalesApi.Infrastructure.Services;
using MediatR;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Extension methods for configuring business domain services.
    /// Registers domain services without any fake implementations.
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

            // Event publisher is registered by MessagingServices
            // No fake implementations in production domain services

            return services;
        }
    }
}