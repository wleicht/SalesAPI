using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SalesApi.Persistence;
using SalesApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BuildingBlocks.Events.Infrastructure;
using Rebus.Config;
using Rebus.Routing.TypeBased;

/// <summary>
/// Main startup class for the SalesApi application.
/// Configures services, middlewares, JWT authentication, event publishing with Rebus, and endpoints.
/// </summary>

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Configure Entity Framework for Sales
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
        "Server=localhost;Database=SalesDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"));

// Configure HTTP clients
builder.Services.AddHttpClient<SalesApi.Services.IInventoryClient, SalesApi.Services.InventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InventoryApi:BaseUrl"] ?? "http://localhost:5000/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<SalesAPI.Services.StockReservationClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InventoryApi:BaseUrl"] ?? "http://localhost:5000/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configure Rebus with RabbitMQ
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ") ?? 
    "amqp://admin:admin123@localhost:5672/";

try
{
    builder.Services.AddRebus(configure => configure
        .Transport(t => t.UseRabbitMq(rabbitMqConnectionString, "sales.api"))
        .Options(o => 
        {
            o.SetNumberOfWorkers(1);
            o.SetMaxParallelism(1);
        }));

    // Register Event Publisher
    builder.Services.AddScoped<IEventPublisher, SalesAPI.Services.EventPublisher>();
    
    Log.Information("Rebus configured successfully for Sales API");
}
catch (Exception ex)
{
    Log.Warning(ex, "Failed to configure Rebus, using dummy event publisher");
    builder.Services.AddScoped<IEventPublisher, SalesAPI.Services.DummyEventPublisher>();
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
    options.AddPolicy("CustomerOrAdmin", policy => policy.RequireRole("customer", "admin"));
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

Log.Information("Sales API starting with JWT authentication and Rebus event publishing enabled");
app.Run();
