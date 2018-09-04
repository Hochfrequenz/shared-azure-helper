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

        Task<string> AuthenticateWithCert(string target, bool overriding = false);
        Task<string> AuthenticateWithToken();
        Task Configure();
        Task<bool> Http_CheckAuth(HttpRequest req, ILogger log);
        Task<ClaimsPrincipal> ValidateTokenAsync(string value);
    }
}