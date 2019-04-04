using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace sharedLibNet
{
    public class LookupHelper
    {
        protected HttpClient httpClient = new HttpClient();
        protected ILogger _logger = null;
        public LookupHelper(ILogger logger)
        {
            _logger = logger;
        }
        public async Task<Dictionary<string, JArray>> RetrieveURLs(List<string> urls, string lookupURL, string clientCertString, string apiKey)
        {

            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.XArrClientCert))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.XArrClientCert);
            }

            httpClient.DefaultRequestHeaders.Add(CustomHeader.XArrClientCert, clientCertString);
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.OcpApimSubscriptionKey))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.OcpApimSubscriptionKey);
            }
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add(CustomHeader.OcpApimSubscriptionKey, apiKey);
            }
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(JsonConvert.SerializeObject(urls), System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            return JsonConvert.DeserializeObject<Dictionary<string, JArray>>(await responseMessage.Content.ReadAsStringAsync());
        }
        public async Task<Dictionary<string, JArray>> RetrieveURLsWithUserToken(List<string> urls, string lookupURL, string token, string apiKey)
        {

            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.Authorization))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.Authorization);
            }

            httpClient.DefaultRequestHeaders.Add(CustomHeader.Authorization, "Bearer " + token);
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.OcpApimSubscriptionKey))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.OcpApimSubscriptionKey);
            }
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add(CustomHeader.OcpApimSubscriptionKey, apiKey);
            }
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(JsonConvert.SerializeObject(urls), System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            return JsonConvert.DeserializeObject<Dictionary<string, JArray>>(await responseMessage.Content.ReadAsStringAsync());
        }
        public async Task<Dictionary<string, JArray>> LookupJsonWithUserToken(string json, string lookupURL, string token, string apiKey)
        {

            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.Authorization))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.Authorization);
            }

            httpClient.DefaultRequestHeaders.Add(CustomHeader.Authorization, "Bearer " + token);
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.OcpApimSubscriptionKey))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.OcpApimSubscriptionKey);
            }
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add(CustomHeader.OcpApimSubscriptionKey, apiKey);
            }
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(json, System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            return JsonConvert.DeserializeObject<Dictionary<string, JArray>>(await responseMessage.Content.ReadAsStringAsync());
        }
    }
}
