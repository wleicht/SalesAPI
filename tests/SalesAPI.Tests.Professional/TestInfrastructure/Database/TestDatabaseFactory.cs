using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesApi.Infrastructure.Data;
using InventoryApi.Persistence;
using SalesAPI.Tests.Professional.TestInfrastructure.Factories;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Database
{
    /// <summary>
    /// Factory for creating database instances for testing.
    /// Refactored to use base factory pattern to eliminate duplication.
    /// Provides isolated databases per test to ensure test independence.
    /// </summary>
    public class TestDatabaseFactory : TestInfrastructureFactoryBase<DbContext>
    {
        // Using SQL Server LocalDB for real database testing
        private const string BaseConnectionString = "Server=(localdb)\\mssqllocaldb;Database={0};Trusted_Connection=true;MultipleActiveResultSets=true;";
        
        public TestDatabaseFactory(ILogger<TestDatabaseFactory> logger) : base(logger)
        {
        }

        protected override DbContext CreateInternal(string testName)
        {
            throw new NotSupportedException("Use CreateContextAsync<T> or CreateInMemoryContext<T> instead");
        }

        /// <summary>
        /// Creates a Sales database context for testing using in-memory database.
        /// </summary>
        /// <param name="testName">Optional test name for unique database naming</param>
        /// <returns>A configured SalesDbContext instance</returns>
        public SalesDbContext CreateSalesDbContext(string? testName = null)
        {
            return CreateInMemoryContext<SalesDbContext>(testName ?? "SalesDbTest");
        }

        /// <summary>
        /// Creates an Inventory database context for testing using in-memory database.
        /// </summary>
        /// <param name="testName">Optional test name for unique database naming</param>
        /// <returns>A configured InventoryDbContext instance</returns>
        public InventoryDbContext CreateInventoryDbContext(string? testName = null)
        {
            return CreateInMemoryContext<InventoryDbContext>(testName ?? "InventoryDbTest");
        }

        /// <summary>
        /// Creates a real database context for testing with a unique database name.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type to create</typeparam>
        /// <param name="testName">Name identifier for the test</param>
        /// <returns>A configured DbContext instance</returns>
        public async Task<TContext> CreateContextAsync<TContext>(string testName) where TContext : DbContext
        {
            var databaseName = GenerateDatabaseName(testName);
            var connectionString = string.Format(BaseConnectionString, databaseName);
            
            Logger.LogInformation("Creating test database: {DatabaseName} for test: {TestName}", databaseName, testName);

            var options = new DbContextOptionsBuilder<TContext>()
                .UseSqlServer(connectionString)
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options;

            var context = (TContext)Activator.CreateInstance(typeof(TContext), options)!;
            
            try
            {
                // Ensure database is created and up to date
                await context.Database.EnsureCreatedAsync();
                CreatedInstances.Add(context);
                
                Logger.LogInformation("Successfully created test database: {DatabaseName}", databaseName);
                return context;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create test database: {DatabaseName}", databaseName);
                await context.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// Creates an in-memory database context for fast unit tests.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type to create</typeparam>
        /// <param name="testName">Name identifier for the test</param>
        /// <returns>A configured in-memory DbContext instance</returns>
        public TContext CreateInMemoryContext<TContext>(string testName) where TContext : DbContext
        {
            var databaseName = $"InMemory_{testName}_{Guid.NewGuid():N}";
            
            var options = new DbContextOptionsBuilder<TContext>()
                .UseInMemoryDatabase(databaseName)
                .EnableSensitiveDataLogging()
                .Options;

            var context = (TContext)Activator.CreateInstance(typeof(TContext), options)!;
            CreatedInstances.Add(context);
            
            Logger.LogInformation("Created in-memory database context: {DatabaseName}", databaseName);
            return context;
        }

        /// <summary>
        /// Seeds a context with test data using the provided seeder function.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type</typeparam>
        /// <param name="context">The context to seed</param>
        /// <param name="seeder">Function to seed the context</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SeedAsync<TContext>(TContext context, Func<TContext, Task> seeder) where TContext : DbContext
        {
            try
            {
                await seeder(context);
                await context.SaveChangesAsync();
                
                Logger.LogInformation("Successfully seeded database for context: {ContextType}", typeof(TContext).Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to seed database for context: {ContextType}", typeof(TContext).Name);
                throw;
            }
        }

        /// <summary>
        /// Cleans all data from the specified context while preserving schema.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type</typeparam>
        /// <param name="context">The context to clean</param>
        /// <returns>Task representing the async operation</returns>
        public async Task CleanAsync<TContext>(TContext context) where TContext : DbContext
        {
            try
            {
                // Get all entity types from the model
                var entityTypes = context.Model.GetEntityTypes().ToList();
                
                // Delete data from all tables in reverse order to respect foreign keys
                for (int i = entityTypes.Count - 1; i >= 0; i--)
                {
                    var entityType = entityTypes[i];
                    var tableName = entityType.GetTableName();
                    var schema = entityType.GetSchema() ?? "dbo";
                    
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        var sql = $"DELETE FROM [{schema}].[{tableName}]";
                        await context.Database.ExecuteSqlRawAsync(sql);
                    }
                }
                
                Logger.LogInformation("Successfully cleaned database for context: {ContextType}", typeof(TContext).Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to clean database for context: {ContextType}", typeof(TContext).Name);
                throw;
            }
        }

        private string GenerateDatabaseName(string testName)
        {
            // Clean the test name to make it database-name friendly
            var cleanTestName = testName.Replace(" ", "_")
                                       .Replace("-", "_")
                                       .Replace(".", "_");
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            
            return $"TestDB_{cleanTestName}_{timestamp}_{uniqueId}";
        }
    }
}