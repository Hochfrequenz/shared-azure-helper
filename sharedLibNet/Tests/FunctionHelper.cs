﻿using System;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Text;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using System.IO;
using Microsoft.AspNetCore.Http.Internal;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using sharedLibNet;
using System.Threading.Tasks;

namespace sharedLibNet.Tests
{
    public abstract class FunctionHelper
    {
        protected ILogger logger = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory().CreateLogger("NULL");
        protected ExecutionContext context = new ExecutionContext() { InvocationId = Guid.NewGuid(), FunctionAppDirectory = AppContext.BaseDirectory };
        public IAuthenticationHelper CreateMockedAuth()
        {
            var authMock = new Mock<IAuthenticationHelper>();
            authMock.Setup<Task<bool>>(auth => auth.Http_CheckAuth(It.IsAny<HttpRequest>(), It.IsAny<ILogger>())).Returns(Task.FromResult<bool>(true));
            return authMock.Object;
        }
        public HttpRequest Arrange(Dictionary<String, StringValues> query, HeaderDictionary headers, object content)
        {
            var reqMock = new Mock<HttpRequest>();

            reqMock.Setup(req => req.Query).Returns(new QueryCollection(query));
            reqMock.Setup(req => req.Headers).Returns(headers);
            reqMock.Setup(req => req.HttpContext).Returns(new DefaultHttpContext());
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            reqMock.Setup(req => req.Body).Returns(stream);

            return reqMock.Object;
        }
    }
}