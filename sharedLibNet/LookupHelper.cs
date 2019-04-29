using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BO4E.meta;
using EshDataExchangeFormats.lookup;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

        public async Task<GenericLookupResult> RetrieveURLs(IList<Bo4eUri> urls, Uri lookupURL, string clientCertString, string apiKey, BOBackendId backendId)
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
            //if (!string.IsNullOrEmpty(backendId))
            //{
            httpClient.DefaultRequestHeaders.Add(CustomHeader.BackendId, backendId.ToString());
            //}
            GenericLookupQuery urlObject = new GenericLookupQuery()
            {
                uris = urls
            };
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(JsonConvert.SerializeObject(urlObject), System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            GenericLookupResult resultObject = null;
            try
            {
                resultObject = (JsonConvert.DeserializeObject<GenericLookupResult>(await responseMessage.Content.ReadAsStringAsync()));
                return resultObject;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not de-serialize result from {JsonConvert.SerializeObject(resultObject)} ", e);
            }
        }


        public async Task<GenericLookupResult> RetrieveURLsWithUserToken(IList<Bo4eUri> urls, Uri lookupURL, string token, string apiKey, BOBackendId backendId)
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
            //if (!string.IsNullOrEmpty(backendId))
            //{
            httpClient.DefaultRequestHeaders.Add(CustomHeader.BackendId, backendId.ToString());
            //}

            GenericLookupQuery urlObject = new GenericLookupQuery()
            {
                uris = urls
            };

            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(JsonConvert.SerializeObject(urlObject), System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            GenericLookupResult resultObject = null;
            try
            {
                resultObject = (JsonConvert.DeserializeObject<GenericLookupResult>(await responseMessage.Content.ReadAsStringAsync()));
                return resultObject;
            }
            catch (Exception e)
            {
                throw new System.Exception($"Could not de-serialise result from {JsonConvert.SerializeObject(resultObject)} ", e);
            }
        }

        /// <summary>
        /// most generic call of the lookup service.
        /// Posts the content from <paramref name="json"/> to the URL specified in <paramref name="lookupURL"/> using the token
        /// </summary>
        /// <param name="json">serialised lookup request</param>
        /// <param name="lookupURL">URL of the lookup service</param>
        /// <param name="token">token to authenticate</param>
        /// <param name="apiKey">api key for gateway</param>
        /// <param name="backendId">ID of Backend</param>
        /// <returns></returns>
        public async Task<string> LookupJsonWithUserToken(string json, Uri lookupURL, string token, string apiKey, BOBackendId backendId)
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
            //if (!string.IsNullOrEmpty(backendId))
            //{
            httpClient.DefaultRequestHeaders.Add(CustomHeader.BackendId, backendId.ToString());
            //}
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(json, System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                string responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase} / {responseContent}; The original request was: {json} POSTed to {lookupURL}");
                return null;
            }
            return JsonConvert.DeserializeObject<string>(await responseMessage.Content.ReadAsStringAsync());
        }
    }
}
