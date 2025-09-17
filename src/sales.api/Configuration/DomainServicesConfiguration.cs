using SalesApi.Domain.Repositories;
using SalesApi.Domain.Services;
using SalesApi.Infrastructure.Repositories;
using SalesApi.Infrastructure.Services;
using SalesApi.Services;
using SalesApi.Application.Validators;
using FluentValidation;
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

            // Register validators
            services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

            // Register enhanced external service clients
            services.AddHttpClient<IInventoryClient, EnhancedInventoryClient>(client =>
            {
                // Use localhost for development - this should be configurable in production
                client.BaseAddress = new Uri("http://localhost:5000/");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "SalesAPI-OrderService/1.0");
            });

            // Register MediatR for CQRS pattern
            services.AddMediatR(cfg => 
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            });

            // Note: IEventPublisher is registered by MessagingServices configuration

            return services;
        }
    }
}