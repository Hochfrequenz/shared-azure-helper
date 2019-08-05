using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using EshDataExchangeFormats;
using EshDataExchangeFormats.lookup;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using StackExchange.Profiling;
namespace sharedLibNet
{
    public class AuthConfiguration
    {
        public string Issuer { get; set; }
        public string ApiKey { get; set; }
        public string Audience { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string AuthURL { get; set; }
        public string[] Issuers { get; set; }
        public string[] Audiences { get; set; }
    }

    public class AuthResult
    {
        public ClaimsPrincipal Principal { get; private set; }
        public SecurityToken TokenInfo { get; private set; }

        public string Token { get; private set; }
        public AuthResult(ClaimsPrincipal principal, SecurityToken tokenInfo, string token)
        {
            this.Principal = principal;
            this.TokenInfo = tokenInfo;
            this.Token = token;
        }
    }

    public class AuthenticationHelper : IAuthenticationHelper
    {
        private IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
        public string _accessToken;
        public Dictionary<string, string> _certStrings = new Dictionary<string, string>();
        //public IConfiguration AppConfiguration { get; set; }
        protected AuthConfiguration _config = null;
        public readonly string CertIssuer = "<PassName>";
        public List<string> allowedCertificates = null;
        protected HttpClient httpClient = new HttpClient();
        protected string _authURL;
        public RestClient authClient = null;


        [Obsolete("Please use the version to specify with an explicit AuthConfiguration", true)]
        public AuthenticationHelper(string certIssuer, string authURL, IConfiguration config)
        {
            _authURL = authURL;
            CertIssuer = certIssuer;

            _config = new AuthConfiguration()
            {
                ApiKey = config[AppConfigurationKey.API_KEY],
                Audience = config[AppConfigurationKey.AUDIENCE],
                ClientId = config[AppConfigurationKey.CLIENT_ID],
                ClientSecret = config[AppConfigurationKey.CLIENT_SECRET],
                Issuer = config[AppConfigurationKey.ISSUER],
                AccessToken = config[AppConfigurationKey.ACCESS_TOKEN],
                AuthURL = config[AppConfigurationKey.AUTH_URL],
                Issuers = config.GetSection("ISSUERS").Get<string[]>(),
                Audiences = config.GetSection("AUDIENCES").Get<string[]>()
            };
        }

        public AuthenticationHelper(string certIssuer, string authURL, AuthConfiguration config)
        {
            _authURL = authURL;
            CertIssuer = certIssuer;
            _config = config;
        }

        public async Task Configure(ILogger log = null)
        {
            if (log != null)
            {
                if (_config == null)
                {
                    log.LogCritical($"{nameof(_config)} is null! AuthenticationHelper is going to cause an exception!");
                }
                else if (_config.Issuer == null)
                {
                    log.LogCritical($"{nameof(_config.Issuer)} is null! AuthenticationHelper is going to cause an exception!");
                }
            }
            var issuer = _config.Issuer;
            if (log != null && string.IsNullOrWhiteSpace(issuer))
            {
                log.LogCritical($"_config.Issuer loaded from config[\"{AppConfigurationKey.ISSUER}\"] is empty!");
            }

            var documentRetriever = new HttpDocumentRetriever
            {
                RequireHttps = issuer.StartsWith("https://")
            };

            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{issuer.Substring(0, issuer.Length - 1)}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                documentRetriever
            );
            //JM20190607: This code path shouldn't be used anymore. Always use Bearer tokens
            //if (_config.AccessToken != null)
            //{
            //    _accessToken = _config.AccessToken;
            //}
            //else
            //{
            //    if (_config.ClientId != null)
            //    {
            //        if (log != null)
            //        {
            //            log.LogDebug("Still haven't found an access token, so trying to get one via client_credentials");
            //        }
            //        _accessToken = await AuthenticateWithToken(log);
            //    }
            //}
        }

        protected async Task GetFingerprints(ILogger log)
        {
            dynamic config = new ExpandoObject();
            if (_config.ApiKey != null)
            {
                log.LogDebug($"Adding {HeaderNames.Azure.SUBSCRIPTION_KEY} from app configuration");
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Azure.SUBSCRIPTION_KEY, _config.ApiKey);
            }
            Uri fingerPrintUrl = new Uri(_authURL + "/fingerprints");
            HttpResponseMessage responseMessage;
            using (MiniProfiler.Current.Step("Awaiting fingerprints"))
            {
                responseMessage = await httpClient.GetAsync(fingerPrintUrl);
            }
            if (!responseMessage.IsSuccessStatusCode)
            {
                log.LogCritical($"Could not get fingerprints from {fingerPrintUrl}: {responseMessage.ReasonPhrase}; returning without setting allowedCertificates");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                log.LogCritical($"The response content was {responseContent}");
                // maybe the API_KEY is not part of your in your 'appsettings.json'?
                return;
            }
            JObject returnObj = (JObject)JsonConvert.DeserializeObject(await responseMessage.Content.ReadAsStringAsync());
            this.allowedCertificates = returnObj["fingerprints"].ToObject<List<string>>();
        }

        /*public async Task<string> GetAccessTokenFromRefreshKey(BOBackendId backendId, string apiKey, string refreshKey)
        {

        }*/

        public async Task<AuthResult> Http_CheckAuth(HttpRequest req, ILogger log, string checkForAudience = null)
        {
            using (MiniProfiler.Current.Step("CheckingAuth"))
            {
                List<AuthenticationHeaderValue> authHeader = new List<AuthenticationHeaderValue>();
                try
                {
                    using (MiniProfiler.Current.Step("Reading header - alternative Version"))
                    {
                        if (req.Headers.TryGetValue(HeaderNames.Auth.HfAuthorization, out var hfauthHeaders))
                        {
                            foreach (var header in hfauthHeaders)
                            {
                                authHeader.Add(AuthenticationHeaderValue.Parse(header));
                            }
                        }
                        if (authHeader != null)
                        {
                            if (req.Headers.TryGetValue(HeaderNames.Auth.Authorization, out var authHeaders))
                            {
                                foreach (var header in authHeaders)
                                {
                                    authHeader.Add(AuthenticationHeaderValue.Parse(header));
                                }
                            }
                        }
                    }
                    //using (MiniProfiler.Current.Step("Reading header"))
                    //{
                    //    authHeader = AuthenticationHeaderValue.Parse(req.Headers[HeaderNames.Authorization]); ;
                    //}
                }
                catch (Exception e)
                {
                    log.LogError($"Exception in Authentication Helper {nameof(Http_CheckAuth)}: {e}");
                }
                log.LogDebug($"Found authHeaders count:{authHeader.Count}");
                if (authHeader.Count == 0 || authHeader.Where(head => head.Scheme == "Bearer").Count() == 0)
                {
                    if (req.Headers.ContainsKey(HeaderNames.Auth.XArrClientCert) || req.Headers.ContainsKey(HeaderNames.Auth.HfClientCert))
                    {
                        string certString = null;
                        try
                        {
                            if (req.Headers.ContainsKey(HeaderNames.Auth.XArrClientCert))
                            {
                                certString = req.Headers[HeaderNames.Auth.XArrClientCert];
                            }
                            else if (req.Headers.ContainsKey(HeaderNames.Auth.HfClientCert))
                            {
                                certString = req.Headers[HeaderNames.Auth.HfClientCert];
                            }
                            byte[] clientCertBytes = null;
                            using (MiniProfiler.Current.Step("Decoding string"))
                            {
                                clientCertBytes = Convert.FromBase64String(certString);
                            }
                            X509Certificate2 clientCert = null;
                            using (MiniProfiler.Current.Step("DecodingCert"))
                            {
                                clientCert = new X509Certificate2(clientCertBytes);
                            }
                            var CN = clientCert.GetNameInfo(X509NameType.DnsName, false);
                            if (CN != CertIssuer)
                            {
                                log.LogCritical($"Certificate has wrong CN {CN} instead of {CertIssuer}");
                                return null;
                            }
                            using (MiniProfiler.Current.Step("CheckingFingerprint"))
                            {
                                if (allowedCertificates != null && allowedCertificates.Contains(clientCert.Thumbprint))
                                {
                                    return new AuthResult(null, null, null);
                                }
                                else
                                {
                                    //try to reload the certList first
                                    await this.GetFingerprints(log);
                                    if (allowedCertificates != null && allowedCertificates.Contains(clientCert.Thumbprint))
                                    {
                                        return new AuthResult(null, null, null);
                                    }
                                    else
                                    {
                                        log.LogCritical("Cert is not allowed. Reject; returning null");
                                        return null;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            log.LogCritical($"Could not parse Certificate: certString = {certString}: Exception = {e}; returning null");
                            return null;
                        }
                    }
                    else
                    {
                        log.LogCritical($"Client Cert header not given; returning null");
                        return null;
                    }
                }
                else
                {
                    AuthResult principal;
                    foreach (var header in authHeader)
                    {
                        log.LogDebug($"Trying to authenticate with header {header.Parameter}");
                        try
                        {
                            if ((principal = await ValidateTokenAsync(header.Parameter, log, checkForAudience)) == null)
                            {
                                //try next token
                                continue;
                            }
                            //token is valid
                            return principal;
                        }
                        catch (ArgumentException ae)
                        {
                            log.LogWarning($"Catched ArgumentException in ValidateTokenAsync. Probably due to malformed token: {ae.Message}. Returning null!");
                            log.LogDebug($"The stacktrace of the ArgumentException is: {ae.StackTrace}");
                            return null;
                        }
                    }
                    //if we get here we haven't found a valid header
                    log.LogCritical($"No valid token found");
                    return null;
                }
            }
        }

        public async Task<string> AuthenticateWithCert(string target, bool overriding = false, ILogger log = null)
        {
            if (!overriding && _certStrings.ContainsKey(target))
            {
                if (log != null)
                {
                    log.LogDebug($"Certificate already in cache, returning {_certStrings[target]}");
                }
                return _certStrings[target];
            }

            if (_accessToken == null)
            {
                if (log != null)
                {
                    log.LogDebug($"Don't have an access token yet. Trying to configure");
                }
                await Configure(log);
                if (_accessToken == null)
                {
                    throw new Exception($"Could not retrieve auth token. Are {AppConfigurationKey.CLIENT_ID},  {AppConfigurationKey.CLIENT_SECRET} and {AppConfigurationKey.NEW_AUDIENCE} set?");
                }
            }

            Uri authUrl = null;
            if (authClient == null)
            {
                authUrl = new Uri($"{_config.AuthURL}/authenticate");
                authClient = new RestClient(authUrl);
            }

            var request = new RestRequest(Method.POST);
            request.AddHeader(HeaderNames.Auth.CertFor, target);
            request.AddHeader(HeaderNames.Auth.CertFrom, CertIssuer);
            if (_config.ApiKey != null)
            {
                request.AddHeader(HeaderNames.Azure.SUBSCRIPTION_KEY, _config.ApiKey);
            }
            request.AddHeader(HeaderNames.Auth.Authorization, "Bearer " + _accessToken);
            request.AddHeader(HeaderNames.Auth.HfAuthorization, "Bearer " + _accessToken);
            IRestResponse response = await authClient.ExecuteTaskAsync(request);
            if (response.IsSuccessful == false)
            {
                string errorMessage = $"AuthService ({authUrl}) could not be reached: {response.StatusCode}";
                if (log != null)
                {
                    log.LogCritical(errorMessage);
                }
                throw new Exception(errorMessage);
            }
            if (!_certStrings.ContainsKey(target))
            {
                if (log != null)
                {
                    log.LogDebug($"adding {target} key from _certStrings");
                }
                _certStrings.Add(target, response.Content);
            }
            return response.Content;
        }

        public async Task<string> AuthenticateWithToken(ILogger log = null)
        {
            try
            {
                //foreach (string appConfKey in new HashSet<string>() {
                //    AppConfigurationKey.ISSUER,
                //    AppConfigurationKey.CLIENT_ID,
                //    AppConfigurationKey.CLIENT_SECRET,
                //    AppConfigurationKey.NEW_AUDIENCE
                //})
                //{
                //    if (string.IsNullOrEmpty(AppConfiguration[appConfKey]))
                //    {
                //        string errorMessage = $"Appconfiguration needs to include {appConfKey}";
                //        if (log != null)
                //        {
                //            log.LogCritical(errorMessage);
                //        }
                //        throw new InvalidOperationException(errorMessage);
                //    }
                //}
                Uri oauthTokenUri = new Uri($"{_config.Issuer}oauth/token");
                var client = new RestClient(oauthTokenUri);
                var request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                JObject parameter = new JObject
                {
                    ["client_id"] = _config.ClientId,
                    ["client_secret"] = _config.ClientSecret,
                    ["audience"] = _config.Audience,
                    ["grant_type"] = "client_credentials"
                };
                if (log != null)
                {
                    log.LogInformation($"Trying to get oauth token from {oauthTokenUri} with client id {_config.ClientId} and audience {_config.Audience}");
                    log.LogInformation(JsonConvert.SerializeObject(parameter));
                }
                try
                {
                    request.AddParameter("application/json", JsonConvert.SerializeObject(parameter), ParameterType.RequestBody);
                }
                catch (Exception e)
                {
                    string errorMessage = $"Could not serialize object: {e}, {e.StackTrace}";
                    if (log != null)
                    {
                        log.LogCritical(errorMessage);
                    }
                    throw new InvalidOperationException(errorMessage);
                }

                IRestResponse response = await client.ExecuteTaskAsync(request);
                if (log != null)
                {
                    log.LogDebug($"Oauth response status code: {response.StatusCode}");
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        log.LogInformation($"Oauth status code not ok, reason:{response.Content}");
                        return "Could not get token: " + response.StatusCode.ToString();
                    }
                }
                try
                {
                    return JsonConvert.DeserializeObject<JObject>(response.Content)["access_token"].Value<string>();
                }
                catch (Exception e)
                {
                    string errorMessage = "Could not ExecuteClientCall " + response.Content;
                    if (log != null)
                    {
                        log.LogCritical(errorMessage);
                    }
                    throw new InvalidOperationException(errorMessage, e);
                }
            }
            catch (Exception e)
            {
                string errorMessage = $"Could not authenticate with token: {e}";
                if (log != null)
                {
                    log.LogCritical(errorMessage);
                }
                throw new Exception(errorMessage, e);
            }
        }

        public async Task<AuthResult> ValidateTokenAsync(string value, ILogger log = null, string checkForAudience = null)
        {
            if (_configurationManager == null)
            {
                await Configure();
            }

            OpenIdConnectConfiguration config;
            try
            {
                config = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                if (log != null)
                {
                    // especially with invalidOperationException
                    log.LogError($"Exception while awaiting GetConfigurationAsync: {e}. Does the service have a stable internet connection?");
                }
                throw e;
            }
            TokenValidationParameters validationParameter;

            if (_config.Issuer != null)
            {
                var issuer = _config.Issuer;
                var audience = _config.Audience;
                if (checkForAudience != null)
                {

                    validationParameter = new TokenValidationParameters()
                    {
                        RequireSignedTokens = true,
                        ValidAudiences = new string[] { audience, checkForAudience },
                        ValidateAudience = true,
                        ValidIssuer = issuer,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        IssuerSigningKeys = config.SigningKeys
                    };
                }
                else
                {
                    validationParameter = new TokenValidationParameters()
                    {
                        RequireSignedTokens = true,
                        ValidAudience = audience,
                        ValidateAudience = true,
                        ValidIssuer = issuer,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        IssuerSigningKeys = config.SigningKeys
                    };
                }
            }

            else if (_config.Issuers != null)
            {
                validationParameter = new TokenValidationParameters()
                {
                    RequireSignedTokens = true,
                    ValidAudiences = _config.Audiences,
                    ValidateAudience = true,
                    ValidIssuers = _config.Issuers,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = false,
                    ValidateLifetime = true,
                };
            }
            else
            {
                if (log != null)
                {
                    log.LogCritical("Please define either ISSUER and AUDIENCE or ISSUERS AND AUDIENCES");
                }
                throw new Exception("Please define either ISSUER and AUDIENCE or ISSUERS AND AUDIENCES");
            }

            ClaimsPrincipal result = null;
            var tries = 0;
            SecurityToken token = null;
            while (result == null && tries <= 1)
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    result = handler.ValidateToken(value, validationParameter, out token);
                }
                catch (SecurityTokenSignatureKeyNotFoundException)
                {
                    // This exception is thrown if the signature key of the JWT could not be found.
                    // This could be the case when the issuer changed its signing keys, so we trigger a 
                    // refresh and retry validation.
                    _configurationManager.RequestRefresh();
                    tries++;
                }
                catch (SecurityTokenException e)
                {
                    if (log != null)
                    {
                        log.LogCritical($"SecurityTokenException {e}; returning null");
                    }
                    return null;
                }
                catch (ArgumentException e)
                {
                    if (log != null)
                    {
                        log.LogCritical($"ArgumentException while Validating Token: '{e.Message}'; Seems like the value '{value}' is corrupted, incomplete or malformed.");
                    }
                    throw e;
                }
            }
            if (log != null)
            {
                log.LogDebug("returning auth result from ValidateTokenAsync");
            }
            return new AuthResult(result, token, value);
        }
    }
}
