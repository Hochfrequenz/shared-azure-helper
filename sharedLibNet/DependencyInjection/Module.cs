using Microsoft.Extensions.DependencyInjection;
using sharedLibNet.DependencyInjection.Interfaces;

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
