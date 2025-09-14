using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace InventoryApi.Configuration
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

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}