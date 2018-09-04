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
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
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
        public AuthenticationHelper(string certIssuer, string authURL, IConfiguration config)
        {
            _authURL = authURL;
            CertIssuer = certIssuer;
            AppConfiguration = config;
        }
        public async Task Configure()
        {
            var issuer = AppConfiguration["ISSUER"];

            var documentRetriever = new HttpDocumentRetriever();
            documentRetriever.RequireHttps = issuer.StartsWith("https://");

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
                    _accessToken = await AuthenticateWithToken();
                }
            }
        }
        protected async Task GetFingeprints(ILogger log)
        {
            dynamic config = new ExpandoObject();

            var responseMessage = await httpClient.GetAsync(_authURL);
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

            ClaimsPrincipal principal;
            AuthenticationHeaderValue authHeader = null;
            try
            {
                authHeader = AuthenticationHeaderValue.Parse(req.Headers[HeaderNames.Authorization]); ;
            }
            catch (Exception) { }

            if (authHeader == null || authHeader.Scheme != "Bearer")
            {

                if (req.Headers.ContainsKey("X-ARR-ClientCert"))
                {
                    try
                    {
                        byte[] clientCertBytes = Convert.FromBase64String(req.Headers["X-ARR-ClientCert"]);
                        var clientCert = new X509Certificate2(clientCertBytes);
                        var CN = clientCert.GetNameInfo(X509NameType.DnsName, false);
                        if (CN != CertIssuer)
                        {
                            log.LogCritical("Certificate has wrong CN:" + CN);
                            return false;
                        }
                        if (allowedCertificates != null && allowedCertificates.Contains(clientCert.Thumbprint))
                        {
                            return true;
                        }
                        else
                        {
                            //try to reload the certList first
                            await this.GetFingeprints(log);
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
                    catch (Exception e)
                    {
                        log.LogCritical($"Could not parse Certificate:{e.ToString()}");

                        return false;
                    }
                }
                else
                {
                    log.LogCritical($"Client Cert header not given");
                    return false;
                }
            }
            else if (( principal = await ValidateTokenAsync(authHeader.Parameter) ) == null)
            {
                log.LogCritical($"Token is invalid");
                return false;
            }
            return true;
        }
        public async Task<string> AuthenticateWithCert(string target, bool overriding = false)
        {
            if (!overriding && _certStrings.ContainsKey(target))
            {
                return _certStrings[target];
            }

            var client = new RestClient($"{AppConfiguration["AUTHURL"]}");
            var request = new RestRequest(Method.POST);
            request.AddHeader("X-Cert-For", target);
            request.AddHeader("X-Cert-From", CertIssuer);
            IRestResponse response = await client.ExecuteTaskAsync(request);
            _certStrings.Add(target, response.Content);
            return response.Content;
        }
        public async Task<string> AuthenticateWithToken()
        {
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
            request.AddParameter("application/json", JsonConvert.SerializeObject(parameter), ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteTaskAsync(request);
            return JsonConvert.DeserializeObject<JObject>(response.Content)["access_token"].Value<string>();
        }
        public async Task<ClaimsPrincipal> ValidateTokenAsync(string value)
        {
            if (_configurationManager == null)
            {
                await Configure();
            }

            var config = await _configurationManager.GetConfigurationAsync(CancellationToken.None);
            var issuer = AppConfiguration["ISSUER"];
            var audience = AppConfiguration["AUDIENCE"];

            var validationParameter = new TokenValidationParameters()
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
