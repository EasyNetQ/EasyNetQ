namespace EasyNetQ.Management.Client.Model
{
    using Newtonsoft.Json;

    public class PolicyDefinition
    {
        [JsonProperty("ha-mode")]
        public HaMode HaMode;
        [JsonProperty("ha-params", NullValueHandling = NullValueHandling.Ignore)]
        public HaParams HaParams;
        [JsonProperty("ha-sync-mode", NullValueHandling = NullValueHandling.Ignore)]
        public HaSyncMode HaSyncMode;
        [JsonProperty("federation-upstream-set", NullValueHandling = NullValueHandling.Ignore)]
        public string FederationUpstreamSet { get; set; }
    }
}