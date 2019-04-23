using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using sharedLibNet.DependencyInjection.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace sharedLibNet.DependencyInjection
{
    public class CoreFunctionFactory : IFunctionFactory
    {
        private readonly IServiceProvider _container;

        public CoreFunctionFactory(IModule module = null)
        {
            this._container = new ContainerBuilder()
                                  .RegisterModule(module)
                                  .Build();
        }

        public TFunction Create<TFunction>(ILogger log)
            where TFunction : IFunction
        {
            // Resolve the function instance directly from the container.
            var function = this._container.GetService<TFunction>();
            function.Log = log;

            return function;
        }
    }
}
