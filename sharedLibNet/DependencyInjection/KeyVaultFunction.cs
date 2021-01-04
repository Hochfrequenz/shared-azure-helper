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

        public async Task<TOutput> InvokeAsync<TInput, TOutput>(TInput input, FunctionOptionBase options)
        {
            var keyOptions = options as KeyVaultFunctionOptions;
            _keyvaultClient = keyOptions.ClientId;
            _keyvaultSecret = keyOptions.ClientSecret;
            _keyvaultUrl = keyOptions.Url;
            KVClient = new KeyVaultClient(GetToken, keyOptions.Client);
            string elementName = input as string;
            TOutput result;
            if (typeof(TOutput) == typeof(SecretBundle))
            {
                Log?.LogDebug("Using GetSecretAsync");
                result = (TOutput)(object)(await KVClient.GetSecretAsync(_keyvaultUrl, elementName));
            }
            else
            {   //if(typeof(TOutput)==typeof(CertificateBundle))
                Log?.LogDebug("Using GetCertificateAsync");
                result = (TOutput)(object)await KVClient.GetCertificateAsync(_keyvaultUrl, elementName);
            }
            return result;
        }

        private static string _keyvaultClient;
        private static string _keyvaultSecret;
        private static string _keyvaultUrl;

        internal static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(
                _keyvaultClient, _keyvaultSecret
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
