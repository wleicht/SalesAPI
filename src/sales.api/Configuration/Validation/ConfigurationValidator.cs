using SalesApi.Configuration.Constants;

namespace SalesApi.Configuration.Validation
{
    /// <summary>
    /// Validates configuration at startup to ensure all values are valid and consistent.
    /// This helps catch configuration errors early in the application lifecycle.
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// Validates the entire configuration and throws detailed exceptions if invalid.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <param name="logger">Logger for warning and info messages</param>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public static void ValidateConfiguration(IConfiguration configuration, ILogger? logger = null)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate ports
            ValidatePorts(configuration, errors, warnings);
            
            // Validate connection strings
            ValidateConnectionStrings(configuration, errors, warnings);
            
            // Validate JWT configuration
            ValidateJwtConfiguration(configuration, errors, warnings);
            
            // Validate health check settings
            ValidateHealthCheckConfiguration(configuration, errors, warnings);

            // Log warnings
            if (warnings.Any() && logger != null)
            {
                foreach (var warning in warnings)
                {
                    logger.LogWarning("Configuration Warning: {Warning}", warning);
                }
            }

            // Throw if there are critical errors
            if (errors.Any())
            {
                var errorMessage = $"Configuration validation failed:\n{string.Join("\n", errors)}";
                throw new InvalidOperationException(errorMessage);
            }

            logger?.LogInformation("Configuration validation completed successfully");
        }

        /// <summary>
        /// Validates port configurations.
        /// </summary>
        private static void ValidatePorts(IConfiguration configuration, List<string> errors, List<string> warnings)
        {
            var portConfigs = new[]
            {
                ("NetworkPorts:SalesApi", "SalesApi"),
                ("NetworkPorts:InventoryApi", "InventoryApi"),
                ("NetworkPorts:Gateway", "Gateway"),
                ("NetworkPorts:RabbitMQ", "RabbitMQ")
            };

            var configuredPorts = new List<int>();

            foreach (var (configKey, serviceName) in portConfigs)
            {
                var port = configuration.GetValue<int?>(configKey);
                
                if (port.HasValue)
                {
                    if (!IsValidPort(port.Value))
                    {
                        errors.Add($"Invalid port for {serviceName}: {port.Value}. Must be between 1024 and 65535.");
                    }
                    else
                    {
                        configuredPorts.Add(port.Value);
                    }
                }
                else
                {
                    warnings.Add($"Port not configured for {serviceName}, will use default value.");
                }
            }

            // Check for port conflicts
            var duplicatePorts = configuredPorts.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var duplicatePort in duplicatePorts)
            {
                errors.Add($"Port conflict detected: Multiple services configured to use port {duplicatePort}");
            }
        }

        /// <summary>
        /// Validates connection string configurations.
        /// </summary>
        private static void ValidateConnectionStrings(IConfiguration configuration, List<string> errors, List<string> warnings)
        {
            // Validate database connection string
            var dbConnectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(dbConnectionString))
            {
                errors.Add("Database connection string 'DefaultConnection' is missing or empty.");
            }
            else if (IsDefaultDevelopmentConnectionString(dbConnectionString))
            {
                warnings.Add("Using default development database connection string. Ensure this is intentional for non-development environments.");
            }

            // Validate RabbitMQ connection string
            var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ");
            if (string.IsNullOrEmpty(rabbitMqConnectionString))
            {
                warnings.Add("RabbitMQ connection string is missing. Will attempt to construct from individual settings.");
            }
            else if (IsDefaultDevelopmentRabbitMqConnectionString(rabbitMqConnectionString))
            {
                warnings.Add("Using default development RabbitMQ connection string. Ensure this is intentional for non-development environments.");
            }
        }

        /// <summary>
        /// Validates JWT configuration.
        /// </summary>
        private static void ValidateJwtConfiguration(IConfiguration configuration, List<string> errors, List<string> warnings)
        {
            var jwtKey = configuration.GetValue<string>("Jwt:Key");
            if (string.IsNullOrEmpty(jwtKey))
            {
                errors.Add("JWT Key is missing or empty.");
            }
            else if (jwtKey.Length < SecurityConstants.Jwt.MinimumKeyLength)
            {
                errors.Add($"JWT Key is too short. Minimum length is {SecurityConstants.Jwt.MinimumKeyLength} characters.");
            }
            else if (jwtKey == SecurityConstants.Development.DevJwtKey)
            {
                warnings.Add("Using default development JWT key. This should NEVER be used in production.");
            }

            var jwtIssuer = configuration.GetValue<string>("Jwt:Issuer");
            if (string.IsNullOrEmpty(jwtIssuer))
            {
                warnings.Add("JWT Issuer is missing. Using default value.");
            }

            var jwtAudience = configuration.GetValue<string>("Jwt:Audience");
            if (string.IsNullOrEmpty(jwtAudience))
            {
                warnings.Add("JWT Audience is missing. Using default value.");
            }

            var jwtExpiryDays = configuration.GetValue<int?>("Jwt:ExpiryInDays");
            if (jwtExpiryDays.HasValue && jwtExpiryDays.Value <= 0)
            {
                errors.Add("JWT expiry days must be greater than 0.");
            }
            else if (jwtExpiryDays.HasValue && jwtExpiryDays.Value > 30)
            {
                warnings.Add("JWT expiry is set to more than 30 days. Consider if this is appropriate for your security requirements.");
            }
        }

        /// <summary>
        /// Validates health check configuration.
        /// </summary>
        private static void ValidateHealthCheckConfiguration(IConfiguration configuration, List<string> errors, List<string> warnings)
        {
            var interval = configuration.GetValue<int?>("HealthCheck:IntervalSeconds");
            var timeout = configuration.GetValue<int?>("HealthCheck:TimeoutSeconds");
            var retries = configuration.GetValue<int?>("HealthCheck:Retries");
            var startPeriod = configuration.GetValue<int?>("HealthCheck:StartPeriodSeconds");

            if (interval.HasValue && interval.Value <= 0)
            {
                errors.Add("Health check interval must be greater than 0.");
            }

            if (timeout.HasValue && timeout.Value <= 0)
            {
                errors.Add("Health check timeout must be greater than 0.");
            }

            if (interval.HasValue && timeout.HasValue && timeout.Value >= interval.Value)
            {
                errors.Add("Health check timeout must be less than the interval.");
            }

            if (retries.HasValue && retries.Value <= 0)
            {
                errors.Add("Health check retries must be greater than 0.");
            }

            if (startPeriod.HasValue && startPeriod.Value <= 0)
            {
                errors.Add("Health check start period must be greater than 0.");
            }
        }

        /// <summary>
        /// Validates if a port number is in the valid range.
        /// </summary>
        private static bool IsValidPort(int port)
        {
            return port is >= 1024 and <= 65535;
        }

        /// <summary>
        /// Checks if the connection string appears to be the default development string.
        /// </summary>
        private static bool IsDefaultDevelopmentConnectionString(string connectionString)
        {
            return connectionString.Contains("Your_password123") || 
                   connectionString.Contains("localhost") ||
                   connectionString.Contains("Password=admin123");
        }

        /// <summary>
        /// Checks if the RabbitMQ connection string appears to be the default development string.
        /// </summary>
        private static bool IsDefaultDevelopmentRabbitMqConnectionString(string connectionString)
        {
            return connectionString.Contains("admin123") || 
                   connectionString.Contains("localhost") ||
                   connectionString.Contains("amqp://admin:admin123");
        }
    }
}