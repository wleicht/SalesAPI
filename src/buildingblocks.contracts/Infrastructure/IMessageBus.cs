using BuildingBlocks.Events.Domain;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace BuildingBlocks.Infrastructure.Messaging
{
    /// <summary>
    /// Provides a high-level abstraction for message bus operations across different messaging providers.
    /// Enables seamless integration with RabbitMQ, Azure Service Bus, AWS SQS, Apache Kafka, and other
    /// messaging systems without coupling business logic to specific infrastructure implementations.
    /// </summary>
    /// <remarks>
    /// The IMessageBus interface represents a strategic abstraction that enables:
    /// 
    /// Technology Independence:
    /// - Switch between messaging providers without code changes
    /// - Support multiple providers simultaneously in hybrid scenarios
    /// - Isolate business logic from infrastructure concerns
    /// - Enable provider-specific optimizations behind consistent interface
    /// 
    /// Architectural Benefits:
    /// - Clean Architecture compliance with dependency inversion
    /// - Hexagonal Architecture support for ports and adapters pattern
    /// - Domain-driven design alignment with infrastructure abstraction
    /// - Microservices architecture support for technology diversity
    /// 
    /// Enterprise Features:
    /// - Comprehensive error handling and retry policies
    /// - Built-in observability and monitoring capabilities
    /// - Security integration for authentication and authorization
    /// - Performance optimization through connection pooling and batching
    /// 
    /// Implementation Strategy:
    /// - Provider-specific implementations registered via dependency injection
    /// - Configuration-driven provider selection for environment flexibility
    /// - Fallback mechanisms for high availability scenarios
    /// - Comprehensive testing support through mock implementations
    /// 
    /// The interface design prioritizes simplicity and consistency while providing
    /// the flexibility required for enterprise messaging scenarios.
    /// </remarks>
    public interface IMessageBus
    {
        /// <summary>
        /// Publishes a domain event to the message bus for consumption by interested subscribers.
        /// Provides reliable delivery guarantees with automatic retry capabilities and comprehensive
        /// error handling for production-ready event-driven architecture implementations.
        /// </summary>
        /// <typeparam name="T">
        /// The specific type of domain event to publish. Must implement IDomainEvent interface
        /// to ensure consistent event metadata and infrastructure compatibility.
        /// </typeparam>
        /// <param name="message">
        /// The domain event instance containing business data and correlation metadata.
        /// Must be fully populated with all required properties before publishing.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token enabling graceful operation cancellation for timeout scenarios
        /// and application shutdown procedures.
        /// </param>
        /// <returns>
        /// Task representing the asynchronous publishing operation. Successful completion
        /// indicates the message has been accepted by the messaging infrastructure with
        /// appropriate durability guarantees based on configuration.
        /// </returns>
        /// <remarks>
        /// Publishing Guarantees and Behavior:
        /// 
        /// Reliability Features:
        /// - At-least-once delivery semantics with duplicate detection support
        /// - Automatic retry with exponential backoff for transient failures
        /// - Dead letter queue integration for permanent failure handling
        /// - Transaction support for atomic operations where available
        /// 
        /// Performance Characteristics:
        /// - Asynchronous operation with non-blocking execution
        /// - Connection pooling for efficient resource utilization
        /// - Batch processing optimizations for high-throughput scenarios
        /// - Memory-efficient serialization and message handling
        /// 
        /// Error Handling Strategy:
        /// - Transient errors: Automatic retry with configurable policies
        /// - Permanent errors: Immediate failure with detailed error information
        /// - Network failures: Connection resilience and failover support
        /// - Serialization errors: Fast failure with comprehensive diagnostics
        /// 
        /// Observability Integration:
        /// - Structured logging with correlation ID propagation
        /// - Metrics collection for throughput and latency monitoring
        /// - Distributed tracing support for end-to-end visibility
        /// - Health monitoring for messaging infrastructure status
        /// 
        /// Security Considerations:
        /// - Message encryption for sensitive data protection
        /// - Authentication and authorization for publisher verification
        /// - Audit logging for compliance and security monitoring
        /// - Rate limiting to prevent abuse and resource exhaustion
        /// 
        /// Best Practices:
        /// - Always publish events after successful business operations
        /// - Include correlation IDs for distributed request tracing
        /// - Use appropriate event granularity for system performance
        /// - Implement idempotent consumers for reliable processing
        /// - Monitor publishing patterns for capacity planning
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the message parameter is null
        /// </exception>
        /// <exception cref="MessageSerializationException">
        /// Thrown when the message cannot be serialized for transport
        /// </exception>
        /// <exception cref="MessageBusConnectionException">
        /// Thrown when connection to the messaging infrastructure fails
        /// </exception>
        /// <exception cref="MessagePublishingException">
        /// Thrown when message publishing fails due to infrastructure issues
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled via the cancellation token
        /// </exception>
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) 
            where T : IDomainEvent;

        /// <summary>
        /// Publishes multiple domain events as a batch operation for improved throughput and efficiency.
        /// Optimized for high-volume scenarios while maintaining ordering guarantees and transactional
        /// semantics where supported by the underlying messaging infrastructure.
        /// </summary>
        /// <typeparam name="T">
        /// The specific type of domain events to publish. All events in the collection must be
        /// of the same type and implement IDomainEvent interface.
        /// </typeparam>
        /// <param name="messages">
        /// Collection of domain event instances to publish in batch. Events are processed
        /// in iteration order when ordering is significant for business logic.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token enabling graceful cancellation of the entire batch operation.
        /// Behavior for partial completion depends on the underlying implementation.
        /// </param>
        /// <returns>
        /// Task representing the asynchronous batch publishing operation. Successful completion
        /// indicates all events have been accepted by the messaging infrastructure according
        /// to the configured durability and consistency guarantees.
        /// </returns>
        /// <remarks>
        /// Batch Processing Advantages:
        /// 
        /// Performance Optimization:
        /// - Reduced per-message overhead for high-volume publishing
        /// - Connection efficiency through single connection usage
        /// - Network optimization with fewer round-trips to message broker
        /// - Memory management optimization for large event collections
        /// 
        /// Transactional Semantics:
        /// - All-or-nothing batch processing where supported by infrastructure
        /// - Atomic commit/rollback for transactional messaging systems
        /// - Partial success handling with detailed error reporting
        /// - Compensation logic support for distributed transaction scenarios
        /// 
        /// Ordering Guarantees:
        /// - Sequential processing maintains event ordering within batch
        /// - FIFO delivery semantics where supported by messaging provider
        /// - Partition affinity for related events requiring strict ordering
        /// - Cross-event dependency handling for complex business workflows
        /// 
        /// Error Handling Strategies:
        /// - Configurable batch failure policies (fail-fast vs. best-effort)
        /// - Individual event error isolation within batch operations
        /// - Retry logic with exponential backoff for transient failures
        /// - Dead letter queue integration for failed batch operations
        /// 
        /// Scalability Considerations:
        /// - Batch size limits based on message broker constraints
        /// - Memory usage optimization for large event collections
        /// - Streaming support for very large batches
        /// - Parallel processing where ordering is not required
        /// 
        /// Use Case Scenarios:
        /// - Bulk data synchronization between services
        /// - High-frequency event publishing optimization
        /// - Event replay and historical data republishing
        /// - Analytics and reporting event stream generation
        /// 
        /// Implementation Guidelines:
        /// - Consider batch size limits and memory constraints
        /// - Implement appropriate error handling for batch scenarios
        /// - Use streaming for very large collections to prevent memory issues
        /// - Monitor batch processing metrics for performance optimization
        /// - Provide clear error reporting for debugging batch failures
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the messages collection is null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the messages collection is empty or contains null elements
        /// </exception>
        /// <exception cref="BatchSizeExceededException">
        /// Thrown when the batch size exceeds messaging infrastructure limits
        /// </exception>
        /// <exception cref="MessageSerializationException">
        /// Thrown when any message in the batch cannot be serialized
        /// </exception>
        /// <exception cref="MessageBusConnectionException">
        /// Thrown when connection to the messaging infrastructure fails
        /// </exception>
        /// <exception cref="BatchPublishingException">
        /// Thrown when batch publishing fails with details about partial success
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled via the cancellation token
        /// </exception>
        Task PublishManyAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) 
            where T : IDomainEvent;

        /// <summary>
        /// Establishes a subscription to receive and process specific types of domain events.
        /// Enables event-driven architecture by connecting event publishers with interested
        /// consumers through flexible subscription patterns and reliable delivery mechanisms.
        /// </summary>
        /// <typeparam name="T">
        /// The specific type of domain event to subscribe to. Must implement IDomainEvent
        /// interface for consistent event processing infrastructure.
        /// </typeparam>
        /// <param name="handler">
        /// Asynchronous handler function that processes received events. Should implement
        /// idempotent processing logic and appropriate error handling for production use.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation token for graceful subscription shutdown during application
        /// termination or configuration changes.
        /// </param>
        /// <returns>
        /// Task representing the subscription establishment operation. Completion indicates
        /// the subscription is active and ready to receive events.
        /// </returns>
        /// <remarks>
        /// Subscription Management:
        /// 
        /// Event Delivery Semantics:
        /// - At-least-once delivery with duplicate detection capabilities
        /// - Ordered delivery where supported by messaging infrastructure
        /// - Load balancing across multiple consumer instances
        /// - Dead letter queue integration for failed message processing
        /// 
        /// Handler Requirements:
        /// - Idempotent processing to handle duplicate events gracefully
        /// - Appropriate error handling with retry and compensation logic
        /// - Efficient processing to maintain system throughput
        /// - Proper resource management and cleanup procedures
        /// 
        /// Scaling and Performance:
        /// - Concurrent message processing for improved throughput
        /// - Automatic scaling based on queue depth and processing latency
        /// - Connection pooling for efficient resource utilization
        /// - Backpressure handling for high-volume event streams
        /// 
        /// Error Recovery:
        /// - Automatic retry with exponential backoff for transient failures
        /// - Circuit breaker patterns for downstream service protection
        /// - Dead letter queue processing for manual intervention scenarios
        /// - Comprehensive error logging and monitoring integration
        /// 
        /// Observability Features:
        /// - Processing metrics for throughput and latency monitoring
        /// - Error rate tracking and alerting capabilities
        /// - Correlation ID propagation for distributed tracing
        /// - Health monitoring for subscription status and performance
        /// 
        /// Subscription Lifecycle:
        /// - Automatic registration during application startup
        /// - Graceful shutdown during application termination
        /// - Dynamic subscription management for configuration changes
        /// - Health monitoring and automatic recovery capabilities
        /// 
        /// Best Practices:
        /// - Implement idempotent message processing logic
        /// - Use correlation IDs for end-to-end request tracing
        /// - Monitor processing times and implement timeouts
        /// - Implement proper error handling and compensation
        /// - Use structured logging for operational visibility
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the handler parameter is null
        /// </exception>
        /// <exception cref="SubscriptionException">
        /// Thrown when subscription establishment fails
        /// </exception>
        /// <exception cref="MessageBusConnectionException">
        /// Thrown when connection to the messaging infrastructure fails
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is cancelled via the cancellation token
        /// </exception>
        Task SubscribeAsync<T>(Func<T, Task> handler, CancellationToken cancellationToken = default) 
            where T : IDomainEvent;
    }
}