namespace SalesAPI.Shared.Configuration
{
    /// <summary>
    /// Centralized configuration settings shared across all microservices.
    /// Eliminates duplication and ensures consistency across the application.
    /// </summary>
    public class SharedConfiguration
    {
        public NetworkPorts Ports { get; set; } = new();
        public HealthCheckSettings HealthCheck { get; set; } = new();
        public JwtSettings Jwt { get; set; } = new();
        public MessagingSettings Messaging { get; set; } = new();
        
        /// <summary>
        /// Network port configurations for all services.
        /// </summary>
        public class NetworkPorts
        {
            public int Gateway { get; set; } = 6000;
            public int SalesApi { get; set; } = 5001;
            public int InventoryApi { get; set; } = 5000;
            public int RabbitMQ { get; set; } = 5672;
            public int SqlServer { get; set; } = 1433;
            public int ContainerInternal { get; set; } = 8080;
        }
        
        /// <summary>
        /// Health check configurations standardized across services.
        /// </summary>
        public class HealthCheckSettings
        {
            public int IntervalSeconds { get; set; } = 30;
            public int TimeoutSeconds { get; set; } = 15;
            public int Retries { get; set; } = 3;
            public int StartPeriodSeconds { get; set; } = 60;
        }
        
        /// <summary>
        /// JWT authentication settings standardized across services.
        /// </summary>
        public class JwtSettings
        {
            public string Key { get; set; } = "MyVeryLongSecretKeyThatShouldBeAtLeast256BitsLongForHMACSHA256Algorithm";
            public string Issuer { get; set; } = "SalesAPI-System";
            public string Audience { get; set; } = "SalesAPI-Services";
            public int ExpiryInDays { get; set; } = 1;
            public bool ValidateIssuer { get; set; } = true;
            public bool ValidateAudience { get; set; } = true;
            public bool ValidateLifetime { get; set; } = true;
            public bool ValidateIssuerSigningKey { get; set; } = true;
        }
        
        /// <summary>
        /// Messaging configurations for RabbitMQ integration.
        /// </summary>
        public class MessagingSettings
        {
            public bool Enabled { get; set; } = true;
            public string ConnectionString { get; set; } = "amqp://admin:admin123@salesapi-rabbitmq:5672/";
            public int Workers { get; set; } = 3;
            public int MaxRetries { get; set; } = 5;
            public int RetryDelaySeconds { get; set; } = 10;
        }
    }
}