using SalesApi.Services;
using SalesAPI.Services;
using SalesApi.Middleware;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Extension methods for configuring HTTP client services.
    /// </summary>
    public static class HttpClientConfiguration
    {
        public static IServiceCollection AddHttpClientServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var inventoryApiUrl = configuration[ConfigurationKeys.InventoryApiUrl]
                ?? ConfigurationKeys.Defaults.InventoryApiUrl;

            // Configure HTTP client for Inventory API with correlation propagation
            services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
            {
                client.BaseAddress = new Uri(inventoryApiUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<CorrelationHttpMessageHandler>();

            // Configure HTTP client for Stock Reservations with correlation propagation  
            services.AddHttpClient<StockReservationClient>(client =>
            {
                client.BaseAddress = new Uri(inventoryApiUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<CorrelationHttpMessageHandler>();

            // Register correlation handler
            services.AddTransient<CorrelationHttpMessageHandler>();

            return services;
        }
    }
}