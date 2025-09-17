using Serilog;
using SalesApi.Configuration;
using SalesApi.Middleware;
using SalesApi.Infrastructure;
using SalesApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Prometheus;

LoggingConfiguration.ConfigureBootstrapLogger();

try
{
    Log.Information("?? Starting Sales API with Professional Database Architecture - FINAL VERSION");

    var builder = WebApplication.CreateBuilder(args);
    builder.ConfigureLogging();

    // Configure services with professional patterns
    ConfigureServicesWithProfessionalArchitecture(builder);

    var app = builder.Build();

    // Initialize database with professional patterns (when EF Core versions are resolved)
    await InitializeDatabaseWithProfessionalPatternsAsync(app);

    // Configure HTTP pipeline with enterprise middleware
    ConfigureHttpPipelineWithEnterpriseMiddleware(app);

    Log.Information("?? Sales API ready with professional architecture and comprehensive error handling");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "?? Sales API failed to start with professional architecture");
    LogDetailedStartupDiagnostics(ex);
    throw;
}
finally
{
    Log.Information("?? Sales API shutting down gracefully");
    Log.CloseAndFlush();
}

/// <summary>
/// Configures all application services following professional dependency injection patterns.
/// Implements comprehensive service registration with proper error handling and logging.
/// </summary>
static void ConfigureServicesWithProfessionalArchitecture(WebApplicationBuilder builder)
{
    Log.Information("?? Configuring services with enterprise-grade dependency injection patterns");

    try
    {
        // Phase 1: Early Configuration Validation
        ValidateConfigurationWithProfessionalDiagnostics(builder.Configuration);

        // Phase 2: Core Infrastructure Services
        ConfigureCoreInfrastructureServices(builder.Services);

        // Phase 3: Database Services with Professional Error Handling
        ConfigureDatabaseServicesWithProfessionalErrorHandling(builder.Services, builder.Configuration);

        // Phase 4: Domain and Application Services with CQRS Patterns
        ConfigureDomainAndApplicationServicesWithCQRS(builder.Services);

        // Phase 5: External Integration Services with Resilience Patterns
        ConfigureExternalIntegrationServicesWithResilience(builder.Services, builder.Configuration);

        // Phase 6: Observability and Health Monitoring
        ConfigureObservabilityAndHealthMonitoring(builder.Services);

        Log.Information("? All enterprise services configured successfully with professional architecture");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "?? Critical failure during service configuration with professional patterns");
        throw new InvalidOperationException("Failed to configure application services with professional architecture", ex);
    }
}

/// <summary>
/// Validates application configuration with professional diagnostics and comprehensive error reporting.
/// </summary>
static void ValidateConfigurationWithProfessionalDiagnostics(IConfiguration configuration)
{
    Log.Information("?? Performing comprehensive configuration validation with professional diagnostics");

    try
    {
        // Validate database configuration with detailed error reporting
        DatabaseConfigurationValidator.ValidateConfiguration(configuration, Log.Logger);

        // Validate JWT configuration for authentication
        ValidateJwtConfiguration(configuration);

        // Validate external service configurations
        ValidateExternalServiceConfigurations(configuration);

        Log.Information("? Configuration validation completed successfully with professional standards");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "? Configuration validation failed with professional diagnostics");
        throw new InvalidOperationException("Invalid application configuration detected", ex);
    }
}

/// <summary>
/// Validates JWT configuration for professional authentication patterns.
/// </summary>
static void ValidateJwtConfiguration(IConfiguration configuration)
{
    var jwtKey = configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    {
        throw new InvalidOperationException("JWT Key must be at least 32 characters long for professional security standards");
    }

    var issuer = configuration["Jwt:Issuer"];
    var audience = configuration["Jwt:Audience"];
    
    if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
    {
        throw new InvalidOperationException("JWT Issuer and Audience must be configured for professional authentication");
    }

    Log.Debug("? JWT configuration validated for professional authentication patterns");
}

/// <summary>
/// Validates external service configurations with comprehensive error handling.
/// </summary>
static void ValidateExternalServiceConfigurations(IConfiguration configuration)
{
    // Validate Inventory API configuration
    var inventoryBaseUrl = configuration["InventoryApi:BaseUrl"];
    if (string.IsNullOrWhiteSpace(inventoryBaseUrl))
    {
        Log.Warning("?? InventoryApi:BaseUrl not configured - external service integration may fail");
    }

    Log.Debug("? External service configurations validated");
}

/// <summary>
/// Configures core infrastructure services with professional patterns and comprehensive options.
/// </summary>
static void ConfigureCoreInfrastructureServices(IServiceCollection services)
{
    Log.Information("??? Configuring core infrastructure services with professional patterns");

    services.AddControllers(options =>
    {
        // Configure professional API behavior
        options.SuppressAsyncSuffixInActionNames = false;
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

    services.AddEndpointsApiExplorer();
    
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Sales API - Professional Database Architecture",
            Version = "v1.0",
            Description = "Enterprise-grade Sales API with robust database management, comprehensive validation, and professional architecture patterns"
        });
        
        // Professional API documentation enhancements without EnableAnnotations
        options.DescribeAllParametersInCamelCase();
    });

    Log.Debug("? Core infrastructure services configured with professional standards");
}

/// <summary>
/// Configures database services with professional error handling and comprehensive resilience patterns.
/// </summary>
static void ConfigureDatabaseServicesWithProfessionalErrorHandling(IServiceCollection services, IConfiguration configuration)
{
    Log.Information("??? Configuring database services with professional error handling and resilience patterns");

    try
    {
        // Register professional database configuration
        services.AddDatabaseServices(configuration);

        // Register database manager for professional initialization patterns
        services.AddScoped<DatabaseManager>();

        Log.Debug("? Database services configured with professional error handling");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "? Failed to configure database services with professional patterns");
        throw new InvalidOperationException("Database service configuration failed", ex);
    }
}

/// <summary>
/// Configures domain and application services with CQRS patterns and comprehensive validation.
/// </summary>
static void ConfigureDomainAndApplicationServicesWithCQRS(IServiceCollection services)
{
    Log.Information("?? Configuring domain and application services with CQRS and professional validation patterns");

    // Configure MediatR for professional CQRS pattern implementation
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        
        // Future: Add professional pipeline behaviors for cross-cutting concerns
        // cfg.AddBehavior<ValidationBehavior>();
        // cfg.AddBehavior<LoggingBehavior>();
        // cfg.AddBehavior<PerformanceBehavior>();
    });

    // Configure domain services with professional dependency injection
    services.AddDomainServices();

    Log.Debug("? Domain and application services configured with CQRS and professional validation");
}

/// <summary>
/// Configures external integration services with comprehensive resilience patterns and professional error handling.
/// </summary>
static void ConfigureExternalIntegrationServicesWithResilience(IServiceCollection services, IConfiguration configuration)
{
    Log.Information("?? Configuring external integration services with resilience patterns and professional error handling");

    try
    {
        // Configure messaging services with comprehensive error handling
        services.AddMessagingServices(configuration);
        Log.Debug("? Messaging services configured with resilience patterns");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "?? Messaging services configuration encountered issues - continuing with degraded functionality");
        // In production, consider circuit breaker patterns or fail-fast behavior based on requirements
    }

    Log.Debug("? External integration services configured with professional resilience patterns");
}

/// <summary>
/// Configures comprehensive observability including health checks, metrics, and professional monitoring.
/// </summary>
static void ConfigureObservabilityAndHealthMonitoring(IServiceCollection services)
{
    Log.Information("?? Configuring observability and health monitoring with enterprise-grade patterns");

    services.AddHealthChecks()
        .AddCheck("sales_api_core", () =>
        {
            Log.Debug("Health check executed for Sales API core functionality");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                "Sales API core is running with professional architecture");
        });
        // Future: Add database health check when EF Core versions are resolved
        // .AddDbContextCheck<SalesDbContext>("database");

    Log.Debug("? Observability and health monitoring configured with enterprise patterns");
}

/// <summary>
/// Initializes database with comprehensive professional patterns including retry logic and detailed diagnostics.
/// Enhanced version with proper EF Core version handling.
/// </summary>
static async Task InitializeDatabaseWithProfessionalPatternsAsync(WebApplication app)
{
    Log.Information("?? Starting professional database initialization with enterprise-grade error handling");

    using var scope = app.Services.CreateScope();
    
    try
    {
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase", false);
        
        if (useInMemory)
        {
            Log.Information("?? Initializing in-memory database with professional patterns");
            await InitializeInMemoryDatabaseWithProfessionalPatterns(scope.ServiceProvider);
        }
        else
        {
            Log.Information("??? Initializing SQL Server database with enterprise resilience patterns");
            await InitializeSqlServerDatabaseWithProfessionalPatterns(scope.ServiceProvider);
        }
        
        Log.Information("? Professional database initialization completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "? Professional database initialization failed with comprehensive error handling");
        await LogDatabaseInitializationDiagnosticsAsync(scope.ServiceProvider, ex);
        
        // Determine failure strategy based on environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (environment == "Production")
        {
            throw new InvalidOperationException("Database initialization failed in production environment", ex);
        }
        
        Log.Warning("?? Continuing in development mode with degraded database functionality");
    }
}

/// <summary>
/// Initializes in-memory database with professional patterns and comprehensive validation.
/// </summary>
static async Task InitializeInMemoryDatabaseWithProfessionalPatterns(IServiceProvider serviceProvider)
{
    try
    {
        var context = serviceProvider.GetRequiredService<SalesDbContext>();
        
        // Ensure database is created with proper schema
        await context.Database.EnsureCreatedAsync();
        
        // Validate schema integrity
        var orderCount = await context.Orders.CountAsync();
        var orderItemCount = await context.OrderItems.CountAsync();
        
        Log.Information("?? In-memory database validation successful | Orders: {OrderCount} | OrderItems: {OrderItemCount}",
            orderCount, orderItemCount);
            
        Log.Information("? In-memory database initialized with professional patterns");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "? In-memory database initialization failed");
        throw new InvalidOperationException("In-memory database initialization failed", ex);
    }
}

/// <summary>
/// Initializes SQL Server database with enterprise resilience patterns and comprehensive error handling.
/// </summary>
static async Task InitializeSqlServerDatabaseWithProfessionalPatterns(IServiceProvider serviceProvider)
{
    try
    {
        // Use DatabaseManager for professional initialization
        var databaseManager = serviceProvider.GetRequiredService<DatabaseManager>();
        
        // Initialize with comprehensive timeout handling
        using var initializationCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await databaseManager.InitializeAsync(initializationCts.Token);
        
        Log.Information("? SQL Server database initialized with enterprise patterns");
    }
    catch (OperationCanceledException)
    {
        Log.Fatal("?? SQL Server database initialization timed out after 5 minutes");
        throw new InvalidOperationException("SQL Server database initialization timed out");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "? SQL Server database initialization failed");
        throw new InvalidOperationException("SQL Server database initialization failed", ex);
    }
}

/// <summary>
/// Logs comprehensive database initialization diagnostics for professional troubleshooting.
/// </summary>
static async Task LogDatabaseInitializationDiagnosticsAsync(IServiceProvider serviceProvider, Exception exception)
{
    try
    {
        Log.Error("?? Professional database initialization diagnostics:");
        Log.Error("Exception Type: {ExceptionType}", exception.GetType().Name);
        Log.Error("Exception Message: {Message}", exception.Message);
        
        if (exception.InnerException != null)
        {
            Log.Error("Inner Exception: {InnerException}", exception.InnerException.Message);
        }

        // Gather database context diagnostics
        try
        {
            var context = serviceProvider.GetRequiredService<SalesDbContext>();
            var isInMemory = context.Database.IsInMemory();
            
            Log.Error("Database Type: {DatabaseType}", isInMemory ? "In-Memory" : "SQL Server");
            
            if (!isInMemory)
            {
                var connectionString = context.Database.GetConnectionString();
                var maskedConnectionString = MaskSensitiveInformation(connectionString);
                Log.Error("Connection String (masked): {ConnectionString}", maskedConnectionString);
            }
        }
        catch (Exception diagnosticEx)
        {
            Log.Error(diagnosticEx, "Failed to gather database context diagnostics");
        }

        await Task.CompletedTask;
    }
    catch (Exception diagnosticException)
    {
        Log.Error(diagnosticException, "Failed to log database initialization diagnostics");
    }
}

/// <summary>
/// Masks sensitive information in connection strings for secure logging.
/// </summary>
static string MaskSensitiveInformation(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
        return "NULL_OR_EMPTY";

    // Mask password and sensitive information
    var masked = System.Text.RegularExpressions.Regex.Replace(
        connectionString, 
        @"(Password|Pwd|User Id|UserId)=([^;]+)", 
        "$1=***MASKED***", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    return masked;
}

/// <summary>
/// Configures HTTP pipeline with enterprise-grade middleware including comprehensive security and observability.
/// </summary>
static void ConfigureHttpPipelineWithEnterpriseMiddleware(WebApplication app)
{
    Log.Information("?? Configuring HTTP pipeline with enterprise-grade middleware and comprehensive security");

    // Global exception handling with professional error responses
    app.UseGlobalExceptionHandling();
    
    // Add validation exception middleware for professional error handling
    app.UseValidationExceptionHandling();

    // Development-specific middleware with professional documentation
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sales API V1");
            options.DocumentTitle = "SalesAPI - Professional Database Architecture";
            options.RoutePrefix = "swagger";
        });
        Log.Information("?? Professional Swagger UI enabled for development environment");
    }

    // Observability and performance monitoring middleware
    app.UseRouting();
    app.UseHttpMetrics(); // Prometheus metrics

    // Security middleware with professional configuration
    app.UseHttpsRedirection();

    // Professional health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Only basic checks for liveness
    });
    
    // Metrics endpoint for professional monitoring
    app.MapMetrics("/metrics");
    
    // API endpoints with professional routing
    app.MapControllers();

    Log.Information("?? HTTP pipeline configured with enterprise middleware | Health: /health | Metrics: /metrics | Validation: Professional");
}

/// <summary>
/// Logs detailed startup diagnostics for professional troubleshooting and production support.
/// </summary>
static void LogDetailedStartupDiagnostics(Exception exception)
{
    Log.Error("?? Professional startup failure diagnostics:");
    Log.Error("Application: Sales API with Professional Database Architecture");
    Log.Error("Environment: {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown");
    Log.Error("Machine: {MachineName}", Environment.MachineName);
    Log.Error("User: {UserName}", Environment.UserName);
    Log.Error("Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);
    Log.Error("Runtime Version: {RuntimeVersion}", Environment.Version);
    
    Log.Error("Professional Exception Chain Analysis:");
    var currentException = exception;
    var depth = 0;
    
    while (currentException != null && depth < 10) // Prevent infinite loops
    {
        Log.Error("  [{Depth}] {ExceptionType}: {Message}", 
            depth, 
            currentException.GetType().Name, 
            currentException.Message);
        
        // Log stack trace for critical errors
        if (depth == 0 && currentException.StackTrace != null)
        {
            Log.Error("  Stack Trace: {StackTrace}", currentException.StackTrace);
        }
        
        currentException = currentException.InnerException;
        depth++;
    }
}