using Serilog;
using InventoryApi.Persistence;
using InventoryApi.Services;
using InventoryApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Prometheus;
using Rebus.ServiceProvider;
using Rebus.Config;
using Rebus.RabbitMq;
using BuildingBlocks.Events.Domain;

/// <summary>
/// Main startup class for the Inventory API with basic observability and event-driven architecture.
/// Configures database, authentication, health checks, RabbitMQ event consumption,
/// structured logging, correlation IDs, and Prometheus metrics.
/// </summary>

// Configure Serilog with structured logging for Inventory service
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Rebus", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationIdHeader("X-Correlation-Id")
    .Enrich.WithProperty("ServiceName", "Inventory")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] ?? {SourceContext} | {CorrelationId} | {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("?? Starting Inventory API service with basic observability and event processing");

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
        .Enrich.WithProperty("ServiceName", "Inventory")
        .WriteTo.Console(outputTemplate: 
            "[{Timestamp:HH:mm:ss} {Level:u3}] ?? {SourceContext} | {CorrelationId} | {Message:lj}{NewLine}{Exception}")
        .ReadFrom.Configuration(ctx.Configuration));

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Database configuration with enhanced logging and retry resilience
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    Log.Information("Configuring database connection for Inventory service with retry resilience");
    builder.Services.AddDbContext<InventoryDbContext>(options =>
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
    
    Log.Information("Configuring Rebus for event processing with RabbitMQ: {ConnectionString}", rabbitMqConnectionString);
    
    builder.Services.AddRebus((configure, serviceProvider) => configure
        .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "inventory.queue"))
        .Options(o =>
        {
            o.SetNumberOfWorkers(1);
            o.SetMaxParallelism(1);
        }));

    // Register all handlers from this assembly
    builder.Services.AutoRegisterHandlersFromAssemblyOf<OrderConfirmedEventHandler>();
    
    Log.Information("Registered event handlers for Inventory service");

    // Health checks with detailed monitoring
    builder.Services.AddHealthChecks()
        .AddCheck("inventory_health", () =>
        {
            Log.Information("Health check executed for Inventory service");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Inventory API is running");
        });

    // JWT Authentication configuration
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

    Log.Information("Configuring JWT authentication for Inventory service with issuer: {Issuer}", jwtIssuer);

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

    var app = builder.Build();

    // Configure the HTTP request pipeline
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
    Log.Information("Prometheus metrics endpoint available at /metrics for Inventory service");

    // Map controllers
    app.MapControllers();

    // Start Rebus and subscribe to events
    using (var scope = app.Services.CreateScope())
    {
        var bus = scope.ServiceProvider.GetRequiredService<Rebus.Bus.IBus>();
        await bus.Subscribe<OrderConfirmedEvent>();
        Log.Information("???? Subscribed to OrderConfirmedEvent for automatic stock deduction");
    }

    Log.Information("?? Inventory API starting with event processing, stock reservations, and basic observability");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "?? Inventory API service failed to start");
    throw;
}
finally
{
    Log.Information("?? Inventory API service shutting down");
    Log.CloseAndFlush();
}
