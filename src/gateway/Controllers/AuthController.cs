using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Contracts.Auth;
using Gateway.Services;

namespace Gateway.Controllers
{
    /// <summary>
    /// Controller for authentication and token management.
    /// Provides JWT token generation for accessing protected resources.
    /// </summary>
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="jwtTokenService">JWT token service.</param>
        /// <param name="logger">Logger instance.</param>
        public AuthController(IJwtTokenService jwtTokenService, ILogger<AuthController> logger)
        {
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and generates a JWT token.
        /// </summary>
        /// <param name="request">Login request with username and password.</param>
        /// <returns>JWT token and user information if authentication is successful.</returns>
        /// <response code="200">Authentication successful, returns JWT token.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="401">Authentication failed, invalid credentials.</response>
        [HttpPost("token")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login request received");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Authentication attempt for user {Username}", request.Username);

            var userInfo = _jwtTokenService.ValidateUser(request.Username, request.Password);
            if (userInfo == null)
            {
                _logger.LogWarning("Authentication failed for user {Username}", request.Username);
                return Unauthorized(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
                    statusCode: 401, 
                    title: "Authentication failed", 
                    detail: "Invalid username or password."));
            }

            var token = _jwtTokenService.GenerateToken(userInfo.Username, userInfo.Role);

            var response = new LoginResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = 3600, // 1 hour in seconds
                Role = userInfo.Role,
                Username = userInfo.Username
            };

            _logger.LogInformation("User {Username} authenticated successfully with role {Role}", 
                userInfo.Username, userInfo.Role);

            return Ok(response);
        }

        /// <summary>
        /// Gets available test users for development purposes.
        /// This endpoint should be removed in production environments.
        /// </summary>
        /// <returns>List of available test users.</returns>
        /// <response code="200">Returns list of test users.</response>
        [HttpGet("test-users")]
        [ProducesResponseType(typeof(IEnumerable<object>), 200)]
        public ActionResult<IEnumerable<object>> GetTestUsers()
        {
            var testUsers = new[]
            {
                new { Username = "admin", Password = "admin123", Role = "admin", Description = "Administrator user - can create/edit products" },
                new { Username = "customer1", Password = "password123", Role = "customer", Description = "Customer user - can create orders" },
                new { Username = "customer2", Password = "password123", Role = "customer", Description = "Another customer user" },
                new { Username = "manager", Password = "manager123", Role = "manager", Description = "Manager user - for future use" }
            };

            _logger.LogInformation("Test users information requested");

            return Ok(new
            {
                Message = "Available test users for development. Remove this endpoint in production!",
                Users = testUsers,
                LoginEndpoint = "/auth/token",
                Usage = "POST to /auth/token with username and password to get JWT token"
            });
        }
    }
}