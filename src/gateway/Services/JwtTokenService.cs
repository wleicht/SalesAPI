using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BuildingBlocks.Contracts.Auth;

namespace Gateway.Services
{
    /// <summary>
    /// Interface for JWT token service operations.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generates a JWT token for the specified user.
        /// </summary>
        /// <param name="username">Username for the token.</param>
        /// <param name="role">User role for authorization.</param>
        /// <returns>Generated JWT token string.</returns>
        string GenerateToken(string username, string role);

        /// <summary>
        /// Validates the provided username and password.
        /// </summary>
        /// <param name="username">Username to validate.</param>
        /// <param name="password">Password to validate.</param>
        /// <returns>User information if valid, null otherwise.</returns>
        UserInfo? ValidateUser(string username, string password);
    }

    /// <summary>
    /// Implementation of JWT token service with in-memory user storage.
    /// This is for development purposes only - in production, use a proper identity provider.
    /// </summary>
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtTokenService> _logger;

        // In-memory user store for development
        private readonly Dictionary<string, (string Password, string Role)> _users = new()
        {
            { "admin", ("admin123", "admin") },
            { "customer1", ("password123", "customer") },
            { "customer2", ("password123", "customer") },
            { "manager", ("manager123", "manager") }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="logger">Logger instance.</param>
        public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public string GenerateToken(string username, string role)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // Token expires in 1 hour
                signingCredentials: signingCredentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation("Generated JWT token for user {Username} with role {Role}", username, role);

            return tokenString;
        }

        /// <inheritdoc />
        public UserInfo? ValidateUser(string username, string password)
        {
            _logger.LogInformation("Validating user credentials for {Username}", username);

            if (_users.TryGetValue(username, out var userData) && userData.Password == password)
            {
                _logger.LogInformation("User {Username} validated successfully with role {Role}", username, userData.Role);
                
                return new UserInfo
                {
                    Username = username,
                    Role = userData.Role,
                    IsActive = true
                };
            }

            _logger.LogWarning("Failed authentication attempt for user {Username}", username);
            return null;
        }
    }
}