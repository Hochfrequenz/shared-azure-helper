using System;

namespace sharedLibNet.DependencyInjection.Interfaces
{
    public interface IContainerBuilder
    {
        IContainerBuilder RegisterModule(IModule module = null);
        IServiceProvider Build();
    }
}
