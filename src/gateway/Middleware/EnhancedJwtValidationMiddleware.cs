using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Gateway.Middleware
{
    /// <summary>
    /// Enhanced JWT validation middleware that provides rigorous token validation
    /// with detailed logging and proper error handling for security compliance.
    /// </summary>
    /// <remarks>
    /// Security Features:
    /// - Strict token format validation
    /// - Comprehensive claims validation
    /// - Proper error logging without exposing sensitive information
    /// - Zero tolerance for malformed tokens
    /// - Detailed audit trail for security monitoring
    /// </remarks>
    public class EnhancedJwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EnhancedJwtValidationMiddleware> _logger;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public EnhancedJwtValidationMiddleware(
            RequestDelegate next,
            ILogger<EnhancedJwtValidationMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _tokenHandler = new JwtSecurityTokenHandler();

            // Configure strict token validation parameters
            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured"),
                ValidAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured"),
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"))),
                ClockSkew = TimeSpan.Zero, // Zero tolerance for clock skew
                LifetimeValidator = ValidateLifetime
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip validation for health check and public endpoints
            if (IsPublicEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var correlationId = GetOrCreateCorrelationId(context);
            var token = ExtractTokenFromHeader(context);

            if (string.IsNullOrEmpty(token))
            {
                // No token provided - let the framework handle authorization
                await _next(context);
                return;
            }

            try
            {
                var validationResult = await ValidateTokenRigorouslyAsync(token, correlationId);
                
                if (validationResult.IsValid)
                {
                    // Set validated principal in context
                    context.User = validationResult.ClaimsPrincipal!;
                    
                    _logger.LogInformation(
                        "?? JWT token validated successfully | User: {Username} | CorrelationId: {CorrelationId}",
                        validationResult.ClaimsPrincipal!.Identity!.Name,
                        correlationId);

                    await _next(context);
                }
                else
                {
                    await WriteUnauthorizedResponseAsync(context, validationResult.ErrorMessage, correlationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? JWT validation failed with exception | CorrelationId: {CorrelationId}",
                    correlationId);

                await WriteUnauthorizedResponseAsync(context, "Token validation failed", correlationId);
            }
        }

        private async Task<JwtValidationResult> ValidateTokenRigorouslyAsync(string token, string correlationId)
        {
            try
            {
                // Step 1: Basic format validation
                if (!IsValidJwtFormat(token))
                {
                    _logger.LogWarning(
                        "?? Invalid JWT format detected | CorrelationId: {CorrelationId}",
                        correlationId);
                    return JwtValidationResult.Failed("Invalid token format");
                }

                // Step 2: Parse and validate structure
                var principal = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

                // Step 3: Additional custom validations
                var customValidationResult = await PerformCustomValidationsAsync(principal, validatedToken, correlationId);
                if (!customValidationResult.IsValid)
                {
                    return customValidationResult;
                }

                _logger.LogDebug(
                    "? JWT token passed all validation checks | Subject: {Subject} | CorrelationId: {CorrelationId}",
                    principal.Identity!.Name,
                    correlationId);

                return JwtValidationResult.Success(principal);
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(
                    "? JWT token expired | ExpirationTime: {ExpirationTime} | CorrelationId: {CorrelationId}",
                    ex.Expires,
                    correlationId);
                return JwtValidationResult.Failed("Token has expired");
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogError(ex,
                    "?? JWT token has invalid signature | CorrelationId: {CorrelationId}",
                    correlationId);
                return JwtValidationResult.Failed("Invalid token signature");
            }
            catch (SecurityTokenValidationException ex)
            {
                _logger.LogWarning(ex,
                    "? JWT token validation failed | Reason: {Reason} | CorrelationId: {CorrelationId}",
                    ex.Message,
                    correlationId);
                return JwtValidationResult.Failed("Token validation failed");
            }
        }

        private async Task<JwtValidationResult> PerformCustomValidationsAsync(
            ClaimsPrincipal principal, 
            SecurityToken validatedToken, 
            string correlationId)
        {
            // Validate required claims
            var requiredClaims = new[] { ClaimTypes.NameIdentifier, ClaimTypes.Name, ClaimTypes.Role };
            var missingClaims = requiredClaims.Where(claim => 
                !principal.Claims.Any(c => c.Type == claim)).ToList();
            
            if (missingClaims.Any())
            {
                _logger.LogWarning(
                    "?? JWT token missing required claims: {MissingClaims} | CorrelationId: {CorrelationId}",
                    string.Join(", ", missingClaims),
                    correlationId);
                return JwtValidationResult.Failed("Token missing required claims");
            }

            // Validate token is not in blacklist (future enhancement)
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (!string.IsNullOrEmpty(jti))
            {
                // TODO: Check against token blacklist
                // var isBlacklisted = await _tokenBlacklistService.IsBlacklistedAsync(jti);
                // if (isBlacklisted) return JwtValidationResult.Failed("Token is blacklisted");
            }

            return JwtValidationResult.Success(principal);
        }

        private static bool IsValidJwtFormat(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var parts = token.Split('.');
            if (parts.Length != 3)
                return false;

            // Validate each part is valid Base64
            foreach (var part in parts)
            {
                if (!IsValidBase64String(part))
                    return false;
            }

            return true;
        }

        private static bool IsValidBase64String(string base64String)
        {
            try
            {
                // JWT uses URL-safe Base64 encoding
                var padded = base64String.PadRight(base64String.Length + (4 - base64String.Length % 4) % 4, '=');
                var replaced = padded.Replace('-', '+').Replace('_', '/');
                Convert.FromBase64String(replaced);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ValidateLifetime(DateTime? notBefore, DateTime? expires, SecurityToken token, TokenValidationParameters parameters)
        {
            var now = DateTime.UtcNow;
            
            if (notBefore.HasValue && now < notBefore.Value)
                return false;
                
            if (expires.HasValue && now >= expires.Value)
                return false;

            return true;
        }

        private static string ExtractTokenFromHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return authHeader.Substring("Bearer ".Length).Trim();
        }

        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = $"gtw-{Guid.NewGuid():N}";
                context.Request.Headers["X-Correlation-Id"] = correlationId;
            }
            return correlationId;
        }

        private static bool IsPublicEndpoint(PathString path)
        {
            var publicPaths = new[] 
            { 
                "/health", 
                "/gateway/status", 
                "/gateway/routes",
                "/auth/token",
                "/auth/test-users"
            };
            
            return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }

        private async Task WriteUnauthorizedResponseAsync(HttpContext context, string errorMessage, string correlationId)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Unauthorized",
                message = errorMessage,
                correlationId = correlationId,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private class JwtValidationResult
        {
            public bool IsValid { get; private set; }
            public ClaimsPrincipal? ClaimsPrincipal { get; private set; }
            public string? ErrorMessage { get; private set; }

            private JwtValidationResult(bool isValid, ClaimsPrincipal? claimsPrincipal = null, string? errorMessage = null)
            {
                IsValid = isValid;
                ClaimsPrincipal = claimsPrincipal;
                ErrorMessage = errorMessage;
            }

            public static JwtValidationResult Success(ClaimsPrincipal claimsPrincipal) =>
                new(true, claimsPrincipal);

            public static JwtValidationResult Failed(string errorMessage) =>
                new(false, errorMessage: errorMessage);
        }
    }
}