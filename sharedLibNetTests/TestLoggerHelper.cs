using BO4E.BO;
using BO4E.Extensions.Encryption;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using sharedLibNet;

using Sodium;

using System;
using System.Dynamic;

using Xunit;

namespace sharedLibNetTests
{
    public class TestLoggerHelper
    {
        [Fact]
        public void TestLoggerBasic()
        {
            var result = LoggerHelper.CreateLogger("Dei Mudder sein Service", null, null);
            var logger = result.logger;
            var traceObject = LoggerHelper.CreateTraceObject("das ist ein test");
            logger.LogTrace(traceObject);
            Assert.Single(result.loggerProvider.Messages);
            Assert.Equal("Dei Mudder sein Service", result.loggerProvider.Messages[0].Category);
            Assert.Contains("das ist ein test", result.loggerProvider.Messages[0].Message);
        }

        [Fact]
        public void TestLoggerEncryption()
        {
            var result = LoggerHelper.CreateLogger("Deim Vadder sein Service", null, null);
            var logger = result.logger;
            dynamic logResult = new ExpandoObject();
            bool exceptionThrown = false;
            try
            {
                LoggerHelper.CreateTraceObject("das ist ein verschlüsselter test", true, "Köaäasdaspfe");
            }
            catch (FormatException)
            {
                exceptionThrown = true;
            }
            Assert.True(exceptionThrown);
            var keyPair = PublicKeyBox.GenerateKeyPair();
            var traceObject = LoggerHelper.CreateTraceObject("das ist ein verschlüsselter test", true, Convert.ToBase64String(keyPair.PublicKey));
            logger.LogTrace(traceObject);
            var message = result.loggerProvider.Messages[0].Message;

            Encrypter dec = new AsymmetricEncrypter(keyPair.PrivateKey);
            JToken content = JObject.Parse(message).GetValue("Content");
            EncryptedObject eo = JsonConvert.DeserializeObject<EncryptedObjectPublicKeyBox>(content.ToString());
            BusinessObject bo = dec.Decrypt(eo);
            Assert.NotNull(bo);
            Assert.Equal(1, bo.VersionStruktur);
            LogObject lo = bo as LogObject;
            Assert.Equal("das ist ein verschlüsselter test", lo.LogMessage);
        }
    }
}
