using BuildingBlocks.Contracts.Inventory;
using System.Text;
using System.Text.Json;

namespace SalesAPI.Services
{
    /// <summary>
    /// HTTP client service for managing stock reservations with the Inventory API.
    /// Provides reliable communication for reservation creation, querying, and management
    /// operations essential for distributed order processing workflows with correlation tracking.
    /// </summary>
    /// <remarks>
    /// Enhanced with observability features:
    /// - Correlation ID propagation for end-to-end request tracing
    /// - Structured logging for monitoring and troubleshooting
    /// - Performance metrics and timing information
    /// - Detailed error context for debugging and alerting
    /// </remarks>
    public class StockReservationClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StockReservationClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

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
        /// Creates stock reservations for the specified order items with correlation tracking.
        /// Implements atomic reservation creation with comprehensive error handling and observability.
        /// </summary>
        /// <param name="request">Stock reservation request containing order details and item specifications</param>
        /// <param name="cancellationToken">Cancellation token for request timeout and graceful cancellation</param>
        /// <returns>
        /// Task containing StockReservationResponse with reservation details or error information
        /// </returns>
        /// <remarks>
        /// Enhanced with observability:
        /// - Correlation ID propagation to Inventory API
        /// - Request/response timing and metrics
        /// - Structured logging with correlation context
        /// - Detailed error categorization for monitoring
        /// </remarks>
        public async Task<StockReservationResponse> CreateReservationAsync(
            StockReservationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation(
                    "????? Starting stock reservation: Order {OrderId} | Items: {ItemCount} | CorrelationId: {CorrelationId}",
                    request.OrderId,
                    request.Items.Count,
                    request.CorrelationId);

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Propagate correlation ID to Inventory API
                if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                {
                    content.Headers.Add("X-Correlation-Id", request.CorrelationId);
                }

                var response = await _httpClient.PostAsync(
                    "api/stockreservations", 
                    content, 
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<StockReservationResponse>(
                        responseContent, 
                        _jsonOptions);

                    if (result == null)
                    {
                        _logger.LogError(
                            "??? Failed to deserialize reservation response: Order {OrderId} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                            request.OrderId,
                            duration.TotalMilliseconds,
                            request.CorrelationId);

                        return new StockReservationResponse
                        {
                            Success = false,
                            ErrorMessage = "Failed to parse reservation response from Inventory API"
                        };
                    }

                    _logger.LogInformation(
                        "??? Stock reservation completed: Order {OrderId} | Success: {Success} | Items: {ItemCount} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                        request.OrderId,
                        result.Success,
                        result.TotalItemsProcessed,
                        duration.TotalMilliseconds,
                        request.CorrelationId);

                    return result;
                }
                else
                {
                    _logger.LogError(
                        "??? Stock reservation failed: Order {OrderId} | Status: {StatusCode} | Duration: {Duration}ms | Response: {Response} | CorrelationId: {CorrelationId}",
                        request.OrderId,
                        response.StatusCode,
                        duration.TotalMilliseconds,
                        responseContent,
                        request.CorrelationId);

                    return new StockReservationResponse
                    {
                        Success = false,
                        ErrorMessage = $"Stock reservation failed: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex,
                    "???? Stock reservation timeout: Order {OrderId} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    request.OrderId,
                    duration.TotalMilliseconds,
                    request.CorrelationId);

                return new StockReservationResponse
                {
                    Success = false,
                    ErrorMessage = "Stock reservation request timed out. Please try again."
                };
            }
            catch (HttpRequestException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex,
                    "???? HTTP error during reservation: Order {OrderId} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    request.OrderId,
                    duration.TotalMilliseconds,
                    request.CorrelationId);

                return new StockReservationResponse
                {
                    Success = false,
                    ErrorMessage = "Unable to communicate with inventory service. Please try again."
                };
            }
            catch (JsonException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex,
                    "???? JSON error during reservation: Order {OrderId} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    request.OrderId,
                    duration.TotalMilliseconds,
                    request.CorrelationId);

                return new StockReservationResponse
                {
                    Success = false,
                    ErrorMessage = "Data format error during stock reservation. Please contact support."
                };
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex,
                    "???? Unexpected error during reservation: Order {OrderId} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    request.OrderId,
                    duration.TotalMilliseconds,
                    request.CorrelationId);

                return new StockReservationResponse
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during stock reservation. Please try again."
                };
            }
        }

        /// <summary>
        /// Retrieves stock reservations for a specific order with correlation tracking.
        /// </summary>
        public async Task<List<StockReservation>?> GetReservationsByOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("???? Retrieving reservations for Order {OrderId}", orderId);

                var response = await _httpClient.GetAsync(
                    $"api/stockreservations/order/{orderId}", 
                    cancellationToken);

                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var reservations = JsonSerializer.Deserialize<List<StockReservation>>(
                        responseContent, 
                        _jsonOptions);

                    _logger.LogInformation(
                        "???? Retrieved {Count} reservations for Order {OrderId} | Duration: {Duration}ms",
                        reservations?.Count ?? 0,
                        orderId,
                        duration.TotalMilliseconds);

                    return reservations;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation(
                        "??? No reservations found for Order {OrderId} | Duration: {Duration}ms", 
                        orderId,
                        duration.TotalMilliseconds);
                    return null;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "??? Failed to retrieve reservations for Order {OrderId} | Status: {StatusCode} | Duration: {Duration}ms | Response: {Response}",
                        orderId,
                        response.StatusCode,
                        duration.TotalMilliseconds,
                        responseContent);

                    return null;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex,
                    "???? Error retrieving reservations for Order {OrderId} | Duration: {Duration}ms",
                    orderId,
                    duration.TotalMilliseconds);

                return null;
            }
        }

        /// <summary>
        /// Retrieves a specific stock reservation by its unique identifier with correlation tracking.
        /// </summary>
        public async Task<StockReservation?> GetReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("???? Retrieving reservation {ReservationId}", reservationId);

                var response = await _httpClient.GetAsync(
                    $"api/stockreservations/{reservationId}", 
                    cancellationToken);

                var duration = DateTime.UtcNow - startTime;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var reservation = JsonSerializer.Deserialize<StockReservation>(
                        responseContent, 
                        _jsonOptions);

                    _logger.LogInformation(
                        "???? Retrieved reservation {ReservationId} for Order {OrderId} | Duration: {Duration}ms",
                        reservationId,
                        reservation?.OrderId,
                        duration.TotalMilliseconds);

                    return reservation;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation(
                        "??? Reservation {ReservationId} not found | Duration: {Duration}ms", 
                        reservationId,
                        duration.TotalMilliseconds);
                    return null;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "??? Failed to retrieve reservation {ReservationId} | Status: {StatusCode} | Duration: {Duration}ms | Response: {Response}",
                        reservationId,
                        response.StatusCode,
                        duration.TotalMilliseconds,
                        responseContent);

                    return null;
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex,
                    "???? Error retrieving reservation {ReservationId} | Duration: {Duration}ms",
                    reservationId,
                    duration.TotalMilliseconds);

                return null;
            }
        }
    }

    /// <summary>
    /// Model class representing a stock reservation as returned by the Inventory API.
    /// Provides complete reservation information for client-side processing and display.
    /// </summary>
    public class StockReservation
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ReservedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? CorrelationId { get; set; }
    }
}