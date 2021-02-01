using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using sharedLibNet.DependencyInjection.Interfaces;

using System;

namespace sharedLibNet.DependencyInjection
{
    public class CoreFunctionFactory : IFunctionFactory
    {
        private readonly IServiceProvider _container;

        public CoreFunctionFactory(IModule module = null)
        {
            _container = new ContainerBuilder()
                                  .RegisterModule(module)
                                  .Build();
        }

        public TFunction Create<TFunction>(ILogger log)
            where TFunction : IFunction
        {
            // Resolve the function instance directly from the container.
            var function = _container.GetService<TFunction>();
            function.Log = log;

            return function;
        }
    }
}
