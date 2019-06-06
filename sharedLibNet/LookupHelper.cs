using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BO4E.meta;
using EshDataExchangeFormats;
using EshDataExchangeFormats.lookup;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace sharedLibNet
{
    public class LookupHelper
    {
        protected HttpClient httpClient = new HttpClient();
        protected ILogger _logger = null;
        protected const string SYMMETRIC_ENCRYPTION_KEY_HEADER_NAME = "encryptionkey";
        private const string MIME_TYPE_JSON = "application/json"; // replace with MediaTypeNames.Application.Json as soon as .net core 2.1 is used
        public LookupHelper(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<GenericLookupResult> RetrieveURLs(IList<Bo4eUri> urls, Uri lookupURL, string clientCertString, string apiKey, BOBackendId backendId)
        {
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.XArrClientCert))
            {
                _logger.LogDebug($"Removing {HeaderNames.Auth.XArrClientCert} header");
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.XArrClientCert);
            }

            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.XArrClientCert, clientCertString);
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY))
            {
                _logger.LogDebug($"Removing {HeaderNames.Azure.SUBSCRIPTION_KEY} header");
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            if (!string.IsNullOrEmpty(apiKey))
            {
                _logger.LogDebug($"Adding {HeaderNames.Azure.SUBSCRIPTION_KEY} header");
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);
            }
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.BACKEND_ID))
            {
                _logger.LogDebug($"Removing {HeaderNames.BACKEND_ID} header");
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.BACKEND_ID);
            }
            //if (!string.IsNullOrEmpty(backendId))
            //{
            httpClient.DefaultRequestHeaders.Add(HeaderNames.BACKEND_ID, backendId.ToString());
            //}
            GenericLookupQuery urlObject = new GenericLookupQuery()
            {
                uris = urls
            };
            string requestBody = JsonConvert.SerializeObject(urlObject);
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(requestBody, System.Text.UTF8Encoding.UTF8, MIME_TYPE_JSON));
            if (!responseMessage.IsSuccessStatusCode)
            {
                string responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase} / {responseContent}; The original request was: {requestBody} POSTed to {lookupURL}");
                return null;
            }
            GenericLookupResult resultObject = null;
            try
            {
                resultObject = (JsonConvert.DeserializeObject<GenericLookupResult>(await responseMessage.Content.ReadAsStringAsync()));
                _logger.LogDebug($"Successfully de-serialised as GenericLookupResult");
                return resultObject;
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not de-serialise result from {JsonConvert.SerializeObject(resultObject)}: {e.ToString()}");
                throw new Exception($"Could not de-serialize result from {JsonConvert.SerializeObject(resultObject)} ", e);
            }
        }


        public async Task<GenericLookupResult> RetrieveURLsWithUserToken(IList<Bo4eUri> urls, Uri lookupURL, string token, string apiKey, BOBackendId backendId)
        {
            RemoveAndReAddHeaders(token, apiKey, backendId);
            GenericLookupQuery urlObject = new GenericLookupQuery()
            {
                uris = urls
            };
            string requestBody = JsonConvert.SerializeObject(urlObject);
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(requestBody, System.Text.UTF8Encoding.UTF8, MIME_TYPE_JSON));
            if (!responseMessage.IsSuccessStatusCode)
            {
                string responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase} / {responseContent}; The original request was: {requestBody} POSTed to {lookupURL}");
                return null;
            }
            GenericLookupResult resultObject = null;
            try
            {
                resultObject = (JsonConvert.DeserializeObject<GenericLookupResult>(await responseMessage.Content.ReadAsStringAsync()));
                _logger.LogDebug($"Successfully de-serialised as GenericLookupResult");
                return resultObject;
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not de-serialise result from {JsonConvert.SerializeObject(resultObject)}: {e.ToString()}");
                throw new System.Exception($"Could not de-serialise result from {JsonConvert.SerializeObject(resultObject)} ", e);
            }
        }
        public async Task<GenericLookupResult> Suggest(string suggestion,string boe4Type, Uri lookupURL, string token, string apiKey, BOBackendId backendId)
        {
            RemoveAndReAddHeaders(token, apiKey, backendId);
            var responseMessage = await httpClient.GetAsync(lookupURL);
            if (!responseMessage.IsSuccessStatusCode)
            {
                string responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase} / {responseContent}; The original request was: {suggestion} POSTed to {lookupURL}");
                return null;
            }
            GenericLookupResult resultObject = null;
            try
            {
                resultObject = (JsonConvert.DeserializeObject<GenericLookupResult>(await responseMessage.Content.ReadAsStringAsync()));
                _logger.LogDebug($"Successfully de-serialised as GenericLookupResult");
                return resultObject;
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not de-serialise result from {JsonConvert.SerializeObject(resultObject)}: {e.ToString()}");
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
        /// <param name="apiKey">API key for gateway</param>
        /// <param name="backendId">ID of Backend</param>
        /// <returns></returns>
        public async Task<string> LookupJsonWithUserToken(string json, Uri lookupURL, string token, string apiKey, BOBackendId backendId)
        {
            _logger.LogDebug("LookupJsonWithUserToken");
            RemoveAndReAddHeaders(token, apiKey, backendId);
            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(json, System.Text.UTF8Encoding.UTF8, "application/json"));
            if (!responseMessage.IsSuccessStatusCode)
            {
                string responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase} / {responseContent}; The original request was: {json} POSTed to {lookupURL}");
                return null;
            }
            _logger.LogDebug($"Sucessfully retrieved response with status code {responseMessage.StatusCode}");
            return await responseMessage.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Initialise the suggestion cache for Backend <paramref name="bobId"/> with the lookup result of <paramref name="initialisationQuery"/>
        /// </summary>
        /// <param name="initialisationQuery">query whose result is used to initialise the suggestion cache</param>
        /// <param name="lookupUrl">URI of the lookup service</param>
        /// <param name="token">auth token</param>
        /// <param name="apiKey">api key for azure</param>
        /// <param name="bobId">unique ID of the backend</param>
        /// <returns>raw lookup response as string in case of success, null in case of failure</returns>
        public async Task<string> InitialiseSuggestionCache(GenericLookupQuery initialisationQuery, Uri lookupUrl, string token, string apiKey, string encryptionKey, BOBackendId bobId)
        {
            _logger.LogDebug("InitialiseSuggestionCache (lookup helper)");
            RemoveAndReAddHeaders(token, apiKey, bobId);
            string serialisedQuery = JsonConvert.SerializeObject(initialisationQuery, new StringEnumConverter());
            var responseMessage = await httpClient.PutAsync(lookupUrl, new StringContent(serialisedQuery, System.Text.Encoding.UTF8, MIME_TYPE_JSON));
            if (!responseMessage.IsSuccessStatusCode)
            {
                string responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical($"Could not perform cache initialisation: {responseMessage.ReasonPhrase}");
                return null;
            }
            _logger.LogDebug($"Successfully initialised cache with status code {responseMessage.StatusCode}");
            return await responseMessage.Content.ReadAsStringAsync();
        }

        private HttpClient RemoveAndReAddHeaders(string token, string apiKey, BOBackendId backendId)
        {
            _logger.LogDebug("RemoveAndReAddHeaders");
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.Authorization))
            {
                _logger.LogDebug($"Removing {HeaderNames.Auth.Authorization} header");
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.Authorization);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.Authorization, "Bearer " + token);
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.HfAuthorization))
            {
                _logger.LogDebug($"Removing {HeaderNames.Auth.HfAuthorization} header");
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.HfAuthorization);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.HfAuthorization, "Bearer " + token);
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY))
            {
                _logger.LogDebug($"Removing {HeaderNames.Azure.SUBSCRIPTION_KEY} header");
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            if (!string.IsNullOrEmpty(apiKey))
            {
                _logger.LogDebug($"Adding {HeaderNames.Azure.SUBSCRIPTION_KEY} header");
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);
            }
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.BACKEND_ID))
            {
                _logger.LogDebug($"Removing {HeaderNames.BACKEND_ID} header");
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.BACKEND_ID);
            }
            //if (!string.IsNullOrEmpty(backendId))
            //{
            _logger.LogDebug($"Adding {HeaderNames.BACKEND_ID} header");
            httpClient.DefaultRequestHeaders.Add(HeaderNames.BACKEND_ID, backendId.ToString());
            //}
            _logger.LogDebug("Removed and readded headers.");
            return httpClient;
        }
    }
}
