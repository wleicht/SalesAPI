namespace SalesAPI.Tests.Professional.TestInfrastructure.Configuration
{
    /// <summary>
    /// Constants used specifically for testing scenarios.
    /// These values are optimized for test performance and reliability.
    /// </summary>
    public static class TestConstants
    {
        /// <summary>
        /// Performance-related constants for test execution.
        /// </summary>
        public static class Performance
        {
            /// <summary>Maximum execution time for individual tests in milliseconds</summary>
            public const int MaxExecutionTimeMs = 3000;
            
            /// <summary>Number of records to use in bulk insert tests</summary>
            public const int BulkInsertRecords = 50;
            
            /// <summary>Maximum timeout for async test operations in milliseconds</summary>
            public const int MaxTestTimeoutMs = 30000;
            
            /// <summary>Maximum time for database operations in milliseconds</summary>
            public const int MaxDatabaseOperationMs = 1000;
            
            /// <summary>Maximum time for message publishing in milliseconds</summary>
            public const int MaxMessagePublishMs = 500;
            
            /// <summary>Delay between retry attempts in milliseconds</summary>
            public const int RetryDelayMs = 100;
            
            /// <summary>Maximum number of retry attempts</summary>
            public const int MaxRetries = 3;
        }
        
        /// <summary>
        /// Sample data constants used in tests to avoid magic numbers.
        /// </summary>
        public static class TestData
        {
            /// <summary>Sample price for complex calculations</summary>
            public const decimal SamplePrice1 = 123.456m;
            
            /// <summary>Another sample price for testing scenarios</summary>
            public const decimal SamplePrice2 = 999.999m;
            
            /// <summary>Sample quantity for order items</summary>
            public const int SampleQuantity = 7;
            
            /// <summary>Alternative quantity for testing</summary>
            public const int AlternativeQuantity = 3;
            
            /// <summary>Sample discount percentage</summary>
            public const decimal SampleDiscountPercent = 0.1m; // 10%
            
            /// <summary>Sample tax rate</summary>
            public const decimal SampleTaxRate = 0.08m; // 8%
            
            /// <summary>Minimum price for products</summary>
            public const decimal MinPrice = 10m;
            
            /// <summary>Maximum price for products</summary>
            public const decimal MaxPrice = 1000m;
            
            /// <summary>Maximum quantity for order items</summary>
            public const int MaxQuantity = 10;
            
            /// <summary>Minimum quantity for order items</summary>
            public const int MinQuantity = 1;
        }
        
        /// <summary>
        /// Database-related test constants.
        /// </summary>
        public static class Database
        {
            /// <summary>Test database name prefix</summary>
            public const string TestDbPrefix = "SalesAPI_Test_";
            
            /// <summary>Maximum time to wait for database initialization</summary>
            public const int InitializationTimeoutMs = 10000;
            
            /// <summary>Number of concurrent database operations to test</summary>
            public const int ConcurrentOperations = 5;
        }
        
        /// <summary>
        /// Messaging-related test constants.
        /// </summary>
        public static class Messaging
        {
            /// <summary>Test correlation ID prefix</summary>
            public const string TestCorrelationPrefix = "test-correlation-";
            
            /// <summary>Maximum time to wait for message processing</summary>
            public const int MessageProcessingTimeoutMs = 5000;
            
            /// <summary>Number of messages to send in batch tests</summary>
            public const int BatchMessageCount = 10;
        }
        
        /// <summary>
        /// HTTP client test constants.
        /// </summary>
        public static class Http
        {
            /// <summary>Test HTTP timeout in milliseconds</summary>
            public const int TestTimeoutMs = 10000;
            
            /// <summary>Maximum time for API response</summary>
            public const int MaxResponseTimeMs = 2000;
            
            /// <summary>Test user agent string</summary>
            public const string TestUserAgent = "SalesAPI-Test-Client/1.0";
        }
        
        /// <summary>
        /// Expected calculation results for test validation.
        /// </summary>
        public static class ExpectedResults
        {
            /// <summary>Expected total for complex calculation test: 7 * 123.456</summary>
            public const decimal ComplexPrice1Total = 864.192m;
            
            /// <summary>Expected total for complex calculation test: 3 * 999.999</summary>
            public const decimal ComplexPrice2Total = 2999.997m;
            
            /// <summary>Expected grand total: ComplexPrice1Total + ComplexPrice2Total</summary>
            public const decimal ComplexGrandTotal = 3864.189m;
        }
    }
}