namespace EasyNetQ.Management.Client.Model
{
    using Newtonsoft.Json;

    public class PolicyDefinition
    {
        [JsonProperty("ha-mode")]
        public HaMode HaMode;
        [JsonProperty("ha-params")]
        public HaParams HaParams;
        [JsonProperty("ha-sync-mode")]
        public HaSyncMode HaSyncMode;
        public string FederationUpstreamSet { get; set; }
    }
}