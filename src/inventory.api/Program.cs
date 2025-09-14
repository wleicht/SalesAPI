using Serilog;
using InventoryApi.Configuration;
using InventoryApi.Middleware;
using InventoryApi.Persistence;
using Microsoft.EntityFrameworkCore;
using Prometheus;

LoggingConfiguration.ConfigureBootstrapLogger();

try
{
    Log.Information("?? Starting Inventory API service with basic observability and event processing");

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
        .AddMessagingServices(builder.Configuration);

    // Configure health checks
    builder.Services.AddHealthChecks()
        .AddCheck("inventory_health", () =>
        {
            Log.Information("Health check executed for Inventory service");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Inventory API is running");
        });

    var app = builder.Build();

    // Apply database migrations automatically
    await ApplyDatabaseMigrations(app);

    // Configure the HTTP request pipeline
    ConfigurePipeline(app);

    // Subscribe to events
    await app.SubscribeToEventsAsync();
    Log.Information("?? Subscribed to OrderConfirmedEvent for automatic stock deduction");

    Log.Information("?? Inventory API starting with event processing, stock reservations, and basic observability");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "? Inventory API service failed to start");
    throw;
}
finally
{
    Log.Information("?? Inventory API service shutting down");
    Log.CloseAndFlush();
}

static async Task ApplyDatabaseMigrations(WebApplication app)
{
    Log.Information("?? Applying database migrations for Inventory service");
    using var scope = app.Services.CreateScope();
    
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await context.Database.MigrateAsync();
        Log.Information("? Database migrations applied successfully for Inventory service");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "? Failed to apply database migrations for Inventory service");
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
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API V1");
            c.DocumentTitle = "SalesAPI Inventory with Stock Reservations, Event Processing & Basic Observability";
        });
        Log.Information("Swagger UI enabled for Inventory service in development");
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

    Log.Information("Prometheus metrics endpoint available at /metrics for Inventory service");
}
