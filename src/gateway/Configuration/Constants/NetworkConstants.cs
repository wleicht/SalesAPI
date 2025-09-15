namespace Gateway.Configuration.Constants
{
    /// <summary>
    /// Network-related constants for the Gateway API.
    /// These constants centralize all network configuration to avoid magic numbers.
    /// </summary>
    public static class NetworkConstants
    {
        /// <summary>
        /// Port numbers used by different services in the system.
        /// </summary>
        public static class Ports
        {
            /// <summary>Gateway API external port</summary>
            public const int Gateway = 6000;
            
            /// <summary>Sales API external port</summary>
            public const int SalesApi = 5001;
            
            /// <summary>Inventory API external port</summary>
            public const int InventoryApi = 5000;
            
            /// <summary>Internal container port for all services</summary>
            public const int ContainerInternal = 8080;
        }
        
        /// <summary>
        /// Host names used in different environments.
        /// </summary>
        public static class Hosts
        {
            /// <summary>Local development host</summary>
            public const string Localhost = "localhost";
            
            /// <summary>Docker service name for inventory</summary>
            public const string InventoryService = "inventory";
            
            /// <summary>Docker service name for sales</summary>
            public const string SalesService = "sales";
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
        }
    }
}