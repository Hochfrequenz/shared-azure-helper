namespace sharedLibNet.Helper
{
    public abstract class EnvironmentVariableNames
    {
        /// <summary>
        /// name of the environment variable containing the RayGun API key
        /// </summary>
        public const string ENV_RAYGUN_API_KEY = "RAYGUN_API";

        /// <summary>
        /// name of the environment variable containing the Hochfrequenz API Key
        /// </summary>
        public const string ENV_HF_API_KEY = "API_KEY";

        /// <summary>
        /// name of the environment variable containing the URL of the authentication service
        /// </summary>
        public const string ENV_AUTH_URL = "AUTH_URL";

        /// <summary>
        /// name of the environment variable containing the URL of the logging service
        /// </summary>
        public const string ENV_LOGGING_URL = "LOGGING_URL";

        /// <summary>
        /// name of the environment variable containing the URL of the lookup service
        /// </summary>
        public const string ENV_LOOKUP_URL = "LOOKUP_URL";

        /// <summary>
        /// name of the environment variable containing the URL of the lookup service
        /// </summary>
        public const string ENV_SERVICE_URL = "SERVICE_URL";

        /// <summary>
        /// name of environment variable containing the URL of Azure Key Vault
        /// </summary>
        /// <seealso cref="ENV_KEYVAULT_CLIENT"/>
        /// <seealso cref="ENV_KEYVAULT_SECRET"/>
        public const string ENV_KEYVAULT_URL = "KeyVaultURL";

        /// <summary>
        /// name of environment variable containing the KeyVault Client Id
        /// </summary>
        /// <seealso cref="ENV_KEYVAULT_SECRET"/>
        /// <seealso cref="ENV_KEYVAULT_URL"/>
        public const string ENV_KEYVAULT_CLIENT = "KeyVaultAppClientId";

        /// <summary>
        /// name of the environment variable containing the KeyVault Client Secret
        /// </summary>
        /// <seealso cref="ENV_KEYVAULT_CLIENT"/>
        /// <seealso cref="ENV_KEYVAULT_URL"/>
        public const string ENV_KEYVAULT_SECRET = "KeyVaultAppClientSecret";
    }
}