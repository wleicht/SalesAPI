namespace SalesApi.Middleware
{
    /// <summary>
    /// HTTP message handler for propagating correlation IDs in outgoing HTTP requests.
    /// Ensures correlation context is maintained across service boundaries.
    /// </summary>
    public class CorrelationHttpMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var correlationId = GetCurrentCorrelationId();

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                request.Headers.Add("X-Correlation-Id", correlationId);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private static string? GetCurrentCorrelationId()
        {
            // Try to get correlation ID from current activity (set by middleware)
            var activity = System.Diagnostics.Activity.Current;
            if (activity?.GetTagItem("correlation_id") is string correlationId)
            {
                return correlationId;
            }

            // Fallback: generate new correlation ID for outgoing requests
            return $"sales-out-{Guid.NewGuid():N}";
        }
    }
}