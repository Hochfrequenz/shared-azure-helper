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
    }
}