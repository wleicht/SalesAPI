namespace InventoryApi.Configuration
{
    /// <summary>
    /// Constants for configuration keys used throughout the application.
    /// </summary>
    public static class ConfigurationKeys
    {
        public const string DatabaseConnection = "ConnectionStrings:DefaultConnection";
        public const string RabbitMqConnection = "ConnectionStrings:RabbitMQ";
        public const string JwtKey = "Jwt:Key";
        public const string JwtIssuer = "Jwt:Issuer";
        public const string JwtAudience = "Jwt:Audience";
        
        public static class Defaults
        {
            public const string RabbitMqConnection = "amqp://admin:admin123@host.docker.internal:5672/";
        }
    }
}