using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using EshDataExchangeFormats;
using EshDataExchangeFormats.lookup;
using EshDataExchangeFormats.metermonitor;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using StackExchange.Profiling;

namespace sharedLibNet
{
    /// <summary>
    /// The MeterMonitorHelper hides raw HTTP requests from the the application.
    /// </summary>
    public class MeterMonitorHelper
    {
        protected static HttpClient HttpClient = new HttpClient();
        protected readonly ILogger Logger = null;

        protected readonly bool SilentFailure;

        private const string MIME_TYPE_JSON = "application/json"; // replace with MediaTypeNames.Application.Json as soon as .net core 2.1 is used

        private const int DEFAULT_TIMEOUT_MINUTES = 10;

        /// <summary>
        /// Initializes the static http client with a default timeout of <see cref="DEFAULT_TIMEOUT_MINUTES"/> minutes.
        /// </summary>
        static MeterMonitorHelper()
        {
            HttpClient.Timeout = TimeSpan.FromMinutes(DEFAULT_TIMEOUT_MINUTES);
        }

        /// <summary>
        /// public constructor for the sake of a public (instance) constructor. See also the static <seealso cref="MeterMonitorHelper()"/>.
        /// </summary>
        /// <param name="silentFailure">set true to return null in case of error, if false an <see cref="HfException"/> is thrown</param>
        public MeterMonitorHelper(bool silentFailure = true)
        {
            SilentFailure = silentFailure;
        }

        /// <summary>
        /// same as <see cref="MeterMonitorHelper()"/> but with a logger
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="silentFailure">set true to return null in case of error, if false an <see cref="HfException"/> is thrown</param>
        public MeterMonitorHelper(ILogger logger, HttpClient client = null, bool silentFailure = true) : this(silentFailure)
        {
            Logger = logger;
            Logger?.LogInformation($"Instantiated {nameof(MeterMonitorHelper)}. The HttpClient {nameof(HttpClient)} has a timeout of {HttpClient.Timeout.TotalSeconds} seconds.");
            if (client != null)
            {
                HttpClient = client;
            }
        }

        /// <summary>
        /// checks if login to backend specified by <paramref name="bobId"/> works using the token 
        /// </summary>
        /// <param name="token">JWToken</param>
        /// <param name="apiKey">hf-api api key</param>
        /// <param name="bobId">ID of backend</param>
        /// <returns>Tuple containing true/false depending if login was successful and returned status code</returns>
        public async Task<Tuple<bool, HttpStatusCode>> CheckLogin(Uri meterMonitorBaseUrl, string token, string apiKey, BOBackendId bobId)
        {
            if (meterMonitorBaseUrl == null)
            {
                throw new ArgumentNullException(nameof(meterMonitorBaseUrl), "could not be null.");
            }
            if (bobId == null)
            {
                throw new ArgumentNullException(nameof(bobId), "could not be null.");
            }
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Head,
                RequestUri = meterMonitorBaseUrl
            };
            AddHeaders(ref request, token, apiKey, bobId);
            HttpResponseMessage response;
            try
            {
                response = await HttpClient.SendAsync(request);
            }
            catch (Exception e)
            {
                Logger.LogError($"Request (try login) failed. {nameof(meterMonitorBaseUrl)}: '{meterMonitorBaseUrl}', {nameof(token)}: '{token}', {nameof(bobId)}: '{bobId}'; Exception: {e}.");
                Logger.LogInformation($"Returning negative result with status code {HttpStatusCode.BadGateway} / 502");
                return new Tuple<bool, HttpStatusCode>(false, HttpStatusCode.BadGateway);
            }
            finally
            {
                request.Dispose();
            }
            Logger.LogDebug($"Response has status code {response.StatusCode}. See lookup service logs for details.");
            return new Tuple<bool, HttpStatusCode>(response.IsSuccessStatusCode, response.StatusCode);
        }

        /// <summary>
        /// simple wrapper around <see cref="JsonConvert.DeserializeObject(string, Type)"/>.
        /// But the result is also logged in the instance logger.
        /// </summary>
        /// <typeparam name="T">Type of expected result</typeparam>
        /// <param name="json">json serialied object of type <typeparamref name="T"/></param>
        /// <param name="throwException">set true to 'forward' exception. If set to false, null is returned but no exception thrown.</param>
        /// <returns>deserialized result or null (null only if <paramref name="throwException"/> is false)</returns>
        protected T DeserializeObjectAndLog<T>(string json, bool throwException = true)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Logger.LogWarning($"String supposed to be valid json but is null or white space: '{json}'");
            }
            try
            {
                T result = JsonConvert.DeserializeObject<T>(json);
                Logger.LogDebug($"Successfully deserialised as {typeof(T)}");
                return result;
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Could not deserialize '{json}' as {typeof(T)}: {e}");
                if (throwException)
                {
                    Logger.LogDebug(e, "Forwarding Exception...");
                    throw e;
                }
                else
                {
                    Logger.LogDebug("Retuning null");
                    object result = null;
                    return (T)result;
                }
            }
        }

        /// <summary>
        /// GetMeterMonitor list
        /// Posts the content from <paramref name="json"/> to the URL specified in <paramref name="lookupURL"/> using the token
        /// </summary>
        /// <param name="token">token to authenticate</param>
        /// <param name="apiKey">API key for gateway</param>
        /// <param name="backendId">ID of Backend</param>
        /// <param name="limit">limit number of results</param>
        /// <param name="offset">number of skip results</param>
        /// <param name="withError">with 'toError'?</param>
        /// <returns>a list of MeterMonitorResult <see cref="MeterMonitorResult"/>></returns>
        ///<exception cref="HfException" >if Could not get the MeterMonitor list and silentFailure is false</exception>
        public async Task<IList<MeterMonitorResult>> GetMeterMonitors(string token, string apiKey, BOBackendId backendId, uint limit = 0, uint offset = 0, bool withError = true)
        {
            using (MiniProfiler.Current.Step(nameof(GetMeterMonitors)))
            {
                Dictionary<string, string> queries = new Dictionary<string, string>
                {
                    {nameof(limit), limit.ToString()},
                    {nameof(offset), offset.ToString()},
                    {nameof(withError), withError.ToString()}
                };
                HttpRequestMessage request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(QueryHelpers.AddQueryString("", queries), UriKind.Relative)
                };
                AddHeaders(ref request, token, apiKey, backendId);
                Logger.LogInformation($"Sending out MeterMonitor List request with token");

                var responseMessage = await HttpClient.SendAsync(request);
                Logger.LogInformation($"Got response from MeterMonitor List request with token");
                if (!responseMessage.IsSuccessStatusCode)
                {
                    string response = await responseMessage.Content.ReadAsStringAsync();
                    Logger.LogCritical($"Could not perform MeterMonitor List: {responseMessage.ReasonPhrase} / {response}; GET to {HttpClient.BaseAddress}");
                    if (SilentFailure)
                    {
                        return null;
                    }
                    else
                    {
                        throw new HfException(responseMessage);
                    }
                }
                Logger.LogDebug($"Successfully retrieved MeterMonitor List response with status code {responseMessage.StatusCode}");
                string responseContent = await responseMessage.Content.ReadAsStringAsync();
                var resultObject = DeserializeObjectAndLog<List<MeterMonitorResult>>(responseContent);
                return resultObject;
            }
        }

        /// <summary>
        /// Get MeterMonitor By Id
        /// </summary>
        /// <param name="internalPodId">internalPodId as a part of key</param>
        /// <param name="logikzw">logikzw as a part of key</param>
        /// <param name="profile">profile as a part of key</param>
        /// <param name="profileRole">profileRole as a part of key</param>
        /// <param name="serviceid">serviceid as a part of key</param>
        /// <param name="token">token to authenticate</param>
        /// <param name="apiKey">API key for gateway</param>
        /// <param name="backendId">ID of Backend</param>
        /// <param name="withError">with 'toError'?</param>
        /// <returns></returns>
        ///<exception cref="HfException" >if Could not get the MeterMonitor and silentFailure is false</exception>
        public async Task<MeterMonitorResult> GetMeterMonitorById(
            string internalPodId,
            string logikzw,
            string profile,
            string profileRole,
            string serviceid,
            string token, string apiKey, BOBackendId backendId, bool withError = true)
        {
            using (MiniProfiler.Current.Step(nameof(GetMeterMonitorById)))
            {
                if (string.IsNullOrEmpty(internalPodId))
                {
                    Logger.LogError($"{nameof(internalPodId)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                    throw new ArgumentNullException(internalPodId, $"{nameof(internalPodId)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                }
                if (string.IsNullOrEmpty(logikzw))
                {
                    Logger.LogError($"{nameof(logikzw)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                    throw new ArgumentNullException(logikzw, $"{nameof(logikzw)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                }
                if (string.IsNullOrEmpty(profile))
                {
                    Logger.LogError($"{nameof(profile)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                    throw new ArgumentNullException(profile, $"{nameof(profile)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                }
                if (string.IsNullOrEmpty(profileRole))
                {
                    Logger.LogError($"{nameof(profileRole)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                    throw new ArgumentNullException(profileRole, $"{nameof(profileRole)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                }
                if (string.IsNullOrEmpty(serviceid))
                {
                    Logger.LogError($"{nameof(serviceid)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                    throw new ArgumentNullException(serviceid, $"{nameof(serviceid)} could not be null in {nameof(MeterMonitorHelper)}{nameof(GetMeterMonitorById)}");
                }
                Dictionary<string, string> queries = new Dictionary<string, string>();
                queries.Add(nameof(internalPodId), internalPodId);
                queries.Add(nameof(logikzw), logikzw);
                queries.Add(nameof(profile), profile);
                queries.Add(nameof(profileRole), profileRole);
                queries.Add(nameof(serviceid), serviceid);
                queries.Add(nameof(withError), withError.ToString());

                HttpRequestMessage request = new HttpRequestMessage()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(QueryHelpers.AddQueryString("getbyid", queries), UriKind.Relative)
                };
                AddHeaders(ref request, token, apiKey, backendId);
                Logger.LogInformation($"Sending out MeterMonitor GetbyId request with token");
                var responseMessage = await HttpClient.SendAsync(request);
                Logger.LogInformation($"Got response from MeterMonitor GetbyId request with token");
                if (!responseMessage.IsSuccessStatusCode)
                {
                    string response = await responseMessage.Content.ReadAsStringAsync();
                    Logger.LogCritical($"Could not perform MeterMonitor GetbyId: {responseMessage.ReasonPhrase} / {response}; GET to {HttpClient.BaseAddress}{request.RequestUri}");
                    if (SilentFailure)
                    {
                        return null;
                    }
                    else
                    {
                        throw new HfException(responseMessage);
                    }
                }
                Logger.LogDebug($"Successfully retrieved MeterMonitor GetbyId response with status code {responseMessage.StatusCode}");
                string responseContent = await responseMessage.Content.ReadAsStringAsync();
                var resultObject = DeserializeObjectAndLog<MeterMonitorResult>(responseContent);
                return resultObject;
            }
        }

        /// <summary>
        /// set requests headers and removes them prior to adding if they're already present.
        /// </summary>
        /// <param name="request">request that is modified (pass by ref)</param>
        /// <param name="token">access token</param>
        /// <param name="apiKey">subscription key for azure</param>
        /// <param name="backendId">ID of the backend</param>
        /// <returns></returns>
        protected void AddHeaders(ref HttpRequestMessage request, string token, string apiKey, BOBackendId backendId)
        {
            Logger.LogDebug(nameof(AddHeaders));
            if (request.Headers.Contains(HeaderNames.Auth.Authorization))
            {
                Logger.LogDebug($"Removing header '{HeaderNames.Auth.Authorization}'");
                request.Headers.Remove(HeaderNames.Auth.Authorization);
            }
            request.Headers.Add(HeaderNames.Auth.Authorization, "Bearer " + token);
            if (request.Headers.Contains(HeaderNames.Auth.HfAuthorization))
            {
                Logger.LogDebug($"Removing header '{HeaderNames.Auth.HfAuthorization}'");
                request.Headers.Remove(HeaderNames.Auth.HfAuthorization);
            }
            request.Headers.Add(HeaderNames.Auth.HfAuthorization, "Bearer " + token);
            if (request.Headers.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY))
            {
                Logger.LogDebug($"Removing header '{HeaderNames.Azure.SUBSCRIPTION_KEY}'");
                request.Headers.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            if (!string.IsNullOrEmpty(apiKey))
            {
                Logger.LogDebug($"Adding header '{HeaderNames.Azure.SUBSCRIPTION_KEY}'");
                request.Headers.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);
            }
            if (request.Headers.Contains(HeaderNames.BACKEND_ID))
            {
                Logger.LogDebug($"Removing header '{HeaderNames.BACKEND_ID}'");
                request.Headers.Remove(HeaderNames.BACKEND_ID);
            }
            Logger.LogDebug($"Adding header '{HeaderNames.BACKEND_ID}'");
            request.Headers.Add(HeaderNames.BACKEND_ID, backendId.ToString());
        }
    }
}
