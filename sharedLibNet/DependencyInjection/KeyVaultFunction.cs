using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using sharedLibNet.DependencyInjection.Interfaces;

using System;
using System.Threading.Tasks;

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
            string elementName = input as string;
            TOutput result;
            if (typeof(TOutput) == typeof(SecretBundle))
            {
                if (Log != null)
                {
                    Log.LogDebug("Using GetSecretAsync");
                }
                result = (TOutput)(object)(await KVClient.GetSecretAsync(keyvault_url, elementName));
            }
            else
            {   //if(typeof(TOutput)==typeof(CertificateBundle))
                if (Log != null)
                {
                    Log.LogDebug("Using GetCertificateAsync");
                }
                result = (TOutput)(object)await KVClient.GetCertificateAsync(keyvault_url, elementName);
            }
            return result;
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
