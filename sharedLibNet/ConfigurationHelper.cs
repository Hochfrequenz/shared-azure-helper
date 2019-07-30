using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using EshDataExchangeFormats;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using sharedLibNet.Model;

namespace sharedLibNet
{
    public class ConfigurationHelper
    {
        protected HttpClient httpClient = new HttpClient();
        protected ILogger _logger = null;
        public ConfigurationHelper(ILogger logger)
        {
            _logger = logger;
        }
        public async Task<List<Stage>> GetConfiguration(string clientCertString, string client, string app, string configURL)
        {
            dynamic config = new ExpandoObject();
            config.client = client;
            config.app = app;
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.XArrClientCert))
            {
                _logger.LogDebug($"Removing {HeaderNames.Auth.XArrClientCert}");
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.XArrClientCert);
            }

            _logger.LogDebug($"Adding {HeaderNames.Auth.XArrClientCert} with own clientCertString");
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.XArrClientCert, clientCertString);

            var responseMessage = await httpClient.PostAsync(configURL, new StringContent(JsonConvert.SerializeObject(config)));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not get configuration: {responseMessage.ReasonPhrase}; returning null");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            _logger.LogDebug($"Successfully retrieved POST response with status code {responseMessage}");
            List<Stage> result;
            try
            {
                result = JsonConvert.DeserializeObject<List<Stage>>(await responseMessage.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                _logger.LogError($"Response could not be deserialied: {e}");
                throw e;
            }
            return result;
        }
        public async Task<List<Stage>> GetConfigurationWithToken(string token, string client, string app, string configURL,string apiKey)
        {
            dynamic config = new ExpandoObject();
            config.client = client;
            config.app = app;
            RemoveAndReAddHeaders(token, apiKey);
            var responseMessage = await httpClient.PostAsync(configURL, new StringContent(JsonConvert.SerializeObject(config)));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not get configuration: {responseMessage.ReasonPhrase}; returning null");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            _logger.LogDebug($"Successfully retrieved POST response with status code {responseMessage}");
            List<Stage> result;
            try
            {
                result = JsonConvert.DeserializeObject<List<Stage>>(await responseMessage.Content.ReadAsStringAsync());
            }
            catch (Exception e)
            {
                _logger.LogError($"Response could not be deserialied: {e}");
                throw e;
            }
            return result;
        }
        private HttpClient RemoveAndReAddHeaders(string token, string apiKey)
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
            
            _logger.LogDebug("Removed and readded headers.");
            return httpClient;
        }
    }
}
