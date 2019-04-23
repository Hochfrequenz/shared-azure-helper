using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace sharedLibNet.DependencyInjection.Interfaces
{
    public interface IModule
    {
        void Load(IServiceCollection services);
    }
}
