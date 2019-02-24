using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace sharedLibNet.Logging
{
    /// Courtesy of https://github.com/StephenClearyApps/DotNetApis
    public sealed class LogMessage
    {
        public LogLevel Type { get; set; }

        [JsonConverter(typeof(EpochTimestampJsonConverter))]
        public DateTimeOffset Timestamp { get; set; }

        public string Message { get; set; }

        public string Category { get; set; }

        public int EventId { get; set; }
    }
}
