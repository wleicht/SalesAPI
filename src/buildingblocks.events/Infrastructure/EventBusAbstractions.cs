using BuildingBlocks.Events.Domain;

namespace BuildingBlocks.Events.Infrastructure
{
    /// <summary>
    /// Defines the contract for publishing domain events to the distributed event bus infrastructure.
    /// Provides a technology-agnostic abstraction over the underlying messaging system (RabbitMQ, Azure Service Bus, etc.).
    /// Supports both individual event publishing and batch operations for high-throughput scenarios.
    /// </summary>
    /// <remarks>
    /// The IEventPublisher interface serves as the primary integration point for services that need
    /// to publish business events to other services in the microservices architecture. Key benefits:
    /// 
    /// Abstraction Benefits:
    /// - Technology Independence: Switch messaging providers without changing business code
    /// - Testability: Mock implementations for unit and integration testing
    /// - Consistency: Standardized event publishing across all services
    /// - Error Handling: Consistent exception handling and retry policies
    /// 
    /// Implementation Patterns:
    /// - Dependency Injection: Register implementations in DI container
    /// - Configuration-Based: Select implementations based on environment settings
    /// - Fallback Support: Graceful degradation when messaging infrastructure is unavailable
    /// - Monitoring Integration: Built-in support for observability and metrics
    /// 
    /// Usage Guidelines:
    /// - Always publish events after successful business operations
    /// - Use correlation IDs for end-to-end tracing
    /// - Consider event ordering requirements for related events
    /// - Implement proper error handling and compensation logic
    /// 
    /// The interface is designed to be simple yet powerful, supporting both
    /// simple fire-and-forget scenarios and complex transactional outbox patterns.
    /// </remarks>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes a single domain event to the distributed event bus asynchronously.
        /// Provides reliable delivery guarantees and automatic retry capabilities for failed operations.
        /// </summary>
        /// <typeparam name="TEvent">
        /// The specific type of domain event to publish. Must inherit from DomainEvent base class
        /// to ensure consistent event metadata and infrastructure support.
        /// </typeparam>
        /// <param name="domainEvent">
        /// The domain event instance containing business data and metadata to publish.
        /// Must be fully populated with all required properties before publishing.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token that allows graceful cancellation of the publishing operation.
        /// Supports timeout scenarios and application shutdown procedures.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous publishing operation. Completion indicates
        /// the event has been successfully accepted by the message broker infrastructure.
        /// </returns>
        /// <remarks>
        /// Publishing Guarantees:
        /// - At-Least-Once Delivery: Events are guaranteed to be delivered to consumers
        /// - Ordering: Single-producer ordering is maintained where supported by infrastructure
        /// - Durability: Events survive broker restarts when using persistent queues
        /// - Acknowledgment: Task completion confirms broker acceptance
        /// 
        /// Error Scenarios:
        /// - Network failures: Automatic retry with exponential backoff
        /// - Broker unavailability: Circuit breaker pattern for graceful degradation  
        /// - Serialization errors: Immediate failure with detailed error information
        /// - Authentication failures: Immediate failure requiring configuration review
        /// 
        /// Performance Considerations:
        /// - Async/await: Non-blocking operation suitable for high-throughput scenarios
        /// - Connection pooling: Efficient resource utilization for frequent publishing
        /// - Batching: Consider PublishManyAsync for multiple events
        /// - Memory usage: Events are serialized and released after successful publishing
        /// 
        /// Best Practices:
        /// - Publish events after successful business transactions
        /// - Include correlation IDs for distributed tracing
        /// - Use structured logging for event publishing operations
        /// - Implement proper exception handling and compensation logic
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when domainEvent parameter is null
        /// </exception>
        /// <exception cref="EventSerializationException">
        /// Thrown when the event cannot be serialized for transport
        /// </exception>
        /// <exception cref="EventBusConnectionException">
        /// Thrown when connection to the message broker fails
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled via the cancellation token
        /// </exception>
        Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : DomainEvent;

        /// <summary>
        /// Publishes multiple domain events to the distributed event bus in an efficient batch operation.
        /// Optimized for high-throughput scenarios while maintaining ordering and consistency guarantees.
        /// </summary>
        /// <typeparam name="TEvent">
        /// The specific type of domain events to publish. All events in the collection must be
        /// of the same type and inherit from DomainEvent base class.
        /// </typeparam>
        /// <param name="domainEvents">
        /// Collection of domain event instances to publish in batch. Cannot be null but may be empty.
        /// Events are processed in iteration order when ordering is significant.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token that allows graceful cancellation of the entire batch operation.
        /// Partial completion behavior depends on the underlying implementation.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous batch publishing operation. Completion indicates
        /// all events have been successfully accepted by the message broker infrastructure.
        /// </returns>
        /// <remarks>
        /// Batch Processing Benefits:
        /// - Improved Throughput: Reduced per-event overhead for high-volume scenarios
        /// - Atomic Operations: All-or-nothing semantics where supported by infrastructure
        /// - Connection Efficiency: Single connection used for multiple events
        /// - Reduced Latency: Fewer round-trips to message broker
        /// 
        /// Ordering Guarantees:
        /// - Sequential Processing: Events are published in collection iteration order
        /// - FIFO Delivery: First-in-first-out ordering maintained where supported
        /// - Partition Affinity: Related events routed to same partition for ordering
        /// - Cross-Event Dependencies: Dependent events processed in correct sequence
        /// 
        /// Error Handling Strategies:
        /// - All-or-Nothing: Entire batch fails if any event fails (transactional)
        /// - Best-Effort: Successful events are published, failures are reported
        /// - Retry Logic: Failed batches are retried with exponential backoff
        /// - Partial Success: Implementation-specific handling of partial failures
        /// 
        /// Performance Optimization:
        /// - Batch Size Limits: Consider message broker limits and memory constraints
        /// - Parallel Processing: Some implementations may process events in parallel
        /// - Memory Management: Large batches may require streaming or chunking
        /// - Connection Pooling: Efficient resource utilization across batch operations
        /// 
        /// Use Cases:
        /// - Bulk Data Processing: Large-scale data migration or synchronization
        /// - Event Replay: Republishing historical events for recovery scenarios
        /// - Analytics Ingestion: High-volume event streams for business intelligence
        /// - Integration Scenarios: Batch synchronization with external systems
        /// 
        /// When to Use Single vs. Batch:
        /// - Single: Real-time processing, immediate feedback, simple workflows
        /// - Batch: High volume, performance optimization, bulk operations
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when domainEvents collection is null
        /// </exception>
        /// <exception cref="EventSerializationException">
        /// Thrown when any event in the batch cannot be serialized for transport
        /// </exception>
        /// <exception cref="EventBusConnectionException">
        /// Thrown when connection to the message broker fails
        /// </exception>
        /// <exception cref="BatchSizeExceededException">
        /// Thrown when the batch size exceeds message broker or implementation limits
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled via the cancellation token
        /// </exception>
        Task PublishManyAsync<TEvent>(IEnumerable<TEvent> domainEvents, CancellationToken cancellationToken = default)
            where TEvent : DomainEvent;
    }

    /// <summary>
    /// Defines the contract for handling incoming domain events from the distributed event bus.
    /// Implement this interface to create event consumers that process business events published by other services.
    /// Supports dependency injection and automatic registration patterns for scalable event processing.
    /// </summary>
    /// <typeparam name="TEvent">
    /// The specific type of domain event this handler processes. Must inherit from DomainEvent
    /// and should represent a specific business event type for focused processing logic.
    /// </typeparam>
    /// <remarks>
    /// The IEventHandler interface enables the implementation of event-driven business logic
    /// in a microservices architecture. Key design principles:
    /// 
    /// Single Responsibility:
    /// - Each handler processes exactly one event type
    /// - Focused business logic with clear boundaries
    /// - Easier testing and maintenance
    /// - Independent deployment and scaling
    /// 
    /// Integration Patterns:
    /// - Message Bus Integration: Automatic registration with messaging infrastructure
    /// - Dependency Injection: Full DI container support for handler dependencies
    /// - Error Handling: Built-in retry and dead letter queue support
    /// - Monitoring: Automatic metrics and logging integration
    /// 
    /// Implementation Guidelines:
    /// - Idempotent Processing: Handle duplicate events gracefully
    /// - Error Recovery: Implement proper compensation and rollback logic
    /// - Performance: Optimize for throughput while maintaining reliability
    /// - State Management: Use appropriate persistence patterns for handler state
    /// 
    /// Handler Lifecycle:
    /// - Registration: Automatically discovered and registered during application startup
    /// - Scaling: Multiple instances can process events in parallel
    /// - Deployment: Independent versioning and deployment capabilities
    /// - Monitoring: Built-in observability for processing metrics and errors
    /// 
    /// The interface design promotes loose coupling between services while enabling
    /// reliable, scalable event processing across the distributed system.
    /// </remarks>
    public interface IEventHandler<in TEvent>
        where TEvent : DomainEvent
    {
        /// <summary>
        /// Processes an incoming domain event with full error handling and transaction support.
        /// Implements the business logic required to handle the specific event type in the context
        /// of this service's domain responsibilities.
        /// </summary>
        /// <param name="domainEvent">
        /// The domain event instance containing business data and metadata to process.
        /// Guaranteed to be non-null and properly deserialized by the messaging infrastructure.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token that allows graceful cancellation of the processing operation.
        /// Should be honored for long-running operations and external service calls.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous event processing operation. Successful completion
        /// indicates the event has been fully processed and any resulting state changes committed.
        /// </returns>
        /// <remarks>
        /// Processing Requirements:
        /// - Idempotency: Must handle duplicate events gracefully without side effects
        /// - Atomicity: Either complete successfully or fail without partial state changes
        /// - Error Handling: Provide meaningful error information for retry/DLQ scenarios
        /// - Performance: Process events efficiently to maintain system throughput
        /// 
        /// Transaction Management:
        /// - Database Operations: Use appropriate transaction scopes for data consistency
        /// - External Calls: Implement compensation patterns for external service failures
        /// - State Persistence: Ensure handler state is properly persisted
        /// - Rollback Logic: Clean up resources and state on processing failures
        /// 
        /// Idempotency Patterns:
        /// - Unique Processing: Track processed event IDs to prevent reprocessing
        /// - State Checking: Verify current state before applying changes
        /// - Natural Idempotency: Design operations to be naturally idempotent
        /// - Compensation Actions: Implement reversible operations where possible
        /// 
        /// Error Recovery:
        /// - Transient Failures: Allow automatic retry for temporary issues
        /// - Permanent Failures: Fail fast for unrecoverable errors
        /// - Dead Letter Handling: Support manual intervention for failed events
        /// - Monitoring Integration: Provide detailed error information for alerting
        /// 
        /// Performance Considerations:
        /// - Async Operations: Use async/await for non-blocking processing
        /// - Resource Management: Efficiently manage database connections and external resources
        /// - Batching: Consider batch processing for high-volume scenarios
        /// - Caching: Cache frequently accessed data to improve performance
        /// 
        /// Best Practices:
        /// - Structured Logging: Include correlation IDs and event details in logs
        /// - Metrics Collection: Track processing times and success/failure rates
        /// - Health Monitoring: Implement health checks for handler dependencies
        /// - Graceful Degradation: Handle dependency failures appropriately
        /// </remarks>
        /// <exception cref="BusinessLogicException">
        /// Thrown when business rules prevent successful event processing
        /// </exception>
        /// <exception cref="DataConsistencyException">
        /// Thrown when data integrity constraints would be violated
        /// </exception>
        /// <exception cref="ExternalServiceException">
        /// Thrown when required external services are unavailable
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled via the cancellation token
        /// </exception>
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Configuration settings for event bus infrastructure including connection parameters,
    /// messaging topology, and operational policies. Supports multiple messaging providers
    /// and deployment scenarios through comprehensive configuration options.
    /// </summary>
    /// <remarks>
    /// This configuration class centralizes all event bus settings to support:
    /// 
    /// Multi-Environment Support:
    /// - Development: Local RabbitMQ with relaxed durability settings
    /// - Staging: Production-like configuration for realistic testing
    /// - Production: High-availability setup with durability and monitoring
    /// - Testing: In-memory or containerized brokers for automated testing
    /// 
    /// Provider Flexibility:
    /// - RabbitMQ: Full configuration support including exchanges and queues
    /// - Azure Service Bus: Topic and subscription configuration
    /// - Amazon SQS/SNS: Queue and topic configuration
    /// - Apache Kafka: Producer and consumer configuration
    /// 
    /// Operational Policies:
    /// - Retry Strategies: Configurable retry counts and backoff algorithms
    /// - Timeout Management: Processing and connection timeout settings
    /// - Durability Options: Message and queue persistence configuration
    /// - Security Settings: Authentication and encryption parameters
    /// 
    /// The configuration is designed to be environment-specific while maintaining
    /// compatibility across different messaging infrastructure providers.
    /// </remarks>
    public class EventBusConfiguration
    {
        /// <summary>
        /// Connection string for the message broker infrastructure (e.g., RabbitMQ, Azure Service Bus).
        /// Contains all necessary connection parameters including authentication credentials and network endpoints.
        /// </summary>
        /// <value>Complete connection string with protocol, host, port, credentials, and options</value>
        /// <remarks>
        /// Connection String Formats:
        /// - RabbitMQ: "amqp://username:password@hostname:port/vhost"
        /// - Azure Service Bus: "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=..."
        /// - Amazon SQS: "https://sqs.region.amazonaws.com/account-id"
        /// - Apache Kafka: "localhost:9092,localhost:9093"
        /// 
        /// Security Considerations:
        /// - Store in secure configuration (Azure Key Vault, AWS Secrets Manager)
        /// - Use environment variables for sensitive credentials
        /// - Implement connection string encryption for configuration files
        /// - Regular credential rotation and access auditing
        /// 
        /// High Availability:
        /// - Multiple broker endpoints for failover scenarios
        /// - Load balancing configuration for distributed brokers
        /// - Connection pooling parameters for performance
        /// - Health monitoring endpoints for broker status
        /// </remarks>
        public required string ConnectionString { get; set; }

        /// <summary>
        /// Name of the message exchange for event publishing and routing.
        /// Defines the logical namespace for organizing related events and routing rules.
        /// </summary>
        /// <value>Exchange name, defaults to "sales.events" for business domain organization</value>
        /// <remarks>
        /// Exchange Naming Strategies:
        /// - Domain-Based: Separate exchanges per business domain (sales.events, inventory.events)
        /// - Service-Based: Individual exchanges per microservice
        /// - Environment-Based: Environment-specific exchange prefixes
        /// - Global: Single exchange with routing key discrimination
        /// 
        /// Routing Implications:
        /// - Topic exchanges enable complex routing patterns
        /// - Direct exchanges provide simple point-to-point messaging
        /// - Fanout exchanges broadcast to all bound queues
        /// - Headers exchanges route based on message headers
        /// 
        /// Operational Considerations:
        /// - Exchange durability for message persistence
        /// - Auto-deletion policies for temporary exchanges
        /// - Access control and security permissions
        /// - Monitoring and alerting configuration
        /// </remarks>
        public string ExchangeName { get; set; } = "sales.events";

        /// <summary>
        /// Type of message exchange controlling routing behavior and delivery patterns.
        /// Determines how messages are routed from publishers to consumer queues.
        /// </summary>
        /// <value>Exchange type, defaults to "topic" for flexible routing patterns</value>
        /// <remarks>
        /// Exchange Types and Use Cases:
        /// - Topic: Flexible routing using routing key patterns (e.g., "order.confirmed", "inventory.updated")
        /// - Direct: Simple routing using exact routing key matches
        /// - Fanout: Broadcast messages to all bound queues regardless of routing key
        /// - Headers: Route based on message header attributes rather than routing keys
        /// 
        /// Topic Exchange Benefits:
        /// - Hierarchical routing keys (domain.entity.action)
        /// - Wildcard subscriptions (* for single word, # for multiple words)
        /// - Service-specific routing without tight coupling
        /// - Easy addition of new consumers without publisher changes
        /// 
        /// Selection Criteria:
        /// - Complexity: Topic for complex routing, direct for simple scenarios
        /// - Performance: Direct exchanges have lower overhead
        /// - Flexibility: Topic exchanges support evolving routing requirements
        /// - Monitoring: Topic exchanges provide better observability
        /// </remarks>
        public string ExchangeType { get; set; } = "topic";

        /// <summary>
        /// Maximum number of retry attempts for failed message processing operations.
        /// Balances system resilience with resource utilization and processing latency.
        /// </summary>
        /// <value>Retry count, defaults to 3 attempts for reasonable resilience without excessive delays</value>
        /// <remarks>
        /// Retry Strategy Considerations:
        /// - Transient Failures: Network timeouts, temporary service unavailability
        /// - Poison Messages: Malformed or unprocessable messages that always fail
        /// - Resource Constraints: Memory or connection pool exhaustion
        /// - Dead Letter Queues: Final destination for messages exceeding retry limits
        /// 
        /// Retry Algorithm Options:
        /// - Immediate: Retry without delay (suitable for transient network issues)
        /// - Fixed Delay: Consistent interval between retries
        /// - Exponential Backoff: Increasing delays to prevent resource exhaustion
        /// - Jittered Backoff: Random variation to prevent thundering herd effects
        /// 
        /// Operational Impact:
        /// - Processing Latency: Higher retry counts increase worst-case processing time
        /// - Resource Usage: Failed messages consume processing resources during retries
        /// - Dead Letter Volume: Lower retry counts increase dead letter queue traffic
        /// - System Stability: Higher retry counts improve resilience to transient failures
        /// 
        /// Monitoring Requirements:
        /// - Retry rate metrics for detecting systemic issues
        /// - Dead letter queue depth monitoring
        /// - Processing time distribution analysis
        /// - Error rate trending and alerting
        /// </remarks>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Maximum time allowed for individual message processing operations before timeout.
        /// Prevents resource exhaustion from long-running or hanging message processors.
        /// </summary>
        /// <value>Timeout duration, defaults to 5 minutes for typical business processing scenarios</value>
        /// <remarks>
        /// Timeout Considerations:
        /// - Processing Complexity: Simple CRUD operations vs. complex business workflows
        /// - External Dependencies: Database queries, API calls, file system operations
        /// - Resource Availability: CPU, memory, and I/O constraints under load
        /// - SLA Requirements: Customer-facing vs. internal processing expectations
        /// 
        /// Timeout Strategies:
        /// - Conservative: Longer timeouts to accommodate slow operations
        /// - Aggressive: Shorter timeouts to maintain system responsiveness
        /// - Adaptive: Dynamic timeouts based on historical processing times
        /// - Hierarchical: Different timeouts for different message types
        /// 
        /// Failure Handling:
        /// - Graceful Cancellation: Support for CancellationToken in message handlers
        /// - Resource Cleanup: Proper disposal of database connections and external resources
        /// - Partial Work: Handling scenarios where work is partially completed
        /// - Compensation: Rollback mechanisms for timeout scenarios
        /// 
        /// Performance Implications:
        /// - Throughput: Shorter timeouts enable faster failure detection and recovery
        /// - Latency: Longer timeouts may delay error detection and user feedback
        /// - Resource Utilization: Optimal timeouts prevent resource leaks and blocking
        /// - Scalability: Appropriate timeouts support higher concurrent message processing
        /// </remarks>
        public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Indicates whether to use durable message infrastructure that survives broker restarts.
        /// Critical for production environments requiring guaranteed message delivery and system reliability.
        /// </summary>
        /// <value>Durability flag, defaults to true for production-ready message persistence</value>
        /// <remarks>
        /// Durability Components:
        /// - Durable Exchanges: Exchange definitions survive broker restarts
        /// - Durable Queues: Queue definitions and contents persist through restarts
        /// - Persistent Messages: Individual messages are written to disk
        /// - Transaction Support: Atomic commit/rollback for message operations
        /// 
        /// Performance Trade-offs:
        /// - Throughput: Durable operations have higher latency due to disk I/O
        /// - Memory Usage: Non-durable queues use only memory for better performance
        /// - Recovery Time: Durable systems take longer to restart but preserve data
        /// - Storage Requirements: Persistent messages consume disk space
        /// 
        /// Environment Strategies:
        /// - Production: Always use durability for data integrity
        /// - Staging: Use durability to match production behavior
        /// - Development: Consider non-durable for faster iteration
        /// - Testing: Non-durable for speed, durable for integration tests
        /// 
        /// Business Impact:
        /// - Data Loss Prevention: Critical for financial and audit-sensitive operations
        /// - System Reliability: Enables graceful recovery from infrastructure failures
        /// - Compliance Requirements: May be required for regulatory compliance
        /// - Customer Experience: Prevents lost orders and processing errors
        /// 
        /// Configuration Guidelines:
        /// - Always enable in production unless explicitly justified
        /// - Consider performance testing with durability enabled
        /// - Plan for increased storage and backup requirements
        /// - Monitor disk usage and implement retention policies
        /// </remarks>
        public bool Durable { get; set; } = true;
    }
}