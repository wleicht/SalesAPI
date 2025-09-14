namespace Gateway.Configuration
{
    /// <summary>
    /// Constants for configuration keys used throughout the application.
    /// </summary>
    public static class ConfigurationKeys
    {
        public const string JwtKey = "Jwt:Key";
        public const string JwtIssuer = "Jwt:Issuer";
        public const string JwtAudience = "Jwt:Audience";
        public const string ReverseProxySection = "ReverseProxy";
    }
}