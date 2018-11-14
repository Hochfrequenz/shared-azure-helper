using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using sharedLibNet.Model;
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
        public ConfigurationHelper(ILogger logger)
        {
            _logger = logger;
        }
        public async Task<List<Stage>> GetConfiguration(string clientCertString, string client, string app, string configURL)
        {
            dynamic config = new ExpandoObject();
            config.client = client;
            config.app = app;
            if (httpClient.DefaultRequestHeaders.Contains("X-ARR-ClientCert"))
            {
                httpClient.DefaultRequestHeaders.Remove("X-ARR-ClientCert");
            }

            httpClient.DefaultRequestHeaders.Add("X-ARR-ClientCert", clientCertString);

            var responseMessage = await httpClient.PostAsync(configURL, new StringContent(JsonConvert.SerializeObject(config)));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not get configuration: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            return JsonConvert.DeserializeObject<List<Stage>>(await responseMessage.Content.ReadAsStringAsync());
        }
    }
}
