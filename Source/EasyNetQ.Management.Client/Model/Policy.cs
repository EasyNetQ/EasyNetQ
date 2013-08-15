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
}
