using System;
using System.Collections.Generic;
using System.Text;

namespace sharedLibNet.DependencyInjection.Interfaces
{
    public interface IContainerBuilder
    {
        IContainerBuilder RegisterModule(IModule module = null);
        IServiceProvider Build();
    }
}
