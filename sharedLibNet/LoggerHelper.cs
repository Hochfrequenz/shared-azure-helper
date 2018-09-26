using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace sharedLibNet
{
    public class LoggerHelper
    {
        public static HttpClient httpClient = new HttpClient();
        public static (ILogger logger, InMemoryLoggerProvider loggerProvider) CreateLogger(string serviceName)
        {
            var factory = new LoggerFactory();
            var loggerProvider = new InMemoryLoggerProvider();
            factory.AddProvider(loggerProvider);
            return (factory.CreateLogger(serviceName), loggerProvider);
        }
        public static async Task<HttpResponseMessage> SendLogToServer(string URL, InMemoryLoggerProvider logger, string certificate)
        {
            var cert = certificate;
            if (httpClient.DefaultRequestHeaders.Contains("X-ARR-ClientCert"))
            {
                httpClient.DefaultRequestHeaders.Remove("X-ARR-ClientCert");
            }

            httpClient.DefaultRequestHeaders.Add("X-ARR-ClientCert", cert);
            return await httpClient.PostAsync(URL, new StringContent(JsonConvert.SerializeObject(logger.Messages)));
        }
    }
}
