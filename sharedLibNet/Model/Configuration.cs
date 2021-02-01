// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using ConfigurationService.Model;
//
//    var configuration = Configuration.FromJson(jsonString);

namespace sharedLibNet.Model
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using System.Collections.Generic;
    using System.Globalization;

    public partial class Configuration
    {
        [JsonProperty("apps", NullValueHandling = NullValueHandling.Ignore)]
        public App[] Apps { get; set; }

        [JsonProperty("clients", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Clients { get; set; }

        [JsonProperty("environments", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Environments { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Tags { get; set; }
        
        // ToDo: is this really called "vaild"? or should it be "valid" and never worked so far?
        [JsonProperty("vaildTo", NullValueHandling = NullValueHandling.Ignore)]
        public string VaildTo { get; set; }

        [JsonProperty("validFrom", NullValueHandling = NullValueHandling.Ignore)]
        public string ValidFrom { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }
    }

    public partial class App
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("pipelines", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, Pipeline> Pipelines { get; set; }
    }
    public partial class Pipeline
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        [JsonProperty("requirements", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Requirements { get; set; }

        [JsonProperty("priority", NullValueHandling = NullValueHandling.Ignore)]
        public int Priority { get; set; }
        [JsonProperty("stages", NullValueHandling = NullValueHandling.Ignore)]
        public Stage[] Stages { get; set; }
    }
    public partial class Stage
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Properties { get; set; }
    }

    public partial class Configuration
    {
        public static Configuration FromJson(string json) => JsonConvert.DeserializeObject<Configuration>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this Configuration self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
