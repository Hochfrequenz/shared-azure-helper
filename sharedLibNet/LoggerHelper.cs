﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using BO4E.BO;
using BO4E.Extensions.Encryption;
using EshDataExchangeFormats;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sodium;

namespace sharedLibNet
{

    public class LoggerHelper
    {
        public static HttpClient httpClient = new HttpClient();
        protected static EventGridClient _eventGridClient;
        protected static string _topicHostname;
        public const string LogEventName = "HF.EnergyCore.EventLog.Created";
        /// <summary>
        /// Creates a new logger and logger provider from a newly instantiated LoggerFactory.
        /// </summary>
        /// <param name="serviceName">name of the service. Is passed to the logger provider.</param>
        /// <returns>Tuple of logger and corresponding loggerProvider</returns>
        public static (ILogger logger, InMemoryLoggerProvider loggerProvider) CreateLogger(string serviceName, string logTopic, string logTopicKey)
        {
            if (logTopic != null)
            {
                string topicEndpoint = logTopic;
                string topicKey = logTopicKey;
                _topicHostname = new Uri(topicEndpoint).Host;
                TopicCredentials topicCredentials = new TopicCredentials(topicKey);
                _eventGridClient = new EventGridClient(topicCredentials);
            }
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

        [Obsolete("Please use SendToLogServer(Uri, ...) instead of SendToLogServer(string, ...).", true)]
        public static async Task<HttpResponseMessage> SendLogToServer(string URL, InMemoryLoggerProvider logger, string certificate)
        {
            Uri uri = new Uri(URL);
            return await SendLogToServer(uri, logger, certificate);
        }


        /// <summary>
        /// POSTs messages of <paramref name="logger"/> to server specified in <paramref name="url"/>.  
        /// </summary>
        /// <param name="url">Uri of the server</param>
        /// <param name="logger">logger containing messages</param>
        /// <param name="certificate">certificate used in HTTP POST header</param>
        /// <returns>HttpClient response of POST</returns>
        public static async Task<HttpResponseMessage> SendLogToServer(Uri url, InMemoryLoggerProvider logger, string certificate)
        {
            var cert = certificate;
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.XArrClientCert))
            {
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.XArrClientCert);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.XArrClientCert, cert);
            return await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(logger.Messages)));
        }

        /// <summary>
        /// POSTs messages of <paramref name="logger"/> to server specified in <paramref name="url"/>.  
        /// </summary>
        /// <param name="url">Uri of the server</param>
        /// <param name="logger">logger containing messages</param>
        /// <param name="certificate">certificate used in HTTP POST header</param>
        /// <returns>HttpClient response of POST</returns>
        public static async Task<HttpResponseMessage> SendLogToServerWithToken(Uri url, InMemoryLoggerProvider logger, string token, string apiKey)
        {
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.Authorization))
            {
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.Authorization);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.Authorization, "Bearer " + token);

            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.HfAuthorization))
            {
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.HfAuthorization);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.HfAuthorization,"Bearer "+ token);

            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY) && apiKey != null)
            {
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);


            return await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(logger.Messages)));
        }

        public static async Task<string> SendLogEvent(string eventId, InMemoryLoggerProvider logger, string subjectPostfix)
        {
            string subject = "EventLog";
            if (!string.IsNullOrEmpty(subjectPostfix))
            {
                subject += subjectPostfix;
            }
            try
            {
                IList<EventGridEvent> eventList = new List<EventGridEvent>();
                foreach (var msg in logger.Messages)
                {
                    dynamic eventData = new ExpandoObject();
                    eventData.eventid = eventId;
                    eventData.message = msg;
                    var newId = Guid.NewGuid().ToString();
                    eventList.Add(new EventGridEvent()
                    {
                        Id = newId,
                        EventType = LogEventName,
                        Data = eventData,
                        EventTime = DateTime.UtcNow,
                        Subject = subject,
                        DataVersion = "2.0"
                    });
                }

                await _eventGridClient.PublishEventsAsync(_topicHostname, eventList);
                return "OK";
            }
            catch (Exception exc)
            {
                return exc.ToString();
            }
        }

        public static async Task<string> SendLogEventData(string eventId, JObject logData, string subjectPostfix)
        {
            string subject = "EventLog";
            if (!string.IsNullOrEmpty(subjectPostfix))
            {
                subject += subjectPostfix;
            }
            try
            {
                List<EventGridEvent> eventList = new List<EventGridEvent>();
                dynamic eventData = new ExpandoObject();
                eventData.eventid = eventId;
                eventData.logData = logData;

                var newId = Guid.NewGuid().ToString();
                eventList.Add(new EventGridEvent()
                {
                    Id = newId,
                    EventType = LogEventName,
                    Data = eventData,
                    EventTime = DateTime.UtcNow,
                    Subject = subject,
                    DataVersion = "2.0"
                });

                await _eventGridClient.PublishEventsAsync(_topicHostname, eventList);
                return newId;
            }
            catch (Exception exc)
            {
                return exc.ToString();
            }
        }

        [Obsolete("Please use GetEventLogs(Uri, ...) instead of GetEventLogs(string, ...).", true)]
        public static async Task<HttpResponseMessage> GetEventLogs(string url, string certificate)
        {
            Uri uri = new Uri(url);
            return await GetEventLogs(uri, certificate);
        }

        /// <summary>
        /// Reads events logs from server.
        /// </summary>
        /// <param name="url">URL of server the log messages are retrieved from</param>
        /// <param name="certificate">certificate used in HTTP GET header</param>
        /// <returns>HttpClient response of GET</returns>
        public static async Task<HttpResponseMessage> GetEventLogs(Uri url, string certificate)
        {
            var cert = certificate;
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.XArrClientCert))
            {
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.XArrClientCert);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.XArrClientCert, cert);
            return await httpClient.GetAsync(url);
        }

        public static async Task<HttpResponseMessage> GetEventLogsWithToken(Uri url, string token, string apiKey)
        {
            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.Authorization))
            {
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.Authorization);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.Authorization, "Bearer " + token);

            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Auth.HfAuthorization))
            {
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Auth.HfAuthorization);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Auth.HfAuthorization,"Bearer "+ token);

            if (httpClient.DefaultRequestHeaders.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY) && apiKey != null)
            {
                httpClient.DefaultRequestHeaders.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);

            return await httpClient.GetAsync(url);
        }
    }
}
