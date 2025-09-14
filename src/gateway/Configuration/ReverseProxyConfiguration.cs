namespace Gateway.Configuration
{
    /// <summary>
    /// Extension methods for configuring YARP reverse proxy services.
    /// </summary>
    public static class ReverseProxyConfiguration
    {
        public static IServiceCollection AddReverseProxyServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddReverseProxy()
                .LoadFromConfig(configuration.GetSection(ConfigurationKeys.ReverseProxySection));

            return services;
        }
    }
}