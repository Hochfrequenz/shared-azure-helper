﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System.Threading.Tasks;

namespace sharedLibNet
{
    public interface IAuthenticationHelper
    {
        // IConfiguration AppConfiguration { get; set; }

        Task<string> AuthenticateWithCert(string target, bool overriding = false, ILogger log = null);
        Task<string> AuthenticateWithToken(ILogger log);
        Task Configure(ILogger log);
        Task<AuthResult> Http_CheckAuth(HttpRequest req, ILogger log, string checkForAudience = null);
        Task<AuthResult> ValidateTokenAsync(string value, ILogger log = null, string checkForAudience = null);
    }
}