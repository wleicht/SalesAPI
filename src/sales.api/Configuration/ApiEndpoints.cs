namespace SalesApi.Configuration
{
    /// <summary>
    /// Constants for API endpoints used throughout the application.
    /// </summary>
    public static class ApiEndpoints
    {
        public const string Health = "/health";
        public const string Metrics = "/metrics";
        
        public static class Orders
        {
            public const string Base = "/api/orders";
            public const string ById = Base + "/{id}";
            public const string Create = Base;
            public const string Cancel = Base + "/{id}/cancel";
            public const string Confirm = Base + "/{id}/confirm";
        }

        public static class Products
        {
            public const string Base = "/api/products";
            public const string ById = Base + "/{id}";
            public const string Search = Base + "/search";
            public const string Stock = Base + "/{id}/stock";
        }

        public static class Auth
        {
            public const string Base = "/auth";
            public const string Token = Base + "/token";
            public const string Refresh = Base + "/refresh";
        }
    }
}