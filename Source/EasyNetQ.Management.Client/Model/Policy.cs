namespace EasyNetQ.Management.Client.Model
{
    public class Policy
    {
        public string Vhost { get; set; }
        public string Name { get; set; }
        public string Pattern { get; set; }
        public PolicyDefinition Definition { get; set; }
        public int Priority { get; set; }
    }

    public class PolicyDefinition
    {
        public HaMode HaMode;
        public HaParams HaParams;
        public HaSyncMode HaSyncMode;
        public string FederationUpstreamSet { get; set; }
    }

    public class HaParams
    {
        public int ExactlyCount { get; set; }
        public string[] Nodes { get; set; }
    }

    public enum HaSyncMode
    {
        Unset,
        Manual,
        Automatic
    }

    public enum HaMode
    {
        Unset,
        All,
        Exactly,
        Nodes
    }
}
