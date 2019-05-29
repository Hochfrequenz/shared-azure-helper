using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using sharedLibNet.DependencyInjection.Interfaces;

namespace sharedLibNet.DependencyInjection
{
    public class KeyVaultFunction : IKeyVaultFunction
    {
        public ILogger Log { get; set; }
        protected KeyVaultClient KVClient;
        public KeyVaultFunction()
        {

        }
        public async Task<TOutput> InvokeAsync<TInput, TOutput>(TInput input, FunctionOptionBase options)
        {
            var keyOptions = options as KeyVaultFunctionOptions;
            keyvault_client = keyOptions.ClientId;
            keyvault_secret = keyOptions.ClientSecret;
            keyvault_url = keyOptions.Url;
            KVClient = new KeyVaultClient(GetToken, keyOptions.Client);
            string certName = input as string;
            var cert = await KVClient.GetCertificateAsync(keyvault_url, certName);
            return (TOutput)(object)cert;
        }
        private static string keyvault_client;
        private static string keyvault_secret;
        private static string keyvault_url;

        internal static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(
                keyvault_client, keyvault_secret
                );
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the key vault token");
            }

            return result.AccessToken;
        }
    }
}
