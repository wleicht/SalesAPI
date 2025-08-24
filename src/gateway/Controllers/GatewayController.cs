using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers
{
    /// <summary>
    /// Controller for Gateway-specific health and status endpoints.
    /// </summary>
    [ApiController]
    [Route("gateway")]
    public class GatewayController : ControllerBase
    {
        private readonly ILogger<GatewayController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GatewayController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public GatewayController(ILogger<GatewayController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the gateway status and information.
        /// </summary>
        /// <returns>Gateway status information.</returns>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            _logger.LogInformation("Gateway status requested");

            var status = new
            {
                Name = "SalesAPI Gateway",
                Version = "1.0.0",
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Routes = new
                {
                    Inventory = "/inventory/*",
                    Sales = "/sales/*"
                },
                Endpoints = new
                {
                    Health = "/health",
                    Swagger = "/swagger",
                    Status = "/gateway/status"
                }
            };

            return Ok(status);
        }

        /// <summary>
        /// Gets information about available routes through the gateway.
        /// </summary>
        /// <returns>Available routes information.</returns>
        [HttpGet("routes")]
        public IActionResult GetRoutes()
        {
            _logger.LogInformation("Gateway routes information requested");

            var routes = new
            {
                Description = "Available routes through the SalesAPI Gateway",
                Routes = new[]
                {
                    new
                    {
                        Service = "Inventory API",
                        Pattern = "/inventory/{**path}",
                        Target = "http://localhost:5000/",
                        Examples = new[]
                        {
                            "/inventory/products",
                            "/inventory/products/{id}",
                            "/inventory/health"
                        }
                    },
                    new
                    {
                        Service = "Sales API",
                        Pattern = "/sales/{**path}",
                        Target = "http://localhost:5001/",
                        Examples = new[]
                        {
                            "/sales/orders",
                            "/sales/orders/{id}",
                            "/sales/health"
                        }
                    }
                }
            };

            return Ok(routes);
        }
    }
}