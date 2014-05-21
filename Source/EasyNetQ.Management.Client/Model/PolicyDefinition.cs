namespace EasyNetQ.Management.Client.Model
{
    using Newtonsoft.Json;

    public class PolicyDefinition
    {
        [JsonProperty("ha-mode", NullValueHandling = NullValueHandling.Ignore)]
        public HaMode? HaMode;
        [JsonProperty("ha-params", NullValueHandling = NullValueHandling.Ignore)]
        public HaParams HaParams;
        [JsonProperty("ha-sync-mode", NullValueHandling = NullValueHandling.Ignore)]
        public HaSyncMode? HaSyncMode;
        [JsonProperty("federation-upstream-set", NullValueHandling = NullValueHandling.Ignore)]
        public string FederationUpstreamSet { get; set; }
        [JsonProperty("alternate-exchange", NullValueHandling = NullValueHandling.Ignore)]
        public string AlternateExchange { get; set; }
        [JsonProperty("dead-letter-exchange", NullValueHandling = NullValueHandling.Ignore)]
        public string DeadLetterExchange { get; set; }
        [JsonProperty("dead-letter-routing-key", NullValueHandling = NullValueHandling.Ignore)]
        public string DeadLetterRoutingKey { get; set; }
        [JsonProperty("message-ttl", NullValueHandling = NullValueHandling.Ignore)]
        public uint? MessageTtl { get; set; }
        [JsonProperty("expires", NullValueHandling = NullValueHandling.Ignore)]
        public uint? Expires { get; set; }
        [JsonProperty("max-length", NullValueHandling = NullValueHandling.Ignore)]
        public uint? MaxLength { get; set; }
    }
}