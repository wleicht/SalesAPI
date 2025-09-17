using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SalesApi.Infrastructure.Data;

namespace SalesApi.Infrastructure.Data
{
    /// <summary>
    /// Design-time DbContext factory for Entity Framework migrations.
    /// This factory is used by EF Core tools when the application is not running.
    /// </summary>
    public class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
    {
        /// <summary>
        /// Creates a new SalesDbContext instance for design-time operations.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the design-time tools</param>
        /// <returns>A configured SalesDbContext instance</returns>
        public SalesDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SalesDbContext>();
            
            // Use a development connection string for migrations
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=SalesDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True",
                options => options.MigrationsAssembly("sales.api"));

            return new SalesDbContext(optionsBuilder.Options);
        }
    }
}