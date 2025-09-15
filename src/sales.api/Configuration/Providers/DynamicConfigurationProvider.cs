using SalesApi.Configuration.Constants;

namespace SalesApi.Configuration.Providers
{
    /// <summary>
    /// Dynamic configuration provider that centralizes access to configuration values
    /// and eliminates magic numbers throughout the application.
    /// </summary>
    public class DynamicConfigurationProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DynamicConfigurationProvider> _logger;

        public DynamicConfigurationProvider(
            IConfiguration configuration, 
            ILogger<DynamicConfigurationProvider> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the port number for a specific service.
        /// </summary>
        /// <param name="serviceName">Name of the service (e.g., "SalesApi", "InventoryApi")</param>
        /// <returns>Port number for the service</returns>
        public int GetPort(string serviceName)
        {
            var configKey = $"NetworkPorts:{serviceName}";
            var port = _configuration.GetValue<int?>(configKey);
            
            if (port.HasValue && IsValidPort(port.Value))
            {
                return port.Value;
            }

            // Fallback to constants
            var fallbackPort = serviceName switch
            {
                "SalesApi" => NetworkConstants.Ports.SalesApi,
                "InventoryApi" => NetworkConstants.Ports.InventoryApi,
                "Gateway" => NetworkConstants.Ports.Gateway,
                "RabbitMQ" => NetworkConstants.Ports.RabbitMQ,
                _ => throw new ArgumentException($"Unknown service: {serviceName}", nameof(serviceName))
            };

            _logger.LogWarning(
                "Port not configured for service {ServiceName}, using fallback: {Port}",
                serviceName, fallbackPort);

            return fallbackPort;
        }

        /// <summary>
        /// Gets the health check interval.
        /// </summary>
        /// <returns>Health check interval as TimeSpan</returns>
        public TimeSpan GetHealthCheckInterval()
        {
            var seconds = _configuration.GetValue<int?>("HealthCheck:IntervalSeconds") 
                         ?? NetworkConstants.Timeouts.HealthCheckInterval;
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Gets the health check timeout.
        /// </summary>
        /// <returns>Health check timeout as TimeSpan</returns>
        public TimeSpan GetHealthCheckTimeout()
        {
            var seconds = _configuration.GetValue<int?>("HealthCheck:TimeoutSeconds") 
                         ?? NetworkConstants.Timeouts.HealthCheckTimeout;
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Gets the health check retry count.
        /// </summary>
        /// <returns>Number of health check retries</returns>
        public int GetHealthCheckRetries()
        {
            return _configuration.GetValue<int?>("HealthCheck:Retries") 
                   ?? NetworkConstants.Timeouts.HealthCheckRetries;
        }

        /// <summary>
        /// Gets the health check start period.
        /// </summary>
        /// <returns>Health check start period as TimeSpan</returns>
        public TimeSpan GetHealthCheckStartPeriod()
        {
            var seconds = _configuration.GetValue<int?>("HealthCheck:StartPeriodSeconds") 
                         ?? NetworkConstants.Timeouts.HealthCheckStartPeriod;
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Gets the base URL for a service.
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="useHttps">Whether to use HTTPS (default: false)</param>
        /// <returns>Complete base URL for the service</returns>
        public string GetServiceBaseUrl(string serviceName, bool useHttps = false)
        {
            var host = _configuration.GetValue<string>($"Services:{serviceName}:Host") 
                      ?? NetworkConstants.Hosts.Localhost;
            var port = GetPort(serviceName);
            var scheme = useHttps ? "https" : "http";

            return $"{scheme}://{host}:{port}";
        }

        /// <summary>
        /// Gets the connection string for the database.
        /// </summary>
        /// <returns>Database connection string</returns>
        public string GetDatabaseConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured");
            }

            return connectionString;
        }

        /// <summary>
        /// Gets the RabbitMQ connection string.
        /// </summary>
        /// <returns>RabbitMQ connection string</returns>
        public string GetRabbitMQConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("RabbitMQ");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                // Build from individual parts
                var username = _configuration.GetValue<string>("RabbitMQ:Username") 
                              ?? SecurityConstants.Development.DefaultRabbitMQUsername;
                var password = _configuration.GetValue<string>("RabbitMQ:Password") 
                              ?? SecurityConstants.Development.DefaultRabbitMQPassword;
                var host = _configuration.GetValue<string>("RabbitMQ:Host") 
                          ?? NetworkConstants.Hosts.Localhost;
                var port = _configuration.GetValue<int?>("RabbitMQ:Port") 
                          ?? NetworkConstants.Ports.RabbitMQ;

                connectionString = string.Format(NetworkConstants.UrlFormats.RabbitMQ, 
                    username, password, host, port);

                _logger.LogWarning(
                    "RabbitMQ connection string not found, constructed from individual settings");
            }

            return connectionString;
        }

        /// <summary>
        /// Gets the JWT configuration.
        /// </summary>
        /// <returns>JWT configuration object</returns>
        public JwtConfiguration GetJwtConfiguration()
        {
            return new JwtConfiguration
            {
                Key = _configuration.GetValue<string>("Jwt:Key") 
                     ?? SecurityConstants.Development.DevJwtKey,
                Issuer = _configuration.GetValue<string>("Jwt:Issuer") 
                        ?? SecurityConstants.Jwt.DefaultIssuer,
                Audience = _configuration.GetValue<string>("Jwt:Audience") 
                          ?? SecurityConstants.Jwt.DefaultAudience,
                ExpiryInDays = _configuration.GetValue<int?>("Jwt:ExpiryInDays") 
                              ?? SecurityConstants.Jwt.DefaultExpiryDays
            };
        }

        /// <summary>
        /// Validates if a port number is valid.
        /// </summary>
        /// <param name="port">Port number to validate</param>
        /// <returns>True if port is valid, false otherwise</returns>
        private static bool IsValidPort(int port)
        {
            return port is >= 1024 and <= 65535;
        }
    }

    /// <summary>
    /// JWT configuration data transfer object.
    /// </summary>
    public class JwtConfiguration
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryInDays { get; set; }
    }
}