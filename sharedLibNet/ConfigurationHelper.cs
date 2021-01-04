using EshDataExchangeFormats;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using sharedLibNet.Model;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

namespace sharedLibNet
{
    public class ConfigurationHelper
    {
        protected HttpClient httpClient = new HttpClient();
        protected ILogger _logger = null;
        private readonly bool _silentFailure;

        /// <summary>
        /// todo add docstring here
        /// </summary>
        /// <param name="silentFailure">set true to return null in case of error, if false an <see cref="HfException"/> is thrown</param>
        public ConfigurationHelper(bool silentFailure = true)
        {
            _silentFailure = silentFailure;
        }

        /// <summary>
        /// todo add docstring here
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="silentFailure">set true to return null in case of error, if false an <see cref="HfException"/> is thrown</param>

        public ConfigurationHelper(ILogger logger, bool silentFailure = true) : this(silentFailure)
        {
            _logger = logger;
        }

        [Obsolete("Use method with Uri instead of stringly typed configURL")]
        public async Task<List<Stage>> GetConfiguration(string clientCertString, string client, string app, String configURL) => await GetConfiguration(clientCertString, client, app, new Uri(configURL));

        public async Task<List<Stage>> GetConfiguration(string clientCertString, string client, string app, Uri configURL)
        {
            dynamic config = new ExpandoObject();
            config.client = client;
            config.app = app;

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(config)),
                RequestUri = configURL
            };

            if (request.Headers.Contains(HeaderNames.Auth.XArrClientCert))
            {
                _logger.LogDebug($"Removing {HeaderNames.Auth.XArrClientCert}");
                request.Headers.Remove(HeaderNames.Auth.XArrClientCert);
            }
            _logger.LogDebug($"Adding {HeaderNames.Auth.XArrClientCert} with own clientCertString");
            request.Headers.Add(HeaderNames.Auth.XArrClientCert, clientCertString);

            var responseMessage = await httpClient.SendAsync(request);
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not get configuration: {responseMessage.ReasonPhrase}; returning null");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                if (_silentFailure)
                {
                    return null;
                }
                else
                {
                    throw new HfException(responseMessage);
                }
            }
            _logger.LogDebug($"Successfully retrieved POST response with status code {responseMessage}");
            List<Stage> result;
            try
            {
                result = JsonConvert.DeserializeObject<List<Stage>>(await responseMessage.Content.ReadAsStringAsync());
            }
            catch (Exception e) // todo: no pokemon exceptio nhandling. Probably we'll only need to catch the JsonReaderException
            {
                _logger.LogError($"Response could not be deserialied: {e}");
                throw e;
            }
            return result;
        }

        [Obsolete("Use method with Uri instead of stringly typed configURL")]
        public async Task<List<Stage>> GetConfigurationWithToken(string token, string client, string app, String configURL, string apiKey) => await GetConfigurationWithToken(token, client, app, new Uri(configURL), apiKey);

        /// <summary>
        /// Get configuration with token
        /// </summary>
        /// <param name="token">token to authenticate</param>
        /// <param name="client"></param>
        /// <param name="app"></param>
        /// <param name="configURL"></param>
        /// <param name="apiKey">api key for azure</param>
        /// <returns></returns>
        public async Task<List<Stage>> GetConfigurationWithToken(string token, string client, string app, Uri configURL, string apiKey)
        {
            dynamic config = new ExpandoObject();
            config.client = client;
            config.app = app;

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(config)),
                RequestUri = configURL
            };
            _logger.LogDebug("RemoveAndReAddHeaders");
            if (request.Headers.Contains(HeaderNames.Auth.Authorization))
            {
                _logger.LogDebug($"Removing {HeaderNames.Auth.Authorization} header");
                request.Headers.Remove(HeaderNames.Auth.Authorization);
            }
            request.Headers.Add(HeaderNames.Auth.Authorization, "Bearer " + token);
            if (request.Headers.Contains(HeaderNames.Auth.HfAuthorization))
            {
                _logger.LogDebug($"Removing {HeaderNames.Auth.HfAuthorization} header");
                request.Headers.Remove(HeaderNames.Auth.HfAuthorization);
            }
            request.Headers.Add(HeaderNames.Auth.HfAuthorization, "Bearer " + token);
            if (request.Headers.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY))
            {
                _logger.LogDebug($"Removing {HeaderNames.Azure.SUBSCRIPTION_KEY} header");
                request.Headers.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            if (!string.IsNullOrEmpty(apiKey))
            {
                _logger.LogDebug($"Adding {HeaderNames.Azure.SUBSCRIPTION_KEY} header");
                request.Headers.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);
            }
            if (request.Headers.Contains(HeaderNames.BACKEND_ID))
            {
                _logger.LogDebug($"Removing {HeaderNames.BACKEND_ID} header");
                request.Headers.Remove(HeaderNames.BACKEND_ID);
            }
            var responseMessage = await httpClient.SendAsync(request);
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not get configuration: {responseMessage.ReasonPhrase}; returning null");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                if (_silentFailure)
                {
                    return null;
                }
                else
                {
                    throw new HfException(responseMessage);
                }
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
