using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using EshDataExchangeFormats;

using Moq;
using Moq.Protected;

using sharedLibNet;

using Xunit;

namespace sharedLibNetTests
{
    public class TestMeterMonitor
    {
        /// <summary>
        /// just for local test
        /// </summary>
        [Fact(Skip = "just for local test")]
        public async Task TestGetAll()
        {
            var result = LoggerHelper.CreateLogger("Dei Mudder sein Service", null, null);
            var logger = result.logger;
            MeterMonitorHelper mmrHelper = new MeterMonitorHelper(logger, new System.Net.Http.HttpClient() { BaseAddress = new Uri("https://hfapi-stage.azure-api.net/metermonitor/"), Timeout = TimeSpan.FromMinutes(10) });
            string token = "";
            string apiKey = "";
            var top1000Results = await mmrHelper.GetMeterMonitors(token, apiKey, new EshDataExchangeFormats.lookup.BOBackendId("HOCHFREQUENZ_BASIC_AUTH"), limit: 1000);
            //Assert.Equal(1000, top1000Results.Count);
            var allResults = await mmrHelper.GetMeterMonitors(token, apiKey, new EshDataExchangeFormats.lookup.BOBackendId("HOCHFREQUENZ_BASIC_AUTH"), withError: false);
            //Assert.True(allResults.Count> 70000);

        }

        [Fact]
        public async Task TestMeterMonitorHelperMocker()
        {
            string json;
            using (StreamReader r = new StreamReader("test_data\\getall-16121856537571.json"))
            {
                json = await r.ReadToEndAsync();
            }
            var handlerMock = new Mock<HttpMessageHandler>();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json),
            };

            handlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
                  )
               .ReturnsAsync(response);

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("https://fake-url/metermonitor/"),
                Timeout = TimeSpan.FromMinutes(10)
            };
            var result = LoggerHelper.CreateLogger("Dei Mudder sein Service", null, null);
            var logger = result.logger;
            MeterMonitorHelper mmrHelper = new MeterMonitorHelper(logger, httpClient);
            string token = "";
            string apiKey = "";
            var allResults = await mmrHelper.GetMeterMonitors(token, apiKey,
                new EshDataExchangeFormats.lookup.BOBackendId("HOCHFREQUENZ_BASIC_AUTH"), withError: true);
            Assert.True(allResults.Count > 20000);
            handlerMock.Protected().Verify("SendAsync", Times.Once(), 
                ItExpr.Is<HttpRequestMessage>(mr => mr.Headers.Contains(HeaderNames.BACKEND_ID) && mr.RequestUri.ToString().Contains("withError")), ItExpr.IsAny<CancellationToken>());
        }
    }
}
