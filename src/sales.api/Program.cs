using Serilog;
using SalesApi.Configuration;
using SalesApi.Middleware;
using SalesApi.Persistence;
using Microsoft.EntityFrameworkCore;
using Prometheus;

LoggingConfiguration.ConfigureBootstrapLogger();

try
{
    Log.Information("?? Starting Sales API service with basic observability and event publishing");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog logging
    builder.ConfigureLogging();

    // Configure services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure application services
    builder.Services
        .AddDatabaseServices(builder.Configuration)
        .AddAuthenticationServices(builder.Configuration)
        .AddMessagingServices(builder.Configuration)
        .AddHttpClientServices(builder.Configuration)
        .AddDomainServices();

    // Configure health checks
    builder.Services.AddHealthChecks()
        .AddCheck("sales_health", () =>
        {
            Log.Information("Health check executed for Sales service");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Sales API is running");
        });

    var app = builder.Build();

    // Apply database migrations automatically
    await ApplyDatabaseMigrations(app);

    // Configure the HTTP request pipeline
    ConfigurePipeline(app);

    Log.Information("?? Sales API starting with event publishing, stock reservations, and basic observability");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "? Sales API service failed to start");
    throw;
}
finally
{
    Log.Information("?? Sales API service shutting down");
    Log.CloseAndFlush();
}

static async Task ApplyDatabaseMigrations(WebApplication app)
{
    Log.Information("?? Applying database migrations for Sales service");
    using var scope = app.Services.CreateScope();
    
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        await context.Database.MigrateAsync();
        Log.Information("? Database migrations applied successfully for Sales service");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "? Failed to apply database migrations for Sales service");
        throw;
    }
}

static void ConfigurePipeline(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sales API V1");
            c.DocumentTitle = "SalesAPI Sales with Stock Reservations, Event Publishing & Basic Observability";
        });
        Log.Information("Swagger UI enabled for Sales service in development");
    }

    // Enable Prometheus metrics collection
    app.UseRouting();
    app.UseHttpMetrics();

    // Correlation middleware should be early in the pipeline
    app.UseCorrelation();

    app.UseHttpsRedirection();

    // Authentication and Authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints
    app.MapHealthChecks("/health");
    app.MapMetrics();
    app.MapControllers();

    Log.Information("Prometheus metrics endpoint available at /metrics for Sales service");
}
