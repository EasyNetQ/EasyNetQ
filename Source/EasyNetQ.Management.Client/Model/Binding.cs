namespace EasyNetQ.Management.Client.Model
{
    public class Binding
    {
        public string source { get; set; }
        public string vhost { get; set; }
        public string destination { get; set; }
        public string destination_type { get; set; }
        public string routing_key { get; set; }
        public Arguments arguments { get; set; }
        public string properties_key { get; set; }
    }
}