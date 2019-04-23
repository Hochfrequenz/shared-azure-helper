using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace sharedLibNet.DependencyInjection.Interfaces
{
    public interface IFunctionFactory
    {
        TFunction Create<TFunction>(ILogger log) where TFunction : IFunction;
    }
}
