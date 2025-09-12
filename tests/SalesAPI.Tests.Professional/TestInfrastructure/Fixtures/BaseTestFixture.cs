using Microsoft.Extensions.Logging;
using Xunit;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Fixtures
{
    /// <summary>
    /// Base fixture to eliminate duplication in fixture implementations
    /// </summary>
    public abstract class BaseTestFixture : IAsyncLifetime
    {
        protected readonly ILogger Logger;
        protected readonly string TestName;
        protected readonly ILoggerFactory LoggerFactory;
        private bool _disposed = false;

        protected BaseTestFixture(string? testPrefix = null)
        {
            TestName = $"{testPrefix ?? GetType().Name}_{Guid.NewGuid():N}";
            LoggerFactory = new LoggerFactory();
            Logger = LoggerFactory.CreateLogger(GetType());
        }

        public async Task InitializeAsync()
        {
            Logger.LogInformation("Initializing {FixtureName} for test: {TestName}", GetType().Name, TestName);

            try
            {
                await InitializeInternalAsync();
                Logger.LogInformation("{FixtureName} initialized successfully for test: {TestName}", GetType().Name, TestName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize {FixtureName} for test: {TestName}", GetType().Name, TestName);
                await DisposeAsync();
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            if (_disposed) return;

            Logger.LogInformation("Disposing {FixtureName} for test: {TestName}", GetType().Name, TestName);

            try
            {
                await DisposeInternalAsync();
                LoggerFactory.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during {FixtureName} disposal for test: {TestName}", GetType().Name, TestName);
            }
            finally
            {
                _disposed = true;
                Logger.LogInformation("{FixtureName} disposed successfully for test: {TestName}", GetType().Name, TestName);
            }
        }

        protected abstract Task InitializeInternalAsync();
        protected abstract Task DisposeInternalAsync();

        protected T ThrowIfNotInitialized<T>(T? value, string componentName) where T : class
        {
            return value ?? throw new InvalidOperationException($"{componentName} not initialized. Call InitializeAsync first.");
        }
    }
}