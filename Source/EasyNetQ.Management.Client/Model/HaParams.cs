namespace EasyNetQ.Management.Client.Model
{
    public class HaParams
    {
        public HaMode AssociatedHaMode { get; set; }
        public long ExactlyCount { get; set; }
        public string[] Nodes { get; set; }
    }
}