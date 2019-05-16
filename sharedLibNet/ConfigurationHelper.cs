using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
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
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.XArrClientCert))
            {
                _logger.LogDebug($"Removing {CustomHeader.XArrClientCert}");
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.XArrClientCert);
            }

            _logger.LogDebug($"Adding {CustomHeader.XArrClientCert} with own clientCertString");
            httpClient.DefaultRequestHeaders.Add(CustomHeader.XArrClientCert, clientCertString);

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
    }
}
