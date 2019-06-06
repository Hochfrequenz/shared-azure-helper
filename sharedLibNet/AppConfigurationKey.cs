namespace sharedLibNet
{
    public abstract class AppConfigurationKey
    {
        public const string API_KEY = "ApiGateway:Key";
        public const string AUTH_URL = "CoreServices:URL";
        public const string CLIENT_ID = "Auth0:SignalR:ClientId";
        public const string CLIENT_SECRET = "Auth0:SignalR:ClientSecret";
        public const string NEW_AUDIENCE = "Auth0:Audience";
        public const string ISSUER = "Auth0:Issuer";
        public const string AUDIENCE = "Auth0:Audience";
        public const string ACCESS_TOKEN = "ACCESS_TOKEN";
    }
}
