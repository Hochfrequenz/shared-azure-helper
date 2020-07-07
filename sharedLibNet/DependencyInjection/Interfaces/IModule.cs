using Microsoft.Extensions.DependencyInjection;

namespace sharedLibNet.DependencyInjection.Interfaces
{
    public interface IModule
    {
        void Load(IServiceCollection services);
    }
}
