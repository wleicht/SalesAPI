using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SalesApi.Persistence;
using SalesApi.Services;

/// <summary>
/// Main startup class for the SalesApi application.
/// Configures services, middlewares, and endpoints.
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

// Configure Inventory HTTP Client with Polly
var inventoryApiUrl = builder.Configuration.GetValue<string>("InventoryApi:BaseUrl") ?? "http://localhost:5000/";
builder.Services.AddInventoryClient(inventoryApiUrl);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
