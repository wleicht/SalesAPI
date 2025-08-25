using Serilog;
using Gateway.Services;
using Gateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Prometheus;

/// <summary>
/// Main startup class for the Gateway application with enhanced observability.
/// Configures YARP reverse proxy, JWT authentication, health checks, routing,
/// structured logging, correlation IDs, and Prometheus metrics.
/// </summary>

// Configure Serilog with structured logging and correlation support
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Yarp", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationIdHeader("X-Correlation-Id")
    .Enrich.WithProperty("ServiceName", "Gateway")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {CorrelationId} | {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("?? Starting Gateway service with enhanced observability");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog as the logging provider
    builder.Host.UseSerilog((ctx, lc) => lc
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
        .MinimumLevel.Override("Yarp", Serilog.Events.LogEventLevel.Information)
        .Enrich.FromLogContext()
        .Enrich.WithCorrelationIdHeader("X-Correlation-Id")
        .Enrich.WithProperty("ServiceName", "Gateway")
        .WriteTo.Console(outputTemplate: 
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {CorrelationId} | {Message:lj}{NewLine}{Exception}")
        .ReadFrom.Configuration(ctx.Configuration));

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure health checks with detailed information
    builder.Services.AddHealthChecks()
        .AddCheck("gateway_health", () => 
        {
            Log.Information("Health check executed for Gateway service");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Gateway is running");
        });

    // Configure JWT Authentication
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

    Log.Information("Configuring JWT authentication with issuer: {Issuer}", jwtIssuer);

    builder.Services.AddAuthentication(options =>
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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero // Remove delay of token when expired
        };
    });

    builder.Services.AddAuthorization();

    // Register JWT Token Service
    builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

    // Configure YARP Reverse Proxy with enhanced logging
    Log.Information("Configuring YARP reverse proxy");
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API V1");
            c.DocumentTitle = "SalesAPI Gateway with JWT Authentication & Observability";
        });
        Log.Information("Swagger UI enabled for development environment");
    }

    // Enable Prometheus metrics collection
    app.UseRouting();
    app.UseHttpMetrics(); // Collect HTTP metrics
    
    // Correlation middleware should be early in the pipeline
    app.UseCorrelation();

    app.UseHttpsRedirection();

    // Authentication and Authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Map health check endpoint
    app.MapHealthChecks("/health");

    // Map Prometheus metrics endpoint
    app.MapMetrics();
    Log.Information("Prometheus metrics endpoint available at /metrics");

    // Map controllers (for custom gateway endpoints and auth)
    app.MapControllers();

    // Map YARP reverse proxy
    app.MapReverseProxy();

    Log.Information("?? Gateway starting on {Address} with JWT authentication and observability enabled", 
        app.Environment.IsDevelopment() ? "http://localhost:6000" : "containerized environment");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "?? Gateway service failed to start");
    throw;
}
finally
{
    Log.Information("?? Gateway service shutting down");
    Log.CloseAndFlush();
}
