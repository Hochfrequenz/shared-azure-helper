using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BO4E.meta;
using EshDataExchangeFormats.lookup;
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
        public async Task<JObject> RetrieveURLs(List<string> urls, string lookupURL, string clientCertString, string apiKey, string backendId)
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
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.BackendId))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.BackendId);
            }
            if (!string.IsNullOrEmpty(backendId))
            {
                httpClient.DefaultRequestHeaders.Add(CustomHeader.BackendId, backendId);
            }
            GenericLookupQuery urlObject = new GenericLookupQuery()
            {
                uris = urls.Select(su => new Bo4eUri(su)).ToList<Bo4eUri>()
            };
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(JsonConvert.SerializeObject(urlObject), System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            JObject resultObject = null;
            try
            {
                resultObject = (JsonConvert.DeserializeObject<JObject>(await responseMessage.Content.ReadAsStringAsync()));
                return resultObject["result"]?.Value<JObject>();
            }
            catch (Exception e)
            {
                throw new Exception($"Could not de-serialize result from {JsonConvert.SerializeObject(resultObject)} ", e);
            }
        }
        public async Task<JObject> RetrieveURLsWithUserToken(List<string> urls, string lookupURL, string token, string apiKey, string backendId)
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
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.BackendId))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.BackendId);
            }
            if (!string.IsNullOrEmpty(backendId))
            {
                httpClient.DefaultRequestHeaders.Add(CustomHeader.BackendId, backendId);
            }

            GenericLookupQuery urlObject = new GenericLookupQuery()
            {
                uris = urls.Select(su => new Bo4eUri(su)).ToList<Bo4eUri>()
            };

            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(JsonConvert.SerializeObject(urlObject), System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            JObject resultObject = null;
            try
            {
                resultObject = (JsonConvert.DeserializeObject<JObject>(await responseMessage.Content.ReadAsStringAsync()));
                return resultObject["result"]?.Value<JObject>();
            }
            catch (Exception e)
            {
                throw new System.Exception($"Could not deserialize result from {JsonConvert.SerializeObject(resultObject)} ", e);
            }
        }
        public async Task<string> LookupJsonWithUserToken(string json, string lookupURL, string token, string apiKey, string backendId)
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
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.BackendId))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.BackendId);
            }
            if (!string.IsNullOrEmpty(backendId))
            {
                httpClient.DefaultRequestHeaders.Add(CustomHeader.BackendId, backendId);
            }
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(json, System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            return JsonConvert.DeserializeObject<string>(await responseMessage.Content.ReadAsStringAsync());
        }
    }
}
