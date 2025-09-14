using Microsoft.EntityFrameworkCore;
using InventoryApi.Persistence;

namespace InventoryApi.Configuration
{
    /// <summary>
    /// Extension methods for configuring database services.
    /// </summary>
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<InventoryDbContext>(options =>
                options.UseSqlServer(connectionString, ConfigureSqlServerOptions));

            return services;
        }

        private static void ConfigureSqlServerOptions(Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder sqlOptions)
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: new int[] { 1205 }); // Include deadlock detection
        }
    }
}