using Microsoft.EntityFrameworkCore;
using SalesApi.Infrastructure.Data;
using SalesApi.Configuration.Constants;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Extension methods for configuring database services.
    /// </summary>
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger? logger = null)
        {
            var connectionString = GetValidatedConnectionString(configuration, logger);

            services.AddDbContext<SalesDbContext>(options =>
                options.UseSqlServer(connectionString, ConfigureSqlServerOptions));

            logger?.LogInformation("Database services configured successfully");
            return services;
        }

        private static string GetValidatedConnectionString(IConfiguration configuration, ILogger? logger)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            // Warn if using development defaults
            if (IsDefaultDevelopmentConnectionString(connectionString))
            {
                logger?.LogWarning("?? Using development database connection string. Ensure this is intentional for non-development environments.");
            }

            return connectionString;
        }

        private static void ConfigureSqlServerOptions(Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder sqlOptions)
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(NetworkConstants.Timeouts.DatabaseTimeout),
                errorNumbersToAdd: new int[] { 1205 }); // Include deadlock detection
                
            sqlOptions.CommandTimeout(NetworkConstants.Timeouts.DatabaseTimeout);
        }

        private static bool IsDefaultDevelopmentConnectionString(string connectionString)
        {
            return connectionString.Contains(SecurityConstants.Development.DefaultSqlPassword) ||
                   connectionString.Contains("localhost") ||
                   connectionString.Contains("(localdb)");
        }
    }
}