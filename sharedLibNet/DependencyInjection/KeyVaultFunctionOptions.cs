using System.Net.Http;

namespace sharedLibNet.DependencyInjection
{
    public class KeyVaultFunctionOptions : FunctionOptionBase
    {
        public HttpClient Client { get; set; }
        public string Url { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
        public KeyVaultFunctionOptions(HttpClient client, string url, string clientId, string clientSecret)
        {
            Client = client;
            Url = url;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }
    }
}
