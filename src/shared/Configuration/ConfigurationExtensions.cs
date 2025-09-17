using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SalesAPI.Shared.Configuration
{
    /// <summary>
    /// Extension methods for registering shared configuration across microservices.
    /// Provides centralized configuration management and reduces code duplication.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Registers shared configuration settings in the DI container.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddSharedConfiguration(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Bind shared configuration from appsettings
            var sharedConfig = new SharedConfiguration();
            configuration.Bind("Shared", sharedConfig);
            
            // Override with individual settings if they exist
            OverrideWithIndividualSettings(sharedConfig, configuration);
            
            // Register as singleton for performance
            services.AddSingleton(sharedConfig);
            services.AddSingleton(sharedConfig.Ports);
            services.AddSingleton(sharedConfig.HealthCheck);
            services.AddSingleton(sharedConfig.Jwt);
            services.AddSingleton(sharedConfig.Messaging);
            
            return services;
        }
        
        /// <summary>
        /// Override shared settings with service-specific configurations when they exist.
        /// This allows flexibility while maintaining defaults.
        /// </summary>
        private static void OverrideWithIndividualSettings(SharedConfiguration sharedConfig, IConfiguration configuration)
        {
            // Override network ports
            var networkPorts = configuration.GetSection("NetworkPorts");
            if (networkPorts.Exists())
            {
                networkPorts.Bind(sharedConfig.Ports);
            }
            
            // Override health check settings
            var healthCheck = configuration.GetSection("HealthCheck");
            if (healthCheck.Exists())
            {
                healthCheck.Bind(sharedConfig.HealthCheck);
            }
            
            // Override JWT settings
            var jwt = configuration.GetSection("Jwt");
            if (jwt.Exists())
            {
                jwt.Bind(sharedConfig.Jwt);
            }
            
            // Override messaging settings
            var messaging = configuration.GetSection("Messaging");
            if (messaging.Exists())
            {
                messaging.Bind(sharedConfig.Messaging);
            }
        }
        
        /// <summary>
        /// Gets a typed configuration section with fallback to shared configuration.
        /// </summary>
        /// <typeparam name="T">Configuration type</typeparam>
        /// <param name="configuration">Application configuration</param>
        /// <param name="sectionName">Section name</param>
        /// <param name="fallbackValue">Fallback value if section doesn't exist</param>
        /// <returns>Configured instance</returns>
        public static T GetConfigurationWithFallback<T>(
            this IConfiguration configuration,
            string sectionName,
            T fallbackValue = default) where T : class, new()
        {
            var section = configuration.GetSection(sectionName);
            if (section.Exists())
            {
                var config = new T();
                section.Bind(config);
                return config;
            }
            
            return fallbackValue ?? new T();
        }
    }
}