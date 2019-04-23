using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using sharedLibNet.DependencyInjection.Interfaces;

namespace sharedLibNet.DependencyInjection
{
    public class KeyVaultModule : Module
    {
        public override void Load(IServiceCollection services)
        {
            base.Load(services);
            
            services.AddTransient<IKeyVaultFunction, KeyVaultFunction>();
        }
    }
}
