using Microsoft.EntityFrameworkCore;
using SalesApi.Infrastructure.Data;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Professional database configuration with comprehensive logging and validation.
    /// Implements enterprise patterns for database service registration and configuration.
    /// </summary>
    public static class DatabaseConfiguration
    {
        /// <summary>
        /// Configures database services with professional patterns and comprehensive logging.
        /// Supports both SQL Server and in-memory databases with proper configuration validation.
        /// </summary>
        /// <param name="services">Service collection for dependency injection</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var logger = Serilog.Log.Logger;
            var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase", false);

            logger.Information("?? Configuring database services | Type: {DatabaseType}", 
                useInMemory ? "In-Memory" : "SQL Server");

            if (useInMemory)
            {
                ConfigureInMemoryDatabase(services, configuration, logger);
            }
            else
            {
                ConfigureSqlServerDatabase(services, configuration, logger);
            }

            // Register database manager for professional initialization
            services.AddScoped<Infrastructure.DatabaseManager>();

            logger.Information("? Database services configured successfully");
            return services;
        }

        /// <summary>
        /// Configures in-memory database for testing scenarios.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger instance</param>
        private static void ConfigureInMemoryDatabase(
            IServiceCollection services, 
            IConfiguration configuration, 
            Serilog.ILogger logger)
        {
            logger.Information("?? Configuring in-memory database for testing");

            services.AddDbContext<SalesDbContext>(options =>
            {
                options.UseInMemoryDatabase("SalesTestDb");
                
                // Enable detailed logging for development
                if (IsDetailedLoggingEnabled(configuration))
                {
                    options.EnableSensitiveDataLogging();
                    options.LogTo(message => logger.Debug("EF Core InMemory: {Message}", message));
                }
                
                // Configure for testing scenarios
                options.EnableServiceProviderCaching(false);
            });

            logger.Warning("?? Using in-memory database - data will not persist");
        }

        /// <summary>
        /// Configures SQL Server database with professional patterns.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger instance</param>
        private static void ConfigureSqlServerDatabase(
            IServiceCollection services, 
            IConfiguration configuration, 
            Serilog.ILogger logger)
        {
            logger.Information("??? Configuring SQL Server database");

            var connectionString = GetValidatedConnectionString(configuration, logger);
            
            services.AddDbContext<SalesDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    // Configure resilience patterns
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: configuration.GetValue<int>("Database:MaxRetryCount", 5),
                        maxRetryDelay: TimeSpan.FromSeconds(configuration.GetValue<int>("Database:MaxRetryDelaySeconds", 30)),
                        errorNumbersToAdd: new int[] { 1205, 1222 }); // Deadlock and lock timeout

                    // Configure timeouts
                    sqlOptions.CommandTimeout(configuration.GetValue<int>("Database:CommandTimeoutSeconds", 30));
                    
                    // Set migrations assembly
                    sqlOptions.MigrationsAssembly("sales.api");
                    
                    // Configure for production performance
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });

                // Configure logging based on environment
                if (IsDetailedLoggingEnabled(configuration))
                {
                    options.EnableSensitiveDataLogging();
                    options.LogTo(message => logger.Debug("EF Core SQL: {Message}", message));
                }

                // Configure change tracking for performance
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
                
                // Enable service provider caching for performance
                options.EnableServiceProviderCaching(true);
            });

            logger.Information("? SQL Server database configured with resilience patterns");
        }

        /// <summary>
        /// Gets and validates the database connection string.
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>Validated connection string</returns>
        /// <exception cref="InvalidOperationException">Thrown when connection string is invalid</exception>
        private static string GetValidatedConnectionString(IConfiguration configuration, Serilog.ILogger logger)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                const string errorMessage = "DefaultConnection not found in configuration";
                logger.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Log warning for development connection strings
            if (IsDefaultDevelopmentConnectionString(connectionString))
            {
                logger.Warning("?? Using development database connection string. " +
                              "Ensure this is intentional for non-development environments");
            }

            // Validate connection string format
            ValidateConnectionStringComponents(connectionString);

            logger.Debug("Connection string validation passed");
            return connectionString;
        }

        /// <summary>
        /// Validates that the connection string contains required components.
        /// </summary>
        /// <param name="connectionString">Connection string to validate</param>
        /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
        private static void ValidateConnectionStringComponents(string connectionString)
        {
            var requiredComponents = new[] { "Server=", "Database=" };
            var missingComponents = requiredComponents
                .Where(component => !connectionString.Contains(component, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (missingComponents.Any())
            {
                throw new InvalidOperationException(
                    $"Connection string is missing required components: {string.Join(", ", missingComponents)}");
            }
        }

        /// <summary>
        /// Checks if the connection string contains development default values.
        /// </summary>
        /// <param name="connectionString">Connection string to check</param>
        /// <returns>True if connection string appears to be for development</returns>
        private static bool IsDefaultDevelopmentConnectionString(string connectionString)
        {
            var developmentIndicators = new[]
            {
                "Your_password123",
                "localhost",
                "(localdb)",
                "127.0.0.1"
            };

            return developmentIndicators.Any(indicator => 
                connectionString.Contains(indicator, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines if detailed EF Core logging should be enabled.
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <returns>True if detailed logging should be enabled</returns>
        private static bool IsDetailedLoggingEnabled(IConfiguration configuration)
        {
            var environment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT", "Production");
            var explicitSetting = configuration.GetValue<bool?>("Database:EnableDetailedLogging");

            // Enable detailed logging in development or if explicitly requested
            return explicitSetting ?? 
                   string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        }
    }
}