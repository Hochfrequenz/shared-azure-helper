using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace sharedLibNet.DependencyInjection.Interfaces
{
    public interface IFunction
    {
        ILogger Log { get; set; }
        Task<TOutput> InvokeAsync<TInput, TOutput>(TInput input, FunctionOptionBase options);
    }
}
