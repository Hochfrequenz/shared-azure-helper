using EshDataExchangeFormats;
using sharedLibNet;
using Xunit;

namespace sharedLibNetTests
{
    public class TestEnums
    {
        [Fact]
        public void TestHeaderToString()
        {
            Assert.Equal("X-ARR-ClientCert", HeaderNames.Auth.XArrClientCert.ToString());
        }

        [Fact]
        public void TestApiToString()
        {
            Assert.Equal("API_KEY", AppConfigurationKey.API_KEY.ToString());
            Assert.Equal("ISSUER", AppConfigurationKey.ISSUER.ToString());
        }
    }
}
