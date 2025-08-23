using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers
{
    /// <summary>
    /// Controller for application health check.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Returns the health status of the application.
        /// </summary>
        [HttpGet]
        public IActionResult Get() => Ok("Healthy");
    }
}
