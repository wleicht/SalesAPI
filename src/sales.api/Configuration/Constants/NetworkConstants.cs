namespace SalesApi.Configuration.Constants
{
    /// <summary>
    /// Network-related constants for the SalesAPI system.
    /// These constants centralize all network configuration to avoid magic numbers.
    /// </summary>
    public static class NetworkConstants
    {
        /// <summary>
        /// Port numbers used by different services in the system.
        /// </summary>
        public static class Ports
        {
            /// <summary>Sales API external port</summary>
            public const int SalesApi = 5001;
            
            /// <summary>Inventory API external port</summary>
            public const int InventoryApi = 5000;
            
            /// <summary>Gateway API external port</summary>
            public const int Gateway = 6000;
            
            /// <summary>Internal container port for all services</summary>
            public const int ContainerInternal = 8080;
            
            /// <summary>RabbitMQ external port</summary>
            public const int RabbitMQ = 5672;
            
            /// <summary>SQL Server external port</summary>
            public const int SqlServer = 1433;
        }
        
        /// <summary>
        /// Host names used in different environments.
        /// </summary>
        public static class Hosts
        {
            /// <summary>Local development host</summary>
            public const string Localhost = "localhost";
            
            /// <summary>Docker internal host for container communication</summary>
            public const string DockerInternal = "host.docker.internal";
            
            /// <summary>Docker service name for inventory</summary>
            public const string InventoryService = "inventory";
            
            /// <summary>Docker service name for sales</summary>
            public const string SalesService = "sales";
            
            /// <summary>Docker service name for gateway</summary>
            public const string GatewayService = "gateway";
        }
        
        /// <summary>
        /// Timeout configurations for various operations.
        /// All values are in seconds unless otherwise specified.
        /// </summary>
        public static class Timeouts
        {
            /// <summary>Health check interval in seconds</summary>
            public const int HealthCheckInterval = 30;
            
            /// <summary>Health check timeout in seconds</summary>
            public const int HealthCheckTimeout = 10;
            
            /// <summary>Health check retry count</summary>
            public const int HealthCheckRetries = 3;
            
            /// <summary>Health check start period in seconds</summary>
            public const int HealthCheckStartPeriod = 40;
            
            /// <summary>HTTP client timeout in seconds</summary>
            public const int HttpClientTimeout = 30;
            
            /// <summary>Database connection timeout in seconds</summary>
            public const int DatabaseTimeout = 30;
        }
        
        /// <summary>
        /// URL patterns and formats used throughout the system.
        /// </summary>
        public static class UrlFormats
        {
            /// <summary>Format for HTTP URLs: http://{host}:{port}</summary>
            public const string Http = "http://{0}:{1}";
            
            /// <summary>Format for HTTPS URLs: https://{host}:{port}</summary>
            public const string Https = "https://{0}:{1}";
            
            /// <summary>Format for health check endpoints</summary>
            public const string HealthCheck = "http://localhost:{0}/health";
            
            /// <summary>Format for RabbitMQ connection strings</summary>
            public const string RabbitMQ = "amqp://{0}:{1}@{2}:{3}/";
        }
    }
}