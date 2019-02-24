using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

namespace sharedLibNet
{

    public class LoggerHelper
    {
        public static HttpClient httpClient = new HttpClient();

        public static string CreateTraceObject(string content, bool sensitive = false, string key = null)
        {
            //TODO: enable encryption of sensitive information
            dynamic obj = new ExpandoObject();
            obj.Content = content;
            obj.Sensitive = sensitive;
            return JsonConvert.SerializeObject(obj);
        }
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
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.XArrClientCert))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.XArrClientCert);
            }

            httpClient.DefaultRequestHeaders.Add(CustomHeader.XArrClientCert, cert);

            return await httpClient.PostAsync(URL, new StringContent(JsonConvert.SerializeObject(logger.Messages)));
        }
        public static async Task<HttpResponseMessage> GetEventLogs(string URL, string certificate)
        {
            var cert = certificate;
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.XArrClientCert))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.XArrClientCert);
            }
            httpClient.DefaultRequestHeaders.Add(CustomHeader.XArrClientCert, cert);

            return await httpClient.GetAsync(URL);
        }
    }
}
