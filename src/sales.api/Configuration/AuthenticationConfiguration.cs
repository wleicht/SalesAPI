using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SalesApi.Configuration.Constants;
using SalesApi.Configuration.Validation;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Extension methods for configuring authentication services.
    /// </summary>
    public static class AuthenticationConfiguration
    {
        public static IServiceCollection AddAuthenticationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger? logger = null)
        {
            // Validate JWT configuration before use
            var jwtConfig = GetValidatedJwtConfiguration(configuration, logger);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtConfig.Issuer,
                        ValidAudience = jwtConfig.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key)),
                        ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
                    };

                    // Configure event handlers for better debugging
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            logger?.LogWarning("JWT Authentication failed: {Error}", context.Exception.Message);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            logger?.LogDebug("JWT Token validated successfully for user: {User}", 
                                context.Principal?.Identity?.Name ?? "Unknown");
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            logger?.LogInformation("JWT Authentication configured successfully");
            return services;
        }

        private static JwtConfiguration GetValidatedJwtConfiguration(IConfiguration configuration, ILogger? logger)
        {
            var jwtKey = configuration[ConfigurationKeys.JwtKey] 
                ?? SecurityConstants.Development.DevJwtKey;
            var jwtIssuer = configuration[ConfigurationKeys.JwtIssuer] 
                ?? SecurityConstants.Jwt.DefaultIssuer;
            var jwtAudience = configuration[ConfigurationKeys.JwtAudience] 
                ?? SecurityConstants.Jwt.DefaultAudience;

            // Validate key length
            if (jwtKey.Length < SecurityConstants.Jwt.MinimumKeyLength)
            {
                throw new InvalidOperationException(
                    $"JWT Key is too short. Minimum length is {SecurityConstants.Jwt.MinimumKeyLength} characters.");
            }

            // Warn if using development defaults
            if (jwtKey == SecurityConstants.Development.DevJwtKey)
            {
                logger?.LogWarning("?? Using development JWT key. This should NEVER be used in production!");
            }

            return new JwtConfiguration
            {
                Key = jwtKey,
                Issuer = jwtIssuer,
                Audience = jwtAudience
            };
        }
    }

    /// <summary>
    /// JWT configuration data transfer object.
    /// </summary>
    public class JwtConfiguration
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
    }
}