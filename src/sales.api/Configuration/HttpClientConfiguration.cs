using SalesApi.Services;
using SalesAPI.Services;
using SalesApi.Middleware;
using SalesApi.Configuration.Constants;
using SalesApi.Configuration.Providers;
using Microsoft.Extensions.Logging;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Extension methods for configuring HTTP client services using centralized constants.
    /// </summary>
    public static class HttpClientConfiguration
    {
        public static IServiceCollection AddHttpClientServices(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger? logger = null)
        {
            // Get inventory API URL directly from configuration without circular dependency
            var inventoryApiUrl = GetInventoryApiUrlSafely(configuration, logger);

            // Configure HTTP client for Inventory API with correlation propagation
            services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
            {
                client.BaseAddress = new Uri(inventoryApiUrl);
                client.Timeout = TimeSpan.FromSeconds(NetworkConstants.Timeouts.HttpClientTimeout);
                client.DefaultRequestHeaders.Add("User-Agent", "SalesAPI-HttpClient/1.0");
            })
            .AddHttpMessageHandler<CorrelationHttpMessageHandler>()
            .ConfigureHttpClientDefaults();

            // Configure HTTP client for Stock Reservations with correlation propagation  
            services.AddHttpClient<StockReservationClient>(client =>
            {
                client.BaseAddress = new Uri(inventoryApiUrl);
                client.Timeout = TimeSpan.FromSeconds(NetworkConstants.Timeouts.HttpClientTimeout);
                client.DefaultRequestHeaders.Add("User-Agent", "SalesAPI-StockReservation/1.0");
            })
            .AddHttpMessageHandler<CorrelationHttpMessageHandler>()
            .ConfigureHttpClientDefaults();

            // Register correlation handler
            services.AddTransient<CorrelationHttpMessageHandler>();
            
            // Register the configuration provider as a service for proper DI
            services.AddSingleton<DynamicConfigurationProvider>();

            logger?.LogInformation("HttpClient services configured successfully");
            return services;
        }

        private static string GetInventoryApiUrlSafely(IConfiguration configuration, ILogger? logger)
        {
            // Try to get from configuration first
            var inventoryApiUrl = configuration[ConfigurationKeys.InventoryApiUrl];
            
            if (string.IsNullOrEmpty(inventoryApiUrl))
            {
                // Build URL from service configuration without circular dependency
                var host = configuration.GetValue<string>("Services:InventoryApi:Host") ?? NetworkConstants.Hosts.Localhost;
                var port = configuration.GetValue<int?>("NetworkPorts:InventoryApi") ?? NetworkConstants.Ports.InventoryApi;
                inventoryApiUrl = $"http://{host}:{port}";
                logger?.LogInformation("Built Inventory API URL from configuration: {InventoryApiUrl}", inventoryApiUrl);
            }
            else
            {
                logger?.LogInformation("Using configured Inventory API URL: {InventoryApiUrl}", inventoryApiUrl);
            }

            // Validate URL format
            if (!Uri.TryCreate(inventoryApiUrl, UriKind.Absolute, out _))
            {
                var fallbackUrl = string.Format(NetworkConstants.UrlFormats.Http, 
                    NetworkConstants.Hosts.Localhost, 
                    NetworkConstants.Ports.InventoryApi);
                    
                logger?.LogWarning("Invalid Inventory API URL: {InvalidUrl}, using fallback: {FallbackUrl}", 
                    inventoryApiUrl, fallbackUrl);
                    
                inventoryApiUrl = fallbackUrl;
            }

            return inventoryApiUrl;
        }
    }

    /// <summary>
    /// Extension methods for HttpClientBuilder to apply common configurations.
    /// </summary>
    public static class HttpClientBuilderExtensions
    {
        /// <summary>
        /// Configures default settings for HTTP clients using centralized constants.
        /// </summary>
        public static IHttpClientBuilder ConfigureHttpClientDefaults(this IHttpClientBuilder builder)
        {
            return builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // Configure connection limits using constants
                MaxConnectionsPerServer = 100,
                UseCookies = false,
                UseDefaultCredentials = false
            });
        }
    }
}