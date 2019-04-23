using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace sharedLibNet.DependencyInjection.Interfaces
{
    public interface IFunction
    {
        ILogger Log { get; set; }
        Task<TOutput> InvokeAsync<TInput, TOutput>(TInput input, FunctionOptionBase options);
    }
}
