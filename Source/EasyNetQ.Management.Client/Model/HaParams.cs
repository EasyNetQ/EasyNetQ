namespace EasyNetQ.Management.Client.Model
{
    using Newtonsoft.Json;

    public class HaParams
    {
        public HaMode AssociatedHaMode { get; set; }
        public long ExactlyCount { get; set; }
        public string[] Nodes { get; set; }
    }
}