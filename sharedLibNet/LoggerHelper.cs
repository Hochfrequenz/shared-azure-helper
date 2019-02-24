using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using BO4E.BO.LogObject;
using BO4E.Extensions.Encryption;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sodium;

namespace sharedLibNet
{

    public class LoggerHelper
    {
        public static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Creates log message; allows for encryption/obfuscation of sensitive information.
        /// </summary>
        /// <param name="content">log message itself</param>
        /// <param name="sensitive">set true if log message contains sensitive, privacy relevant or confidential information</param>
        /// <param name="key">base64 encoded public key (using libsodium PublicKeyBox encryption standard)</param>
        /// <returns></returns>
        public static string CreateTraceObject(string content, bool sensitive = false, string publicKey = null)
        {
            //TODO: enable encryption of sensitive information
            dynamic obj = new ExpandoObject();
            obj.Sensitive = sensitive;
            if (sensitive)
            {
                if (publicKey == null)
                {
                    throw new ArgumentNullException("To encrypt sensitive data you need to provide a non null public key");
                }
                byte[] publicKeyBytes;
                try
                {
                    publicKeyBytes = Convert.FromBase64String(publicKey);
                }
                catch (FormatException e)
                {
                    throw new FormatException($"The public key string {publicKey} you provided is no valid base64 string: {e.Message}");
                }
                KeyPair asykeyPairSender = PublicKeyBox.GenerateKeyPair(); // newly generated every single time???
                AsymmetricEncrypter enc = new AsymmetricEncrypter(asykeyPairSender);
                LogObject lo = new LogObject();
                lo.datetime = DateTime.UtcNow;
                lo.logMessage = content;
                obj.Content = enc.Encrypt(lo, publicKey);
            }
            else
            {
                obj.Content = content;
            }
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
