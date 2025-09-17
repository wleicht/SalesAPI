using Serilog;
using Gateway.Configuration;
using Gateway.Middleware;
using Prometheus;

LoggingConfiguration.ConfigureBootstrapLogger();

try
{
    Log.Information("?? Starting Gateway service with enhanced observability");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog logging
    builder.ConfigureLogging();

    // Configure services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure application services
    builder.Services
        .AddAuthenticationServices(builder.Configuration)
        .AddReverseProxyServices(builder.Configuration);

    // Configure health checks
    builder.Services.AddHealthChecks()
        .AddCheck("gateway_health", () => 
        {
            Log.Information("Health check executed for Gateway service");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Gateway is running");
        });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    ConfigurePipeline(app);

    Log.Information("?? Gateway starting on {Address} with enhanced JWT security and observability", 
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

static void ConfigurePipeline(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway API V1");
            c.DocumentTitle = "SalesAPI Gateway with Enhanced JWT Security & Observability";
        });
        Log.Information("Swagger UI enabled for development environment");
    }

    // Enable Prometheus metrics collection
    app.UseRouting();
    app.UseHttpMetrics();
    
    // Correlation middleware should be early in the pipeline
    app.UseCorrelation();

    app.UseHttpsRedirection();

    // Enhanced JWT validation middleware (before authentication)
    app.UseMiddleware<EnhancedJwtValidationMiddleware>();

    // Authentication and Authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints
    app.MapHealthChecks("/health");
    app.MapMetrics();
    app.MapControllers();

    // Map YARP reverse proxy
    app.MapReverseProxy();

    Log.Information("?? Enhanced JWT validation middleware enabled");
    Log.Information("?? Prometheus metrics endpoint available at /metrics");
}
