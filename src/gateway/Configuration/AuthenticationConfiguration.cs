using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Gateway.Services;

namespace Gateway.Configuration
{
    /// <summary>
    /// Extension methods for configuring authentication services.
    /// </summary>
    public static class AuthenticationConfiguration
    {
        public static IServiceCollection AddAuthenticationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtKey = configuration[ConfigurationKeys.JwtKey] 
                ?? throw new InvalidOperationException("JWT Key not configured");
            var jwtIssuer = configuration[ConfigurationKeys.JwtIssuer] 
                ?? throw new InvalidOperationException("JWT Issuer not configured");
            var jwtAudience = configuration[ConfigurationKeys.JwtAudience] 
                ?? throw new InvalidOperationException("JWT Audience not configured");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero // Zero tolerance for clock skew
                };

                // Enhanced JWT Bearer events for detailed logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = async context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("?? JWT Authentication failed: {Exception} | Path: {Path}", 
                            context.Exception?.Message, 
                            context.Request.Path);
                        
                        context.NoResult();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        
                        var response = new
                        {
                            error = "Authentication failed",
                            message = "Invalid or expired token",
                            path = context.Request.Path.Value,
                            timestamp = DateTime.UtcNow
                        };
                        
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                    },
                    
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        var username = context.Principal?.Identity?.Name ?? "Unknown";
                        logger.LogInformation("? JWT Token validated for user: {Username}", username);
                        return Task.CompletedTask;
                    },
                    
                    OnChallenge = async context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("?? JWT Challenge issued for path: {Path} | Error: {Error}", 
                            context.Request.Path, 
                            context.Error);
                        
                        // Override default challenge response
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        
                        var response = new
                        {
                            error = "Authorization required",
                            message = "A valid JWT token is required to access this resource",
                            path = context.Request.Path.Value,
                            timestamp = DateTime.UtcNow
                        };
                        
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                    },
                    
                    OnMessageReceived = context =>
                    {
                        // Log token receipt for debugging (without exposing token value)
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        var hasToken = !string.IsNullOrEmpty(context.Token);
                        logger.LogDebug("?? JWT Token received: {HasToken} | Path: {Path}", 
                            hasToken ? "Yes" : "No", 
                            context.Request.Path);
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                // Add custom authorization policies
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));
                    
                options.AddPolicy("CustomerOrAdmin", policy =>
                    policy.RequireRole("Customer", "Admin"));
            });

            // Register JWT Token Service
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            return services;
        }
    }
}