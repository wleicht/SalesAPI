using Serilog;

/// <summary>
/// Main startup class for the Gateway application.
/// Configures YARP reverse proxy, health checks, and routing.
/// </summary>

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .ReadFrom.Configuration(ctx.Configuration));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure health checks
builder.Services.AddHealthChecks();

// Configure YARP Reverse Proxy
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
        c.DocumentTitle = "SalesAPI Gateway";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map health check endpoint
app.MapHealthChecks("/health");

// Map controllers (for custom gateway endpoints if needed)
app.MapControllers();

// Map YARP reverse proxy
app.MapReverseProxy();

Log.Information("Gateway starting on {Address}", "http://localhost:6000");
app.Run();
