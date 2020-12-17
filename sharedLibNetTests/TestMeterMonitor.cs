using System;
using System.Collections.Generic;

using EshDataExchangeFormats;

using sharedLibNet;

using Xunit;

namespace sharedLibNetTests
{
    public class TestMeterMonitor
    {
        /// <summary>
        /// just for local test
        /// </summary>
        [Fact]
        public async void TestGetAll()
        {
            var result = LoggerHelper.CreateLogger("Dei Mudder sein Service", null, null);
            var logger = result.logger;
            MeterMonitorHelper mmrHelper = new MeterMonitorHelper(logger,new System.Net.Http.HttpClient() { BaseAddress = new Uri("https://hfapi-stage.azure-api.net/metermonitor/"), Timeout = TimeSpan.FromMinutes(10) });
            string token = "";
            string apiKey = "";
            var top1000Results = await mmrHelper.GetMeterMonitors(token, apiKey, new EshDataExchangeFormats.lookup.BOBackendId("HOCHFREQUENZ_BASIC_AUTH"),limit: 1000);
            //Assert.Equal(1000, top1000Results.Count);
            var allResults = await mmrHelper.GetMeterMonitors(token, apiKey, new EshDataExchangeFormats.lookup.BOBackendId("HOCHFREQUENZ_BASIC_AUTH"),withError:false);
            //Assert.True(allResults.Count> 70000);

        }
    }
}
