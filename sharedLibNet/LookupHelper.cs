using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using sharedLibNet.Model;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

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
        public async Task<List<Stage>> RetrieveURLs(List<string> urls, string lookupURL, string clientCertString)
        {
           
            if (httpClient.DefaultRequestHeaders.Contains("X-ARR-ClientCert"))
            {
                httpClient.DefaultRequestHeaders.Remove("X-ARR-ClientCert");
            }

            httpClient.DefaultRequestHeaders.Add("X-ARR-ClientCert", clientCertString);

            var responseMessage = await httpClient.PostAsync(lookupURL, new StringContent(JsonConvert.SerializeObject(urls)));
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical(responseContent);
                return null;
            }
            return JsonConvert.DeserializeObject<List<Stage>>(await responseMessage.Content.ReadAsStringAsync());
        }
    }
}
