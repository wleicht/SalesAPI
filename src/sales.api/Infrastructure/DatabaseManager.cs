using Microsoft.EntityFrameworkCore;
using SalesApi.Infrastructure.Data;

namespace SalesApi.Infrastructure
{
    /// <summary>
    /// Professional database manager that handles initialization, migrations, and validation.
    /// Implements enterprise-grade patterns including retry logic, proper error handling,
    /// and comprehensive logging for production environments.
    /// </summary>
    /// <remarks>
    /// Key Features:
    /// - Robust connection testing with timeout handling
    /// - Automatic migration application with extended timeouts
    /// - Database schema validation for integrity
    /// - Exponential backoff retry patterns
    /// - Comprehensive structured logging
    /// - Support for both SQL Server and in-memory databases
    /// - Professional error handling and diagnostics
    /// </remarks>
    public class DatabaseManager
    {
        private readonly SalesDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseManager> _logger;

        /// <summary>
        /// Initializes a new instance of the DatabaseManager with required dependencies.
        /// </summary>
        /// <param name="context">Entity Framework database context</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Structured logger for diagnostic information</param>
        public DatabaseManager(
            SalesDbContext context, 
            IConfiguration configuration,
            ILogger<DatabaseManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes the database with comprehensive error handling and retry logic.
        /// Supports both SQL Server and in-memory database configurations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Task representing the async initialization operation</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            const int maxRetries = 10;
            var baseRetryDelay = TimeSpan.FromSeconds(2);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "?? Database initialization attempt {Attempt}/{MaxRetries} | Type: {DatabaseType}",
                        attempt, maxRetries, GetDatabaseType());

                    if (_context.Database.IsInMemory())
                    {
                        await InitializeInMemoryDatabaseAsync(cancellationToken);
                    }
                    else
                    {
                        await InitializeSqlServerDatabaseAsync(cancellationToken);
                    }

                    await ValidateDatabaseAsync(cancellationToken);
                    
                    _logger.LogInformation("? Database initialization completed successfully after {Attempts} attempts", attempt);
                    return; // Success
                }
                catch (Exception ex) when (attempt < maxRetries && IsRetryableException(ex))
                {
                    var retryDelay = TimeSpan.FromMilliseconds(baseRetryDelay.TotalMilliseconds * Math.Pow(1.5, attempt - 1));
                    
                    _logger.LogWarning(ex,
                        "?? Database initialization failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms | Error: {Error}",
                        attempt, maxRetries, retryDelay.TotalMilliseconds, ex.Message);

                    await Task.Delay(retryDelay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "?? Database initialization failed permanently on attempt {Attempt}/{MaxRetries} | Error: {Error}",
                        attempt, maxRetries, ex.Message);
                    throw;
                }
            }

            throw new InvalidOperationException($"Failed to initialize database after {maxRetries} attempts");
        }

        /// <summary>
        /// Initializes an in-memory database for testing scenarios.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task InitializeInMemoryDatabaseAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("?? Initializing in-memory database for testing");
            
            await _context.Database.EnsureCreatedAsync(cancellationToken);
            
            _logger.LogInformation("? In-memory database created successfully");
        }

        /// <summary>
        /// Initializes SQL Server database with connection testing and migration application.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task InitializeSqlServerDatabaseAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("??? Initializing SQL Server database");

            // Test connection with timeout
            await TestConnectionWithTimeoutAsync(cancellationToken);

            // Create database if it doesn't exist
            await EnsureDatabaseExistsAsync(cancellationToken);

            // Apply pending migrations
            await ApplyPendingMigrationsAsync(cancellationToken);

            _logger.LogInformation("? SQL Server database initialized successfully");
        }

        /// <summary>
        /// Tests database connection with configurable timeout.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task TestConnectionWithTimeoutAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("?? Testing database connection...");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                var canConnect = await _context.Database.CanConnectAsync(timeoutCts.Token);
                if (!canConnect)
                {
                    throw new InvalidOperationException("Cannot establish database connection");
                }

                _logger.LogDebug("? Database connection test successful");
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                throw new TimeoutException("Database connection test timed out after 30 seconds");
            }
        }

        /// <summary>
        /// Ensures the database exists, creating it if necessary.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("?? Ensuring database exists...");
            
            var created = await _context.Database.EnsureCreatedAsync(cancellationToken);
            if (created)
            {
                _logger.LogInformation("?? Database created successfully");
            }
            else
            {
                _logger.LogDebug("?? Database already exists");
            }
        }

        /// <summary>
        /// Applies any pending database migrations with extended timeout.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task ApplyPendingMigrationsAsync(CancellationToken cancellationToken)
        {
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingMigrationsList = pendingMigrations.ToList();

            if (!pendingMigrationsList.Any())
            {
                _logger.LogInformation("?? Database is up to date, no migrations needed");
                return;
            }

            _logger.LogInformation(
                "?? Applying {Count} pending migrations: {Migrations}",
                pendingMigrationsList.Count,
                string.Join(", ", pendingMigrationsList));

            // Set extended timeout for migrations
            var originalTimeout = _context.Database.GetCommandTimeout();
            _context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));

            try
            {
                await _context.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("? Database migrations applied successfully");
            }
            finally
            {
                // Restore original timeout
                _context.Database.SetCommandTimeout(originalTimeout);
            }
        }

        /// <summary>
        /// Validates database by performing basic operations to ensure schema integrity.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task ValidateDatabaseAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("?? Validating database schema...");

            try
            {
                // Test basic table access and count operations
                var orderCount = await _context.Orders.CountAsync(cancellationToken);
                var orderItemCount = await _context.OrderItems.CountAsync(cancellationToken);

                _logger.LogInformation(
                    "?? Database validation complete | Orders: {OrderCount} | OrderItems: {OrderItemCount}",
                    orderCount, orderItemCount);

                // Additional validation: ensure DbSets are queryable
                var canQueryOrders = await _context.Orders.Take(1).AnyAsync(cancellationToken);
                var canQueryOrderItems = await _context.OrderItems.Take(1).AnyAsync(cancellationToken);

                _logger.LogDebug("? Database schema validation passed - all tables accessible");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Database schema validation failed: {Error}", ex.Message);
                throw new InvalidOperationException("Database schema validation failed", ex);
            }
        }

        /// <summary>
        /// Determines if an exception is retryable (transient failure).
        /// Enhanced with EF Core specific error detection.
        /// </summary>
        /// <param name="ex">Exception to evaluate</param>
        /// <returns>True if the exception indicates a transient failure</returns>
        private static bool IsRetryableException(Exception ex)
        {
            return ex switch
            {
                TimeoutException => true,
                OperationCanceledException => false, // Don't retry cancellations
                InvalidOperationException ioe when ioe.Message.Contains("connection") => true,
                InvalidOperationException ioe when ioe.Message.Contains("timeout") => true,
                TypeLoadException => false, // Don't retry type loading issues - these are configuration problems
                System.IO.IOException => true, // Network-related IO issues
                Microsoft.Data.SqlClient.SqlException sqlEx => IsRetryableSqlException(sqlEx),
                System.Net.Sockets.SocketException => true, // Network connectivity issues
                Microsoft.EntityFrameworkCore.DbUpdateException => false, // These are usually data issues, not transient
                _ => false
            };
        }

        /// <summary>
        /// Determines if a SQL exception is retryable based on error codes.
        /// </summary>
        /// <param name="sqlEx">SQL exception to evaluate</param>
        /// <returns>True if the SQL exception indicates a transient failure</returns>
        private static bool IsRetryableSqlException(Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            // Common transient error codes
            var retryableErrorCodes = new[]
            {
                2,     // Timeout
                53,    // Network path not found
                121,   // Semaphore timeout
                1205,  // Deadlock
                1222,  // Lock request timeout
                49918, // Cannot process request
                49919, // Cannot process create or update request
                49920, // Cannot process request
                4060,  // Cannot open database requested by the login
                40197, // Service has encountered an error processing your request
                40501, // Service is currently busy
                40613, // Database on server is not currently available
                42108, // Can not connect to the SQL pool
                42109  // SQL pool is warming up
            };

            return retryableErrorCodes.Contains(sqlEx.Number);
        }

        /// <summary>
        /// Gets a user-friendly description of the database type being used.
        /// </summary>
        /// <returns>String description of database type</returns>
        private string GetDatabaseType()
        {
            return _context.Database.IsInMemory() ? "In-Memory" : "SQL Server";
        }
    }
}