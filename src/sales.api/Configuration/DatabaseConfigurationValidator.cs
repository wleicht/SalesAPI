using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Professional database configuration validator that performs early validation
    /// of database settings to catch configuration issues before application startup.
    /// Implements enterprise patterns for configuration validation and diagnostics.
    /// </summary>
    /// <remarks>
    /// Validation Features:
    /// - Connection string format validation
    /// - Required configuration presence checks
    /// - Early failure detection to prevent runtime errors
    /// - Comprehensive diagnostic logging
    /// - Support for different database providers
    /// - Environment-specific validation rules
    /// </remarks>
    public static class DatabaseConfigurationValidator
    {
        /// <summary>
        /// Validates the complete database configuration including connection strings,
        /// provider settings, and environment-specific requirements.
        /// </summary>
        /// <param name="configuration">Application configuration to validate</param>
        /// <param name="logger">Serilog logger for diagnostic output</param>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
        public static void ValidateConfiguration(IConfiguration configuration, Serilog.ILogger logger)
        {
            logger.Information("?? Starting database configuration validation");

            try
            {
                var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase", false);
                
                if (useInMemory)
                {
                    ValidateInMemoryConfiguration(configuration, logger);
                }
                else
                {
                    ValidateSqlServerConfiguration(configuration, logger);
                }

                ValidateOptionalSettings(configuration, logger);

                logger.Information("? Database configuration validation completed successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "? Database configuration validation failed: {Error}", ex.Message);
                throw new InvalidOperationException($"Database configuration validation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates configuration for in-memory database usage.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <param name="logger">Serilog logger for diagnostics</param>
        private static void ValidateInMemoryConfiguration(IConfiguration configuration, Serilog.ILogger logger)
        {
            logger.Warning("?? Using in-memory database - data will not persist between application restarts");
            logger.Warning("?? In-memory database should only be used for testing or development");

            // Validate that we're not in production with in-memory database
            var environment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
            if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "In-memory database cannot be used in Production environment");
            }

            logger.Information("? In-memory database configuration is valid");
        }

        /// <summary>
        /// Validates configuration for SQL Server database usage.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <param name="logger">Serilog logger for diagnostics</param>
        private static void ValidateSqlServerConfiguration(IConfiguration configuration, Serilog.ILogger logger)
        {
            logger.Information("??? Validating SQL Server database configuration");

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "SQL Server connection string 'DefaultConnection' is required when UseInMemoryDatabase is false");
            }

            ValidateConnectionStringFormat(connectionString, logger);
            ValidateConnectionStringSecurity(connectionString, logger);

            logger.Information("? SQL Server database configuration is valid");
        }

        /// <summary>
        /// Validates the format and required components of a SQL Server connection string.
        /// </summary>
        /// <param name="connectionString">Connection string to validate</param>
        /// <param name="logger">Serilog logger for diagnostics</param>
        private static void ValidateConnectionStringFormat(string connectionString, Serilog.ILogger logger)
        {
            logger.Debug("?? Validating connection string format");

            // Check for required connection string components
            var requiredComponents = new Dictionary<string, string[]>
            {
                { "Server", new[] { "Server=", "Data Source=", "DataSource=" } },
                { "Database", new[] { "Database=", "Initial Catalog=", "InitialCatalog=" } }
            };

            var missingComponents = new List<string>();

            foreach (var component in requiredComponents)
            {
                var hasComponent = component.Value.Any(pattern => 
                    connectionString.Contains(pattern, StringComparison.OrdinalIgnoreCase));
                
                if (!hasComponent)
                {
                    missingComponents.Add(component.Key);
                }
            }

            if (missingComponents.Any())
            {
                throw new InvalidOperationException(
                    $"Connection string is missing required components: {string.Join(", ", missingComponents)}. " +
                    $"Required format: Server=...; Database=...; [authentication parameters]");
            }

            logger.Debug("? Connection string format validation passed");
        }

        /// <summary>
        /// Validates security aspects of the connection string.
        /// </summary>
        /// <param name="connectionString">Connection string to validate</param>
        /// <param name="logger">Serilog logger for diagnostics</param>
        private static void ValidateConnectionStringSecurity(string connectionString, Serilog.ILogger logger)
        {
            logger.Debug("?? Validating connection string security");

            // Check for authentication method
            var hasIntegratedSecurity = connectionString.Contains("Integrated Security=", StringComparison.OrdinalIgnoreCase) ||
                                      connectionString.Contains("Trusted_Connection=", StringComparison.OrdinalIgnoreCase);
            
            var hasSqlAuthentication = connectionString.Contains("User Id=", StringComparison.OrdinalIgnoreCase) ||
                                     connectionString.Contains("UserId=", StringComparison.OrdinalIgnoreCase) ||
                                     connectionString.Contains("User=", StringComparison.OrdinalIgnoreCase);

            if (!hasIntegratedSecurity && !hasSqlAuthentication)
            {
                throw new InvalidOperationException(
                    "Connection string must specify authentication method (either Integrated Security or User Id/Password)");
            }

            // Warn about development defaults
            if (connectionString.Contains("Your_password123", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("?? Connection string contains default development password. " +
                                "This should be changed for production environments.");
            }

            // Check for SSL/Encryption settings
            if (!connectionString.Contains("TrustServerCertificate=", StringComparison.OrdinalIgnoreCase) &&
                !connectionString.Contains("Encrypt=", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("?? Connection string does not specify encryption settings. " +
                                "Consider adding 'Encrypt=true' for production environments.");
            }

            logger.Debug("? Connection string security validation completed");
        }

        /// <summary>
        /// Validates optional database-related settings.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <param name="logger">Serilog logger for diagnostics</param>
        private static void ValidateOptionalSettings(IConfiguration configuration, Serilog.ILogger logger)
        {
            logger.Debug("?? Validating optional database settings");

            // Validate command timeout settings
            var commandTimeout = configuration.GetValue<int?>("Database:CommandTimeout");
            if (commandTimeout.HasValue)
            {
                if (commandTimeout.Value < 0)
                {
                    throw new InvalidOperationException("Database command timeout cannot be negative");
                }
                
                if (commandTimeout.Value > 600) // 10 minutes
                {
                    logger.Warning("?? Database command timeout is set to {Timeout} seconds, which is quite high", 
                        commandTimeout.Value);
                }
            }

            // Validate retry settings
            var maxRetryCount = configuration.GetValue<int?>("Database:MaxRetryCount");
            if (maxRetryCount.HasValue && (maxRetryCount.Value < 0 || maxRetryCount.Value > 10))
            {
                logger.Warning("?? Database max retry count of {Count} may not be optimal. Recommended range: 0-10", 
                    maxRetryCount.Value);
            }

            // Validate query diagnosis settings
            var enableQueryDiagnosis = configuration.GetValue<bool?>("Database:EnableQueryDiagnosis");
            if (enableQueryDiagnosis.HasValue && enableQueryDiagnosis.Value)
            {
                logger.Information("?? Query diagnosis is enabled - queries will be logged for performance analysis");
            }

            logger.Debug("? Optional database settings validation completed");
        }

        /// <summary>
        /// Performs a quick validation check without throwing exceptions.
        /// Useful for health checks or diagnostics.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <param name="logger">Serilog logger for diagnostics</param>
        /// <returns>ValidationResult indicating success/failure with details</returns>
        public static ValidationResult ValidateQuickCheck(IConfiguration configuration, Serilog.ILogger logger)
        {
            logger.Debug("?? Performing quick database configuration check");

            var result = new ValidationResult { Success = true };

            try
            {
                var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase", false);
                
                if (useInMemory)
                {
                    ValidateInMemoryConfiguration(configuration, logger);
                }
                else
                {
                    ValidateSqlServerConfiguration(configuration, logger);
                }

                ValidateOptionalSettings(configuration, logger);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Error: {ex.Message}");
                logger.Warning(ex, "?? Quick database configuration check failed: {Error}", ex.Message);
            }

            logger.Debug("? Quick database configuration check completed");
            return result;
        }
    }

    /// <summary>
    /// Represents the result of a validation check.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets the list of error messages encountered during validation.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();
    }
}