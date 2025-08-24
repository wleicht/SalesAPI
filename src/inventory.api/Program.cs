using InventoryApi.Persistence;
using InventoryApi.Models;
using InventoryApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BuildingBlocks.Events.Infrastructure;
using BuildingBlocks.Events.Domain;
using Rebus.Config;
using Rebus.Routing.TypeBased;

/// <summary>
/// Main startup class for the InventoryApi application.
/// Configures services, middlewares, JWT authentication, event consumption with Rebus, and endpoints.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog((ctx, lc) => lc
            .WriteTo.Console()
            .ReadFrom.Configuration(ctx.Configuration));

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();

        // Configure Entity Framework
        builder.Services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                "Server=localhost;Database=InventoryDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"));

        // Configure Rebus with RabbitMQ
        var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ") ?? 
            "amqp://admin:admin123@localhost:5672/";

        var rebusConfigured = false;

        try
        {
            // Register message handlers first
            builder.Services.AddScoped<OrderConfirmedEventHandler>();

            builder.Services.AddRebus(configure => configure
                .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "inventory.api"))
                .Options(o => 
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                }));

            // Register handlers automatically
            builder.Services.AutoRegisterHandlersFromAssemblyOf<OrderConfirmedEventHandler>();

            // Register Event Publisher
            builder.Services.AddScoped<IEventPublisher, EventPublisher>();
            
            rebusConfigured = true;
            Log.Information("Rebus configured successfully for Inventory API");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure Rebus: {Message}", ex.Message);
        }

        // Fallback to dummy implementation if Rebus not configured
        if (!rebusConfigured)
        {
            Log.Information("Using dummy event publisher - events will be logged but not processed");
            builder.Services.AddScoped<IEventPublisher, DummyEventPublisher>();
        }

        // Configure JWT Authentication
        var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        
        // Authentication and Authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();
        app.MapHealthChecks("/health");

        var eventStatus = rebusConfigured ? "Rebus event consumption" : "dummy event logging";
        Log.Information("Inventory API starting with JWT authentication and {EventStatus}", eventStatus);
        app.Run();
    }
}
