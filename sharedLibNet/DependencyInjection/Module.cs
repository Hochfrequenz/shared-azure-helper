using Microsoft.Extensions.DependencyInjection;
using sharedLibNet.DependencyInjection.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace sharedLibNet.DependencyInjection
{
    public class Module : IModule
    {
        public virtual void Load(IServiceCollection services)
        {
            return;
        }
    }
}
