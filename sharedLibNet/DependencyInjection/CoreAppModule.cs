using System;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Mindscape.Raygun4Net.AspNetCore;
using sharedLibNet.Helper;

namespace sharedLibNet.DependencyInjection
{
    public class CoreAppModule : Module
    {
        protected string _authServiceName;
        protected string _configName;
        public CoreAppModule(string authServiceName, string configName = "local.settings.json")
        {
            this._authServiceName = authServiceName;
            this._configName = configName;
        }
        public override void Load(IServiceCollection services)
        {
            var config = new ConfigurationBuilder()
                    .AddJsonFile(_configName, optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables()
                    .AddAzureAppConfiguration(options =>
                    {
                        options.Connect(Environment.GetEnvironmentVariable("AzureAppConfigConnectionString"))
                               .Use(KeyFilter.Any, LabelFilter.Null)
                               .Use(KeyFilter.Any, Environment.GetEnvironmentVariable("Environment"));
                    })
                    .Build();
            var _config = new AuthConfiguration()
            {
                ApiKey = config[EnvironmentVariableNames.ENV_HF_API_KEY],
                AuthURL = config[EnvironmentVariableNames.ENV_AUTH_URL],
                Issuers = config.GetSection("ISSUERS")?.Get<string[]>(),
                Audiences = config.GetSection("AUDIENCES")?.Get<string[]>()
            };
            var _authHelper = new AuthenticationHelper(_authServiceName, config[EnvironmentVariableNames.ENV_AUTH_URL], _config);
            var _raygun = new RaygunClient(config[EnvironmentVariableNames.ENV_RAYGUN_API_KEY]);
            services.AddSingleton<IAuthenticationHelper>(_authHelper);
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(_raygun);
            services.AddSingleton<HttpClient>();
        }
    }
}
