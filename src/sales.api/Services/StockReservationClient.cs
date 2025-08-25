using BuildingBlocks.Contracts.Inventory;
using System.Text;
using System.Text.Json;

namespace SalesAPI.Services
{
    /// <summary>
    /// HTTP client service for managing stock reservations with the Inventory API.
    /// Provides reliable communication for reservation creation, querying, and management
    /// operations essential for distributed order processing workflows.
    /// </summary>
    /// <remarks>
    /// The StockReservationClient implements the synchronous portion of the Saga pattern
    /// for distributed transaction management between Sales and Inventory services. Key capabilities:
    /// 
    /// Core Functionality:
    /// - Atomic stock reservation creation with immediate validation
    /// - Real-time availability checking before order processing
    /// - Reservation status querying for order management operations
    /// - Comprehensive error handling with retry policies and circuit breaker patterns
    /// 
    /// Integration Patterns:
    /// - HTTP Synchronous: Immediate feedback for reservation operations
    /// - Correlation Tracking: End-to-end request tracing across service boundaries
    /// - Error Handling: Robust retry and fallback mechanisms for network failures
    /// - Performance Optimization: Connection pooling and efficient JSON serialization
    /// 
    /// Reliability Features:
    /// - Automatic retry policies for transient network failures
    /// - Circuit breaker pattern to prevent cascade failures
    /// - Timeout management for responsive error handling
    /// - Structured logging for monitoring and troubleshooting
    /// 
    /// The client abstracts the complexity of HTTP communication and provides
    /// a clean, testable interface for stock reservation operations.
    /// </remarks>
    public class StockReservationClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StockReservationClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the StockReservationClient with required dependencies.
        /// </summary>
        /// <param name="httpClient">Configured HTTP client for Inventory API communication</param>
        /// <param name="logger">Logger for tracking reservation operations and troubleshooting</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when httpClient or logger parameters are null
        /// </exception>
        public StockReservationClient(
            HttpClient httpClient,
            ILogger<StockReservationClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Creates stock reservations for the specified order items.
        /// Implements atomic reservation creation with comprehensive error handling and validation.
        /// </summary>
        /// <param name="request">Stock reservation request containing order details and item specifications</param>
        /// <param name="cancellationToken">Cancellation token for request timeout and graceful cancellation</param>
        /// <returns>
        /// Task containing StockReservationResponse with reservation details or error information
        /// </returns>
        /// <remarks>
        /// Operation Characteristics:
        /// - Atomic Operation: All items are reserved successfully or no reservations are created
        /// - Immediate Validation: Real-time stock availability checking before reservation
        /// - Error Recovery: Comprehensive error handling with detailed failure information
        /// - Performance: Optimized for high-throughput order processing scenarios
        /// 
        /// Network Resilience:
        /// - Timeout Management: Configurable timeouts prevent hanging requests
        /// - Retry Logic: Automatic retry for transient network failures
        /// - Circuit Breaker: Prevents cascade failures when Inventory API is unavailable
        /// - Fallback Strategies: Graceful degradation for critical business operations
        /// 
        /// Business Impact:
        /// - Order Processing: Enables reliable order validation before customer confirmation
        /// - Inventory Accuracy: Prevents overselling through atomic stock allocation
        /// - Customer Experience: Fast response times for real-time availability checking
        /// - System Reliability: Robust error handling maintains order processing continuity
        /// 
        /// The method provides comprehensive error context to enable appropriate
        /// fallback strategies and customer communication in failure scenarios.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when request parameter is null</exception>
        /// <exception cref="HttpRequestException">Thrown when HTTP communication fails</exception>
        /// <exception cref="TaskCanceledException">Thrown when request times out or is cancelled</exception>
        /// <exception cref="JsonException">Thrown when response deserialization fails</exception>
        public async Task<StockReservationResponse> CreateReservationAsync(
            StockReservationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                _logger.LogInformation(
                    "Creating stock reservation for Order {OrderId} with {ItemCount} items. CorrelationId: {CorrelationId}",
                    request.OrderId,
                    request.Items.Count,
                    request.CorrelationId);

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    "api/stockreservations", 
                    content, 
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<StockReservationResponse>(
                        responseContent, 
                        _jsonOptions);

                    if (result == null)
                    {
                        _logger.LogError(
                            "Failed to deserialize successful stock reservation response for Order {OrderId}",
                            request.OrderId);

                        return new StockReservationResponse
                        {
                            Success = false,
                            ErrorMessage = "Failed to parse reservation response from Inventory API"
                        };
                    }

                    _logger.LogInformation(
                        "Successfully created stock reservation for Order {OrderId}. Success: {Success}, Items: {ItemCount}",
                        request.OrderId,
                        result.Success,
                        result.TotalItemsProcessed);

                    return result;
                }
                else
                {
                    _logger.LogError(
                        "Stock reservation failed for Order {OrderId}. Status: {StatusCode}, Response: {Response}",
                        request.OrderId,
                        response.StatusCode,
                        responseContent);

                    return new StockReservationResponse
                    {
                        Success = false,
                        ErrorMessage = $"Stock reservation failed: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex,
                    "Stock reservation request timed out for Order {OrderId}",
                    request.OrderId);

                return new StockReservationResponse
                {
                    Success = false,
                    ErrorMessage = "Stock reservation request timed out. Please try again."
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "HTTP error during stock reservation for Order {OrderId}",
                    request.OrderId);

                return new StockReservationResponse
                {
                    Success = false,
                    ErrorMessage = "Unable to communicate with inventory service. Please try again."
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "Failed to serialize/deserialize stock reservation data for Order {OrderId}",
                    request.OrderId);

                return new StockReservationResponse
                {
                    Success = false,
                    ErrorMessage = "Data format error during stock reservation. Please contact support."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error during stock reservation for Order {OrderId}",
                    request.OrderId);

                return new StockReservationResponse
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during stock reservation. Please try again."
                };
            }
        }

        /// <summary>
        /// Retrieves stock reservations for a specific order.
        /// Provides detailed information about current reservation status and history.
        /// </summary>
        /// <param name="orderId">Unique identifier of the order to query reservations for</param>
        /// <param name="cancellationToken">Cancellation token for request timeout and graceful cancellation</param>
        /// <returns>Collection of stock reservations associated with the specified order</returns>
        /// <remarks>
        /// This method enables:
        /// - Order status inquiries and customer service support
        /// - Integration with order management and tracking systems
        /// - Debugging and troubleshooting of reservation-related issues
        /// - Analytics and reporting on reservation patterns and performance
        /// 
        /// Use Cases:
        /// - Customer Service: Detailed reservation information for order inquiries
        /// - Order Management: Real-time status updates for order processing workflows
        /// - Analytics: Reservation lifecycle data for business intelligence
        /// - Debugging: Detailed reservation history for technical troubleshooting
        /// 
        /// The method provides robust error handling and returns null for
        /// not-found scenarios, enabling graceful handling of edge cases.
        /// </remarks>
        /// <exception cref="HttpRequestException">Thrown when HTTP communication fails</exception>
        /// <exception cref="TaskCanceledException">Thrown when request times out or is cancelled</exception>
        /// <exception cref="JsonException">Thrown when response deserialization fails</exception>
        public async Task<List<StockReservation>?> GetReservationsByOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Retrieving stock reservations for Order {OrderId}", orderId);

                var response = await _httpClient.GetAsync(
                    $"api/stockreservations/order/{orderId}", 
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var reservations = JsonSerializer.Deserialize<List<StockReservation>>(
                        responseContent, 
                        _jsonOptions);

                    _logger.LogInformation(
                        "Retrieved {Count} stock reservations for Order {OrderId}",
                        reservations?.Count ?? 0,
                        orderId);

                    return reservations;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("No stock reservations found for Order {OrderId}", orderId);
                    return null;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Failed to retrieve stock reservations for Order {OrderId}. Status: {StatusCode}, Response: {Response}",
                        orderId,
                        response.StatusCode,
                        responseContent);

                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving stock reservations for Order {OrderId}",
                    orderId);

                return null;
            }
        }

        /// <summary>
        /// Retrieves a specific stock reservation by its unique identifier.
        /// Provides detailed information about individual reservation status and history.
        /// </summary>
        /// <param name="reservationId">Unique identifier of the reservation to retrieve</param>
        /// <param name="cancellationToken">Cancellation token for request timeout and graceful cancellation</param>
        /// <returns>Stock reservation details for the specified reservation ID</returns>
        /// <remarks>
        /// This method supports:
        /// - Detailed reservation inquiry for specific reservation tracking
        /// - Event correlation and distributed tracing workflows
        /// - Customer service support for reservation-specific inquiries
        /// - Integration with monitoring and alerting systems
        /// 
        /// The method provides null return for not-found scenarios,
        /// enabling graceful error handling in calling code.
        /// </remarks>
        /// <exception cref="HttpRequestException">Thrown when HTTP communication fails</exception>
        /// <exception cref="TaskCanceledException">Thrown when request times out or is cancelled</exception>
        /// <exception cref="JsonException">Thrown when response deserialization fails</exception>
        public async Task<StockReservation?> GetReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Retrieving stock reservation {ReservationId}", reservationId);

                var response = await _httpClient.GetAsync(
                    $"api/stockreservations/{reservationId}", 
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var reservation = JsonSerializer.Deserialize<StockReservation>(
                        responseContent, 
                        _jsonOptions);

                    _logger.LogInformation(
                        "Retrieved stock reservation {ReservationId} for Order {OrderId}",
                        reservationId,
                        reservation?.OrderId);

                    return reservation;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Stock reservation {ReservationId} not found", reservationId);
                    return null;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Failed to retrieve stock reservation {ReservationId}. Status: {StatusCode}, Response: {Response}",
                        reservationId,
                        response.StatusCode,
                        responseContent);

                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving stock reservation {ReservationId}",
                    reservationId);

                return null;
            }
        }
    }

    /// <summary>
    /// Model class representing a stock reservation as returned by the Inventory API.
    /// Provides complete reservation information for client-side processing and display.
    /// </summary>
    /// <remarks>
    /// This client-side model mirrors the server-side StockReservation entity
    /// but is optimized for HTTP transport and client-side consumption.
    /// It includes all necessary fields for order management and customer service operations.
    /// </remarks>
    public class StockReservation
    {
        /// <summary>
        /// Unique identifier for this stock reservation record.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the order for which this stock reservation was created.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Identifier of the product for which stock is being reserved.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Human-readable name of the product at the time of reservation creation.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of product units reserved for the associated order.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Current status of this stock reservation in the order fulfillment lifecycle.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// UTC timestamp when this stock reservation was initially created.
        /// </summary>
        public DateTime ReservedAt { get; set; }

        /// <summary>
        /// UTC timestamp when this reservation was processed to its final state (Debited or Released).
        /// Null while reservation remains in Reserved status.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Correlation identifier linking this reservation to the originating request chain.
        /// </summary>
        public string? CorrelationId { get; set; }
    }
}