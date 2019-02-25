﻿using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using BO4E.BO;
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
        /// Creates a new logger and logger provider from a newly instantiated LoggerFactory.
        /// </summary>
        /// <param name="serviceName">name of the service. Is passed to the logger provider.</param>
        /// <returns>Tuple of logger and corresponding loggerProvider</returns>
        public static (ILogger logger, InMemoryLoggerProvider loggerProvider) CreateLogger(string serviceName)
        {
            var factory = new LoggerFactory();
            var loggerProvider = new InMemoryLoggerProvider();
            factory.AddProvider(loggerProvider);
            return (factory.CreateLogger(serviceName), loggerProvider);
        }

        /// <summary>
        /// Creates log message; allows for encryption/obfuscation of sensitive information.
        /// </summary>
        /// <param name="content">log message itself</param>
        /// <param name="sensitive">set true if log message contains sensitive, privacy relevant or confidential information</param>
        /// <param name="key">base64 encoded public key (using libsodium PublicKeyBox encryption standard)</param>
        /// <returns></returns>
        public static string CreateTraceObject(string content, bool sensitive = false, string publicKey = null)
        {
            dynamic obj = new ExpandoObject();
            obj.Sensitive = sensitive;
            if (sensitive)
            {
                if (publicKey == null)
                {
                    throw new ArgumentNullException("To encrypt sensitive data you have to provide a non-null public key");
                }
                byte[] publicKeyBytes;
                try
                {
                    publicKeyBytes = Convert.FromBase64String(publicKey);
                }
                catch (FormatException e)
                {
                    throw new FormatException($"The provided public key string '{publicKey}' is no valid base64 string: {e.Message}");
                }
                KeyPair asykeyPairSender = PublicKeyBox.GenerateKeyPair(); // newly generated every single time???
                AsymmetricEncrypter enc = new AsymmetricEncrypter(asykeyPairSender);
                LogObject logObject = new LogObject
                {
                    datetime = DateTime.UtcNow,
                    logMessage = content
                };
                obj.Content = enc.Encrypt(logObject, publicKey);
            }
            else
            {
                obj.Content = content;
            }
            return JsonConvert.SerializeObject(obj);
        }

        [Obsolete("Please use SendToLogServer(Uri, ...) instead of SendToLogServer(string, ...).")]
        public static async Task<HttpResponseMessage> SendLogToServer(string URL, InMemoryLoggerProvider logger, string certificate)
        {
            Uri uri = new Uri(URL);
            return await SendLogToServer(uri, logger, certificate);
        }


        /// <summary>
        /// POSTs messages of <paramref name="logger"/> to server specified in <paramref name="URL"/>.  
        /// </summary>
        /// <param name="URL">Uri of the server</param>
        /// <param name="logger">logger containing messages</param>
        /// <param name="certificate">certificate used in HTTP POST header</param>
        /// <returns>HttpClient response of POST</returns>
        public static async Task<HttpResponseMessage> SendLogToServer(Uri URL, InMemoryLoggerProvider logger, string certificate)
        {
            var cert = certificate;
            if (httpClient.DefaultRequestHeaders.Contains(CustomHeader.XArrClientCert))
            {
                httpClient.DefaultRequestHeaders.Remove(CustomHeader.XArrClientCert);
            }
            httpClient.DefaultRequestHeaders.Add(CustomHeader.XArrClientCert, cert);
            return await httpClient.PostAsync(URL, new StringContent(JsonConvert.SerializeObject(logger.Messages)));
        }

        [Obsolete("Please use GetEventLogs(Uri, ...) instead of GetEventLogs(string, ...).")]
        public static async Task<HttpResponseMessage> GetEventLogs(string URL, string certificate)
        {
            Uri uri = new Uri(URL);
            return await GetEventLogs(uri, certificate);
        }

        /// <summary>
        /// Reads events logs from server.
        /// </summary>
        /// <param name="URL">URL of server the log messages are retrieved from</param>
        /// <param name="certificate">certificate used in HTTP GET header</param>
        /// <returns>HttpClient response of GET</returns>
        public static async Task<HttpResponseMessage> GetEventLogs(Uri URL, string certificate)
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
