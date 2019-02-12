using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using StackExchange.Profiling;
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
namespace sharedLibNet
{
    public class AuthenticationHelper : IAuthenticationHelper
    {
        private IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
        public string _accessToken;
        public Dictionary<string, string> _certStrings = new Dictionary<string, string>();
        public IConfiguration AppConfiguration { get; set; }
        public readonly string CertIssuer = "<PassName>";
        public List<string> allowedCertificates = null;
        protected HttpClient httpClient = new HttpClient();
        protected string _authURL;
        public RestClient authClient = null;
        public AuthenticationHelper(string certIssuer, string authURL, IConfiguration config)
        {
            _authURL = authURL;
            CertIssuer = certIssuer;
            AppConfiguration = config;
        }
        public async Task Configure(ILogger log = null)
        {
            var issuer = AppConfiguration["ISSUER"];

            var documentRetriever = new HttpDocumentRetriever
            {
                RequireHttps = issuer.StartsWith("https://")
            };

            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{issuer.Substring(0, issuer.Length - 1)}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                documentRetriever
            );
            if (AppConfiguration["ACCESS_TOKEN"] != null)
            {
                _accessToken = AppConfiguration["ACCESS_TOKEN"];
            }
            else
            {
                if (AppConfiguration["CLIENT_ID"] != null)
                {
                    if (log != null)
                    {
                        log.LogDebug("Still haven't found an access token, so trying to get one via client_credentials");
                    }
                    _accessToken = await AuthenticateWithToken(log);
                }
            }
        }
        protected async Task GetFingerprints(ILogger log)
        {
            dynamic config = new ExpandoObject();
            if (AppConfiguration["API_KEY"] != null)
            {
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", AppConfiguration["API_KEY"]);
            }

            var responseMessage = await httpClient.GetAsync(_authURL + "/fingerprints");
            if (!responseMessage.IsSuccessStatusCode)
            {
                log.LogCritical($"Could not get fingerprints: {responseMessage.ReasonPhrase}");
                var responseContent = await responseMessage.Content.ReadAsStringAsync();
                log.LogCritical(responseContent);
                return;
            }
            JObject returnObj = (JObject)JsonConvert.DeserializeObject(await responseMessage.Content.ReadAsStringAsync());
            this.allowedCertificates = returnObj["fingerprints"].ToObject<List<string>>();
        }
        public async Task<bool> Http_CheckAuth(HttpRequest req, ILogger log)
        {
            using (MiniProfiler.Current.Step("CheckingAuth"))
            {
                ClaimsPrincipal principal;
                List<AuthenticationHeaderValue> authHeader = new List<AuthenticationHeaderValue>();
                try
                {
                    using (MiniProfiler.Current.Step("Reading header - alternative Version"))
                    {
                        if (req.Headers.TryGetValue("HF-Authorization", out var hfauthHeaders))
                        {
                            foreach (var header in hfauthHeaders)
                            {
                                authHeader.Add(AuthenticationHeaderValue.Parse(header));
                            }
                        }
                        if (authHeader != null)
                        {
                            if (req.Headers.TryGetValue("Authorization", out var authHeaders))
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
                catch (Exception) { }
                if (authHeader.Count == 0 || authHeader.Where(head => head.Scheme == "Bearer").Count() == 0)
                {
                    if (req.Headers.ContainsKey("X-ARR-ClientCert") || req.Headers.ContainsKey("HF-ClientCert"))
                    {
                        string certString = null;
                        try
                        {

                            if (req.Headers.ContainsKey("X-ARR-ClientCert"))
                                certString = req.Headers["X-ARR-ClientCert"];
                            else if (req.Headers.ContainsKey("HF-ClientCert"))
                            {
                                certString = req.Headers["HF-ClientCert"];
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
                                return false;
                            }
                            using (MiniProfiler.Current.Step("CheckingFingerprint"))
                            {
                                if (allowedCertificates != null && allowedCertificates.Contains(clientCert.Thumbprint))
                                {
                                    return true;
                                }
                                else
                                {
                                    //try to reload the certList first
                                    await this.GetFingerprints(log);
                                    if (allowedCertificates != null && allowedCertificates.Contains(clientCert.Thumbprint))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        log.LogCritical("Cert is not allowed. Reject");
                                        return false;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            log.LogCritical($"Could not parse Certificate: {certString} : {e.ToString()}");

                            return false;
                        }
                    }
                    else
                    {
                        log.LogCritical($"Client Cert header not given");
                        return false;
                    }
                }
                else
                {
                    foreach (var header in authHeader)
                    {
                        if ((principal = await ValidateTokenAsync(header.Parameter)) == null)
                        {
                            //try next token
                            continue;
                        }
                        //token is valid
                        return true;
                    }
                    //if we get here we haven't found a valid header
                    log.LogCritical($"No valid token found");
                    return false;
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
                    throw new Exception("Could not retrieve auth token. Is CLIENT_ID, CLIENT_SECRET and NEW_AUDIENCE set?");
                }
            }

            if (authClient == null)
            {
                authClient = new RestClient($"{AppConfiguration["AUTH_URL"]}/authenticate");
            }

            var request = new RestRequest(Method.POST);
            request.AddHeader("X-Cert-For", target);
            request.AddHeader("X-Cert-From", CertIssuer);
            if (AppConfiguration["API_KEY"] != null)
            {
                request.AddHeader("Ocp-Apim-Subscription-Key", AppConfiguration["API_KEY"]);
            }
            request.AddHeader("Authorization", "Bearer " + _accessToken);
            request.AddHeader("HF-Authorization", "Bearer " + _accessToken);
            IRestResponse response = await authClient.ExecuteTaskAsync(request);
            if (response.IsSuccessful == false)
            {
                throw new Exception($"AuthService could not be reached:{response.StatusCode}");
            }
            if (!_certStrings.ContainsKey(target))
                _certStrings.Add(target, response.Content);
            {
            }

            return response.Content;
        }
        public async Task<string> AuthenticateWithToken(ILogger log = null)
        {
            try
            {
                if (String.IsNullOrEmpty(AppConfiguration["ISSUER"]))
                {
                    throw new InvalidOperationException("Appconfiguration needs to include ISSUER");
                }
                if (String.IsNullOrEmpty(AppConfiguration["CLIENT_ID"]))
                {
                    throw new InvalidOperationException("Appconfiguration needs to include CLIENT_ID");
                }
                if (String.IsNullOrEmpty(AppConfiguration["CLIENT_SECRET"]))
                {
                    throw new InvalidOperationException("Appconfiguration needs to include CLIENT_SECRET");
                }
                if (String.IsNullOrEmpty(AppConfiguration["NEW_AUDIENCE"]))
                {
                    throw new InvalidOperationException("Appconfiguration needs to include CLIENT_SECRET");
                }
                var client = new RestClient($"{AppConfiguration["ISSUER"]}oauth/token");
                var request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                JObject parameter = new JObject
                {
                    ["client_id"] = AppConfiguration["CLIENT_ID"],
                    ["client_secret"] = AppConfiguration["CLIENT_SECRET"],
                    ["audience"] = AppConfiguration["NEW_AUDIENCE"],
                    ["grant_type"] = "client_credentials"
                };
                if (log != null)
                {
                    log.LogInformation($"Trying to get oauth token from {AppConfiguration["ISSUER"]}oauth/token with client id {AppConfiguration["CLIENT_ID"]} and audience {AppConfiguration["NEW_AUDIENCE"]}");
                }
                try
                {
                    request.AddParameter("application/json", JsonConvert.SerializeObject(parameter), ParameterType.RequestBody);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Could not serialize object " + e.StackTrace);
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
                    throw new InvalidOperationException("Could not ExecuteClientCall " + response);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not authenticate with token", e);
            }
        }
        public async Task<ClaimsPrincipal> ValidateTokenAsync(string value)
        {
            if (_configurationManager == null)
            {
                await Configure();
            }

            var config = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
            TokenValidationParameters validationParameter = null;
            if (AppConfiguration["ISSUER"] != null)
            {
                var issuer = AppConfiguration["ISSUER"];
                var audience = AppConfiguration["AUDIENCE"];

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
            else if (AppConfiguration.GetSection("ISSUERS") != null)
            {
                validationParameter = new TokenValidationParameters()
                {
                    RequireSignedTokens = true,
                    ValidAudiences = AppConfiguration.GetSection("AUDIENCES").Get<string[]>(),
                    ValidateAudience = true,
                    ValidIssuers = AppConfiguration.GetSection("ISSUERS").Get<string[]>(),
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = false,
                    ValidateLifetime = true,
                };
            }
            else
            {
                throw new Exception("Please define either ISSUER and AUDIENCE or ISSUERS AND AUDIENCES");
            }

            ClaimsPrincipal result = null;
            var tries = 0;

            while (result == null && tries <= 1)
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    result = handler.ValidateToken(value, validationParameter, out var token);
                }
                catch (SecurityTokenSignatureKeyNotFoundException)
                {
                    // This exception is thrown if the signature key of the JWT could not be found.
                    // This could be the case when the issuer changed its signing keys, so we trigger a 
                    // refresh and retry validation.
                    _configurationManager.RequestRefresh();
                    tries++;
                }
                catch (SecurityTokenException)
                {
                    return null;
                }
            }

            return result;
        }
    }
}
