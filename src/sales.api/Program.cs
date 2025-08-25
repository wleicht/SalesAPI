using Serilog;
using SalesApi.Persistence;
using SalesApi.Services;
using SalesApi.Middleware;
using SalesAPI.Services;
using BuildingBlocks.Events.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Prometheus;
using Rebus.ServiceProvider;
using Rebus.Config;
using Rebus.RabbitMq;

/// <summary>
/// Main startup class for the Sales API with basic observability and event-driven architecture.
/// Configures database, authentication, HTTP clients, health checks, RabbitMQ event publishing,
/// structured logging, correlation IDs, and Prometheus metrics.
/// </summary>

// Configure Serilog with structured logging for Sales service
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Rebus", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationIdHeader("X-Correlation-Id")
    .Enrich.WithProperty("ServiceName", "Sales")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] ?? {SourceContext} | {CorrelationId} | {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("?? Starting Sales API service with basic observability and event publishing");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog as the logging provider
    builder.Host.UseSerilog((ctx, lc) => lc
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Rebus", Serilog.Events.LogEventLevel.Information)
        .Enrich.FromLogContext()
        .Enrich.WithCorrelationIdHeader("X-Correlation-Id")
        .Enrich.WithProperty("ServiceName", "Sales")
        .WriteTo.Console(outputTemplate: 
            "[{Timestamp:HH:mm:ss} {Level:u3}] ?? {SourceContext} | {CorrelationId} | {Message:lj}{NewLine}{Exception}")
        .ReadFrom.Configuration(ctx.Configuration));

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Database configuration with enhanced logging and retry resilience
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    Log.Information("Configuring database connection for Sales service with retry resilience");
    builder.Services.AddDbContext<SalesDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            // Enable retry on failure for transient errors (deadlocks, timeouts, etc.)
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: new int[] { 1205 }); // Include deadlock detection
        }));

    // Configure Rebus for event-driven architecture
    var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ") 
        ?? "amqp://admin:admin123@host.docker.internal:5672/";
    
    Log.Information("Configuring Rebus for event publishing with RabbitMQ: {ConnectionString}", rabbitMqConnectionString);
    
    builder.Services.AddRebus((configure, serviceProvider) => configure
        .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "sales.queue"))
        .Options(o =>
        {
            o.SetNumberOfWorkers(1);
            o.SetMaxParallelism(1);
        }));

    // Health checks with detailed monitoring
    builder.Services.AddHealthChecks()
        .AddCheck("sales_health", () =>
        {
            Log.Information("Health check executed for Sales service");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Sales API is running");
        });

    // JWT Authentication configuration
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

    Log.Information("Configuring JWT authentication for Sales service with issuer: {Issuer}", jwtIssuer);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

    builder.Services.AddAuthorization();

    // Configure HTTP clients with correlation propagation
    var inventoryApiUrl = builder.Configuration["Services:InventoryApi"] ?? "http://localhost:5000";
    Log.Information("Configuring HTTP client for Inventory API at {InventoryApiUrl}", inventoryApiUrl);

    // Configure HTTP client for Inventory API with correlation propagation
    builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
    {
        client.BaseAddress = new Uri(inventoryApiUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler(() => new CorrelationHttpMessageHandler());

    // Configure HTTP client for Stock Reservations with correlation propagation  
    builder.Services.AddHttpClient<StockReservationClient>(client =>
    {
        client.BaseAddress = new Uri(inventoryApiUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler(() => new CorrelationHttpMessageHandler());

    // Register real event publisher for event-driven architecture
    builder.Services.AddScoped<IEventPublisher, RealEventPublisher>();
    Log.Information("Registered RealEventPublisher for event-driven architecture");

    var app = builder.Build();

    // Apply database migrations automatically
    Log.Information("??? Applying database migrations for Sales service");
    using (var scope = app.Services.CreateScope())
    {
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

    // Configure the HTTP request pipeline
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
    Log.Information("Prometheus metrics endpoint available at /metrics for Sales service");

    // Map controllers
    app.MapControllers();

    Log.Information("?? Sales API starting with event publishing, stock reservations, and basic observability");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "?? Sales API service failed to start");
    throw;
}
finally
{
    Log.Information("?? Sales API service shutting down");
    Log.CloseAndFlush();
}

/// <summary>
/// HTTP message handler for propagating correlation IDs in outgoing HTTP requests.
/// Ensures correlation context is maintained across service boundaries.
/// </summary>
public class CorrelationHttpMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // Extract correlation ID from current HTTP context or activity
        var correlationId = GetCurrentCorrelationId();
        
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static string? GetCurrentCorrelationId()
    {
        // Try to get correlation ID from current activity (set by middleware)
        var activity = System.Diagnostics.Activity.Current;
        if (activity?.GetTagItem("correlation_id") is string correlationId)
        {
            return correlationId;
        }

        // Fallback: generate new correlation ID for outgoing requests
        return $"sales-out-{Guid.NewGuid():N}";
    }
}
