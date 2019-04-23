using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mindscape.Raygun4Net.AspNetCore;
using sharedLibNet.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace sharedLibNet.DependencyInjection
{
    public class CoreAppModule : Module
    {
        public override void Load(IServiceCollection services)
        {
            var config = new ConfigurationBuilder()
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables()
                    .Build();
            var _authHelper = new AuthenticationHelper("MasterProcessData", config[EnvironmentVariableNames.ENV_AUTH_URL], config);
            var _raygun = new RaygunClient(config[EnvironmentVariableNames.ENV_RAYGUN_API_KEY]);
            services.AddSingleton<IAuthenticationHelper>(_authHelper);
            services.AddSingleton(_raygun);
            services.AddSingleton<HttpClient>();
        }
    }
}
