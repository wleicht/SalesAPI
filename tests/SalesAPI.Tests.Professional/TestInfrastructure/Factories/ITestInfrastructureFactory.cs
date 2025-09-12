using Microsoft.Extensions.Logging;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Factories
{
    /// <summary>
    /// Unified interface for test infrastructure factories
    /// </summary>
    public interface ITestInfrastructureFactory<T> : IDisposable
    {
        T Create(string testName);
        Task<T> CreateAsync(string testName);
    }

    /// <summary>
    /// Base implementation with common patterns
    /// </summary>
    public abstract class TestInfrastructureFactoryBase<T> : ITestInfrastructureFactory<T>
    {
        protected readonly ILogger Logger;
        protected readonly List<T> CreatedInstances = new();
        private bool _disposed = false;

        protected TestInfrastructureFactoryBase(ILogger logger)
        {
            Logger = logger;
        }

        public virtual T Create(string testName)
        {
            var instance = CreateInternal(testName);
            CreatedInstances.Add(instance);
            Logger.LogInformation("Created {InstanceType} for test: {TestName}", typeof(T).Name, testName);
            return instance;
        }

        public virtual async Task<T> CreateAsync(string testName)
        {
            var instance = await CreateInternalAsync(testName);
            CreatedInstances.Add(instance);
            Logger.LogInformation("Created {InstanceType} for test: {TestName}", typeof(T).Name, testName);
            return instance;
        }

        protected abstract T CreateInternal(string testName);
        protected virtual Task<T> CreateInternalAsync(string testName) => Task.FromResult(CreateInternal(testName));

        public virtual void Dispose()
        {
            if (_disposed) return;

            Logger.LogInformation("Disposing {FactoryType} with {InstanceCount} created instances", 
                GetType().Name, CreatedInstances.Count);

            foreach (var instance in CreatedInstances)
            {
                if (instance is IDisposable disposable)
                    disposable.Dispose();
                else if (instance is IAsyncDisposable asyncDisposable)
                    asyncDisposable.DisposeAsync().AsTask().Wait();
            }

            CreatedInstances.Clear();
            _disposed = true;
        }
    }
}