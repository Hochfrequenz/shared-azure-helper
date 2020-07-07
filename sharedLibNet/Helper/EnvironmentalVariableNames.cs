namespace sharedLibNet.Helper
{
    public abstract class EnvironmentVariableNames
    {
        /// <summary>
        /// name of the environment variable containing the RayGun API key
        /// </summary>
        public const string ENV_RAYGUN_API_KEY = "Raygun:ApiKey";

        /// <summary>
        /// name of the environment variable containing the Hochfrequenz API Key
        /// </summary>
        public const string ENV_HF_API_KEY = "ApiGateway:Key";

        /// <summary>
        /// name of the environment variable containing the URL of the authentication service
        /// </summary>
        public const string ENV_AUTH_URL = "CoreServices:URL";

        /// <summary>
        /// name of the environment variable containing the URL of the lookup service
        /// </summary>
        public const string ENV_SERVICE_URL = "EnergyFunctions:URL";

        /// <summary>
        /// name of environment variable containing the URL of Azure Key Vault
        /// </summary>
        /// <seealso cref="ENV_KEYVAULT_CLIENT"/>
        /// <seealso cref="ENV_KEYVAULT_SECRET"/>
        public const string ENV_KEYVAULT_URL = "KeyVault:URL";

        /// <summary>
        /// name of environment variable containing the KeyVault Client Id
        /// </summary>
        /// <seealso cref="ENV_KEYVAULT_SECRET"/>
        /// <seealso cref="ENV_KEYVAULT_URL"/>
        public const string ENV_KEYVAULT_CLIENT = "KeyVault:ClientId";

        /// <summary>
        /// name of the environment variable containing the KeyVault Client Secret
        /// </summary>
        /// <seealso cref="ENV_KEYVAULT_CLIENT"/>
        /// <seealso cref="ENV_KEYVAULT_URL"/>
        public const string ENV_KEYVAULT_SECRET = "KeyVault:ClientSecret";


        /// <summary>
        /// connection string for loading configuration from azure portal
        /// </summary>
        public const string ENV_CONFIG_CONNECTION = "AzureAppConfigConnectionString";

        /// <summary>
        /// system context/environment; e.g. 'Stage' or 'Production'
        /// </summary>
        public const string SYSTEM_ENVIRONMENT = "Environment";

        /// <summary>
        /// requested audience for the jwt token
        /// </summary>
        public const string ENV_AUDIENCE = "Auth0:Audience";

        /// <summary>
        /// issuer of the JWT token
        /// </summary>
        public const string ENV_ISSUER = "Auth0:Issuer";

        /// <summary>
        /// name of the environment variable containing the client ID for SAP Cloud Connector
        /// </summary>
        public const string ENV_SCC_CLIENT = "CloudConnector:ClientId";

        /// <summary>
        /// name of the environment variable containing the secret for the client specified in <see cref="ENV_SCC_CLIENT"/>
        /// </summary>
        public const string ENV_SCC_SECRET = "CloudConnector:ClientSecret";


        /// <summary>
        /// name of the environment variable containing the URL of the cloud connector authentication service
        /// </summary>
        public const string ENV_SCC_AUTH_URL = "CloudConnector:Auth:URL";
    }
}