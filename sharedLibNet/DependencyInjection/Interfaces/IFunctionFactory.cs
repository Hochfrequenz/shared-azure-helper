using Microsoft.Extensions.Logging;

namespace sharedLibNet.DependencyInjection.Interfaces
{
    public interface IFunctionFactory
    {
        TFunction Create<TFunction>(ILogger log) where TFunction : IFunction;
    }
}
