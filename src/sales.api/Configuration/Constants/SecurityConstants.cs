namespace SalesApi.Configuration.Constants
{
    /// <summary>
    /// Security-related constants and configuration keys.
    /// Contains both development defaults and production environment key names.
    /// </summary>
    public static class SecurityConstants
    {
        /// <summary>
        /// Default values for development environments.
        /// ?? WARNING: These should NEVER be used in production!
        /// </summary>
        public static class Development
        {
            /// <summary>Default RabbitMQ username for development</summary>
            public const string DefaultRabbitMQUsername = "admin";
            
            /// <summary>Default RabbitMQ password for development</summary>
            public const string DefaultRabbitMQPassword = "admin123";
            
            /// <summary>Default SQL Server password for development</summary>
            public const string DefaultSqlPassword = "Your_password123";
            
            /// <summary>Default SQL Server username for development</summary>
            public const string DefaultSqlUsername = "sa";
            
            /// <summary>Development JWT signing key (insecure - for dev only)</summary>
            public const string DevJwtKey = "MyVeryLongSecretKeyThatShouldBeAtLeast256BitsLongForHMACSHA256Algorithm";
        }
        
        /// <summary>
        /// Environment variable keys for production configuration.
        /// These should be used to retrieve secure values from environment.
        /// </summary>
        public static class EnvironmentKeys
        {
            /// <summary>Environment variable for RabbitMQ username</summary>
            public const string RabbitMQUsername = "RABBITMQ_USERNAME";
            
            /// <summary>Environment variable for RabbitMQ password</summary>
            public const string RabbitMQPassword = "RABBITMQ_PASSWORD";
            
            /// <summary>Environment variable for SQL Server password</summary>
            public const string SqlPassword = "SQL_SERVER_PASSWORD";
            
            /// <summary>Environment variable for SQL Server username</summary>
            public const string SqlUsername = "SQL_SERVER_USERNAME";
            
            /// <summary>Environment variable for JWT signing key</summary>
            public const string JwtKey = "JWT_SIGNING_KEY";
            
            /// <summary>Environment variable for JWT issuer</summary>
            public const string JwtIssuer = "JWT_ISSUER";
            
            /// <summary>Environment variable for JWT audience</summary>
            public const string JwtAudience = "JWT_AUDIENCE";
        }
        
        /// <summary>
        /// JWT token configuration constants.
        /// </summary>
        public static class Jwt
        {
            /// <summary>Default token expiry in days</summary>
            public const int DefaultExpiryDays = 1;
            
            /// <summary>Minimum key length for HMAC SHA256</summary>
            public const int MinimumKeyLength = 32;
            
            /// <summary>Default issuer for development</summary>
            public const string DefaultIssuer = "SalesAPI";
            
            /// <summary>Default audience for development</summary>
            public const string DefaultAudience = "SalesAPI.Users";
        }
        
        /// <summary>
        /// Database configuration constants.
        /// </summary>
        public static class Database
        {
            /// <summary>Default database name for Sales API</summary>
            public const string SalesDbName = "SalesDb";
            
            /// <summary>Default database name for Inventory API</summary>
            public const string InventoryDbName = "InventoryDb";
            
            /// <summary>Connection string template for SQL Server</summary>
            public const string SqlServerConnectionTemplate = 
                "Server={0};Database={1};User Id={2};Password={3};TrustServerCertificate=True";
        }
    }
}