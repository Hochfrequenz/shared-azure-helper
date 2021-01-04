using BO4E.BO;
using BO4E.meta;

using EshDataExchangeFormats;
using EshDataExchangeFormats.lookup;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using StackExchange.Profiling;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace sharedLibNet
{
    /// <summary>
    /// The LookupHelper hides raw HTTP requests from the the application.
    /// </summary>
    public class LookupHelper
    {
        protected static HttpClient httpClient = new HttpClient();
        protected ILogger _logger = null;

        protected readonly bool _silentFailure;

        private const string MIME_TYPE_JSON = "application/json"; // replace with MediaTypeNames.Application.Json as soon as .net core 2.1 is used

        private const int DEFAULT_TIMEOUT_MINUTES = 10;

        /// <summary>
        /// Initializes the static http client with a default timeout of <see cref="DEFAULT_TIMEOUT_MINUTES"/> minutes.
        /// </summary>
        static LookupHelper()
        {
            httpClient.Timeout = TimeSpan.FromMinutes(DEFAULT_TIMEOUT_MINUTES);
        }

        /// <summary>
        /// public constructor for the sake of a public (instance) constructor. See also the static <seealso cref="LookupHelper()"/>.
        /// </summary>
        /// <param name="silentFailure">set true to return null in case of error, if false an <see cref="HfException"/> is thrown</param>
        public LookupHelper(bool silentFailure = true)
        {
            _silentFailure = silentFailure;
        }

        /// <summary>
        /// same as <see cref="LookupHelper()"/> but with a logger
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="silentFailure">set true to return null in case of error, if false an <see cref="HfException"/> is thrown</param>
        public LookupHelper(ILogger logger, HttpClient client = null, bool silentFailure = true) : this(silentFailure)
        {
            _logger = logger;
            if (_logger != null)
            {
                _logger.LogInformation($"Instantiated {nameof(LookupHelper)}. The HttpClient {nameof(httpClient)} has a timeout of {httpClient.Timeout.TotalSeconds} seconds.");
            }
            if (client != null)
            {
                httpClient = client;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="lookupURL"></param>
        /// <param name="clientCertString"></param>
        /// <param name="apiKey"></param>
        /// <param name="backendId"></param>
        /// <returns></returns>
        /// <exception cref="HfException" >if Could not perform lookup and silentFailure is false</exception>

        public async Task<GenericLookupResult> RetrieveURLs(IList<Bo4eUri> urls, Uri lookupURL, string clientCertString, string apiKey, BOBackendId backendId, string correlationId = null)
        {
            if (string.IsNullOrWhiteSpace(clientCertString))
            {
                _logger.LogWarning($"{nameof(clientCertString)} is initial: '{clientCertString}'");
            }
            if (lookupURL == null)
            {
                _logger.LogCritical($"{nameof(lookupURL)} is null!");
            }

            GenericLookupQuery urlObject = new GenericLookupQuery()
            {
                Uris = urls
            };
            string requestBody = JsonConvert.SerializeObject(urlObject, new StringEnumConverter());
            var request = new HttpRequestMessage()
            {
                Content = new StringContent(requestBody, System.Text.Encoding.UTF8, MIME_TYPE_JSON),
                RequestUri = lookupURL,
                Method = HttpMethod.Post
            };
            if (request.Headers.Contains(HeaderNames.Auth.XArrClientCert))
            {
                _logger.LogDebug($"Removing header '{HeaderNames.Auth.XArrClientCert}'");
                request.Headers.Remove(HeaderNames.Auth.XArrClientCert);
            }

            request.Headers.Add(HeaderNames.Auth.XArrClientCert, clientCertString);
            if (request.Headers.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY))
            {
                _logger.LogDebug($"Removing header '{HeaderNames.Azure.SUBSCRIPTION_KEY}'");
                request.Headers.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            if (!string.IsNullOrEmpty(apiKey))
            {
                _logger.LogDebug($"Adding {HeaderNames.Azure.SUBSCRIPTION_KEY} header");
                request.Headers.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);
            }
            else
            {
                _logger.LogWarning($"{nameof(apiKey)} is initial: '{apiKey}'");
            }
            if (request.Headers.Contains(HeaderNames.BACKEND_ID))
            {
                _logger.LogDebug($"Removing {HeaderNames.BACKEND_ID} header");
                request.Headers.Remove(HeaderNames.BACKEND_ID);
            }
            request.Headers.Add(HeaderNames.BACKEND_ID, backendId.ToString());

            if (request.Headers.Contains("x-correlation-id"))
            {
                _logger.LogDebug($"Removing header 'x-correlation-id'");
                request.Headers.Remove("x-correlation-id");
            }
            if (!string.IsNullOrEmpty(correlationId))
            {
                _logger.LogDebug($"Adding x-correlation-id header");
                request.Headers.Add("x-correlation-id", correlationId);
            }

            var responseMessage = await httpClient.SendAsync(request);
            if (!responseMessage.IsSuccessStatusCode)
            {
                string responseContentPost = await responseMessage.Content.ReadAsStringAsync();
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase} / {responseContentPost}; The original request was: {requestBody} POSTed to {lookupURL}");
                if (_silentFailure)
                {
                    return null;
                }
                else
                {
                    throw new HfException(responseMessage);
                }
            }

            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            GenericLookupResult resultObject = DeserializeObjectAndLog<GenericLookupResult>(responseContent);
            return resultObject;
        }

        /// <summary>
        /// checks if login to backend specified by <paramref name="bobId"/> works using the token 
        /// </summary>
        /// <param name="token">JWToken</param>
        /// <param name="apiKey">hf-api api key</param>
        /// <param name="bobId">ID of backend</param>
        /// <returns>Tuple containing true/false depending if login was successful and returned status code</returns>
        public async Task<Tuple<bool, HttpStatusCode>> CheckLogin(Uri lookupBaseUrl, string token, string apiKey, BOBackendId bobId, string correlationId = null)
        {
            if (lookupBaseUrl == null)
            {
                throw new ArgumentNullException(nameof(lookupBaseUrl), "lookup Url must not be null.");
            }
            if (bobId == null)
            {
                throw new ArgumentNullException(nameof(bobId), "Backend ID must not be null.");
            }
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Head,
                RequestUri = lookupBaseUrl
            };
            AddHeaders(ref request, token, apiKey, bobId, correlationId);
            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request);
            }
            catch (Exception e)
            {
                _logger.LogError($"Request (try login) failed. {nameof(lookupBaseUrl)}: '{lookupBaseUrl}', {nameof(token)}: '{token}', {nameof(bobId)}: '{bobId}'; Exception: {e}.");
                _logger.LogInformation($"Returning negative result with status code {HttpStatusCode.BadGateway} / 502");
                return new Tuple<bool, HttpStatusCode>(false, HttpStatusCode.BadGateway);
            }
            finally
            {
                request.Dispose();
            }
            _logger.LogDebug($"Response has status code {response.StatusCode}. See lookup service logs for details.");
            return new Tuple<bool, HttpStatusCode>(response.IsSuccessStatusCode, response.StatusCode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="lookupURL"></param>
        /// <param name="token"></param>
        /// <param name="apiKey"></param>
        /// <param name="backendId"></param>
        /// <returns>todo: insert docstring</returns>
        /// <exception cref="HfException" >if Could not perform lookup and silentFailure is false</exception>
        public async Task<GenericLookupResult> RetrieveURLsWithUserToken(IList<Bo4eUri> urls, Uri lookupURL, string token, string apiKey, BOBackendId backendId)
        {
            GenericLookupQuery urlObject = new GenericLookupQuery()
            {
                Uris = urls
            };
            string requestBody = JsonConvert.SerializeObject(urlObject);
            var request = new HttpRequestMessage()
            {
                Content = new StringContent(requestBody, System.Text.Encoding.UTF8, MIME_TYPE_JSON),
                Method = HttpMethod.Post,
                RequestUri = lookupURL
            };
            var responseMessage = await httpClient.SendAsync(request);
            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase} / {responseContent}; The original request was: {requestBody} POSTed to {lookupURL}");
                _logger.LogDebug($"Returning null from lookup helper because of negative response code '{responseMessage.StatusCode}'");
                if (_silentFailure)
                {
                    return null;
                }
                else
                {
                    throw new HfException(responseMessage);
                }
            }
            GenericLookupResult resultObject = DeserializeObjectAndLog<GenericLookupResult>(responseContent);
            return resultObject;
        }

        /// <summary>
        /// Fetching the list of BusinessObjects as a suggested list in base of <paramref name="suggestion"/> string.
        /// </summary>
        /// <param name="suggestion"></param>
        /// <param name="boe4Type"></param>
        /// <param name="lookupURL"></param>
        /// <param name="token"></param>
        /// <param name="apiKey"></param>
        /// <param name="backendId"></param>
        /// <param name="anonymizedResultsOnly"></param>
        /// <returns>Returns a listof suggested BusinessObject</returns>
        /// <exception cref="HfException" >if Could not perform lookup and silentFailure is false</exception>
        public async Task<List<BusinessObject>> Suggest(string suggestion, string boe4Type, Uri lookupURL, string token, string apiKey, BOBackendId backendId, bool anonymizedResultsOnly = false, string correlationId = null)
        {
            var uri = new Uri($"{lookupURL}/suggestion/{boe4Type}/{suggestion}");
            _logger.LogDebug($"Suggestion URL is {uri}");
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = uri
            };
            AddHeaders(ref request, token, apiKey, backendId, correlationId);
            if (request.Headers.Contains(HeaderNames.ANONYMIZED_RESULTS_ONLY))
            {
                _logger.LogDebug($"Removing {HeaderNames.ANONYMIZED_RESULTS_ONLY} header");
                request.Headers.Remove(HeaderNames.ANONYMIZED_RESULTS_ONLY);
            }
            request.Headers.Add(HeaderNames.ANONYMIZED_RESULTS_ONLY, anonymizedResultsOnly.ToString());
            var responseMessage = await httpClient.SendAsync(request);
            string responseContent = await responseMessage.Content.ReadAsStringAsync();
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase} / {responseContent}; The original request was: {suggestion} GETed from {lookupURL}");
                if (_silentFailure)
                {
                    return null;
                }
                else
                {
                    throw new HfException(responseMessage);
                }
            }
            List<BusinessObject> resultObject = DeserializeObjectAndLog<List<BusinessObject>>(responseContent);
            return resultObject;
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
                _logger.LogWarning($"String supposed to be valid json but is null or white space: '{json}'");
            }
            try
            {
                T result = JsonConvert.DeserializeObject<T>(json);
                _logger.LogDebug($"Successfully deserialised as {typeof(T)}");
                return result;
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Could not deserialize '{json}' as {typeof(T)}: {e}");
                if (throwException)
                {
                    _logger.LogDebug("Forwarding Exception...");
                    throw e;
                }
                else
                {
                    _logger.LogDebug("Retuning null");
                    object result = null;
                    return (T)result;
                }
            }
        }

        /// <summary>
        /// most generic call of the lookup service.
        /// Posts the content from <paramref name="json"/> to the URL specified in <paramref name="lookupURL"/> using the token
        /// </summary>
        /// <param name="json">serialised lookup request</param>
        /// <param name="lookupURL">URL of the lookup service</param>
        /// <param name="token">token to authenticate</param>
        /// <param name="apiKey">API key for gateway</param>
        /// <param name="backendId">ID of Backend</param>
        /// <returns></returns>
        ///<exception cref="HfException" >if Could not perform lookup and silentFailure is false</exception>

        public async Task<string> LookupJsonWithUserToken(string json, Uri lookupURL, string token, string apiKey, BOBackendId backendId, string correlationId = null)
        {
            using (MiniProfiler.Current.Step(nameof(LookupJsonWithUserToken)))
            {
                HttpRequestMessage request = new HttpRequestMessage()
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, MIME_TYPE_JSON),
                    Method = HttpMethod.Post,
                    RequestUri = lookupURL
                };
                AddHeaders(ref request, token, apiKey, backendId, correlationId);
                _logger.LogInformation($"Sending out lookup request with token");
                var responseMessage = await httpClient.SendAsync(request);
                _logger.LogInformation($"Got response from lookup request with token");
                if (!responseMessage.IsSuccessStatusCode)
                {
                    string responseContent = await responseMessage.Content.ReadAsStringAsync();
                    _logger.LogCritical($"Could not perform lookup: {responseMessage.ReasonPhrase} / {responseContent}; The original request was: {json} POSTed to {lookupURL}");
                    if (_silentFailure)
                    {
                        return null;
                    }
                    else
                    {
                        throw new HfException(responseMessage); // todo @hamid put the full object in the exception
                    }
                }
                _logger.LogDebug($"Successfully retrieved response with status code {responseMessage.StatusCode}");
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Initialise the suggestion cache for Backend <paramref name="bobId"/> with the lookup result of <paramref name="initialisationQuery"/>
        /// </summary>
        /// <param name="initialisationQuery">query whose result is used to initialise the suggestion cache</param>
        /// <param name="lookupUrl">URI of the lookup service</param>
        /// <param name="token">auth token</param>
        /// <param name="apiKey">api key for azure</param>
        /// <param name="bobId">unique ID of the backend</param>
        /// <returns>raw lookup response as string in case of success, null in case of failure</returns>
        /// <exception cref="HfException" >if Could not perform lookup and silentFailure is false</exception>
        public async Task<string> InitialiseSuggestionCache(GenericCachingQuery initialisationQuery, Uri cacheUrl, string token, string apiKey, string encryptionKey, BOBackendId bobId, string correlationId = null)
        {
            using (MiniProfiler.Current.Step($"{nameof(InitialiseSuggestionCache)} ({nameof(LookupHelper)})"))
            {
                string serialisedQuery = JsonConvert.SerializeObject(initialisationQuery, new StringEnumConverter());
                HttpRequestMessage request = new HttpRequestMessage()
                {
                    Content = new StringContent(serialisedQuery, System.Text.Encoding.UTF8, MIME_TYPE_JSON),
                    Method = HttpMethod.Put,
                    RequestUri = cacheUrl
                };
                AddHeaders(ref request, token, apiKey, bobId, correlationId);
                request.Headers.Add(HeaderNames.CacheService.ENCRYPTION_KEY_PUBLIC, encryptionKey);
                var responseMessage = await httpClient.SendAsync(request);
                if (!responseMessage.IsSuccessStatusCode)
                {
                    string responseContent = await responseMessage.Content.ReadAsStringAsync();
                    _logger.LogCritical($"Could not perform cache initialisation: {responseMessage.ReasonPhrase}");
                    if (_silentFailure)
                    {
                        return null;
                    }
                    else
                    {
                        throw new HfException(responseMessage);
                    }
                }
                _logger.LogDebug($"Successfully initialised cache with status code {responseMessage.StatusCode}");
                return await responseMessage.Content.ReadAsStringAsync();
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
        protected void AddHeaders(ref HttpRequestMessage request, string token, string apiKey, BOBackendId backendId, string correlationId)
        {
            _logger.LogDebug(nameof(AddHeaders));
            if (request.Headers.Contains(HeaderNames.Auth.Authorization))
            {
                _logger.LogDebug($"Removing header '{HeaderNames.Auth.Authorization}'");
                request.Headers.Remove(HeaderNames.Auth.Authorization);
            }
            request.Headers.Add(HeaderNames.Auth.Authorization, "Bearer " + token);
            if (request.Headers.Contains(HeaderNames.Auth.HfAuthorization))
            {
                _logger.LogDebug($"Removing header '{HeaderNames.Auth.HfAuthorization}'");
                request.Headers.Remove(HeaderNames.Auth.HfAuthorization);
            }
            request.Headers.Add(HeaderNames.Auth.HfAuthorization, "Bearer " + token);
            if (request.Headers.Contains(HeaderNames.Azure.SUBSCRIPTION_KEY))
            {
                _logger.LogDebug($"Removing header '{HeaderNames.Azure.SUBSCRIPTION_KEY}'");
                request.Headers.Remove(HeaderNames.Azure.SUBSCRIPTION_KEY);
            }
            if (!string.IsNullOrEmpty(apiKey))
            {
                _logger.LogDebug($"Adding header '{HeaderNames.Azure.SUBSCRIPTION_KEY}'");
                request.Headers.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, apiKey);
            }
            if (request.Headers.Contains(HeaderNames.BACKEND_ID))
            {
                _logger.LogDebug($"Removing header '{HeaderNames.BACKEND_ID}'");
                request.Headers.Remove(HeaderNames.BACKEND_ID);
            }
            _logger.LogDebug($"Adding header '{HeaderNames.BACKEND_ID}'");
            request.Headers.Add(HeaderNames.BACKEND_ID, backendId.ToString());

            if (request.Headers.Contains("x-correlation-id"))
            {
                _logger.LogDebug($"Removing header 'x-correlation-id'");
                request.Headers.Remove("x-correlation-id");
            }
            if (!string.IsNullOrEmpty(correlationId))
            {
                _logger.LogDebug($"Adding x-correlation-id header");
                request.Headers.Add("x-correlation-id", correlationId);
            }
        }
    }
}
