using Serilog;

namespace SalesApi.Configuration
{
    /// <summary>
    /// Extension methods for configuring Serilog logging.
    /// </summary>
    public static class LoggingConfiguration
    {
        private const string OutputTemplate = 
            "[{Timestamp:HH:mm:ss} {Level:u3}] ?? {SourceContext} | {CorrelationId} | {Message:lj}{NewLine}{Exception}";

        public static void ConfigureBootstrapLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Rebus", Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithCorrelationIdHeader("X-Correlation-Id")
                .Enrich.WithProperty("ServiceName", "Sales")
                .WriteTo.Console(outputTemplate: OutputTemplate)
                .CreateBootstrapLogger();
        }

        public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((ctx, lc) => lc
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Rebus", Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithCorrelationIdHeader("X-Correlation-Id")
                .Enrich.WithProperty("ServiceName", "Sales")
                .WriteTo.Console(outputTemplate: OutputTemplate)
                .ReadFrom.Configuration(ctx.Configuration));

            return builder;
        }
    }
}