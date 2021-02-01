using BO4E.BO;
using BO4E.Extensions.Encryption;

using EshDataExchangeFormats;

using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Sodium;

using StackExchange.Profiling;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;

namespace sharedLibNet
{

    public class LoggerHelper
    {
        public class LargeLog
        {
            public string eventId { get; set; }
            public string logName { get; set; }
            public JToken content { get; set; }

        }
        public static readonly HttpClient httpClient = new HttpClient();
        protected static EventGridClient EventGridClient;
        protected static string TopicHostname;
        public const string LogEventName = "HF.EnergyCore.EventLog.Created";
        protected static CloudStorageAccount StorageAccount = null;
        protected static CloudBlobClient BlobClient = null;
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
                TopicHostname = new Uri(topicEndpoint).Host;
                TopicCredentials topicCredentials = new TopicCredentials(topicKey);
                EventGridClient = new EventGridClient(topicCredentials);
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
        public static string CreateTraceObject(string content, bool sensitive = false, string publicKey = null, string id = null)
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
                if (id == null)
                {
                    id = Guid.NewGuid().ToString();
                }
                KeyPair asykeyPairSender = PublicKeyBox.GenerateKeyPair(); // newly generated every single time???
                using (AsymmetricEncrypter enc = new AsymmetricEncrypter(asykeyPairSender))
                {
                    LogObject logObject = new LogObject
                    {
                        DateTime = DateTime.UtcNow,
                        Id = id,
                        LogMessage = content
                    };
                    obj.Content = enc.Encrypt(logObject, publicKey);
                }
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
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(logger.Messages)),
                RequestUri = url
            };
            if (request.Headers.Contains(HeaderNames.Auth.XArrClientCert))
            {
                request.Headers.Remove(HeaderNames.Auth.XArrClientCert);
            }
            request.Headers.Add(HeaderNames.Auth.XArrClientCert, cert);
            return await httpClient.SendAsync(request);
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
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent(JsonConvert.SerializeObject(logger.Messages))
            };

            if (request.Headers.Contains(HeaderNames.Auth.Authorization))
            {
                request.Headers.Remove(HeaderNames.Auth.Authorization);
            }
            request.Headers.Add(HeaderNames.Auth.Authorization, "Bearer " + token);

            if (request.Headers.Contains(HeaderNames.Auth.HfAuthorization))
            {
                request.Headers.Remove(HeaderNames.Auth.HfAuthorization);
            }
            request.Headers.Add(HeaderNames.Auth.HfAuthorization, "Bearer " + token);

            if (request.Headers.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY) && apiKey != null)
            {
                request.Headers.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            request.Headers.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);
            return await httpClient.SendAsync(request);
        }

        protected const string DEFAULT_SUBJECT = "EventLog";

        public static async Task<string> SendLogEvent(string eventId, InMemoryLoggerProvider logger, string subjectPostfix, string eventType = LogEventName, bool silent = true)
        {
            using (MiniProfiler.Current.Step(nameof(SendLogEvent)))
            {
                try
                {
                    string subject = DEFAULT_SUBJECT;
                    if (!string.IsNullOrEmpty(subjectPostfix))
                    {
                        subject += subjectPostfix;
                    }

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
                            EventType = eventType,
                            Data = eventData,
                            EventTime = DateTime.UtcNow,
                            Subject = subject,
                            DataVersion = "2.0"
                        });
                    }

                    await EventGridClient.PublishEventsAsync(TopicHostname, eventList);
                    return "OK";
                }
                catch (Exception e)
                {
                    if (silent)
                    {
                        return e.Message;
                    }
                    else
                    {
                        throw e;
                    }
                }
            }
        }

        public static async Task<string> SendLogEventData(string eventId, JObject logData, string subjectPostfix, string eventType = LogEventName, bool silent = true)
        {
            using (MiniProfiler.Current.Step(nameof(SendLogEventData)))
            {
                try
                {
                    string subject = DEFAULT_SUBJECT;
                    if (!string.IsNullOrEmpty(subjectPostfix))
                    {
                        subject += subjectPostfix;
                    }
                    List<EventGridEvent> eventList = new List<EventGridEvent>();
                    dynamic eventData = new ExpandoObject();
                    eventData.eventid = eventId;
                    eventData.logData = logData;

                    var newId = Guid.NewGuid().ToString();
                    eventList.Add(new EventGridEvent()
                    {
                        Id = newId,
                        EventType = eventType,
                        Data = eventData,
                        EventTime = DateTime.UtcNow,
                        Subject = subject,
                        DataVersion = "2.0"
                    });

                    await EventGridClient.PublishEventsAsync(TopicHostname, eventList);
                    return newId;
                }
                catch (Exception e)
                {
                    if (silent)
                    {
                        return e.Message;
                    }
                    else
                    {
                        throw e;
                    }
                }
                finally
                {

                }
            }
        }

        protected static async Task ConnectToBlob(IConfiguration config)
        {
            if (StorageAccount == null)
            {
                StorageAccount = CloudStorageAccount.Parse(
                   config["Log:Blob:URL"]);
                BlobClient = StorageAccount.CreateCloudBlobClient();
            }
        }

        public static async Task<LargeLog> StoreLargeLogObject(IConfiguration config, LargeLog logEntry)
        {
            await ConnectToBlob(config);
            var container = BlobClient.GetContainerReference(logEntry.eventId);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(logEntry.logName);
            await blob.UploadTextAsync(JsonConvert.SerializeObject(logEntry.content));
            return new LargeLog() { eventId = logEntry.eventId, logName = logEntry.logName };
        }

        public static async Task<LargeLog> RetrieveLargeLogObject(IConfiguration config, LargeLog logEntry)
        {
            await ConnectToBlob(config);
            var container = BlobClient.GetContainerReference(logEntry.eventId);
            try
            {
                var blob = container.GetBlockBlobReference(logEntry.logName);
                logEntry.content = JsonConvert.DeserializeObject<JToken>(await blob.DownloadTextAsync());
                return logEntry;
            }
            catch (Exception) // ToDo: fix pokemon catching
            {
                return null;
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

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = url
            };

            if (request.Headers.Contains(HeaderNames.Auth.XArrClientCert))
            {
                request.Headers.Remove(HeaderNames.Auth.XArrClientCert);
            }
            request.Headers.Add(HeaderNames.Auth.XArrClientCert, cert);
            return await httpClient.SendAsync(request);
        }

        public static async Task<HttpResponseMessage> GetEventLogsWithToken(Uri url, string token, string apiKey)
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = url
            };
            if (request.Headers.Contains(HeaderNames.Auth.Authorization))
            {
                request.Headers.Remove(HeaderNames.Auth.Authorization);
            }
            request.Headers.Add(HeaderNames.Auth.Authorization, "Bearer " + token);

            if (request.Headers.Contains(HeaderNames.Auth.HfAuthorization))
            {
                request.Headers.Remove(HeaderNames.Auth.HfAuthorization);
            }
            request.Headers.Add(HeaderNames.Auth.HfAuthorization, "Bearer " + token);

            if (request.Headers.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY) && apiKey != null)
            {
                request.Headers.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            request.Headers.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);

            return await httpClient.SendAsync(request);
        }
    }
}
