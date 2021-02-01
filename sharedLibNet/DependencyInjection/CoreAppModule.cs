using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;

using Mindscape.Raygun4Net.AspNetCore;

using sharedLibNet.Helper;

using System;
using System.Net.Http;

namespace sharedLibNet.DependencyInjection
{
    public class CoreAppModule : Module
    {
        protected string _authServiceName;
        protected string _configName;
        public CoreAppModule(string authServiceName, string configName = "local.settings.json")
        {
            _authServiceName = authServiceName;
            _configName = configName;
        }
        public override void Load(IServiceCollection services)
        {
            var config = new ConfigurationBuilder()
                    .AddJsonFile(_configName, optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables()
                    .AddAzureAppConfiguration(options =>
                    {
                        options.Connect(Environment.GetEnvironmentVariable(EnvironmentVariableNames.ENV_CONFIG_CONNECTION))
                               .Select(KeyFilter.Any)
                               .Select(KeyFilter.Any, Environment.GetEnvironmentVariable(EnvironmentVariableNames.SYSTEM_ENVIRONMENT));
                    })
                    .Build();
            var authConfiguration = new AuthConfiguration()
            {
                ApiKey = config[EnvironmentVariableNames.ENV_HF_API_KEY],
                AuthURL = config[EnvironmentVariableNames.ENV_AUTH_URL],
                Issuers = config.GetSection("ISSUERS")?.Get<string[]>(),
                Audiences = config.GetSection("AUDIENCES")?.Get<string[]>(),

                Audience = config[AppConfigurationKey.AUDIENCE],
                ClientId = config[AppConfigurationKey.CLIENT_ID],
                ClientSecret = config[AppConfigurationKey.CLIENT_SECRET],
                Issuer = config[AppConfigurationKey.ISSUER],
                AccessToken = config[AppConfigurationKey.ACCESS_TOKEN],
            };
            var authHelper = new AuthenticationHelper(_authServiceName, config[EnvironmentVariableNames.ENV_AUTH_URL], authConfiguration);
            var raygun = new RaygunClient(config[EnvironmentVariableNames.ENV_RAYGUN_API_KEY]);
            services.AddSingleton<IAuthenticationHelper>(authHelper);
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(raygun);
            services.AddSingleton<HttpClient>();
        }
    }
}
