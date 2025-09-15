namespace BuildingBlocks.Configuration.Constants
{
    /// <summary>
    /// Shared constants across all microservices in the SalesAPI ecosystem.
    /// These constants provide consistency and eliminate magic numbers across services.
    /// </summary>
    public static class SharedConstants
    {
        /// <summary>
        /// Standard HTTP headers used across all services.
        /// </summary>
        public static class HttpHeaders
        {
            /// <summary>Correlation ID header for request tracing</summary>
            public const string CorrelationId = "X-Correlation-Id";
            
            /// <summary>Request ID header for unique request identification</summary>
            public const string RequestId = "X-Request-Id";
            
            /// <summary>User ID header for authentication context</summary>
            public const string UserId = "X-User-Id";
            
            /// <summary>Tenant ID header for multi-tenancy support</summary>
            public const string TenantId = "X-Tenant-Id";
            
            /// <summary>API version header</summary>
            public const string ApiVersion = "X-API-Version";
        }

        /// <summary>
        /// Standard timeout configurations used across all services.
        /// </summary>
        public static class Timeouts
        {
            /// <summary>Default HTTP request timeout in seconds</summary>
            public const int HttpRequestTimeout = 30;
            
            /// <summary>Database connection timeout in seconds</summary>
            public const int DatabaseConnection = 30;
            
            /// <summary>Database command timeout in seconds</summary>
            public const int DatabaseCommand = 30;
            
            /// <summary>Message processing timeout in seconds</summary>
            public const int MessageProcessing = 60;
            
            /// <summary>Health check timeout in seconds</summary>
            public const int HealthCheck = 10;
            
            /// <summary>Graceful shutdown timeout in seconds</summary>
            public const int GracefulShutdown = 30;
        }

        /// <summary>
        /// Standard retry configurations used across all services.
        /// </summary>
        public static class Retry
        {
            /// <summary>Default maximum retry attempts</summary>
            public const int MaxAttempts = 3;
            
            /// <summary>Base delay for exponential backoff in milliseconds</summary>
            public const int BaseDelayMs = 1000;
            
            /// <summary>Maximum delay for exponential backoff in milliseconds</summary>
            public const int MaxDelayMs = 30000;
            
            /// <summary>Jitter factor for retry delays (0.0 to 1.0)</summary>
            public const double JitterFactor = 0.1;
        }

        /// <summary>
        /// Standard cache configurations used across all services.
        /// </summary>
        public static class Cache
        {
            /// <summary>Default cache expiration in minutes</summary>
            public const int DefaultExpirationMinutes = 15;
            
            /// <summary>Short-lived cache expiration in minutes</summary>
            public const int ShortExpirationMinutes = 5;
            
            /// <summary>Long-lived cache expiration in minutes</summary>
            public const int LongExpirationMinutes = 60;
            
            /// <summary>Maximum cache size in entries</summary>
            public const int MaxCacheSize = 1000;
        }

        /// <summary>
        /// Standard pagination configurations used across all services.
        /// </summary>
        public static class Pagination
        {
            /// <summary>Default page size for listings</summary>
            public const int DefaultPageSize = 20;
            
            /// <summary>Minimum allowed page size</summary>
            public const int MinPageSize = 1;
            
            /// <summary>Maximum allowed page size</summary>
            public const int MaxPageSize = 100;
            
            /// <summary>Default page number</summary>
            public const int DefaultPageNumber = 1;
        }

        /// <summary>
        /// Standard validation rules used across all services.
        /// </summary>
        public static class Validation
        {
            /// <summary>Maximum string length for names</summary>
            public const int MaxNameLength = 100;
            
            /// <summary>Maximum string length for descriptions</summary>
            public const int MaxDescriptionLength = 500;
            
            /// <summary>Maximum string length for notes</summary>
            public const int MaxNotesLength = 1000;
            
            /// <summary>Minimum string length for required fields</summary>
            public const int MinRequiredLength = 1;
            
            /// <summary>Maximum decimal precision for monetary values</summary>
            public const int MoneyPrecision = 18;
            
            /// <summary>Maximum decimal scale for monetary values</summary>
            public const int MoneyScale = 2;
        }

        /// <summary>
        /// Standard logging configurations used across all services.
        /// </summary>
        public static class Logging
        {
            /// <summary>Correlation ID property name for structured logging</summary>
            public const string CorrelationIdProperty = "CorrelationId";
            
            /// <summary>Request ID property name for structured logging</summary>
            public const string RequestIdProperty = "RequestId";
            
            /// <summary>User ID property name for structured logging</summary>
            public const string UserIdProperty = "UserId";
            
            /// <summary>Service name property name for structured logging</summary>
            public const string ServiceNameProperty = "ServiceName";
            
            /// <summary>Operation name property name for structured logging</summary>
            public const string OperationNameProperty = "OperationName";
        }

        /// <summary>
        /// Standard event patterns used across all services.
        /// </summary>
        public static class Events
        {
            /// <summary>Event ID property name</summary>
            public const string EventIdProperty = "EventId";
            
            /// <summary>Event type property name</summary>
            public const string EventTypeProperty = "EventType";
            
            /// <summary>Event timestamp property name</summary>
            public const string EventTimestampProperty = "EventTimestamp";
            
            /// <summary>Event source property name</summary>
            public const string EventSourceProperty = "EventSource";
            
            /// <summary>Default event timeout in seconds</summary>
            public const int DefaultEventTimeoutSeconds = 300; // 5 minutes
        }

        /// <summary>
        /// Standard security configurations used across all services.
        /// </summary>
        public static class Security
        {
            /// <summary>Minimum password length</summary>
            public const int MinPasswordLength = 8;
            
            /// <summary>Maximum password length</summary>
            public const int MaxPasswordLength = 128;
            
            /// <summary>JWT minimum key length in bytes</summary>
            public const int JwtMinKeyLength = 32;
            
            /// <summary>Default token expiration in minutes</summary>
            public const int DefaultTokenExpirationMinutes = 60;
            
            /// <summary>Maximum clock skew in minutes</summary>
            public const int MaxClockSkewMinutes = 5;
        }

        /// <summary>
        /// Standard environment names used across all services.
        /// </summary>
        public static class Environments
        {
            /// <summary>Development environment name</summary>
            public const string Development = "Development";
            
            /// <summary>Testing environment name</summary>
            public const string Testing = "Testing";
            
            /// <summary>Staging environment name</summary>
            public const string Staging = "Staging";
            
            /// <summary>Production environment name</summary>
            public const string Production = "Production";
        }

        /// <summary>
        /// Standard content types used across all services.
        /// </summary>
        public static class ContentTypes
        {
            /// <summary>JSON content type</summary>
            public const string Json = "application/json";
            
            /// <summary>XML content type</summary>
            public const string Xml = "application/xml";
            
            /// <summary>Form URL encoded content type</summary>
            public const string FormUrlEncoded = "application/x-www-form-urlencoded";
            
            /// <summary>Multipart form data content type</summary>
            public const string MultipartFormData = "multipart/form-data";
            
            /// <summary>Plain text content type</summary>
            public const string Text = "text/plain";
        }

        /// <summary>
        /// Standard date/time formats used across all services.
        /// </summary>
        public static class DateTimeFormats
        {
            /// <summary>ISO 8601 date format</summary>
            public const string IsoDate = "yyyy-MM-dd";
            
            /// <summary>ISO 8601 date-time format</summary>
            public const string IsoDateTime = "yyyy-MM-ddTHH:mm:ssZ";
            
            /// <summary>File-friendly timestamp format</summary>
            public const string FileTimestamp = "yyyyMMdd_HHmmss";
            
            /// <summary>Human-readable date format</summary>
            public const string DisplayDate = "MMM dd, yyyy";
            
            /// <summary>Human-readable date-time format</summary>
            public const string DisplayDateTime = "MMM dd, yyyy HH:mm";
        }
    }
}