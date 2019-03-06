using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace sharedLibNet
{
    public interface IAuthenticationHelper
    {
        IConfiguration AppConfiguration { get; set; }

        Task<string> AuthenticateWithCert(string target, bool overriding = false, ILogger log = null);
        Task<string> AuthenticateWithToken(ILogger log);
        Task Configure(ILogger log);
        Task<AuthResult> Http_CheckAuth(HttpRequest req, ILogger log);
        Task<AuthResult> ValidateTokenAsync(string value);
    }
}