using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildingBlocks.Infrastructure.Messaging.Exceptions
{
    /// <summary>
    /// Base exception class for all message bus related errors.
    /// Provides common exception handling infrastructure for messaging operations
    /// with support for error classification and diagnostic information.
    /// </summary>
    /// <remarks>
    /// This base exception establishes consistent error handling patterns across
    /// all messaging infrastructure operations:
    /// 
    /// Error Classification:
    /// - Transient errors that may succeed on retry
    /// - Permanent errors requiring manual intervention
    /// - Configuration errors indicating setup problems
    /// - Network errors related to connectivity issues
    /// 
    /// Diagnostic Support:
    /// - Correlation ID tracking for distributed error investigation
    /// - Operation context for understanding error scenarios
    /// - Nested exception support for root cause analysis
    /// - Structured error data for monitoring and alerting
    /// 
    /// Exception Design Principles:
    /// - Inherit from this base for all messaging exceptions
    /// - Include sufficient context for error resolution
    /// - Support serialization for distributed error handling
    /// - Provide meaningful error messages for operations teams
    /// </remarks>
    public abstract class MessageBusException : Exception
    {
        /// <summary>
        /// Correlation ID associated with the failed operation, if available.
        /// Enables linking errors to specific request flows for investigation.
        /// </summary>
        public string? CorrelationId { get; }

        /// <summary>
        /// Indicates whether this error condition may be transient and worth retrying.
        /// Used by retry logic to determine appropriate error handling strategies.
        /// </summary>
        public virtual bool IsTransient => false;

        /// <summary>
        /// Initializes a new message bus exception with a descriptive error message.
        /// </summary>
        /// <param name="message">Descriptive error message for operations and debugging</param>
        protected MessageBusException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new message bus exception with error message and correlation context.
        /// </summary>
        /// <param name="message">Descriptive error message for operations and debugging</param>
        /// <param name="correlationId">Correlation ID for request flow tracking</param>
        protected MessageBusException(string message, string? correlationId) : base(message)
        {
            CorrelationId = correlationId;
        }

        /// <summary>
        /// Initializes a new message bus exception with error message and underlying cause.
        /// </summary>
        /// <param name="message">Descriptive error message for operations and debugging</param>
        /// <param name="innerException">Underlying exception that caused this error</param>
        protected MessageBusException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new message bus exception with complete error context.
        /// </summary>
        /// <param name="message">Descriptive error message for operations and debugging</param>
        /// <param name="correlationId">Correlation ID for request flow tracking</param>
        /// <param name="innerException">Underlying exception that caused this error</param>
        protected MessageBusException(string message, string? correlationId, Exception innerException) 
            : base(message, innerException)
        {
            CorrelationId = correlationId;
        }
    }

    /// <summary>
    /// Exception thrown when message serialization or deserialization fails.
    /// Indicates problems with message format, content, or serialization infrastructure
    /// that prevent successful message transport and processing.
    /// </summary>
    /// <remarks>
    /// Common serialization error scenarios:
    /// - Invalid message format or structure
    /// - Missing required properties or metadata
    /// - Type compatibility issues during deserialization
    /// - Circular reference problems in object graphs
    /// - Size limitations exceeding message broker constraints
    /// 
    /// Resolution strategies:
    /// - Validate message structure before publishing
    /// - Implement proper serialization attributes and contracts
    /// - Handle schema version compatibility explicitly
    /// - Monitor message size and optimize large payloads
    /// </remarks>
    public class MessageSerializationException : MessageBusException
    {
        /// <summary>
        /// Type of the message that failed serialization, if available.
        /// </summary>
        public Type? MessageType { get; }

        public MessageSerializationException(string message) : base(message) { }

        public MessageSerializationException(string message, Type messageType) : base(message)
        {
            MessageType = messageType;
        }

        public MessageSerializationException(string message, Exception innerException) 
            : base(message, innerException) { }

        public MessageSerializationException(string message, Type messageType, Exception innerException) 
            : base(message, innerException)
        {
            MessageType = messageType;
        }

        public MessageSerializationException(string message, string? correlationId, Type messageType, Exception innerException) 
            : base(message, correlationId, innerException)
        {
            MessageType = messageType;
        }
    }

    /// <summary>
    /// Exception thrown when connection to the message bus infrastructure fails.
    /// Indicates network connectivity, authentication, or configuration problems
    /// that prevent establishing communication with the messaging system.
    /// </summary>
    /// <remarks>
    /// Common connection error scenarios:
    /// - Network connectivity issues or firewall restrictions
    /// - Authentication or authorization failures
    /// - Message broker unavailability or maintenance
    /// - Connection string or configuration errors
    /// - Resource exhaustion (connection pool limits)
    /// 
    /// This exception is typically transient and may succeed on retry
    /// after network conditions improve or broker becomes available.
    /// </remarks>
    public class MessageBusConnectionException : MessageBusException
    {
        /// <summary>
        /// Connection exceptions are typically transient network issues.
        /// </summary>
        public override bool IsTransient => true;

        public MessageBusConnectionException(string message) : base(message) { }

        public MessageBusConnectionException(string message, string? correlationId) : base(message, correlationId) { }

        public MessageBusConnectionException(string message, Exception innerException) 
            : base(message, innerException) { }

        public MessageBusConnectionException(string message, string? correlationId, Exception innerException) 
            : base(message, correlationId, innerException) { }
    }

    /// <summary>
    /// Exception thrown when message publishing operations fail.
    /// Indicates problems during the actual message publishing process
    /// that occur after successful connection establishment.
    /// </summary>
    /// <remarks>
    /// Common publishing error scenarios:
    /// - Message broker resource constraints (disk space, memory)
    /// - Exchange or queue configuration problems
    /// - Message size exceeding broker limits
    /// - Permission or authorization issues for specific operations
    /// - Transaction or consistency failures
    /// 
    /// May be transient (resource constraints) or permanent (configuration issues).
    /// </remarks>
    public class MessagePublishingException : MessageBusException
    {
        /// <summary>
        /// Number of messages that were successfully published before the error occurred.
        /// Useful for batch operations to understand partial success scenarios.
        /// </summary>
        public int SuccessfulCount { get; }

        /// <summary>
        /// Total number of messages attempted in the publishing operation.
        /// </summary>
        public int TotalCount { get; }

        public MessagePublishingException(string message) : base(message) { }

        public MessagePublishingException(string message, string? correlationId) : base(message, correlationId) { }

        public MessagePublishingException(string message, Exception innerException) 
            : base(message, innerException) { }

        public MessagePublishingException(string message, string? correlationId, Exception innerException) 
            : base(message, correlationId, innerException) { }

        public MessagePublishingException(string message, int successfulCount, int totalCount) 
            : base(message)
        {
            SuccessfulCount = successfulCount;
            TotalCount = totalCount;
        }

        public MessagePublishingException(string message, string? correlationId, int successfulCount, int totalCount, Exception innerException) 
            : base(message, correlationId, innerException)
        {
            SuccessfulCount = successfulCount;
            TotalCount = totalCount;
        }
    }

    /// <summary>
    /// Exception thrown when batch operations exceed size limits or constraints.
    /// Indicates that the requested batch operation cannot be completed
    /// due to infrastructure limitations or configuration restrictions.
    /// </summary>
    /// <remarks>
    /// Common batch size error scenarios:
    /// - Message broker batch size limitations
    /// - Memory constraints for large message collections
    /// - Transaction size limits in transactional systems
    /// - Network payload size restrictions
    /// 
    /// Resolution typically requires breaking large batches into smaller chunks
    /// or adjusting system configuration for larger batch support.
    /// </remarks>
    public class BatchSizeExceededException : MessageBusException
    {
        /// <summary>
        /// Attempted batch size that caused the error.
        /// </summary>
        public int AttemptedSize { get; }

        /// <summary>
        /// Maximum allowed batch size for the operation.
        /// </summary>
        public int MaximumSize { get; }

        public BatchSizeExceededException(string message, int attemptedSize, int maximumSize) 
            : base(message)
        {
            AttemptedSize = attemptedSize;
            MaximumSize = maximumSize;
        }

        public BatchSizeExceededException(string message, string? correlationId, int attemptedSize, int maximumSize) 
            : base(message, correlationId)
        {
            AttemptedSize = attemptedSize;
            MaximumSize = maximumSize;
        }
    }

    /// <summary>
    /// Exception thrown when batch publishing operations fail with partial success.
    /// Provides detailed information about which messages succeeded and failed
    /// to enable appropriate error handling and recovery strategies.
    /// </summary>
    /// <remarks>
    /// Batch publishing can fail in several ways:
    /// - Complete failure: No messages were published successfully
    /// - Partial failure: Some messages succeeded, others failed
    /// - Individual errors: Each failed message may have different error reasons
    /// 
    /// This exception provides comprehensive information to support
    /// sophisticated error handling and retry strategies.
    /// </remarks>
    public class BatchPublishingException : MessagePublishingException
    {
        /// <summary>
        /// Collection of individual errors for messages that failed to publish.
        /// Each entry contains the message index and associated error information.
        /// </summary>
        public IReadOnlyList<(int MessageIndex, Exception Error)> IndividualErrors { get; }

        public BatchPublishingException(
            string message, 
            int successfulCount, 
            int totalCount, 
            IEnumerable<(int MessageIndex, Exception Error)> individualErrors) 
            : base(message, successfulCount, totalCount)
        {
            IndividualErrors = individualErrors.ToList().AsReadOnly();
        }

        public BatchPublishingException(
            string message, 
            string? correlationId, 
            int successfulCount, 
            int totalCount, 
            IEnumerable<(int MessageIndex, Exception Error)> individualErrors,
            Exception innerException) 
            : base(message, correlationId, successfulCount, totalCount, innerException)
        {
            IndividualErrors = individualErrors.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Exception thrown when message subscription operations fail.
    /// Indicates problems establishing or maintaining event subscriptions
    /// that prevent receiving and processing messages.
    /// </summary>
    /// <remarks>
    /// Common subscription error scenarios:
    /// - Queue or exchange configuration problems
    /// - Permission or authorization issues for subscription operations
    /// - Consumer registration conflicts or limitations
    /// - Handler registration or dependency injection failures
    /// 
    /// May require configuration changes or system administration
    /// intervention to resolve subscription issues.
    /// </remarks>
    public class SubscriptionException : MessageBusException
    {
        /// <summary>
        /// Type of event that was being subscribed to when the error occurred.
        /// </summary>
        public Type? EventType { get; }

        public SubscriptionException(string message) : base(message) { }

        public SubscriptionException(string message, Type eventType) : base(message)
        {
            EventType = eventType;
        }

        public SubscriptionException(string message, Exception innerException) 
            : base(message, innerException) { }

        public SubscriptionException(string message, Type eventType, Exception innerException) 
            : base(message, innerException)
        {
            EventType = eventType;
        }

        public SubscriptionException(string message, string? correlationId, Type eventType, Exception innerException) 
            : base(message, correlationId, innerException)
        {
            EventType = eventType;
        }
    }
}