using System.ComponentModel.DataAnnotations;

namespace BuildingBlocks.Contracts.Auth
{
    /// <summary>
    /// Data Transfer Object for user login request.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Username for authentication.
        /// </summary>
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, ErrorMessage = "Username must be at most 50 characters.")]
        public required string Username { get; set; }

        /// <summary>
        /// Password for authentication.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public required string Password { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for successful authentication response.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// JWT access token for authenticated requests.
        /// </summary>
        public required string AccessToken { get; set; }

        /// <summary>
        /// Token type (typically "Bearer").
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Token expiration time in seconds.
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// User role associated with the token.
        /// </summary>
        public required string Role { get; set; }

        /// <summary>
        /// Username of the authenticated user.
        /// </summary>
        public required string Username { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for user information.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Unique username identifier.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// User's role in the system.
        /// </summary>
        public required string Role { get; set; }

        /// <summary>
        /// Indicates if the user is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}